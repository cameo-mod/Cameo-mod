#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	public enum ArmyShareSide { None, Army, Building }

	/// <summary>
	/// PROD-IDLE_PLAN_03 section 3 / PLAN_02 section 2 projected army share, pure and world-free.
	/// A = value of own living combat units (attack trait, not a building, not a harvester) plus the full
	/// cost of every in-flight unit item; B = value of own living buildings (defences and walls included)
	/// plus the full cost of every in-flight building item. A scan refreshes the snapshot; every order the
	/// bot records after the scan is added on top, so two building admissions in one pass cannot both pass
	/// an old snapshot. Orders are recorded BEFORE the next candidate is tested. Read-only toward the sim:
	/// no RNG, no orders.
	/// </summary>
	public sealed class ArmyShareCA
	{
		long armyLive, armyInflight, buildingLive, buildingInflight;
		long armyRecorded, buildingRecorded;

		/// <summary>Which side of the share an actor or queue item counts on. Buildings win over attack traits
		/// (a defence is in B); harvesters and attack-less units (MCV, support) are in neither.</summary>
		public static ArmyShareSide Classify(bool isBuilding, bool hasAttack, bool isHarvester) =>
			isBuilding ? ArmyShareSide.Building : hasAttack && !isHarvester ? ArmyShareSide.Army : ArmyShareSide.None;

		public long Army => armyLive + armyInflight + armyRecorded;
		public long Buildings => buildingLive + buildingInflight + buildingRecorded;

		/// <summary>Replace the snapshot from a fresh scan and drop the orders recorded since the last one
		/// (the scan sees them as in-flight items now). Negative inputs clamp to 0.</summary>
		public void Refresh(long armyLive, long armyInflight, long buildingLive, long buildingInflight)
		{
			this.armyLive = System.Math.Max(0, armyLive);
			this.armyInflight = System.Math.Max(0, armyInflight);
			this.buildingLive = System.Math.Max(0, buildingLive);
			this.buildingInflight = System.Math.Max(0, buildingInflight);
			armyRecorded = 0;
			buildingRecorded = 0;
		}

		/// <summary>Count an order emitted since the last scan. Negative cost counts as 0.</summary>
		public void Record(ArmyShareSide side, long cost)
		{
			var c = System.Math.Max(0, cost);
			if (side == ArmyShareSide.Army)
				armyRecorded += c;
			else if (side == ArmyShareSide.Building)
				buildingRecorded += c;
		}

		/// <summary>
		/// The K6 test: A' x 1000 >= T x (A' + B' + cost) with this snapshot plus the recorded orders.
		/// A negative target is the unset BotLimits sentinel (-1): a configuration error, so it fails closed
		/// (the building is held) instead of admitting with no ratio.
		/// </summary>
		public bool BuildingAdmits(long cost, int targetPermille) =>
			targetPermille >= 0 && RecoverySpendPolicyCA.ProjectedShareAdmits(Army, 0, Buildings, 0, cost, targetPermille);
	}
}
