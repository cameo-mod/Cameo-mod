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
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// LATEPUSH-01 P2 (rows lp_army_cap, lp_all_in_publish, lp_reinforce, lp_handover; design 01 D3 to D5, user decision audit A6). Every row is off in code and classic, behind the one launcher, with its own ablation latch;
	// a row off = the old text of the call site (pinned). No scan, no RNG, no read of any enemy trait or condition: the figures are the seat's own squads and pool, the team's eligible army and the leader's observed ratio.
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateLpArmyCap;
		AblationLatch ablateLpAllInPublish;
		AblationLatch ablateLpReinforce;
		AblationLatch ablateLpHandover;
		int latePushAllInLogTick = -1000;

		/// <summary>lp_army_cap: the age-scaled bar is capped at the learned share of the eligible army (needs lp_age_min, which owns the cap).</summary>
		internal bool LatePushArmyCapOn => LatePushAgeMinOn && Info.LatePushArmyCapEnabled && !ablateLpArmyCap.Off(Player, AblationIds.LpArmyCap);

		/// <summary>lp_all_in_publish: an open all-in trigger publishes on the eligible army and the guard claims all of it (needs the all-in rows and the one launcher).</summary>
		internal bool LatePushAllInPublishOn => ArmyLaunchArbiterOn && TeamLeadAllInOn && Info.LatePushAllInPublishEnabled && !ablateLpAllInPublish.Off(Player, AblationIds.LpAllInPublish);

		/// <summary>lp_reinforce: one reinforcement per seat and order, as one group.</summary>
		internal bool LatePushReinforceOn => ArmyLaunchArbiterOn && Info.LatePushReinforceEnabled && !ablateLpReinforce.Off(Player, AblationIds.LpReinforce);

		/// <summary>lp_handover: a personality switch hands the pushed squads to the open order.</summary>
		internal bool LatePushHandoverOn => ArmyLaunchArbiterOn && Info.LatePushHandoverEnabled && !ablateLpHandover.Off(Player, AblationIds.LpHandover);

		// The cap of the age-scaled bar, permille of the eligible army: the learned share with lp_army_cap on, else the fixed P1 percent (x 10, the same number as before).
		// TODO(LA02, knob latepush.army_cap): until the learnables registry and the IBotLearnedKnobs seam land (the single LA02 integration commit) the share is the yaml prior through the table (DEV overrides only);
		// then it is read through IBotLearnedKnobs.TryResolvePermille with the prior as the fallback.
		int LatePushArmyCapNow() => LatePushArmyCapOn ? LatePushParameters.ArmyCapPermille : Info.LatePushCapPercent * 10;

		// ---- lp_all_in_publish (design 01 D3) ----
		// The claim reads no personality-scaled number (D4a): not the idle limit, the squad value bars, the reserve share, the attack delay, the harass cap or the personality percent.

		// TeamLeadPublish, all-in trigger open: the ready test counts what the team can send (the seats' eligible armies) when that is more than the blob; the bars stay WAVE-MIN (LatePushBars keeps them for an all-in).
		(int Units, long Value) LatePushAllInBlob(TeamLeadTeam team, int now, (int Units, long Value) blob)
		{
			if (!LatePushAllInPublishOn)
				return blob;

			var eligible = team.Eligible(now, Info.TeamLeadPoolMaxAgeTicks);
			var claim = LatePush.AllInClaim(blob.Units, blob.Value, eligible.Units, eligible.Value);
			if (DiagnosticWriter != null && (claim.Units != blob.Units || claim.Value != blob.Value) && now - latePushAllInLogTick >= 250)
			{
				latePushAllInLogTick = now;
				DiagnosticTeamEvent("lp_all_in_publish", "tick=" + now + " blob=" + blob.Units + "/" + blob.Value + " eligible=" + eligible.Units + "/" + eligible.Value);
			}

			return claim;
		}

		// LatePushLend, an all-in order open: every guard unit above the floor goes (the floor is 0 in an all-in unless the own core is under attack); the guard-floor lend contract still narrows the claim.
		int LatePushAllInClaim(int spareUnits) => Math.Min(spareUnits, armyGuardLend.Count);

		// ---- one group out of the pool (lp_reinforce, lp_handover) ----

		// Forms ONE squad for the open order from at most `send` joinable pool units (only those in `only` when it is given): the rest of the pool is set aside around the launch and put back, so the launch sees
		// exactly the group. Returns how many units left the pool.
		int LatePushSendGroup(IBot bot, int send, HashSet<uint> only)
		{
			var original = new List<UnitWposWrapper>(unitsHangingAroundTheBase);
			var keep = new List<UnitWposWrapper>(send);
			var aside = new List<UnitWposWrapper>();
			foreach (var u in unitsHangingAroundTheBase)
			{
				if (keep.Count < send && TeamLeadJoinable(u) && (only == null || only.Contains(u.Actor.ActorID)))
					keep.Add(u);
				else
					aside.Add(u);
			}

			if (keep.Count == 0)
				return 0;

			unitsHangingAroundTheBase.Clear();
			unitsHangingAroundTheBase.AddRange(keep);
			CreateAttackForce(bot);
			var sent = keep.Count - unitsHangingAroundTheBase.Count;
			// The pool goes back in its original arrival order (the first-N joinable pick reads it).
			var stay = new HashSet<UnitWposWrapper>(unitsHangingAroundTheBase);
			stay.UnionWith(aside);
			unitsHangingAroundTheBase.Clear();
			foreach (var u in original)
				if (stay.Remove(u))
					unitsHangingAroundTheBase.Add(u);

			unitsHangingAroundTheBase.AddRange(stay);
			foreach (var n in notifyIdleBaseUnits)
				n.UpdatedIdleBaseUnits(unitsHangingAroundTheBase);

			return sent;
		}

		// ---- lp_reinforce (design 01 D5) ----

		// ArmyReinforce, first: with the row on the pushed order is reinforced once per seat (the old WAVE-MIN group every pass does not run). True = handled.
		// The key lives on the order and is the seat's: committed = the peak of the units its pushed squads held before the send, Sent = spent. A takeover by another module of the same seat does not rearm it.
		bool LatePushReinforce(IBot bot, TeamLeadOrder order, int now)
		{
			if (!LatePushReinforceOn || teamLeadKey == null)
				return false;

			if (now < initialAttackDelay)
				return true;

			var key = order.ReinforceKeyOf(teamLeadKey);
			var alive = 0;
			foreach (var s in Squads)
				if (s.IsValid && s.Type == SquadCAType.Rush && s.TeamPushSeq == order.Seq)
					alive += s.Units.Count;

			if (!key.Sent && alive > key.Committed)
				key.Committed = alive;

			// A spent key never fires again; nothing committed yet = nothing to replace; a handed-over group is on its way back (and just rejoined squads are not counted yet).
			if (key.Sent || key.Committed == 0 || now < key.HoldUntil || order.HasHandover(teamLeadKey))
				return true;

			var ratio = teamLeadTeam?.AheadRatio(now, Math.Max(Info.TeamLeadPoolMaxAgeTicks, 4 * Info.ArmyAheadScanTicks)) ?? 100;
			if (!LatePush.ReinforceDue(alive, key.Committed, Info.LatePushReinforceBelowPercent, ratio, Info.LatePushReinforceMinRatioPercent))
				return true;

			unitsHangingAroundTheBase.RemoveAll(u => unitCannotBeOrdered(u.Actor));
			var joinable = unitsHangingAroundTheBase.Count(TeamLeadJoinable);
			var send = LatePush.ReinforceSend(key.Committed, alive, joinable);

			// One real group or nothing: below the join floor the key stays unspent and the units wait in the pool.
			if (send < Info.TeamLeadJoinMinUnits)
				return true;

			var sent = LatePushSendGroup(bot, send, null);
			if (sent > 0)
			{
				key.Sent = true;
				DiagnosticTeamEvent("lp_reinforce", "seq=" + order.Seq + " committed=" + key.Committed + " alive=" + alive + " ratio=" + ratio + " sent=" + sent + " pool=" + unitsHangingAroundTheBase.Count);
			}

			return true;
		}

		// ---- lp_handover (design 01 D4b) ----

		// TraitDisabled, before the squads are dismissed: a personality switch hands the pushed squads of this module to the open order. The ids of their units go on the order under the seat's key; the module that
		// takes the seat over re-joins them (LatePushHandover). The order object already outlives the module (tl_order_survive).
		void LatePushHandoverRecord()
		{
			if (!LatePushHandoverOn || teamLeadKey == null || TeamLeadOpenOrder() is not { Persistent: true, Pushed: true } order)
				return;

			var ids = new List<uint>();
			foreach (var s in Squads)
				if (s.IsValid && s.TeamPushSeq == order.Seq)
					foreach (var u in s.Units)
						if (u.Actor != null)
							ids.Add(u.Actor.ActorID);

			if (ids.Count == 0)
				return;

			order.RecordHandover(teamLeadKey, ids);
			DiagnosticTeamEvent("lp_handover", "record seq=" + order.Seq + " units=" + ids.Count);
		}

		// ArmyReinforce, before lp_reinforce: the units a disabled module handed to the order and that are in this module's pool again form ONE group at once, whatever its size (they were already part of the
		// accepted order). It is not a reinforcement: the key is neither spent nor rearmed, it only holds the reinforcement for two team checks while the rejoined squad reports. True = handled this pass.
		bool LatePushHandover(IBot bot, TeamLeadOrder order, int now)
		{
			if (!LatePushHandoverOn || teamLeadKey == null || !order.TryGetHandover(teamLeadKey, out var ids))
				return false;

			unitsHangingAroundTheBase.RemoveAll(u => unitCannotBeOrdered(u.Actor));
			var only = new HashSet<uint>(ids);
			var available = unitsHangingAroundTheBase.Count(u => only.Contains(u.Actor.ActorID) && TeamLeadJoinable(u));
			if (available == 0)
				return false;

			var sent = LatePushSendGroup(bot, available, only);
			if (sent <= 0)
				return false;

			order.ClearHandover(teamLeadKey);
			order.ReinforceKeyOf(teamLeadKey).HoldUntil = now + 2 * Info.TeamLeadCheckTicks;
			DiagnosticTeamEvent("lp_handover", "rejoin seq=" + order.Seq + " sent=" + sent + " recorded=" + ids.Count);
			return true;
		}
	}
}
