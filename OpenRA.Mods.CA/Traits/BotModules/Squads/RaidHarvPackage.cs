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
using System.Linq;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>One raid squad's offer for a target: its id, its value (cost), its actor count and its explored-route ETA in ticks (the slowest member's, see <see cref="RaidRoute.Eta"/>).</summary>
	public readonly record struct RaidOffer(int SquadId, int Value, int Units, int EtaTicks);

	/// <summary>
	/// RAID-HARV-01 design 01 D2 (delta 02 section 3): which raid squads go. The requirement is a VALUE the package must reach; the package is the smallest ETA-ordered prefix of the offers that reaches it AND has the join minimum of
	/// ACTORS (an actor count, never a value). No prefix = no raid: a half raid is worse than none. The simple form of Fransbot's Pareto frontier: one scalar and one floor make a plain prefix scan exact.
	/// </summary>
	public static class RaidHarvPackage
	{
		/// <summary>
		/// The value the package must reach: the target value x the learned margin, or the observed defence at the target x (100 + risk margin), whichever is larger (ceilings). The defence is the merged remembered threat plus the cost of visible armed enemy
		/// actors within the learned defence radius (the caller reads only what the player sees or remembers); 0 = unknown, never penalised.
		/// </summary>
		public static long Required(int targetValue, int reqMarginPercent, int observedDefenceValue, int riskMarginPercent)
		{
			var byTarget = ((long)Math.Max(0, targetValue) * Math.Max(0, reqMarginPercent) + 99) / 100;
			var byDefence = ((long)Math.Max(0, observedDefenceValue) * (100 + Math.Max(0, riskMarginPercent)) + 99) / 100;
			return Math.Max(byTarget, byDefence);
		}

		/// <summary>
		/// The smallest ETA-ordered prefix (ties by squad id) of the usable offers (value and ETA positive) whose value reaches <paramref name="required"/> and whose actor count reaches <paramref name="minUnits"/>. False with an empty package
		/// when no prefix does, or when the requirement is not positive.
		/// </summary>
		public static bool TrySelect(IReadOnlyList<RaidOffer> offers, long required, int minUnits, out List<RaidOffer> package)
		{
			package = new List<RaidOffer>();
			if (offers == null || required <= 0)
				return false;

			var ordered = offers.Where(o => o.Value > 0 && o.Units > 0 && o.EtaTicks > 0 && o.EtaTicks != int.MaxValue)
				.OrderBy(o => o.EtaTicks).ThenBy(o => o.SquadId).ToList();

			long value = 0;
			long units = 0;
			foreach (var o in ordered)
			{
				package.Add(o);
				value += o.Value;
				units += o.Units;
				if (value >= required && units >= Math.Max(0, minUnits))
					return true;
			}

			package.Clear();
			return false;
		}

		/// <summary>The package's ETA: the slowest selected offer (the group arrives together).</summary>
		public static int PackageEta(IReadOnlyList<RaidOffer> package) => package == null || package.Count == 0 ? 0 : package.Max(o => o.EtaTicks);
	}
}
