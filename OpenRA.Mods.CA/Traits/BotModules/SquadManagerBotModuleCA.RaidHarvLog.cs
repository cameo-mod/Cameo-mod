#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

#if CAMEO_DEV_LOGGING
using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// RAID-HARV-01 P3 (DEV only: this file does not exist in an official build; the call sites in SquadManagerBotModuleCA.RaidHarvRun.cs are inside #if CAMEO_DEV_LOGGING). The raid's DEV rows for the learner:
	// one raid_harv_seat row per seat (the roster of every enemy player) and one raid_harv_signal row per SAMPLE of the nine keys (never per tick). The rows go to ICameoLogSink.RaidHarvRow (the DEV writer queues them; no I/O here).
	// Everything is OBSERVATION: the squad's own value, the target as the visibility resolver returns it, armed enemies this player can view. Nothing here writes an order or a raid field the raid reads.
	// The target owner and its observed style are snapshotted AT THE PICK (RaidLog) and repeated unchanged on every row of the raid. The only per-tick cost is a handful of integer comparisons on the 5-tick check the raid already runs; the
	// post-release watches run once per 25 ticks and only while one is open; actor scans happen at the pick and at a loss incident only.
	public partial class SquadManagerBotModuleCA
	{
		const int RaidLogPassTicks = 25;
		const int RaidLogReconWatchTicks = 600;

		sealed class RaidLogReconWatch
		{
			public RaidLog Snap;
			public uint TargetId;
			public CPos LostCell;
			public int Radius;
			public int Current;
			public int StartTick;
		}

		sealed class RaidLogLossWatch
		{
			public RaidLog Snap;
			public uint TargetId;
			public RaidIncidentLossWatch Watch;
			public List<(Actor Actor, int Cost)> Units;
		}

		sealed class RaidLogDecayWatch
		{
			public RaidLog Snap;
			public RaidIncidentDecayWatch Watch;
		}

		readonly List<RaidLogReconWatch> raidLogRecon = new();
		readonly List<RaidLogLossWatch> raidLogLoss = new();
		readonly List<RaidLogDecayWatch> raidLogDecay = new();
		ICameoLogSink raidLogSink;
		string raidLogGameId;
		bool raidLogChecked;
		HashSet<string> raidLogRoster;
		IBotObservedStyleSource[] raidLogStyleSources;
		int raidLogCounter;
		int raidLogNextPass;

		string RaidLogSeat => string.IsNullOrWhiteSpace(Player.InternalName) ? "seat" + Array.IndexOf(World.Players, Player) : Player.InternalName;

		/// <summary>The DEV sink when raid rows may be written (an enabled sink, a regular live game, a valid game id); null otherwise. Decided once.</summary>
		ICameoLogSink RaidLogSinkOrNull()
		{
			if (!raidLogChecked)
			{
				raidLogChecked = true;
				var sink = World.WorldActor.TraitOrDefault<CameoLogHub>()?.Sink;
				var uid = World.LobbyInfo?.GlobalSettings?.GameUid;
				if (sink != null && sink.Enabled && !World.IsReplay && World.Type == WorldType.Regular && RaidHarvLogRows.ValidId(uid))
				{
					raidLogSink = sink;
					raidLogGameId = uid;
				}
			}

			return raidLogSink;
		}

		void RaidLogEmit(RaidLog log, RaidKey key, int now, string payload)
		{
			if (log == null || raidLogSink == null)
				return;

			raidLogSink.RaidHarvRow(RaidHarvLogRows.Signal(raidLogGameId, RaidLogSeat, now, log.RaidId, log.TargetPlayer, log.TargetStyle, key, payload), now);
		}

		/// <summary>The seat's roster row, once: every enemy player of this seat (a non-allied combatant), public lobby data.</summary>
		void RaidLogSeatRow(int now)
		{
			if (raidLogRoster != null)
				return;

			raidLogRoster = new HashSet<string>();
			var enemies = new List<(string Player, string Faction)>();
			foreach (var p in World.Players)
			{
				if (p == Player || !p.Playable || p.NonCombatant || string.IsNullOrEmpty(p.InternalName) || Player.RelationshipWith(p) != PlayerRelationship.Enemy)
					continue;

				enemies.Add((p.InternalName, p.Faction?.InternalName ?? "unknown"));
				raidLogRoster.Add(p.InternalName);
			}

			var personality = Player.PlayerActor.TraitsImplementing<IBotCurrentPersonality>().Select(x => x.CurrentPersonality).FirstOrDefault(s => !string.IsNullOrEmpty(s));
			raidLogSink.RaidHarvRow(RaidHarvLogRows.Seat(raidLogGameId, RaidLogSeat, personality, Player.Faction?.InternalName, enemies), now);
		}

		/// <summary>The style the observation classifier reports for the owner now; null = unseen (no classifier registered, or it has not qualified).</summary>
		string RaidLogStyle(Player owner)
		{
			raidLogStyleSources ??= Player.PlayerActor.TraitsImplementing<IBotObservedStyleSource>().ToArray();
			foreach (var source in raidLogStyleSources)
			{
				var style = source.ObservedStyle(owner);
				if (RaidHarvLogRows.ValidId(style))
					return style;
			}

			return null;
		}

		bool RaidLogArmedEnemy(Actor a)
		{
			// Visibility first, as RaidRequired does: nothing of an actor this player cannot view is read.
			if (!a.CanBeViewedByPlayer(Player) || !a.IsInWorld || a.IsDead || a.Owner == null)
				return false;

			return Player.RelationshipWith(a.Owner) == PlayerRelationship.Enemy && a.Info.HasTraitInfo<AttackBaseInfo>();
		}

		// ---- the hooks (all called from RaidHarvRun.cs) ------------------------------------------------------------------------------------

		/// <summary>The pick: the roster row (first raid of the seat), the target owner and style snapshot, the armed enemies seen inside the defence radius, a re-strike mark for the open incident watches on this target.</summary>
		void RaidLogStart(RaidRun run, RaidRoutePlan plan, RaidCandidate cand, Actor target, int now)
		{
			if (RaidLogSinkOrNull() == null || target?.Owner == null)
				return;

			RaidLogSeatRow(now);

			// A raid against an owner outside the roster would refuse the whole game in the extractor: it is not logged.
			var owner = target.Owner;
			if (!raidLogRoster.Contains(owner.InternalName))
				return;

			var log = new RaidLog
			{
				RaidId = RaidLogSeat + "-" + (++raidLogCounter),
				TargetPlayer = owner.InternalName,
				TargetStyle = RaidLogStyle(owner),
				PrevValue = run.StartValue,
			};

			log.Legs.SetPlan(plan.Waypoints, plan.Lane);
			foreach (var a in World.FindActorsInCircle(World.Map.CenterOfCell(cand.Cell), WDist.FromCells(RaidValue(RaidKey.DefenceRadius))))
				if (RaidLogArmedEnemy(a))
					log.InsideAtPick.Add(a.ActorID);

			foreach (var w in raidLogLoss)
				if (w.TargetId == run.TargetId)
					w.Watch.RestrikeStarted = true;

			run.Log = log;
		}

		/// <summary>raid.move_refresh: once per refresh interval of the learned value, the drift between the last ordered destination and the target's visible cell (before this tick's step re-issues the order). Travel and Strike only; the extractor decides eligibility.</summary>
		void RaidLogRefresh(RaidRun run, Actor actor, int now)
		{
			var log = run.Log;
			if (log == null || run.LastOrderTick < 0 || run.Phase is not (RaidPhase.Travel or RaidPhase.Strike))
				return;

			if (now - Math.Max(run.LastOrderTick, log.LastRefreshTick) < RaidValue(RaidKey.MoveRefresh))
				return;

			var visibleNow = actor != null;
			var drift = visibleNow ? RaidCheb(actor.Location, run.LastOrderCell) : 0;
			RaidLogEmit(log, RaidKey.MoveRefresh, now, RaidHarvLogRows.MoveRefresh(drift, log.LastRefreshVisible, visibleNow, run.Phase.ToString(), now - run.PhaseTick));
			log.LastRefreshTick = now;
			log.LastRefreshVisible = visibleNow;
		}

		/// <summary>The raid's 5-tick check: the first loss of the window (raid.incident_window), the leg of the lane (raid.waypoint_spacing), the pass of a remembered incident cell (raid.incident_decay).</summary>
		void RaidLogCheck(RaidRun run, CPos leadCell, int value, int now)
		{
			var log = run.Log;
			if (log == null)
				return;

			log.LastLeadCell = leadCell;
			if (value < log.PrevValue && log.FirstLossTick < 0)
				log.FirstLossTick = now;

			log.PrevValue = value;
			if (run.Phase == RaidPhase.Travel)
			{
				var left = log.Legs.Sample(run.Waypoints, run.WaypointIndex, leadCell);
				if (left != null)
					RaidLogEmit(log, RaidKey.WaypointSpacing, now, RaidHarvLogRows.WaypointSpacing(left.Value));
			}
			else
				log.Legs.Drop();

			foreach (var w in raidLogDecay)
				w.Watch.Passing(now, leadCell, log.RaidId);
		}

		/// <summary>The loss window restarted: a loss before it is not the first loss of the next one.</summary>
		static void RaidLogWindowReset(RaidRun run, int value)
		{
			if (run.Log == null)
				return;

			run.Log.FirstLossTick = -1;
			run.Log.PrevValue = value;
		}

		/// <summary>A lost-target episode starts and ends here (raid.lost_recon, raid.recon_radius). Called before the phase changes. A kill is not an exhausted recon.</summary>
		void RaidLogPhase(RaidRun run, RaidEvent ev, RaidPhase next, int now)
		{
			var log = run.Log;
			if (log == null)
				return;

			if (next == RaidPhase.LostRecon && run.Phase != RaidPhase.LostRecon)
			{
				log.EpisodeStartTick = now;
				log.EpisodeLostCell = run.LastSeenCell;
				log.EpisodeRadius = RaidValue(RaidKey.ReconRadius);
				return;
			}

			if (run.Phase != RaidPhase.LostRecon || log.EpisodeStartTick < 0)
				return;

			if (ev == RaidEvent.TargetReacquired)
			{
				RaidLogEmit(log, RaidKey.LostRecon, now, RaidHarvLogRows.LostReacquired(now - log.EpisodeStartTick, RaidValue(RaidKey.LostRecon)));

				// RaidTick has already moved LastSeenCell to the cell the target was reacquired at; the episode kept the cell it was lost at.
				RaidLogEmit(log, RaidKey.ReconRadius, now, RaidHarvLogRows.RadiusReacquired(RaidCheb(run.LastSeenCell, log.EpisodeLostCell), RaidValue(RaidKey.ReconRadius)));
			}
			else if (ev == RaidEvent.ReconBudgetUsed && !run.KillObserved)
			{
				RaidLogEmit(log, RaidKey.LostRecon, now, RaidHarvLogRows.LostExhausted(RaidValue(RaidKey.LostRecon)));
				raidLogRecon.Add(new RaidLogReconWatch
				{
					Snap = log,
					TargetId = run.TargetId,
					LostCell = log.EpisodeLostCell,
					Radius = log.EpisodeRadius,
					Current = RaidValue(RaidKey.ReconRadius),
					StartTick = now,
				});
			}

			log.EpisodeStartTick = -1;
		}

		/// <summary>A risk incident (called first in RaidIncident): only a loss incident is a sample source. The window span, a further incident for the open watches, the defence scan, and the two watches the incident starts.</summary>
		void RaidLogIncident(SquadCA squad, RaidRun run, CPos at, int now, string reason)
		{
			var log = run.Log;
			if (log == null || reason != "loss")
				return;

			foreach (var w in raidLogLoss)
				if (w.TargetId == run.TargetId && w.Watch.StartTick < now)
					w.Watch.FurtherIncident = true;

			foreach (var w in raidLogDecay)
				w.Watch.Incident(now, at, log.RaidId);

			// The check runs every 5 ticks: a loss and its trigger inside one check is a span of one check.
			RaidLogEmit(log, RaidKey.IncidentWindow, now, RaidHarvLogRows.IncidentWindow(Math.Max(5, log.FirstLossTick >= 0 ? now - log.FirstLossTick : 0), RaidValue(RaidKey.IncidentWindow)));
			log.FirstLossTick = -1;

			if (!log.OutsideDefenderTookPart && RaidLogOutsideDefenderTookPart(log, run, at))
				log.OutsideDefenderTookPart = true;

			var units = new List<(Actor Actor, int Cost)>();
			var total = 0;
			foreach (var a in RaidMovers(squad))
			{
				var cost = a.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;
				units.Add((a, cost));
				total += cost;
			}

			raidLogLoss.Add(new RaidLogLossWatch { Snap = log, TargetId = run.TargetId, Watch = new RaidIncidentLossWatch(now, total), Units = units });
			raidLogDecay.Add(new RaidLogDecayWatch { Snap = log, Watch = new RaidIncidentDecayWatch(at, now, RaidValue(RaidKey.IncidentDecay), log.RaidId) });
		}

		/// <summary>The release: the raid's result (raid.req_margin: a sample only when it killed) and, with losses, whether an armed enemy first seen outside the defence radius took part (raid.defence_radius).</summary>
		void RaidLogRelease(RaidRun run, int lost, int now)
		{
			var log = run.Log;
			if (log == null)
				return;

			var loss = run.StartValue > 0 ? (int)Math.Min(1000L, 1000L * lost / run.StartValue) : 0;
			RaidLogEmit(log, RaidKey.ReqMargin, now, RaidHarvLogRows.ReqMargin(run.KillObserved, loss));
			if (lost <= 0)
				return;

			if (!log.OutsideDefenderTookPart && RaidLogOutsideDefenderTookPart(log, run, log.LastLeadCell))
				log.OutsideDefenderTookPart = true;

			RaidLogEmit(log, RaidKey.DefenceRadius, now, RaidHarvLogRows.DefenceRadius(log.OutsideDefenderTookPart));
		}

		/// <summary>Armed enemies this player can view near the squad whose first sighting was outside the defence radius of the target: not seen inside it at the pick, and now farther than the radius from the target's last seen cell.</summary>
		bool RaidLogOutsideDefenderTookPart(RaidLog log, RaidRun run, CPos center)
		{
			var radius = RaidValue(RaidKey.DefenceRadius);
			foreach (var a in World.FindActorsInCircle(World.Map.CenterOfCell(center), WDist.FromCells(RaidLog.FightCells)))
				if (RaidLogArmedEnemy(a) && !log.InsideAtPick.Contains(a.ActorID) && RaidCheb(a.Location, run.LastSeenCell) > radius)
					return true;

			return false;
		}

		/// <summary>The post-release watches, once per 25 ticks and only while one is open (called from the bot tick).</summary>
		void RaidLogPass()
		{
			if (raidLogRecon.Count + raidLogLoss.Count + raidLogDecay.Count == 0)
				return;

			var now = World.WorldTick;
			if (now < raidLogNextPass)
				return;

			raidLogNextPass = now + RaidLogPassTicks;
			for (var i = raidLogRecon.Count - 1; i >= 0; i--)
			{
				// raid.recon_radius, budget used up: was the target seen farther than the radius within 600 ticks after the release? The sighting goes through the visibility resolver.
				var w = raidLogRecon[i];
				var actor = RaidTargetResolve(w.TargetId);
				var far = actor != null && RaidCheb(actor.Location, w.LostCell) > w.Radius;
				if (far || now - w.StartTick >= RaidLogReconWatchTicks)
				{
					RaidLogEmit(w.Snap, RaidKey.ReconRadius, now, RaidHarvLogRows.RadiusExhausted(far, w.Current));
					raidLogRecon.RemoveAt(i);
				}
			}

			for (var i = raidLogLoss.Count - 1; i >= 0; i--)
			{
				var w = raidLogLoss[i];
				long lost = 0;
				foreach (var (actor, cost) in w.Units)
					if (actor.IsDead)
						lost += cost;

				var outcome = w.Watch.Step(now, lost);
				if (outcome == null)
					continue;

				if (outcome.Value.LostHalfWithin600 || outcome.Value.RestrikeClean)
					RaidLogEmit(w.Snap, RaidKey.IncidentLoss, now, RaidHarvLogRows.IncidentLoss(outcome.Value));

				raidLogLoss.RemoveAt(i);
			}

			for (var i = raidLogDecay.Count - 1; i >= 0; i--)
			{
				var w = raidLogDecay[i];
				var outcome = w.Watch.Step(now);
				if (outcome == null)
					continue;

				if (outcome.Value.ReincidentWithin1500 || outcome.Value.PassedCleanBeforeDecay)
					RaidLogEmit(w.Snap, RaidKey.IncidentDecay, now, RaidHarvLogRows.IncidentDecay(outcome.Value));

				raidLogDecay.RemoveAt(i);
			}
		}
	}
}
#endif
