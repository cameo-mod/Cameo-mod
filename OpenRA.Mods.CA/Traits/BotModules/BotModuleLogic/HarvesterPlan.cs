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
	/// <summary>What one harvester-count decision said (the DEV stream records it; no decision reads it back).</summary>
	public enum HarvesterVerdict
	{
		/// <summary>No harvester is wanted by this site now.</summary>
		Enough = 0,

		/// <summary>HarvesterBotModuleCA asked the unit builder for a harvester (ProduceHarvesters).</summary>
		ModuleRequest = 1,

		/// <summary>The unit builder starts a harvester below the target (TryServeHarvesterTarget).</summary>
		TargetStart = 2,

		/// <summary>The unit builder refuses one more harvester at the cap (ShouldBuild).</summary>
		OverCapRefused = 3,
	}

	/// <summary>
	/// The unit builder's harvester counts, offered to HarvesterBotModuleCA (CONSOLIDATE-03 P9 (b), row hv_module_counts_queued). Read-only: the answer only widens what the module
	/// counts as present; it issues no order. False = no count (the row is off, the unit builder has no harvester module): the module counts live harvesters only.
	/// </summary>
	public interface IBotHarvesterCounts
	{
		/// <summary>The harvesters already in a production queue, including starts the queue has not shown yet.</summary>
		bool TryGetQueued(out int queued);
	}

	/// <summary>
	/// CONSOLIDATE-03 P9 (a) (CONSOLIDATE_03_P9_DESIGN_01.md): the ONE place that answers "are there enough harvesters". HarvesterBotModuleCA.ProduceHarvesters (the module's need),
	/// UnitBuilderBotModuleCA.TryServeHarvesterTarget (the replacement below the target) and HarvesterOverCap (the refusal at the cap) all ask it. The numbers stay in HarvesterTarget
	/// (HARV-01); this class holds the comparisons, so the three sites cannot drift apart. Pure, no world.
	/// </summary>
	public static class HarvesterPlan
	{
		public const int SiteModule = 1;
		public const int SiteTarget = 2;
		public const int SiteCap = 3;

		/// <summary>
		/// The module's need: harvesters present below refineries x perRefinery + additional. Present = live, plus the queued ones when <paramref name="countQueued"/>
		/// (row hv_module_counts_queued). countQueued false is the old HarvesterBotModuleCA formula exactly.
		/// </summary>
		public static bool ModuleTooLow(int live, int queued, int refineries, int perRefinery, int additional, bool countQueued) =>
			live + (countQueued ? queued : 0) < ModuleNeed(refineries, perRefinery, additional);

		/// <summary>The module's wanted harvester count (HarvestersPerRefinery per refinery plus AdditionalHarvesters).</summary>
		public static int ModuleNeed(int refineries, int perRefinery, int additional) => refineries * perRefinery + additional;

		/// <summary>The unit builder's replacement is due: live + queued below the target and the bank at the minimum (HarvesterTarget.Due).</summary>
		public static bool TargetDue(int live, int queued, int target, int bank, int minBank) => HarvesterTarget.Due(live, queued, target, bank, minBank);

		/// <summary>One more harvester would pass the cap (HarvesterTarget.OverCap).</summary>
		public static bool OverCap(int live, int queued, int cap) => HarvesterTarget.OverCap(live, queued, cap);

		public static HarvesterVerdict ModuleVerdict(bool tooLow) => tooLow ? HarvesterVerdict.ModuleRequest : HarvesterVerdict.Enough;

		public static HarvesterVerdict TargetVerdict(bool due) => due ? HarvesterVerdict.TargetStart : HarvesterVerdict.Enough;

		public static HarvesterVerdict CapVerdict(bool over) => over ? HarvesterVerdict.OverCapRefused : HarvesterVerdict.Enough;
	}
}
