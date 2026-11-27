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
	/// <summary>One game's reading for one key: no sample (the learner keeps the previous estimate, never reads an undefined denominator as zero) or a relative error in permille, clipped.</summary>
	public readonly record struct RaidSignal(bool HasSample, int ErrorPermille)
	{
		public static RaidSignal None => default;
	}

	/// <summary>A resolved raid: did it kill its target, and what share of the package value was lost (permille).</summary>
	public readonly record struct RaidResult(bool TargetKilled, int PackageLossPermille);

	/// <summary>A retreat after a risk incident: the squad lost half or more of its value within the 600-tick horizon, or the raid re-struck the same target with no further incident.</summary>
	public readonly record struct RetreatOutcome(bool LostHalfWithin600, bool RestrikeClean);

	/// <summary>An incident cell at its decay: a new incident at the same cell within 1500 ticks, or a later raid passing it clean before the decay.</summary>
	public readonly record struct DecayOutcome(bool ReincidentWithin1500, bool PassedCleanBeforeDecay);

	/// <summary>
	/// The observed single-game signals of the nine raid keys (delta 02 section 1 table): each reads ONLY what the game observed (own squads, visible targets), declares its denominator and horizon through its inputs, and returns a relative error
	/// in permille clipped to +-<see cref="RaidHarvKeys.ErrorClipPermille"/>. No outcome model, no counterfactual. The learner (the shared LA02 core, P3) turns the error into one bounded step per game; no step arithmetic lives here.
	/// </summary>
	public static class RaidHarvSignals
	{
		/// <summary>The move-refresh eligibility window (delta 02): a refresh counts only when the target was visible at the previous and at this refresh, the phase is Approach or Strike, and it is not within this many ticks of a phase change or of the raid's first order.</summary>
		public const int RefreshSettleTicks = 25;

		/// <summary>Drift (cells between the last ordered destination and the target's visible cell at the refresh): at or above this the order was late; at or below <see cref="WastedDriftCells"/> the refresh was wasted.</summary>
		public const int LateDriftCells = 3;
		public const int WastedDriftCells = 1;

		public static int Clip(long permille) => (int)Math.Clamp(permille, -RaidHarvKeys.ErrorClipPermille, RaidHarvKeys.ErrorClipPermille);

		/// <summary>The median of a list (the average of the two middle values, rounded toward zero, for an even count). Empty = 0.</summary>
		public static int Median(IReadOnlyList<int> values)
		{
			if (values == null || values.Count == 0)
				return 0;

			var sorted = values.OrderBy(v => v).ToArray();
			var mid = sorted.Length / 2;
			return sorted.Length % 2 == 1 ? sorted[mid] : (int)(((long)sorted[mid - 1] + sorted[mid]) / 2);
		}

		/// <summary>The relative error of a target against the current value, permille, clipped. A current value of zero or less has no defined denominator: no sample.</summary>
		public static RaidSignal Relative(long target, long current) =>
			current <= 0 ? RaidSignal.None : new RaidSignal(true, Clip(1000 * (target - current) / current));

		static RaidSignal MeanOf(long sum, int count) => count <= 0 ? RaidSignal.None : new RaidSignal(true, Clip(sum / count));

		/// <summary>raid.lost_recon (ticks): per lost-target episode. Reacquired after N ticks: the target is 1.25 x the median N. A budget used up without reacquire: the target is 0.8 x the current budget (error -200).</summary>
		public static RaidSignal LostRecon(int currentTicks, IReadOnlyList<int> reacquireTicks, int exhaustedCount)
		{
			var s = reacquireTicks?.Count ?? 0;
			var f = Math.Max(0, exhaustedCount);
			if (s + f == 0 || currentTicks <= 0)
				return RaidSignal.None;

			long sum = -RaidHarvKeys.CategoricalPermille * (long)f;
			if (s > 0)
			{
				var target = Median(reacquireTicks) * 5L / 4;
				sum += Relative(target, currentTicks).ErrorPermille * (long)s;
			}

			return MeanOf(sum, s + f);
		}

		/// <summary>
		/// raid.move_refresh (ticks): over the ELIGIBLE refreshes of the game (see <see cref="RefreshSettleTicks"/>), each given as its drift in cells. A late share over 50 percent = the interval is too long (error -200); a wasted share over 60 percent =
		/// too short (+200); otherwise 0. No eligible refresh = no sample.
		/// </summary>
		public static RaidSignal MoveRefresh(IReadOnlyList<int> driftCells)
		{
			var n = driftCells?.Count ?? 0;
			if (n == 0)
				return RaidSignal.None;

			var late = driftCells.Count(d => d >= LateDriftCells);
			var wasted = driftCells.Count(d => d <= WastedDriftCells);
			if (late * 100 > n * 50)
				return new RaidSignal(true, -RaidHarvKeys.CategoricalPermille);

			return new RaidSignal(true, wasted * 100 > n * 60 ? RaidHarvKeys.CategoricalPermille : 0);
		}

		/// <summary>raid.req_margin (percent): per resolved raid that killed its target. Over 60 percent of the package value lost = up (+200); under 30 percent = down (-200); between = 0. A raid that did not kill its target is not a sample.</summary>
		public static RaidSignal ReqMargin(IReadOnlyList<RaidResult> results)
		{
			long sum = 0;
			var n = 0;
			if (results != null)
				foreach (var r in results)
				{
					if (!r.TargetKilled)
						continue;

					n++;
					if (r.PackageLossPermille > 600)
						sum += RaidHarvKeys.CategoricalPermille;
					else if (r.PackageLossPermille < 300)
						sum -= RaidHarvKeys.CategoricalPermille;
				}

			return MeanOf(sum, n);
		}

		/// <summary>raid.recon_radius (cells): reacquire at distance d from the last-seen cell: the target is 1.25 x the median d. A budget used up and the target seen farther than the radius within 600 ticks after release = up (+200).</summary>
		public static RaidSignal ReconRadius(int currentCells, IReadOnlyList<int> reacquireDistanceCells, int exhaustedFarSeenCount)
		{
			var s = reacquireDistanceCells?.Count ?? 0;
			var f = Math.Max(0, exhaustedFarSeenCount);
			if (s + f == 0 || currentCells <= 0)
				return RaidSignal.None;

			long sum = RaidHarvKeys.CategoricalPermille * (long)f;
			if (s > 0)
				sum += Relative(Median(reacquireDistanceCells) * 5L / 4, currentCells).ErrorPermille * (long)s;

			return MeanOf(sum, s + f);
		}

		/// <summary>raid.incident_loss (percent): per retreat. The squad lost half or more of its value within 600 ticks = too loose (down, -200); else a clean re-strike on the same target = too tight (up, +200); neither observed = not a sample.</summary>
		public static RaidSignal IncidentLoss(IReadOnlyList<RetreatOutcome> retreats)
		{
			long sum = 0;
			var n = 0;
			if (retreats != null)
				foreach (var r in retreats)
				{
					if (r.LostHalfWithin600)
					{
						n++;
						sum -= RaidHarvKeys.CategoricalPermille;
					}
					else if (r.RestrikeClean)
					{
						n++;
						sum += RaidHarvKeys.CategoricalPermille;
					}
				}

			return MeanOf(sum, n);
		}

		/// <summary>raid.incident_window (ticks): the span from the first loss of a lost raid to the incident trigger; the target is 1.5 x the median span.</summary>
		public static RaidSignal IncidentWindow(int currentTicks, IReadOnlyList<int> spanTicks) =>
			spanTicks == null || spanTicks.Count == 0 ? RaidSignal.None : Relative(Median(spanTicks) * 3L / 2, currentTicks);

		/// <summary>raid.incident_decay (ticks): per incident cell at its decay. A new incident at the same cell within 1500 ticks = up (+200); a later raid passing it clean before the decay = down (-200); neither = not a sample.</summary>
		public static RaidSignal IncidentDecay(IReadOnlyList<DecayOutcome> cells)
		{
			long sum = 0;
			var n = 0;
			if (cells != null)
				foreach (var c in cells)
				{
					if (c.ReincidentWithin1500)
					{
						n++;
						sum += RaidHarvKeys.CategoricalPermille;
					}
					else if (c.PassedCleanBeforeDecay)
					{
						n++;
						sum -= RaidHarvKeys.CategoricalPermille;
					}
				}

			return MeanOf(sum, n);
		}

		/// <summary>raid.waypoint_spacing (cells): per leg, whether the lead left the explored lane by more than 2 cells (the engine replanned). Any such leg = down (-200); every leg inside the lane = up (+200); no leg = no sample.</summary>
		public static RaidSignal WaypointSpacing(IReadOnlyList<bool> legLeftLane)
		{
			if (legLeftLane == null || legLeftLane.Count == 0)
				return RaidSignal.None;

			return new RaidSignal(true, legLeftLane.Any(l => l) ? -RaidHarvKeys.CategoricalPermille : RaidHarvKeys.CategoricalPermille);
		}

		/// <summary>raid.defence_radius (cells): per resolved raid WITH losses, whether armed enemy actors first seen outside the radius took part (from the bot's own or shared-ally vision only). Taking part = up (+200); none = down (-200).</summary>
		public static RaidSignal DefenceRadius(IReadOnlyList<bool> outsideDefenderTookPart)
		{
			var n = outsideDefenderTookPart?.Count ?? 0;
			if (n == 0)
				return RaidSignal.None;

			long sum = 0;
			foreach (var took in outsideDefenderTookPart)
				sum += took ? RaidHarvKeys.CategoricalPermille : -RaidHarvKeys.CategoricalPermille;

			return MeanOf(sum, n);
		}
	}
}
