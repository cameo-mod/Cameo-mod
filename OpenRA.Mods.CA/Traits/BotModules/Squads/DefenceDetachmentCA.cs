// IMPL02 DEFENCE-DETACH (Opus ruling on agg-team C1/C2, user): a home threat must not freeze the whole attack (a tiny raid would be an exploit).
// Instead of a full stop, a proportional detachment of the units CLOSEST to home answers the threat while the rest keeps pushing/attacking;
// only a threat that is large against the own army recalls everything. One pure function shared by A7 (finishing push), A8 (target pick),
// the A5 launches and, through the FREEZE9 merge, the breaker and the agg-core idle/march decisions. No world access, no RNG, deterministic.
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>One unit that may be detached: its value, its squared distance to home, and whether it already stands at home (pool or quiet squad).</summary>
	public readonly record struct DetachCandidate(uint Id, int Value, long DistSq, bool AtHome);

	/// <summary>The outcome of one planning step. With RecallAll every candidate is detached (the whole push is recalled).</summary>
	public sealed class DefenceDetachmentPlan
	{
		public static readonly DefenceDetachmentPlan None = new(false, new HashSet<uint>(), 0, 0);

		public DefenceDetachmentPlan(bool recallAll, HashSet<uint> detached, long detachedValue, long netThreat)
		{
			RecallAll = recallAll;
			Detached = detached;
			DetachedValue = detachedValue;
			NetThreat = netThreat;
		}

		public bool RecallAll { get; }

		public HashSet<uint> Detached { get; }

		public long DetachedValue { get; }

		/// <summary>The home threat value the plan answered (observed enemy value minus the defence share of own static defences).</summary>
		public long NetThreat { get; }

		public bool IsActive => RecallAll || Detached.Count > 0;

		/// <summary>True when the unit stays home for the defence (any unit under a recall, else a detached one).</summary>
		public bool Contains(uint id) => RecallAll || Detached.Contains(id);

		/// <summary>True when at least one of the ids is detached.</summary>
		public bool ContainsAny(IEnumerable<uint> ids) => ids != null && ids.Any(Contains);

		/// <summary>True when the group is non-empty and every member is detached (the group as a whole is a home defender).</summary>
		public bool ContainsAll(IEnumerable<uint> ids)
		{
			if (ids == null)
				return false;
			var any = false;
			foreach (var id in ids)
			{
				any = true;
				if (!Contains(id))
					return false;
			}

			return any;
		}
	}

	public static class DefenceDetachment
	{
		/// <summary>
		/// The threat to answer: observed enemy combat value near home minus defenceSharePercent of the value of the own static defences near home
		/// (a defence fights the raid too). Never negative; the share is clamped to 0-100.
		/// </summary>
		public static long NetThreat(long enemyValueNearHome, long ownDefenceValueNearHome, int defenceSharePercent)
		{
			var share = Math.Clamp(defenceSharePercent, 0, 100);
			return Math.Max(0, Math.Max(0, enemyValueNearHome) - Math.Max(0, ownDefenceValueNearHome) * share / 100);
		}

		/// <summary>
		/// One planning step. No threat: nothing is detached. A threat of at least recallSharePercent of the own army (a value of 0 disables the recall)
		/// recalls everything. Otherwise units are detached nearest-to-home first (units already at home before units in the field, then the
		/// smaller distance, then the lower id) until the detached value reaches defendMarginPercent of the threat; a unit that would take the detached
		/// value above maxDetachSharePercent of the own army is skipped (a cheaper later unit may still fit).
		/// </summary>
		public static DefenceDetachmentPlan Plan(long netThreat, long armyValue, IEnumerable<DetachCandidate> units,
			int defendMarginPercent, int maxDetachSharePercent, int recallSharePercent)
		{
			if (netThreat <= 0)
				return DefenceDetachmentPlan.None;

			var list = units?.Where(u => u.Value > 0).ToList() ?? new List<DetachCandidate>();
			var army = Math.Max(0, armyValue);
			if (recallSharePercent > 0 && netThreat * 100 >= army * recallSharePercent)
			{
				var all = new HashSet<uint>(list.Select(u => u.Id));
				return new DefenceDetachmentPlan(true, all, list.Sum(u => (long)u.Value), netThreat);
			}

			var target = (netThreat * Math.Max(0, defendMarginPercent) + 99) / 100;
			var cap = army * Math.Clamp(maxDetachSharePercent, 0, 100) / 100;
			var detached = new HashSet<uint>();
			long value = 0;
			foreach (var u in list.OrderByDescending(u => u.AtHome).ThenBy(u => u.DistSq).ThenBy(u => u.Id))
			{
				if (value >= target)
					break;
				if (value + u.Value > cap)
					continue;
				detached.Add(u.Id);
				value += u.Value;
			}

			return new DefenceDetachmentPlan(false, detached, value, netThreat);
		}

		/// <summary>
		/// The plan in force: a live threat uses the fresh plan; with no threat now the last plan is kept while the home latch is active (the threat
		/// may be out of sight for a moment); with neither it is cleared, which returns the detached units to the reserve/push.
		/// </summary>
		public static DefenceDetachmentPlan Latch(DefenceDetachmentPlan last, DefenceDetachmentPlan fresh, bool threatNow, bool homeLatchActive)
		{
			if (threatNow)
				return fresh ?? DefenceDetachmentPlan.None;
			return homeLatchActive && last != null ? last : DefenceDetachmentPlan.None;
		}
	}
}
