﻿#define LOG_ENABLED //remove on build

using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Common;
using Sandbox.ModAPI;

namespace Rynchodon
{
	public static class Settings
	{
		#region settings
		public enum BoolSetName : byte { /*bAllowJumpCreative, bAllowJumpSurvival,*/ bAllowAutopilot, bAllowRadar, bAllowTurretControl, bUseRemoteControl, bUseColourState }
		public enum IntSetName : byte { }
		public enum FloatSetName : byte { fDefaultSpeed, fMaxSpeed }
		public enum DoubleSetName : byte { }
		public enum StringSetName : byte { sSmartTurretDefaultPlayer, sSmartTurretDefaultNPC }

		public static Dictionary<BoolSetName, bool> boolSettings = new Dictionary<BoolSetName, bool>();
		public static Dictionary<IntSetName, int> intSettings = new Dictionary<IntSetName, int>();
		public static Dictionary<FloatSetName, float> floatSettings = new Dictionary<FloatSetName, float>();
		public static Dictionary<DoubleSetName, double> doubleSettings = new Dictionary<DoubleSetName, double>();
		public static Dictionary<StringSetName, string> stringSettings = new Dictionary<StringSetName, string>();
		#endregion

		private static string settings_file_name = "AutopilotSettings.txt";
		private static System.IO.TextReader settingsReader;
		private static System.IO.TextWriter settingsWriter;

		private static string strVersion = "Version";
		private static int latestVersion = 33; // in sequence of updates on steam

		private static Logger myLogger = new Logger(null, "Settings");
		[System.Diagnostics.Conditional("LOG_ENABLED")]
		private static void log(string toLog, string method = null, Logger.severity level = Logger.severity.DEBUG)
		{ myLogger.log(level, method, toLog); }

		static Settings()
		{
			buildSettings();

			int fileVersion = readAll();
			if (fileVersion != latestVersion)
				MyAPIGateway.Utilities.ShowNotification("Autopilot has been updated.", 10000, MyFontEnum.White);
			log("file version: " + fileVersion + ", latest version: " + latestVersion, "static Constructor", Logger.severity.INFO);

			writeAll(); // writing immediately decreases user errors & whining
		}

		/// <summary>
		/// put each setting into a dictionary with its default value
		/// </summary>
		private static void buildSettings()
		{
			//boolSettings.Add(BoolSetName.bAllowJumpCreative, true);
			//boolSettings.Add(BoolSetName.bAllowJumpSurvival, true);
			boolSettings.Add(BoolSetName.bAllowAutopilot, true);
			boolSettings.Add(BoolSetName.bAllowRadar, true);
			boolSettings.Add(BoolSetName.bAllowTurretControl, true);
			boolSettings.Add(BoolSetName.bUseRemoteControl, false);
			boolSettings.Add(BoolSetName.bUseColourState, true);

			floatSettings.Add(FloatSetName.fDefaultSpeed, 100);
			floatSettings.Add(FloatSetName.fMaxSpeed, float.MaxValue);

			stringSettings.Add(StringSetName.sSmartTurretDefaultNPC, "[ Warhead, Turret, Rocket, Gatling, Reactor, Battery, Solar ]");
			stringSettings.Add(StringSetName.sSmartTurretDefaultPlayer, "");
		}

		/// <summary>
		/// Read all settings from file
		/// </summary>
		/// <returns>version of file</returns>
		private static int readAll()
		{
			if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(settings_file_name, typeof(Settings)))
				return -1;
			try
			{
				settingsReader = MyAPIGateway.Utilities.ReadFileInLocalStorage(settings_file_name, typeof(Settings));

				string[] versionLine = settingsReader.ReadLine().Split('=');
				if (versionLine.Length != 2 || !versionLine[0].Equals(strVersion))
					return -2; // first line is not version
				int fileVersion;
				if (!int.TryParse(versionLine[1], out fileVersion))
					return -3; // could not parse version

				// read settings
				while (true)
				{
					string line = settingsReader.ReadLine();
					//log("got a line: " + line, "readAll()", Logger.severity.TRACE);
					if (line == null)
						break;
					parse(line);
				}

				return fileVersion;
			}
			finally
			{
				settingsReader.Close();
				settingsReader = null;
			}
		}

		/// <summary>
		/// write all settings to file
		/// </summary>
		private static void writeAll()
		{
			try
			{
				// write to file
				settingsWriter = MyAPIGateway.Utilities.WriteFileInLocalStorage(settings_file_name, typeof(Settings));

				write(strVersion, latestVersion.ToString()); // must be first line

				// write settings
				foreach (KeyValuePair<BoolSetName, bool> pair in boolSettings)
					write(pair.Key.ToString(), pair.Value.ToString());
				foreach (KeyValuePair<IntSetName, int> pair in intSettings)
					write(pair.Key.ToString(), pair.Value.ToString());
				foreach (KeyValuePair<FloatSetName, float> pair in floatSettings)
					write(pair.Key.ToString(), pair.Value.ToString());
				foreach (KeyValuePair<DoubleSetName, double> pair in doubleSettings)
					write(pair.Key.ToString(), pair.Value.ToString());
				foreach (KeyValuePair<StringSetName, string> pair in stringSettings)
					write(pair.Key.ToString(), pair.Value.ToString());

			}
			finally
			{
				settingsWriter.Flush();
				settingsWriter.Close();
				settingsWriter = null;
			}
		}

		/// <summary>
		/// write a single setting to file, format is name=value
		/// </summary>
		/// <param name="name">name of setting</param>
		/// <param name="value">value of setting</param>
		private static void write(string name, string value)
		{
			StringBuilder toWrite = new StringBuilder();
			toWrite.Append(name);
			toWrite.Append('=');
			toWrite.Append(value);
			settingsWriter.WriteLine(toWrite);
		}

		/// <summary>
		/// convert a line of format name=value into a setting and apply it
		/// </summary>
		/// <remarks>
		/// guesses setting type based on first letter of name, but will check against all setting types
		/// </remarks>
		private static void parse(string line)
		{
			if (line.Length < 4)
			{
				log("line too short: " + line, "parse()", Logger.severity.WARNING);
				return;
			}

			char first = line[0];
			string[] split = line.Split('=');

			if (split.Length != 2)
			{
				log("split wrong length: " + split.Length, "parse()", Logger.severity.WARNING);
				return;
			}

			string name = split[0];
			string value = split[1];

			switch (first)
			{
				case 'b':
				default:
					if (parseAsBool(name, value)
						|| parseAsInt(name, value)
						|| parseAsFloat(name, value)
						|| parseAsDouble(name, value)
						|| parseAsString(name, value))
						return;
					break;
				case 'd':
					if (parseAsDouble(name, value)
						|| parseAsBool(name, value)
						|| parseAsInt(name, value)
						|| parseAsFloat(name, value)
						|| parseAsString(name, value))
						return;
					break;
				case 'i':
					if (parseAsInt(name, value)
						|| parseAsBool(name, value)
						|| parseAsFloat(name, value)
						|| parseAsDouble(name, value)
						|| parseAsString(name, value))
						return;
					break;
				case 'f':
					if (parseAsFloat(name, value)
						|| parseAsBool(name, value)
						|| parseAsInt(name, value)
						|| parseAsDouble(name, value)
						|| parseAsString(name, value))
						return;
					break;
				case 's':
					if (parseAsString(name, value)
						|| parseAsBool(name, value)
						|| parseAsInt(name, value)
						|| parseAsFloat(name, value)
						|| parseAsDouble(name, value))
						return;
					break;
			}
			log("failed to parse: " + line, "parse()", Logger.severity.WARNING);
		}

		#region parse methods

		private static bool parseAsBool(string name, string value)
		{
			BoolSetName bsn;
			bool bValue;
			if (!Enum.TryParse<BoolSetName>(name, out bsn) || !bool.TryParse(value, out bValue))
				return false;
			log("got bool setting: " + bsn + ", with value: " + bValue, "parseAsBool()", Logger.severity.DEBUG);
			boolSettings[bsn] = bValue;
			return true;
		}

		private static bool parseAsInt(string name, string value)
		{
			IntSetName isn;
			int iValue;
			if (!Enum.TryParse<IntSetName>(name, out isn) || !int.TryParse(value, out iValue))
				return false;
			log("got int setting: " + isn + ", with value: " + iValue, "parseAsInt()", Logger.severity.DEBUG);
			intSettings[isn] = iValue;
			return false;
		}

		private static bool parseAsFloat(string name, string value)
		{
			FloatSetName fsn;
			float fValue;
			if (!Enum.TryParse<FloatSetName>(name, out fsn) || !float.TryParse(value, out fValue))
				return false;
			floatSettings[fsn] = fValue;
			log("got float setting: " + fsn + ", with value: " + fValue, "parseAsFloat()", Logger.severity.DEBUG);
			return true;
		}

		private static bool parseAsDouble(string name, string value)
		{
			DoubleSetName dsn;
			double dValue;
			if (!Enum.TryParse<DoubleSetName>(name, out dsn) || !double.TryParse(value, out dValue))
				return false;
			doubleSettings[dsn] = dValue;
			log("got double setting: " + dsn + ", with value: " + dValue, "parseAsDouble()", Logger.severity.DEBUG);
			return true;
		}

		private static bool parseAsString(string name, string value)
		{
			StringSetName ssn;
			if (!Enum.TryParse<StringSetName>(name, out ssn))
				return false;
			stringSettings[ssn] = value;
			log("got string setting: " + ssn + ", with value: " + value, "parseAsString()", Logger.severity.DEBUG);
			return true;
		}

		#endregion
	}
}
