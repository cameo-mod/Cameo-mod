#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// A fog-honest hostile-strength reading around a cell, from whatever memory
	/// the provider owns (region memory, scout danger marks). 0 means unknown —
	/// never penalised. Consumed by <c>SquadManagerBotModuleCA</c>'s pre-commit
	/// risk gate (AI_FRANSBOT_RESEARCH.md 6c); providers live in OpenRA.Mods.Cameo
	/// and must not be referenced by name from this assembly.
	/// </summary>
	public interface IBotRegionThreatProvider
	{
		int RememberedEnemyThreatAt(CPos cell);
	}

	/// <summary>
	/// The declared merge for the multi-provider <see cref="IBotRegionThreatProvider"/>
	/// seam (AR-5; ai_arch_audit R7): the maximum reading across ENABLED providers —
	/// the strongest remembered evidence wins. The providers publish overlapping
	/// estimates of the same underlying strength, so summing double-counts a region
	/// both memories observed. 0 stays "unknown — never penalised"; a disabled,
	/// null or absent provider contributes 0. Consumers must call this — never
	/// ad-hoc Sum/Max over the array.
	/// </summary>
	public static class BotRegionThreatMerge
	{
		public static int MergedThreatAt(this IEnumerable<IBotRegionThreatProvider> providers, CPos cell)
		{
			var threat = 0;
			if (providers == null)
				return threat;

			foreach (var provider in providers)
				if (provider != null && provider.IsTraitEnabled())
					threat = Math.Max(threat, provider.RememberedEnemyThreatAt(cell));

			return threat;
		}
	}
}
