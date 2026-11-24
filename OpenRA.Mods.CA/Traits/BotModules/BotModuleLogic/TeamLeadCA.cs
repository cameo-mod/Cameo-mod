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

		// ---- TEAM-LEAD-02 (P0 FIX 2026-11-24: V01 was a 60k stalemate, the team hoarded and never pushed) ----

		/// <summary>
		/// How often the leader must publish a team push while the blob is ready: the old per-bot attack cadence (the MinimumAttackForceDelay
		/// reset, scaled by late escalation after its start tick), never slower than <paramref name="fallbackTicks"/>. The fallback is the hard
		/// guarantee: with no order for that long the leader publishes at once, whatever its own cadence or pool says.
		/// </summary>
		public static int PublishInterval(int cadenceTicks, int fallbackTicks) =>
			Math.Max(1, Math.Min(Math.Max(1, cadenceTicks), Math.Max(1, fallbackTicks)));

		/// <summary>The combined blob (every member's idle army, formed quiet squads included) may move out: enough units, and the WAVE-MIN tier when one applies.</summary>
		public static bool BlobReady(int units, long value, int floorUnits, bool waveMinLimits, int waveMinUnits, int waveMinValue) =>
			units >= Math.Max(1, floorUnits) && (!waveMinLimits || WaveMinCA.Passes(units, value, waveMinUnits, waveMinValue));

		/// <summary>
		/// The leader publishes now: no open order, a ready blob, and an interval since the last order (the first order has none to wait for).
		/// A member whose idle army has reached its tier's idle-unit limit (<paramref name="anyAtCap"/>) does not wait for the interval: its units
		/// are blocking production, so the push goes at once (the blob must still be ready).
		/// </summary>
		public static bool OrderDue(bool orderOpen, bool blobReady, int now, int lastOrderTick, int intervalTicks, bool anyAtCap = false) =>
			!orderOpen && blobReady && (anyAtCap || lastOrderTick < 0 || now - lastOrderTick >= Math.Max(1, intervalTicks));


		/// <summary>The last order is older than the fallback (the push was forced rather than on cadence); for the record only.</summary>
		public static bool FallbackForced(int now, int lastOrderTick, int fallbackTicks) =>
			lastOrderTick >= 0 && now - lastOrderTick >= Math.Max(1, fallbackTicks);

		/// <summary>
		/// TEAM-BLOB-01 push bar (USER rule 2026-11-24: never push small): an order whose push was held back for the WAVE-MIN bars since
		/// <paramref name="since"/> (-1 = never held) is dissolved once the hold has lasted <paramref name="maxTicks"/>.
		/// </summary>
		public static bool PushBarDissolve(int now, int since, int maxTicks) => since >= 0 && now - since >= Math.Max(1, maxTicks);

		/// <summary>
		/// TEAM-BLOB-01 push bar: the squads standing at the rally (<paramref name="units"/>, <paramref name="value"/>) may advance. The unit bar is the blob launch
		/// minimum (BlobLaunch01CA.MinUnits: the WAVE-MIN tier or half of the team's idle army, whichever is more), at least the join floor; the value bar is the
		/// WAVE-MIN value. Both must pass: nothing below WAVE-MIN advances, and neither does a share of a much bigger army.
		/// </summary>
		public static bool PushBarPassed(int units, long value, int floorUnits, int needUnits, long needValue) =>
			units >= Math.Max(1, Math.Max(floorUnits, needUnits)) && value >= Math.Max(0, needValue);

		/// <summary>The finish focus is re-evaluated: never yet (<paramref name="lastEval"/> below 0), or a full check window ago.</summary>
		public static bool FocusDue(int now, int lastEval, int checkTicks) => lastEval < 0 || now - lastEval >= Math.Max(1, checkTicks);

		/// <summary>
		/// The enemy seat the team finishes: the WEAKEST known one (fewest known buildings, ties to the lower seat id; seats with none known
		/// cannot be targeted). The previous pick sticks until it dies or has no known building left, so the team finishes it before it
		/// switches - except that a focus held longer than <paramref name="maxFocusTicks"/> (an unreachable seat) rotates to the next weakest
		/// (again after every such period; it can return to the first seat later). -1 = no seat to finish.
		/// </summary>
		public static int PickWeakest(IReadOnlyList<(int Seat, int Buildings)> seats, int focusSeat, int focusSince, int now, int maxFocusTicks)
		{
			var best = -1;
			var bestBuildings = int.MaxValue;
			var focusKnown = false;
			foreach (var (seat, buildings) in seats)
			{
				if (buildings <= 0)
					continue;
				if (seat == focusSeat)
					focusKnown = true;
				if (buildings < bestBuildings || (buildings == bestBuildings && seat < best))
				{
					best = seat;
					bestBuildings = buildings;
				}
			}

			if (best < 0)
				return -1;
			if (!focusKnown)
				return best;
			if (now - focusSince < Math.Max(1, maxFocusTicks))
				return focusSeat;

			// The focus has been held too long: take the weakest of the others, or stay when it is the only one left.
			var other = -1;
			var otherBuildings = int.MaxValue;
			foreach (var (seat, buildings) in seats)
			{
				if (buildings <= 0 || seat == focusSeat)
					continue;
				if (buildings < otherBuildings || (buildings == otherBuildings && seat < other))
				{
					other = seat;
					otherBuildings = buildings;
				}
			}

			return other >= 0 ? other : focusSeat;
		}

		// ---- TEAM-BLOB-01 (USER P0, V02 RCA 2026-11-24): the no-order guarantee ----

		/// <summary>Why the leader publishes: the ready blob (an attack order), or the fallback's rally order (gather only, never an attack).</summary>
		public enum PublishWhy { None, Ready, Gather }

		/// <summary>TEAM-GATHER-01 tl_gather_hold: before the push a gathering squad keeps its order on contact (it fights what is in range and walks on to the rally).</summary>
		public static bool GatherHoldsOnContact(bool holdOn, bool pushed) => holdOn && !pushed;

		/// <summary>TEAM-GATHER-01 tl_grace_hold: an order whose push is held (extending) is never cancelled for missing reports; its own dissolve ends it.</summary>
		public static bool GraceHolds(bool graceHoldOn, bool extending) => graceHoldOn && extending;

		/// <summary>TEAM-GATHER-02 tl_grace_rally: a rally (gather-only) order is never cancelled for missing reports; the order max age ends it.</summary>
		public static bool RallyGraceHolds(bool graceRallyOn, bool gatherOnly) => graceRallyOn && gatherOnly;

		/// <summary>TEAM-GATHER-02 tl_defend_respects_order: a squad gathering for the open order, or formed less than <paramref name="freshTicks"/> ago, is not free for ally defence.</summary>
		public static bool SquadKeptForOrder(bool respectsOrder, bool inOrder, int now, int formedTick, int freshTicks) =>
			respectsOrder && (inOrder || (formedTick >= 0 && now - formedTick < Math.Max(0, freshTicks)));

		/// <summary>TEAM-GATHER-02 tl_join_merge: a rally order is published only when the team idle army reaches the WAVE-MIN bars (the units bar is BlobLaunch01CA.MinUnits).</summary>
		public static bool RallyFloorPassed(int teamUnits, long teamValue, int needUnits, long needValue) => teamUnits >= Math.Max(1, needUnits) && teamValue >= Math.Max(0, needValue);

		/// <summary>TEAM-GATHER-02 tl_grace_rally: no order is published within <paramref name="holdTicks"/> of a cancel (-1 = none yet).</summary>
		public static bool RepublishHeld(int now, int cancelTick, int holdTicks) => cancelTick >= 0 && now - cancelTick < Math.Max(0, holdTicks);

		/// <summary>TEAM-GATHER-02 c2 gt_bounded_retarget: a squad of a pushed order, or one at WAVE-MIN size or more, keeps going after its target dies (the next enemy of the focus seat) instead of standing down.</summary>
		public static bool KeepsGoing(bool pushedOrder, int squadUnits, int waveMinUnits) => pushedOrder || (waveMinUnits > 0 && squadUnits >= waveMinUnits);

		/// <summary>TEAM-GATHER-02 c2 tl_issue_floor: a leader squad issues an order when it reaches the WAVE-MIN bars itself, or when the team joinable army does.</summary>
		public static bool LeaderMayIssue(int squadUnits, long squadValue, int teamUnits, long teamValue, int waveMinUnits, long waveMinValue) =>
			(squadUnits >= waveMinUnits && squadValue >= waveMinValue) || (teamUnits >= waveMinUnits && teamValue >= waveMinValue);

		/// <summary>TEAM-GATHER-02 c2 tl_pincer_double: both prongs meet WAVE-MIN only when the gathered blob holds twice the bars.</summary>
		public static bool PincerGathered(int units, long value, int waveMinUnits, long waveMinValue) => units >= 2 * waveMinUnits && value >= 2 * waveMinValue;

		/// <summary>TEAM-GATHER-02 c3 gt_bounded_retarget: a pushed blob that stands within the scan radius of a valid target is sieging it: the stuck rule (no move for 100 ticks) does not drop it to idle.</summary>
		public static bool SiegeHolds(bool pushedAndKeepsGoing, bool targetValid, long distanceSquared, long scanRadiusSquared) => pushedAndKeepsGoing && targetValid && distanceSquared <= scanRadiusSquared;

		/// <summary>TEAM-GATHER-02 c3 tl_rally_mid: the army staging cell counts for the rally only close to the mid point.</summary>
		public static bool StagingNearMid(WPos mid, WPos staging, int cells) => (staging - mid).HorizontalLengthSquared <= (long)Math.Max(0, cells) * CellUnits * Math.Max(0, cells) * CellUnits;

		/// <summary>TEAM-GATHER-02 c2 tl_rally_mid: the point <paramref name="percent"/> percent (0-100) of the way from <paramref name="from"/> to <paramref name="to"/>.</summary>
		public static WPos RallyBetween(WPos from, WPos to, int percent)
		{
			var p = Math.Clamp(percent, 0, 100);
			return new WPos(from.X + (int)((long)(to.X - from.X) * p / 100), from.Y + (int)((long)(to.Y - from.Y) * p / 100), 0);
		}

		/// <summary>
		/// The no-order guarantee (USER rule 2026-11-24: bots never trickle in small groups; nothing below WAVE-MIN ever attacks). It is called only when the
		/// ready path (OrderDue) said no, so the blob is below the WAVE-MIN bars: after <paramref name="sinceOrder"/> ticks without any team order
		/// (BlobLaunch01CA.TicksSinceOrder) of at least <paramref name="fallbackTicks"/> the leader re-publishes the RALLY order - a gather-only order whose
		/// squads walk to the rally point and wait there; it turns into the attack only when the gathered squads pass the WAVE-MIN bars. Needs at least
		/// <paramref name="floorUnits"/> units in the blob (an empty blob has nothing to gather). 0 fallback ticks = off. The ready path is OrderDue.
		/// </summary>
		public static PublishWhy FallbackPublish(int sinceOrder, int fallbackTicks, int units, int floorUnits)
		{
			if (units < Math.Max(1, floorUnits))
				return PublishWhy.None;

			return fallbackTicks > 0 && sinceOrder >= fallbackTicks ? PublishWhy.Gather : PublishWhy.None;
		}

		// ---- TEAM-PINCER-01 (USER P0, 2026-11-24): a big blob splits into two prongs that stage on opposite sides of one target and push together ----
		// Reference: Crystallized Nexus (CN, Astor / DoGyAUT, GPLv3) Squads/States/CNWaveStates.cs + the pincer wave plan of its squad manager
		// (TryBuildPincerWavePlans, IsWaveStageReady): two flanks on opposite approaches to ONE target, a per-flank arrival bar that rounds UP,
		// release only when both flanks are in place. Ported in intent: this order has no region/door graph, so a prong's staging point is a
		// terrain-checked cell on its own side of the target.

		/// <summary>The hold cap of a pincer: the first ready prong never waits longer than this for the other (the assault fan's AssaultSyncHoldTicks pattern).</summary>
		public const int PincerHoldCapTicks = 750;

		/// <summary>
		/// The combined blob can field TWO prongs: twice the join floor, and - when the WAVE-MIN tier applies - twice its unit and value bars,
		/// so each prong alone would still pass the tier. Anything smaller stays one blob.
		/// </summary>
		public static bool PincerBlob(int units, long value, int floorUnits, bool waveMinLimits, int waveMinUnits, int waveMinValue) =>
			BlobReady(units, value, 2 * Math.Max(1, floorUnits), waveMinLimits, 2 * waveMinUnits, 2 * waveMinValue);

		/// <summary>The prong the next squad joins: the lighter one by value, then the one with fewer squads, then prong 0.</summary>
		public static int PickProng(long value0, long value1, int squads0, int squads1) =>
			value1 < value0 || (value1 == value0 && squads1 < squads0) ? 1 : 0;

		/// <summary>
		/// The lighter prong carries at least <paramref name="minSharePercent"/> percent (0-50) of the committed value. Nothing committed yet
		/// is undecided (true): the check only ever collapses a pincer that has real squads behind it.
		/// </summary>
		public static bool PincerBalanced(long value0, long value1, int minSharePercent)
		{
			var total = value0 + value1;
			if (total <= 0)
				return true;

			return Math.Min(value0, value1) * 100 >= total * Math.Clamp(minSharePercent, 0, 50);
		}

		/// <summary>A prong is ready when it has squads and <paramref name="arrivePercent"/> percent of its committed value stands at its own staging point.</summary>
		public static bool ProngReady(long committed, long arrived, int arrivePercent) =>
			committed > 0 && arrived * 100 >= committed * Math.Clamp(arrivePercent, 0, 100);

		/// <summary>
		/// The pincer pushes when the gather window is over (<paramref name="maxTicks"/>), or - never before <paramref name="minTicks"/> - when both
		/// prongs are ready, or when the first ready prong (<paramref name="firstReadyTick"/>, -1 = none yet) has waited
		/// <paramref name="holdTicks"/> (capped at <see cref="PincerHoldCapTicks"/>) for the other. Both prongs then push on the same tick.
		/// </summary>
		public static bool PincerPushDue(int now, int issueTick, int minTicks, int maxTicks, int holdTicks, bool ready0, bool ready1, int firstReadyTick)
		{
			var elapsed = now - issueTick;
			if (elapsed >= maxTicks)
				return true;
			if (elapsed < minTicks)
				return false;
			if (ready0 && ready1)
				return true;

			return firstReadyTick >= 0 && now - firstReadyTick >= Math.Clamp(holdTicks, 1, PincerHoldCapTicks);
		}

		/// <summary>The target is far enough from home to be worth flanking.</summary>
		public static bool PincerTargetFar(WPos from, WPos target, int minCells) =>
			(target - from).HorizontalLengthSquared >= (long)Math.Max(1, minCells) * CellUnits * Math.Max(1, minCells) * CellUnits;

		/// <summary>The two staging points are far enough apart to be two approaches, not one.</summary>
		public static bool PincerSeparated(WPos a, WPos b, int minCells) =>
			(a - b).HorizontalLengthSquared >= (long)Math.Max(1, minCells) * CellUnits * Math.Max(1, minCells) * CellUnits;

		/// <summary>
		/// The staging points for one side of the target (<paramref name="side"/> below 0 = one side, otherwise the other), best first:
		/// <paramref name="standOffCells"/> from the target, <paramref name="spreadDegrees"/> round from the line target-to-home; then the same
		/// point swung 15 and 25 degrees closer to the line and pulled 4 cells in. The caller takes the first one the terrain allows.
		/// </summary>
		public static List<WPos> PincerStaging(WPos from, WPos target, int side, int standOffCells, int spreadDegrees)
		{
			var result = new List<WPos>();
			var toHome = from - target;
			var length = toHome.HorizontalLength;
			if (length <= 0)
				return result;

			var spread = Math.Clamp(spreadDegrees, 45, 90);
			var standOff = Math.Max(4, standOffCells);
			foreach (var (swing, pull) in new[] { (0, 0), (-15, 0), (-25, 0), (0, 4), (-15, 4), (-25, 4) })
			{
				var radius = (long)Math.Max(4, standOff - pull) * CellUnits;
				var line = new WVec((int)(toHome.X * radius / length), (int)(toHome.Y * radius / length), 0);
				var angle = (side < 0 ? -1 : 1) * Math.Max(30, spread + swing);
				var cell = target + line.Rotate(WRot.FromYaw(WAngle.FromDegrees(angle)));
				result.Add(new WPos(cell.X, cell.Y, 0));
			}

			return result;
		}
	}

	/// <summary>One team order: where the army gathers, what it pushes, and who has reported in. Created by the leader, read by every member.</summary>
	public sealed class TeamLeadOrder
	{
		public readonly int Seq;
		public string LeaderKey { get; private set; }
		public readonly WPos Rally;
		public WPos Target { get; private set; }
		public uint TargetActorId { get; private set; }
		public int IssueTick { get; private set; }

		/// <summary>
		/// TEAM-BLOB-01 rally order (the no-order fallback): the squads gather at <see cref="Rally"/> and wait; the order never pushes and is exempt from the max-age cancel
		/// while it is gather-only (an order with no squad report at all is still cancelled after the report grace, and the leader re-publishes it). <see cref="Promote"/> turns it into the ordinary attack order once the gathered squads pass the WAVE-MIN bars.
		/// </summary>
		public bool GatherOnly { get; private set; }

		public void MakeGatherOnly() => GatherOnly = true;

		/// <summary>TEAM-BLOB-01 push bar: the tick the push was first held back because the squads at the rally were below the WAVE-MIN bars (-1 = never).</summary>
		public int PushBarSince { get; private set; } = -1;

		/// <summary>The push is being held for the bars: the order stays at the rally, keeps merging, and is dissolved at the hard cap.</summary>
		public bool Extending => PushBarSince >= 0;

		public void HoldPush(int now)
		{
			if (PushBarSince < 0)
				PushBarSince = now;
		}

		/// <summary>The gathered squads are a wave: the order becomes the attack order. Its gather window restarts now, and it takes a fresh target.</summary>
		public void Promote(int now, WPos target, uint targetActorId)
		{
			GatherOnly = false;
			IssueTick = now;
			Target = target;
			TargetActorId = targetActorId;
		}

		/// <summary>The tick the blob was released (-1 = still gathering).</summary>
		public int PushTick = -1;

		public bool Cancelled;

		/// <summary>Why the order was cancelled (set with <see cref="Cancelled"/>; null while it lives): leader_changed, max_age, no_reports.</summary>
		public string CancelReason;

		/// <summary>How the order was released: "arrived" (the arrive bar), "window" (the gather window ran out), "pincer_ready", "pincer_hold", "pincer_window".</summary>
		public string PushReason;

		/// <summary>TEAM-BLOB-01: the elected leader changed while the order lived: the new leader takes the order over (it is not cancelled).</summary>
		public void HandOver(string newLeaderKey) => LeaderKey = newLeaderKey;

		readonly Dictionary<object, (long Value, bool Arrived, int Units, int ArrivedUnits)> reports = new();

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

		/// <summary>The tick of the latest squad report (-1 = none yet); the no-report grace of a surviving order counts from it.</summary>
		public int LastReportTick = -1;

		/// <param name="arrivedUnits">TEAM-GATHER-02 c2 (tl_join_merge): the units of the squad that stand at the rally; with it (>= 0) arrival is counted per unit (value pro rata), so units merged in at home do not undo the arrival of the rest. -1 = the squad is arrived as a whole (<paramref name="arrived"/>).</param>
		public void Report(object squad, long value, bool arrived, int units = 0, int arrivedUnits = -1) => reports[squad] = (value, arrived, units, arrivedUnits);

		static long ArrivedValue(long value, bool arrived, int units, int arrivedUnits) =>
			arrivedUnits < 0 ? (arrived ? value : 0) : units > 0 ? value * Math.Min(arrivedUnits, units) / units : 0;

		/// <summary>The units and value of the squads that stand at the rally point (the push bar measures these).</summary>
		public (int Units, long Value) ArrivedTotals()
		{
			var units = 0;
			long value = 0;
			foreach (var r in reports.Values)
			{
				var n = r.ArrivedUnits < 0 ? (r.Arrived ? r.Units : 0) : Math.Min(r.ArrivedUnits, r.Units);
				if (n <= 0)
					continue;

				units += n;
				value += ArrivedValue(r.Value, r.Arrived, r.Units, r.ArrivedUnits);
			}

			return (units, value);
		}

		/// <summary>The units and value the reporting squads committed (what a gather-only order measures against the WAVE-MIN bars).</summary>
		public (int Units, long Value) CommittedTotals()
		{
			var units = 0;
			long value = 0;
			foreach (var r in reports.Values)
			{
				units += r.Units;
				value += r.Value;
			}

			return (units, value);
		}

		public void Forget(object squad)
		{
			reports.Remove(squad);
			prongOf.Remove(squad);
		}

		/// <summary>Drop the reports of squads that no longer take part (the caller says who still does).</summary>
		public void Prune(Func<object, bool> stillTakesPart)
		{
			List<object> gone = null;
			foreach (var key in reports.Keys)
				if (!stillTakesPart(key))
					(gone ??= new List<object>()).Add(key);
			foreach (var key in prongOf.Keys)
				if (!reports.ContainsKey(key) && !stillTakesPart(key))
					(gone ??= new List<object>()).Add(key);
			if (gone != null)
				foreach (var key in gone)
					Forget(key);
		}

		// ---- TEAM-PINCER-01: two prongs with their own staging points; null = the single blob gathering at Rally ----

		WPos[] prongRallies;
		readonly Dictionary<object, (int Prong, long Value)> prongOf = new();

		/// <summary>The pincer collapsed back into one blob (a prong too light): everyone pushes as the single-blob order does.</summary>
		public bool PincerCollapsed;

		/// <summary>The tick the first prong was ready at its staging point (-1 = none yet); the hold for the other prong runs from here.</summary>
		public int FirstReadyTick = -1;

		/// <summary>The order was published with two prongs (it may have collapsed since).</summary>
		public bool HasProngs => prongRallies != null;

		/// <summary>Two prongs are in force: published with them and not collapsed.</summary>
		public bool Pincer => prongRallies != null && !PincerCollapsed;

		public void SetProngs(WPos rally0, WPos rally1) => prongRallies = new[] { rally0, rally1 };

		public WPos ProngRally(int prong) => prongRallies[prong == 0 ? 0 : 1];

		/// <summary>The prong a squad belongs to; the first ask shares it out (lighter prong first), so a joining squad is placed once.</summary>
		public int ProngOf(object squad, long value)
		{
			if (prongOf.TryGetValue(squad, out var assigned))
				return assigned.Prong;

			long value0 = 0;
			long value1 = 0;
			var squads0 = 0;
			var squads1 = 0;
			foreach (var (prong, v) in prongOf.Values)
			{
				if (prong == 0)
				{
					value0 += v;
					squads0++;
				}
				else
				{
					value1 += v;
					squads1++;
				}
			}

			var pick = TeamLeadCA.PickProng(value0, value1, squads0, squads1);
			prongOf[squad] = (pick, value);
			return pick;
		}

		/// <summary>The squad's prong if it has one, else -1.</summary>
		public int ProngOrNone(object squad) => prongOf.TryGetValue(squad, out var assigned) ? assigned.Prong : -1;

		/// <summary>The reported value of the squads of one prong, and how much of it stands at the prong's staging point.</summary>
		public (long Committed, long Arrived) Totals(int prong)
		{
			long committed = 0;
			long arrived = 0;
			foreach (var (squad, r) in reports)
			{
				if (!prongOf.TryGetValue(squad, out var assigned) || assigned.Prong != prong)
					continue;

				committed += r.Value;
				arrived += ArrivedValue(r.Value, r.Arrived, r.Units, r.ArrivedUnits);
			}

			return (committed, arrived);
		}

		/// <summary>The first tick a prong was ready is kept; later calls change nothing.</summary>
		public void NoteReady(int now)
		{
			if (FirstReadyTick < 0)
				FirstReadyTick = now;
		}

		public (long Committed, long Arrived) Totals()
		{
			long committed = 0;
			long arrived = 0;
			foreach (var r in reports.Values)
			{
				committed += r.Value;
				arrived += ArrivedValue(r.Value, r.Arrived, r.Units, r.ArrivedUnits);
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

		/// <summary>The tick the last order was cancelled (a leader change excepted; -1 = never); tl_grace_rally holds the next publish for a while.</summary>
		public int LastCancelTick = -1;

		/// <summary>TEAM-LEAD-02 finish-the-weakest focus: the enemy seat (its player-actor id, -1 = none), the tick it was taken, and the cached pick (re-evaluated at most every check window).</summary>
		public int FocusSeat = -1;
		public int FocusSince = -1;
		public int FocusEvalTick = -1;
		public bool FocusHasTarget;
		public WPos FocusTarget;
		public uint FocusTargetId;

		readonly Dictionary<string, (bool AtCap, int Tick)> caps = new(StringComparer.Ordinal);

		public void Publish(string key, int units, long value, int tick, bool atCap = false)
		{
			pools[key] = (units, value, tick);
			caps[key] = (atCap, tick);
		}

		/// <summary>Some seat published within <paramref name="maxAgeTicks"/> that its idle army is at the tier's idle-unit limit.</summary>
		public bool AnyAtCap(int now, int maxAgeTicks)
		{
			foreach (var c in caps.Values)
				if (c.AtCap && now - c.Tick <= maxAgeTicks)
					return true;

			return false;
		}

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
