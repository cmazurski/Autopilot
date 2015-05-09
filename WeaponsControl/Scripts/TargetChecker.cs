#define LOG_ENABLED //remove on build

using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRageMath;

namespace Rynchodon.Autopilot.Weapons
{
	public class TargetChecker
	{
		/// <summary>
		/// <para>Test line segment between weapon and target for obstructing entities.</para>
		/// <para>Tests for obstructing voxel map, non-hostile character, or non-hostile grid.</para>
		/// </summary>
		/// <param name="weapon">weapon to check from</param>
		/// <param name="targetPos">entity to shoot</param>
		/// <param name="ignoreSourceGrid">ignore intersections with grid that weapon is part of</param>
		public static bool CanShootAt(IMyCubeBlock weapon, Vector3D targetPos, bool ignoreSourceGrid = false)
		{
			if (weapon == null)
				throw new ArgumentNullException("weapon");

			Vector3D weaponPos = weapon.GetPosition();

			// Voxel Test
			Vector3 boundary;
			if (MyAPIGateway.Entities.RayCastVoxel(weaponPos, targetPos, out boundary))
				return false;

			// Get entities in AABB
			BoundingBoxD AABB = BoundingBoxD.CreateFromPoints(new Vector3D[] { weaponPos, targetPos });
			HashSet<IMyEntity> entitiesInAABB = new HashSet<IMyEntity>();
			MyAPIGateway.Entities.GetEntitiesInAABB_Safe_NoBlock(AABB, entitiesInAABB,
				(entity) => { return entity is IMyCubeGrid || entity is IMyCharacter; });
			if (entitiesInAABB.Count == 0)
				return true;

			LineD laser = new LineD(weaponPos, targetPos);
			Vector3I position = new Vector3I();
			double distance = 0;

			// Test each entity
			foreach (IMyEntity entity in entitiesInAABB)
			{
				if (weapon.canConsiderHostile(entity))
					continue;

				IMyCharacter asChar = entity as IMyCharacter;
				if (asChar != null)
				{
					if (entity.WorldAABB.Intersects(laser, out distance))
						return false;
					continue;
				}

				IMyCubeGrid asGrid = entity as IMyCubeGrid;
				if (asGrid != null)
				{
					if (asGrid.GetLineIntersectionExactGrid(ref laser, ref position, ref distance))
						return false;
					continue;
				}
			}

			// no obstruction found
			return true;
		}

		#region Target Prediction

		/// <summary>
		/// Determines the direction a projectile must be fired in to hit the target.
		/// </summary>
		/// <param name="attacker">entity firing the shot</param>
		/// <param name="target">target</param>
		/// <param name="projectileSpeed">the speed of the projectile</param>
		/// <returns>the required direction</returns>
		public static Vector3? FiringDirection(IMyEntity attacker, IMyEntity target, float projectileSpeed)
		{
			if (attacker == null)
				throw new ArgumentNullException("attacker");
			if (target == null)
				throw new ArgumentNullException("target");

			//Vector3D attackerPos = attacker.GetPosition();
			//Vector3D attackerVelocity = attacker.GetLinearVelocity();
			
			//Vector3D targetPos = target.GetPosition();
			//Vector3D targetVelocity = target.GetLinearVelocity();

			Vector3D relativePos = target.GetPosition() - attacker.GetPosition();
			Vector3 relativeVelocity = target.GetLinearVelocity() - attacker.GetLinearVelocity();

			return FiringDirection(relativePos, relativeVelocity, projectileSpeed);
		}

		private static float? smallest_positive_root_of_quadratic_equation(float a, float b, float c)
		{
			float sqrt = b * b - 4 * a * c;
			if (sqrt < 0)
				return null;
			sqrt = (float)Math.Sqrt(sqrt);

			float root = -b - sqrt / (2 * a);
			if (root > 0)
				return root;
			root = -b + sqrt / (2 * a);
			if (root > 0)
				return root;

			return null;
		}

		/// <remarks>
		/// Based on http://stackoverflow.com/a/17749335
		/// </remarks>
		private static Vector3? FiringDirection(Vector3 relativePosition, Vector3 relativeVelocity, float projectileSpeed)
		{
			float a = projectileSpeed * projectileSpeed - relativeVelocity.LengthSquared();
			float b = -2 * relativePosition.Dot(relativeVelocity);
			float c = -relativePosition.LengthSquared();

			float? time = smallest_positive_root_of_quadratic_equation(a, b, c);
			if (time == null)
				return null;

			return relativeVelocity + relativePosition / time.Value;
		}

		#endregion
	}
}
