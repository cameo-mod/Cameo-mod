#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

// LATEPUSH-01 P0 (LATEPUSH_01_DESIGN.md 4 D6, Opus-approved first piece): DEV-only evidence rows, no behaviour. The whole file exists only in a CAMEO_DEV_LOGGING build; a release
// build has neither these methods nor their call sites nor the record name. Every method reads state and never writes it (no RNG, no order, no squad or pool change), so a DEV
// game plays exactly like the same game without the rows. Rows go through the existing passive writer (DiagnosticWriter -> sink -> AiLogWriteQueue); there is no per-tick scan:
// the guard view is read at an order event, at an army_plan pass (every ArmyPlanTicks) and for the two fixed outcome horizons of an assignment. The join pass repeats until the seat has
// joined, so JoinBegin is one cheap walk over pool and guard (nothing at all when both are empty) and the guard's lend clauses are read only after a pass that sent units (CM review FIX).
#if CAMEO_DEV_LOGGING
using System.Collections.Generic;
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.CA.Traits
{
	public partial class SquadManagerBotModuleCA
	{
		const int LatePushFirstHorizonTicks = 600;
		const int LatePushSecondHorizonTicks = 1500;

		// What the home guard looks like right now and which clause of the guard-lend predicate stands (the same inputs as ArmyGuardPick; nothing is picked or lent here).
		sealed class LatePushGuardView
		{
			public bool Exists, TargetValid, Rally, Request, Perceived, Quiet;
			public int Units, Joinable, Far, Predicted, QuietTicks = -1;
			public long Value;
			public string State;
		}

		// The seat before one join: the pool, the guard and the guard's clauses. Diffed against the seat after the join to name the units that went.
		sealed class LatePushJoinContext
		{
			public TeamLeadOrder Order;
			public LatePushGuardView Guard;
			public readonly HashSet<Actor> Before = new();
			public readonly HashSet<Actor> GuardBefore = new();
			public int FreeUnits;
			public long FreeValue;
		}

		// One assignment waiting for its outcome rows.
		sealed class LatePushPending
		{
			public int Seq, Tick;
			public bool AllIn, FirstDone, SecondDone, ClosedDone;
			public string Why;
			public List<Actor> Units;
		}

		readonly List<LatePushPending> latePushPending = new();

		LatePushGuardView LatePushGuardNow()
		{
			var view = new LatePushGuardView();
			LatePushGuardUnits(view);
			LatePushGuardClauses(view);
			return view;
		}

		// The guard squad's size and where its units stand (home base or away from it). Cheap: one walk over the guard, no enemy or request lookup.
		void LatePushGuardUnits(LatePushGuardView view)
		{
			var guard = GetSquadOfType(SquadCAType.Protection);
			if (guard == null || !guard.IsValid)
				return;

			view.Exists = true;
			view.State = guard.FuzzyStateMachine?.CurrentState?.GetType().Name;
			view.TargetValid = guard.IsTargetValid;
			var baseRadius = (long)Info.MaxBaseRadius * Info.MaxBaseRadius;
			foreach (var u in guard.Units)
			{
				if (u.Actor == null)
					continue;

				view.Units++;
				view.Value += UnitValue(u.Actor);
				var inBase = (u.Actor.Location - initialBaseCenter).LengthSquared <= baseRadius;
				if (!inBase)
					view.Far++;
				else if (TeamLeadJoinable(u))
					view.Joinable++;
			}
		}

		// Which clauses of the guard-lend predicate stand (the same inputs as ArmyGuardPick). TargetValid was read with the guard's units, before any lend (it depends on the guard's first
		// unit and on the guard being non-empty). The rest read the rally, the requests, the threat providers and the quiet timer, which ArmyGuardLendTo and the join never write (checked
		// by reading both), so a join reads them after the pass and only when units went: a pass that sends nothing never pays for the request lookup.
		void LatePushGuardClauses(LatePushGuardView view)
		{
			if (!view.Exists)
				return;

			view.Rally = TryGetProtectionRally(out _);
			view.Request = SelectProtectionRequest() != null;
			if (threatPredictionProviders != null)
				foreach (var p in threatPredictionProviders)
				{
					view.Perceived |= p.PerceivedBaseThreat;
					view.Predicted += p.PredictedThreats.Count;
				}

			view.Quiet = GuardLend.Quiet(view.TargetValid, view.Rally, view.Request, view.Perceived || view.Predicted > 0);
			view.QuietTicks = protectionQuietSinceTick < 0 ? -1 : World.WorldTick - protectionQuietSinceTick;
		}

		// Three fields appended to the army_plan row: the guard's size, how many of its units stand away from the own base (H2), which lend clauses stand (H1) and how long it has been quiet.
		string LatePushPlanFields()
		{
			var g = LatePushGuardNow();
			if (!g.Exists)
				return " guard=0";

			return " guard=" + g.Units + "/" + g.Value + " gjoin=" + g.Joinable + " gfar=" + g.Far + " gstate=" + g.State
				+ " gcl=" + (g.TargetValid ? "T" : "") + (g.Rally ? "R" : "") + (g.Request ? "Q" : "") + (g.Perceived ? "P" : "") + (g.Predicted > 0 ? "D" + g.Predicted : "")
				+ " gquiet=" + (g.Quiet ? 1 : 0) + " gqt=" + g.QuietTicks;
		}

		// The leader published a team order: the team blob it counted, the WAVE-MIN bars it needed, and what it observes of the enemy (its last AheadScan, observed values only).
		// The enemy members are null while the seat never scanned (a follower seat).
		void LatePushOrderRow(TeamLeadOrder order, int blobUnits, long blobValue, int barUnits, long barValue)
		{
			var w = DiagnosticWriter;
			if (w == null || order == null)
				return;

			var scanned = aheadScanTick >= 0;
			var v = aheadView;
			w.TryEnqueueAttackAssign(new BotPassiveAttackAssignSnapshot
			{
				Tick = World.WorldTick, Phase = "order", OrderSeq = order.Seq, Why = order.AllIn ? order.AllInWhy : null, AllIn = order.AllIn,
				BlobUnits = blobUnits, BlobValue = blobValue, BarUnits = barUnits, BarValue = barValue,
				OwnArmy = scanned ? v.OwnArmy : null, EnemyArmy = scanned ? v.EnemyArmy : null, EnemyDefence = scanned ? v.EnemyDefence : null,
				EnemyBuildings = scanned ? v.EnemyBuildings : null, ScanTick = scanned ? aheadScanTick : null, ArmyKnown = scanned ? v.ArmyKnown : null
			});
		}

		// Called around a join pass (TeamLeadJoin's call site, ArmyReinforce) with the order the pass serves, before the pool is trimmed, the guard lends and the pool joins; null without a
		// writer (a release build never gets here) or an order, and when nothing could go (no pooled unit and no guard unit). The join pass repeats until the seat has joined, so this stays
		// one cheap walk over pool and guard: the guard's clauses are read in LatePushJoinEnd, only if units went. A unit the seat cannot order is skipped (TeamLeadJoin drops it).
		LatePushJoinContext LatePushJoinBegin(TeamLeadOrder order)
		{
			if (DiagnosticWriter == null || order == null)
				return null;

			var guard = GetSquadOfType(SquadCAType.Protection);
			var guardValid = guard != null && guard.IsValid;
			if (unitsHangingAroundTheBase.Count == 0 && (!guardValid || guard.Units.Count == 0))
				return null;

			var ctx = new LatePushJoinContext { Order = order, Guard = new LatePushGuardView() };
			LatePushGuardUnits(ctx.Guard);
			foreach (var u in unitsHangingAroundTheBase)
			{
				if (u.Actor == null || unitCannotBeOrdered(u.Actor))
					continue;

				ctx.Before.Add(u.Actor);
				ctx.FreeUnits++;
				ctx.FreeValue += UnitValue(u.Actor);
			}

			if (guardValid)
				foreach (var u in guard.Units)
					if (u.Actor != null && !unitCannotBeOrdered(u.Actor))
					{
						ctx.GuardBefore.Add(u.Actor);
						ctx.Before.Add(u.Actor);
					}

			return ctx;
		}

		// Called after the join pass: the units of pool and guard that are in neither any more are the units that went into the order. No row when none went.
		void LatePushJoinEnd(LatePushJoinContext ctx, TeamLeadOrder order, string phase)
		{
			var w = DiagnosticWriter;
			if (ctx == null || w == null || order == null)
				return;

			var poolNow = new HashSet<Actor>();
			foreach (var u in unitsHangingAroundTheBase)
				if (u.Actor != null)
					poolNow.Add(u.Actor);

			var guardNow = new HashSet<Actor>();
			var guard = GetSquadOfType(SquadCAType.Protection);
			if (guard != null && guard.IsValid)
				foreach (var u in guard.Units)
					if (u.Actor != null)
						guardNow.Add(u.Actor);

			var sent = new List<Actor>();
			long sentValue = 0;
			foreach (var a in ctx.Before)
				if (!poolNow.Contains(a) && !guardNow.Contains(a))
				{
					sent.Add(a);
					sentValue += UnitValue(a);
				}

			if (sent.Count == 0)
				return;

			var lent = 0;
			foreach (var a in ctx.GuardBefore)
				if (!guardNow.Contains(a))
					lent++;

			var g = ctx.Guard;
			LatePushGuardClauses(g);
			var scanned = aheadScanTick >= 0;
			var v = aheadView;
			w.TryEnqueueAttackAssign(new BotPassiveAttackAssignSnapshot
			{
				Tick = World.WorldTick, Phase = phase, OrderSeq = order.Seq, Why = order.AllIn ? order.AllInWhy : null, AllIn = order.AllIn,
				FreeUnits = ctx.FreeUnits, FreeValue = ctx.FreeValue,
				GuardUnits = g.Units, GuardValue = g.Value, GuardJoinable = g.Joinable, GuardFar = g.Far, GuardState = g.State,
				ClauseTargetValid = g.TargetValid, ClauseRally = g.Rally, ClauseRequest = g.Request, ClausePerceived = g.Perceived, ClausePredicted = g.Predicted,
				GuardQuiet = g.Quiet, QuietTicks = g.QuietTicks, LendOn = ArmyGuardLendOn, Lent = lent,
				SentUnits = sent.Count, SentValue = sentValue,
				OwnArmy = scanned ? v.OwnArmy : null, EnemyArmy = scanned ? v.EnemyArmy : null, EnemyDefence = scanned ? v.EnemyDefence : null,
				EnemyBuildings = scanned ? v.EnemyBuildings : null, ScanTick = scanned ? aheadScanTick : null, ArmyKnown = scanned ? v.ArmyKnown : null
			});

			latePushPending.Add(new LatePushPending { Seq = order.Seq, Tick = World.WorldTick, AllIn = order.AllIn, Why = order.AllIn ? order.AllInWhy : null, Units = sent });
		}

		// Once per BotTick from DiagnosticTick: a no-op while nothing is pending. The units that went are read again 600 and 1500 ticks later and when their order is no longer open.
		void LatePushOutcomeTick()
		{
			if (latePushPending.Count == 0)
				return;

			var w = DiagnosticWriter;
			if (w == null)
			{
				latePushPending.Clear();
				return;
			}

			var now = World.WorldTick;
			var open = TeamLeadOpenOrder();
			for (var i = latePushPending.Count - 1; i >= 0; i--)
			{
				var p = latePushPending[i];
				var age = now - p.Tick;
				if (!p.ClosedDone && (open == null || open.Seq != p.Seq))
				{
					p.ClosedDone = true;
					LatePushOutcomeRow(w, p, "closed", now);
				}

				if (!p.FirstDone && age >= LatePushFirstHorizonTicks)
				{
					p.FirstDone = true;
					LatePushOutcomeRow(w, p, "out600", now);
				}

				if (!p.SecondDone && age >= LatePushSecondHorizonTicks)
				{
					p.SecondDone = true;
					LatePushOutcomeRow(w, p, "out1500", now);
				}

				if (p.SecondDone)
					latePushPending.RemoveAt(i);
			}
		}

		void LatePushOutcomeRow(BotPassiveChannel w, LatePushPending p, string phase, int now)
		{
			var alive = 0;
			long aliveValue = 0;
			foreach (var a in p.Units)
				if (!a.IsDead && a.IsInWorld)
				{
					alive++;
					aliveValue += UnitValue(a);
				}

			var guard = GetSquadOfType(SquadCAType.Protection);
			w.TryEnqueueAttackAssign(new BotPassiveAttackAssignSnapshot
			{
				Tick = now, Phase = phase, OrderSeq = p.Seq, Why = p.Why, AllIn = p.AllIn, SentUnits = p.Units.Count, AliveUnits = alive, AliveValue = aliveValue,
				IdleHomeUnits = unitsHangingAroundTheBase.Count, GuardNowUnits = guard != null && guard.IsValid ? guard.Units.Count : 0
			});
		}
	}
}
#endif
