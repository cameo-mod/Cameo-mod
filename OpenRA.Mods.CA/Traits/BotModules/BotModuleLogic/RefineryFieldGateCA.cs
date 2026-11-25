#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// REF-FIELD-02 (USER PRIORITY 11-24, refinery spam; AUDIT_BASE_01 BASE-01). Pure rules, no state:
	/// - the planner side (row rf_field_gate): a field takes at most <c>cap</c> refineries, so the total is at most fields in reach x cap;
	///   a field where an own refinery was lost is locked until a minimum lock has run AND no enemy has been seen there for the quiet time;
	/// - the economy rung (row rf_economy_gate): ref1, then the first vehicle factory, then ref2; a minimum interval between refineries;
	///   from the third refinery on, a pending first copy (tech chain, each building once) goes first, for at most the yield time. A yard with
	///   no refinery of its own skips all three (the expansion refinery is the very next item after a new yard deploys, DESIGNER 11-22).
	/// A player with no refinery is never gated (no economy otherwise).
	/// </summary>
	public static class RefineryFieldGateCA
	{
		public enum EconomyVerdict { Allow, WaitFactory, WaitInterval, YieldEssential }

		/// <summary>The field already holds <paramref name="cap"/> refineries (own standing + pending). A cap of 0 or less never binds.</summary>
		public static bool Capped(int fieldRefineries, int cap) => cap > 0 && fieldRefineries >= cap;

		/// <summary>
		/// A refinery lost at <paramref name="lossTick"/> (negative = none) locks its field for <paramref name="lockTicks"/>, and after that
		/// while an enemy was seen there within <paramref name="quietTicks"/> (<paramref name="threatTick"/> negative = never). A loss older
		/// than <paramref name="memoryTicks"/> is forgotten.
		/// </summary>
		public static bool Locked(int now, int lossTick, int threatTick, int lockTicks, int quietTicks, int memoryTicks)
		{
			if (lossTick < 0 || now - lossTick > memoryTicks)
				return false;

			if (now - lossTick < lockTicks)
				return true;

			return threatTick >= 0 && now - threatTick < quietTicks;
		}

		/// <summary>The planner's extra block on an anchor: only while the row is on and the player owns a refinery.</summary>
		public static bool Blocks(bool on, int ownRefineries, bool capped, bool locked) => on && ownRefineries > 0 && (capped || locked);

		/// <summary>
		/// REF-FIELD-02 c2 (CM FIX, Opus ruling): the war-factory wait never deadlocks. It ends when a vehicle factory type is no-space banned
		/// or used up its placement budget, or <paramref name="waitTicks"/> after the first refinery stood (<paramref name="firstStoodTick"/>
		/// negative = not yet). "No vehicle factory buildable" is the factoryBuildable input of <see cref="Economy"/>.
		/// </summary>
		public static bool FactoryWaitReleased(bool factoryBanned, bool placementFailed, int now, int firstStoodTick, int waitTicks) =>
			factoryBanned || placementFailed || (firstStoodTick >= 0 && now - firstStoodTick >= waitTicks);

		/// <summary>
		/// The economy rung's verdict for the next refinery. <paramref name="refineries"/> = owned + in production; <paramref name="yardWithoutRefinery"/> =
		/// an own construction yard has no own refinery near it; <paramref name="lastRefineryTick"/> = when the count last rose (negative = never);
		/// <paramref name="yieldSince"/> = when a pending first copy was first seen (negative = not yet); <paramref name="factoryWaitReleased"/> =
		/// <see cref="FactoryWaitReleased"/>.
		/// </summary>
		public static EconomyVerdict Economy(bool on, int refineries, bool yardWithoutRefinery, bool factoryBuildable, bool factoryOwnedOrQueued,
			bool factoryWaitReleased, int now, int lastRefineryTick, int minIntervalTicks, bool essentialPending, int yieldSince, int yieldTicks)
		{
			if (!on || refineries <= 0 || yardWithoutRefinery)
				return EconomyVerdict.Allow;

			if (factoryBuildable && !factoryOwnedOrQueued && !factoryWaitReleased)
				return EconomyVerdict.WaitFactory;

			if (lastRefineryTick >= 0 && now - lastRefineryTick < minIntervalTicks)
				return EconomyVerdict.WaitInterval;

			if (refineries >= 2 && essentialPending && (yieldSince < 0 || now - yieldSince < yieldTicks))
				return EconomyVerdict.YieldEssential;

			return EconomyVerdict.Allow;
		}
	}
}
