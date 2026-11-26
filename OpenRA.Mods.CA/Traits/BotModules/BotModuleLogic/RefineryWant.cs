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
	// CONSOLIDATE-03 P3 (Opus FINISH-01, Sol RULE_CONFLICT_MAP_01 item 2): the verdict vocabulary of the refinery WANT. The chain itself stays in
	// BaseBuilderQueueManagerCA.ChooseWantedRefinery (the economy gate moves clocks, so it runs only when reached); every call records the ONE gate that held, and the DEV hold label
	// is read from it instead of re-asking the chain (RefineryHoldGate was a second copy). Binding inputs: designer-refinery-spacing (ref1, war factory, ref2),
	// designer-expansion-refinery (very next item after a yard deploy), designer-separate-queues-independent (no cross-queue gate).
	public enum RefineryGate
	{
		None, // every gate passed: the want stands
		NoTypes, // adequacy: no refinery type configured (disabled)
		Met, // adequacy: no anchor is claimable / the count is reached (met)
		NoPowerPlant, // adequacy: no power plant yet (disabled until one stands)
		NoYard, // adequacy: no construction yard (disabled)
		UnsafeAll, // every wanting claim is unsafe (held)
		UnsafeField, // the head wanted field is unsafe (held)
		NotBuildable, // this queue cannot build a refinery (held)
		MaxRefineries, // the refinery limit is reached (held)
		Economy, // ref1 -> war factory -> ref2, interval, first-copy yield (held)
	}

	/// <summary>The first true sub-condition of HasAdequateRefineryCount, in its short-circuit order.</summary>
	public enum RefineryAdequacy { Inadequate, NoTypes, Met, NoPowerPlant, NoYard }

	public static class RefineryWant
	{
		public static RefineryGate ToGate(RefineryAdequacy a) => a switch
		{
			RefineryAdequacy.NoTypes => RefineryGate.NoTypes,
			RefineryAdequacy.Met => RefineryGate.Met,
			RefineryAdequacy.NoPowerPlant => RefineryGate.NoPowerPlant,
			RefineryAdequacy.NoYard => RefineryGate.NoYard,
			_ => RefineryGate.None,
		};

		public static bool IsAdequacy(RefineryGate g) => g >= RefineryGate.NoTypes && g <= RefineryGate.NoYard;

		/// <summary>The DEV bb_refinery_held label of the verdict (the strings of the old re-asked chain). None = the chain passed but the pick came back empty: the economy verdict or the power substitute.</summary>
		public static string Label(RefineryGate g, bool economyGateOn, string economyVerdict) => g switch
		{
			RefineryGate.NoTypes or RefineryGate.Met or RefineryGate.NoPowerPlant or RefineryGate.NoYard => "adequate",
			RefineryGate.UnsafeAll => "unsafe_all",
			RefineryGate.UnsafeField => "unsafe_field",
			RefineryGate.NotBuildable => "not_buildable",
			RefineryGate.MaxRefineries => "max_refineries",
			_ => economyGateOn ? "economy_" + economyVerdict : "power",
		};
	}
}
