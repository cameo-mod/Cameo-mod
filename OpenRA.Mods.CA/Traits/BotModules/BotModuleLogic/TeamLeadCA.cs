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
using System.Runtime.CompilerServices;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// TEAM-LEAD-01 (USER rule, 2026-11-23): the World-free half. Every team of bots has one leader, the bot with the highest in-game score
	/// (hysteresis: a challenger needs a lead of <c>leadPercent</c> and the leader holds the post for <c>holdTicks</c>; a dead leader is
	/// replaced at once). The leader decides when the team moves out and where: its wave becomes the team order, every follower sends its
	/// idle army to the order's one rally point, and when the gathered blob is in place all of it pushes the same target together.
	/// A follower never launches an attack of its own. Sharing own-side state between allies is allowed; nothing here reads enemy state.
	/// </summary>
	public static class TeamLeadCA
	{
		public const int CellUnits = 1024;

		/// <summary>One bot of the team: its participant key, its score and whether it still plays.</summary>
		public readonly record struct Seat(string Key, long Score, bool Alive);

		/// <summary>The current leader (null = none) and the tick it took the post.</summary>
		public readonly record struct Election(string Leader, int SinceTick)
		{
			public static readonly Election None = new(null, -1);
		}

		/// <summary>The team's identity: the lowest ordinal key of ALL its seats, alive or not, so a death never renames the team.</summary>
		public static string TeamKey(IReadOnlyList<Seat> seats)
		{
			string key = null;
			foreach (var s in seats)
				if (key == null || string.CompareOrdinal(s.Key, key) < 0)
					key = s.Key;
			return key;
		}

		/// <summary>
		/// The leader after this check. No living seat: none. No leader yet, or the leader is dead or gone: the best living seat at once
		/// (highest score, ties to the lower ordinal key). Otherwise the leader keeps the post for <paramref name="holdTicks"/>, and after
		/// that only a challenger with at least <paramref name="leadPercent"/> percent more score takes it.
		/// </summary>
		public static Election Elect(IReadOnlyList<Seat> seats, Election current, int now, int leadPercent, int holdTicks)
		{
			Seat? best = null;
			Seat? incumbent = null;
			foreach (var s in seats)
			{
				if (!s.Alive)
					continue;
				if (current.Leader != null && s.Key == current.Leader)
					incumbent = s;
				if (best == null || s.Score > best.Value.Score || (s.Score == best.Value.Score && string.CompareOrdinal(s.Key, best.Value.Key) < 0))
					best = s;
			}

			if (best == null)
				return Election.None;
			if (incumbent == null)
				return new Election(best.Value.Key, now);
			if (best.Value.Key == incumbent.Value.Key || now - current.SinceTick < holdTicks)
				return current;

			var lead = Math.Max(0, leadPercent);
			if (best.Value.Score > incumbent.Value.Score && best.Value.Score * 100 >= incumbent.Value.Score * (100L + lead))
				return new Election(best.Value.Key, now);

			return current;
		}

		/// <summary>The leader has formed no order for <paramref name="stallTicks"/> since <paramref name="sinceTick"/> (the later of its election, the last order and the first attack tick).</summary>
		public static bool Stalled(int now, int sinceTick, int stallTicks) => now - sinceTick >= Math.Max(1, stallTicks);

		/// <summary>
		/// A stalled leader hands the post to the living seat with the largest idle pool - but only to one that holds more than the leader does, so
		/// a handover always moves the command to the bot that can actually form a wave (ties: lower key). No better seat: the leader stays.
		/// </summary>
		public static Election Handover(IReadOnlyList<Seat> seats, Election current, Func<string, int> poolUnits, int now)
		{
			Seat? best = null;
			var bestUnits = -1;
			foreach (var s in seats)
			{
				if (!s.Alive || s.Key == current.Leader)
					continue;
				var units = poolUnits(s.Key);
				if (best == null || units > bestUnits || (units == bestUnits && string.CompareOrdinal(s.Key, best.Value.Key) < 0))
				{
					best = s;
					bestUnits = units;
				}
			}

			if (best == null || bestUnits <= (current.Leader == null ? 0 : poolUnits(current.Leader)))
				return current;
			return new Election(best.Value.Key, now);
		}

		/// <summary>At least two living seats and a leader: below that the team has nobody to command and every bot plays as before.</summary>
		public static bool Engaged(IReadOnlyList<Seat> seats, Election election)
		{
			if (election.Leader == null)
				return false;
			var alive = 0;
			foreach (var s in seats)
				if (s.Alive)
					alive++;
			return alive >= 2;
		}

		/// <summary>The rally point <paramref name="forwardCells"/> from <paramref name="from"/> toward <paramref name="target"/>; never past the target.</summary>
		public static WPos RallyPoint(WPos from, WPos target, int forwardCells)
		{
			var offset = target - from;
			var distance = offset.HorizontalLength;
			var forward = (long)Math.Max(0, forwardCells) * CellUnits;
			if (distance <= 0 || distance <= forward)
				return from;
			return from + new WVec((int)(offset.X * forward / distance), (int)(offset.Y * forward / distance), 0);
		}

		/// <summary>
		/// The gathered blob pushes when the gather window is over (<paramref name="maxTicks"/>), or earlier - never before
		/// <paramref name="minTicks"/> - once <paramref name="arrivePercent"/> percent of the committed value stands at the rally point.
		/// </summary>
		public static bool PushDue(int now, int issueTick, int minTicks, int maxTicks, long committedValue, long arrivedValue, int arrivePercent)
		{
			var elapsed = now - issueTick;
			if (elapsed >= maxTicks)
				return true;
			if (elapsed < minTicks || committedValue <= 0)
				return false;
			return arrivedValue * 100 >= committedValue * Math.Clamp(arrivePercent, 0, 100);
		}

		/// <summary>The team's joint guerrilla/harass target is still in force: published at <paramref name="since"/> (-1 = none) and not older than <paramref name="maxTicks"/>.</summary>
		public static bool HarassLive(int now, int since, int maxTicks) => since >= 0 && now - since <= maxTicks;

		/// <summary>A follower may form a wave only for an open order, and only with at least <paramref name="minUnits"/> idle units.</summary>
		public static bool FollowerMayLaunch(bool orderOpen, int poolUnits, int minUnits) =>
			orderOpen && poolUnits >= Math.Max(1, minUnits);

		/// <summary>The leader may form the team wave only with a real contribution of its own (never an empty squad).</summary>
		public static bool LeaderMayLaunch(int poolUnits, int minUnits) => poolUnits >= Math.Max(1, minUnits);

		/// <summary>
		/// The idle pool of the whole team: the sum over the seats that published within <paramref name="maxAgeTicks"/>. This is the number the
		/// leader's tier gate (WAVE-MIN-01) and launch bars measure - the combined blob, not each contributor.
		/// </summary>
		public static (int Units, long Value) TeamPool(IEnumerable<(int Units, long Value, int Tick)> pools, int now, int maxAgeTicks)
		{
			var units = 0;
			long value = 0;
			foreach (var p in pools)
			{
				if (now - p.Tick > maxAgeTicks)
					continue;
				units += p.Units;
				value += p.Value;
			}

			return (units, value);
		}
	}

	/// <summary>One team order: where the army gathers, what it pushes, and who has reported in. Created by the leader, read by every member.</summary>
	public sealed class TeamLeadOrder
	{
		public readonly int Seq;
		public readonly string LeaderKey;
		public readonly WPos Rally;
		public readonly WPos Target;
		public readonly uint TargetActorId;
		public readonly int IssueTick;

		/// <summary>The tick the blob was released (-1 = still gathering).</summary>
		public int PushTick = -1;

		public bool Cancelled;

		readonly Dictionary<object, (long Value, bool Arrived)> reports = new();

		public TeamLeadOrder(int seq, string leaderKey, WPos rally, WPos target, uint targetActorId, int issueTick)
		{
			Seq = seq;
			LeaderKey = leaderKey;
			Rally = rally;
			Target = target;
			TargetActorId = targetActorId;
			IssueTick = issueTick;
		}

		public bool Pushed => PushTick >= 0;

		/// <summary>Squads may still be formed for it: not cancelled, and either still gathering or pushed no more than <paramref name="lateJoinTicks"/> ago.</summary>
		public bool Open(int now, int lateJoinTicks) => !Cancelled && (!Pushed || now - PushTick <= lateJoinTicks);

		public int ReportCount => reports.Count;

		public void Report(object squad, long value, bool arrived) => reports[squad] = (value, arrived);

		public void Forget(object squad) => reports.Remove(squad);

		/// <summary>Drop the reports of squads that no longer take part (the caller says who still does).</summary>
		public void Prune(Func<object, bool> stillTakesPart)
		{
			List<object> gone = null;
			foreach (var key in reports.Keys)
				if (!stillTakesPart(key))
					(gone ??= new List<object>()).Add(key);
			if (gone != null)
				foreach (var key in gone)
					reports.Remove(key);
		}

		public (long Committed, long Arrived) Totals()
		{
			long committed = 0;
			long arrived = 0;
			foreach (var r in reports.Values)
			{
				committed += r.Value;
				if (r.Arrived)
					arrived += r.Value;
			}

			return (committed, arrived);
		}
	}

	/// <summary>The shared state of one team of bots: the election, the open order and what each seat published. Host-side, unsynced (bots run on the host).</summary>
	public sealed class TeamLeadTeam
	{
		public TeamLeadCA.Election Election = TeamLeadCA.Election.None;
		public int NextEvalTick;
		public TeamLeadOrder Order;
		int orderSeq;

		/// <summary>The joint guerrilla/harass group's one target (actor id, 0 = none) and the tick it was published (-1 = never).</summary>
		public uint HarassTargetId;
		public int HarassTick = -1;

		readonly Dictionary<string, (int Units, long Value, int Tick)> pools = new(StringComparer.Ordinal);

		/// <summary>The tick of the last order any seat of the team issued (-1 = none yet); with the election and the first attack tick it is the stall clock.</summary>
		public int LastOrderTick = -1;

		public void Publish(string key, int units, long value, int tick) => pools[key] = (units, value, tick);

		/// <summary>The idle units a seat published within <paramref name="maxAgeTicks"/>; 0 when stale or never.</summary>
		public int PoolUnitsOf(string key, int now, int maxAgeTicks) =>
			pools.TryGetValue(key, out var p) && now - p.Tick <= maxAgeTicks ? p.Units : 0;

		public (int Units, long Value) Pool(int now, int maxAgeTicks) => TeamLeadCA.TeamPool(pools.Values, now, maxAgeTicks);

		public TeamLeadOrder Issue(string leaderKey, WPos rally, WPos target, uint targetActorId, int now)
		{
			LastOrderTick = now;
			return Order = new TeamLeadOrder(++orderSeq, leaderKey, rally, target, targetActorId, now);
		}
	}

	/// <summary>The per-World table of team states, keyed by <see cref="TeamLeadCA.TeamKey"/>.</summary>
	public static class TeamLeadBoard
	{
		static readonly ConditionalWeakTable<World, Dictionary<string, TeamLeadTeam>> Teams = new();

		public static TeamLeadTeam For(World world, string teamKey)
		{
			var teams = Teams.GetValue(world, _ => new Dictionary<string, TeamLeadTeam>(StringComparer.Ordinal));
			if (!teams.TryGetValue(teamKey, out var team))
				teams[teamKey] = team = new TeamLeadTeam();
			return team;
		}
	}
}
