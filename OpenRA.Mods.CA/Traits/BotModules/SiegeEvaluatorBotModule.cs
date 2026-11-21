#region Copyright & License Information
/*
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA;

namespace OpenRA.Mods.CA.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("CA-2a siege telemetry (AI_ARCHITECTURE §12.6): every EvaluationInterval ticks, logs what",
		"stand-off / artillery-first / commit / retreat WOULD have decided for each ground assault",
		"squad, from fog-honest remembered defences and regional threat. Issues no orders — the",
		"behaviour lands in CA-2b behind a switch, after this telemetry validates the inputs.")]
	public class SiegeEvaluatorBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("World ticks between evaluation passes.")]
		public readonly int EvaluationInterval = 100;

		[Desc("Cells of safety margin added to the longest remembered defence range when computing the stand-off line (DESIGN §19.1 equal-step margin placeholder until per-difficulty margins land).")]
		public readonly int StandOffMarginCells = 2;

		// SIEGE-FIX-01 S5: a remembered defence covers the approach when the squad-to-target segment passes within its range plus
		// StandOffMarginCells (it can hit the approach), not when it merely stands within a fixed radius of the target.

		[Desc("Commit when the squad's summed unit value reaches this percent of the remembered defence value covering the approach (rule 4's R, x100). A constant on every tier.",
			"The remembered regional army is NOT in this measure (SIEGE-FIX-01 S4): the 6c risk gate already screens it at target selection.")]
		public readonly int CommitRatioPercent = 150;

		[Desc("Minimum summed squad unit value worth evaluating — empty or token squads are noise.")]
		public readonly int MinimumSquadValue = 500;

		// SIEGE-FIX-01 S8 (Opus D1, user rule "no ladder tier may switch a feature off"): the CA-2b BehaviourEnabled switch is gone.
		// The verdict is always served; its strength scales on one straight line (DESIGN 19.1), noob = easiest minus one equal step.
		// Min* is the easiest tier, Max* the cameogod tier; BotDifficultyLadder.Scale interpolates by tier index (noob extrapolates).
		// StandOffMaxTicks = 3000 - 150 r, SiegeReengagePct = 40 - 2 r, r = 0 (noob) .. 10 (cameogod): the stronger tier decides sooner.
		[Desc("S2: longest a squad holds at the stand-off line, in ticks, at the easiest tier. After it the squad commits (the fight is predicted won).")]
		public readonly int StandOffMaxTicksEasy = 2850;

		[Desc("S2: the same bound at the cameogod tier.")]
		public readonly int StandOffMaxTicksHard = 1500;

		[Desc("S6: percent the squad's value must grow after a retreat verdict before it may re-engage, at the easiest tier.")]
		public readonly int SiegeReengagePctEasy = 38;

		[Desc("S6: the same percent at the cameogod tier.")]
		public readonly int SiegeReengagePctHard = 20;

		// S3: HysteresisBandPct = 25 - r (noob 25 .. cameogod 15).
		[Desc("S3: a changed verdict is served after two consecutive agreeing evaluations, or at once when the squad's value moved by this percent since the served verdict, at the easiest tier.")]
		public readonly int HysteresisBandPctEasy = 24;

		[Desc("S3: the same band at the cameogod tier.")]
		public readonly int HysteresisBandPctHard = 15;

		[Desc("CA-2c (§12.6 rule 5): a retreat verdict writes a failed-siege memory into the target",
			"region and remembered failures inflate the obstacle value on later evaluations,",
			"so the next plan avoids the wall that beat it. False = verdicts compute against",
			"raw memory only (control-arm identical).")]
		public readonly bool SiegeMemoryEnabled = false;

		[Desc("BM_live_combat_model: price the stand-off fight with the balance pipeline's",
			"effective-damage model (reliability, falloff, every warhead, charge-up) instead of the",
			"classic main-warhead DPS. False = classic numbers, bit-identical.")]
		public readonly bool UseEffectiveDamageModel = false;

		public override object Create(ActorInitializer init) => new SiegeEvaluatorBotModule(init.Self, this);
	}

	public class SiegeEvaluatorBotModule : ConditionalTrait<SiegeEvaluatorBotModuleInfo>, IBotTick, IBotSiegeAdvisor
	{
		readonly World world;
		readonly Player player;
		SquadManagerBotModuleCA[] squadManagers;
		IBotRegionThreatProvider[] threatProviders;
		IBotRememberedDefenceProvider[] defenceProviders;
		IBotSiegeFailureMemory[] failureMemory;
		readonly Dictionary<SquadCA, (SiegeVerdict Verdict, CPos StandOff)> verdicts = new();

		// S2/S6 per-squad memory (hold timer, latch, re-engage base value); dropped with the squad.
		readonly Dictionary<SquadCA, SiegeHold> holds = new();
		int lastEvaluationTick = -1;

		// QW-B: the evaluation tick within the interval; ClientIndex % interval (shared by map-side bots) unless the bot phase-offsets its timers.
		int evaluationPhase = -1;

		/// <summary>The tier's hold bound (S2); the retreat-side pull-back hold is capped by the same line (Opus ruling).</summary>
		public int StandOffMaxTicks => BotDifficultyLadder.Scale(Info.StandOffMaxTicksEasy, Info.StandOffMaxTicksHard, player, Info.StandOffMaxTicksHard);
		int SiegeReengagePct => BotDifficultyLadder.Scale(Info.SiegeReengagePctEasy, Info.SiegeReengagePctHard, player, Info.SiegeReengagePctHard);
		int HysteresisBandPct => BotDifficultyLadder.Scale(Info.HysteresisBandPctEasy, Info.HysteresisBandPctHard, player, Info.HysteresisBandPctHard);

		public SiegeEvaluatorBotModule(Actor self, SiegeEvaluatorBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		void IBotTick.BotTick(IBot bot)
		{
			var clock = BotVisitClock.Of(bot);
			if (IsTraitDisabled || clock.VisitTick == lastEvaluationTick)
				return;

			// QW-B phase (ClientIndex % interval unless the bot phase-offsets its timers) is the residue of the visit window.
			if (evaluationPhase < 0)
				evaluationPhase = BotSlotPhases.Resolve(bot, Info.EvaluationInterval, player.ClientIndex % Info.EvaluationInterval);

			if (!BotPeriod.ResidueHit(clock.VisitTick, clock.Elapsed, Info.EvaluationInterval, evaluationPhase))
				return;
			lastEvaluationTick = clock.VisitTick;

			// Personality-named squad managers (@rush/@turtle/…) enable only after
			// the personality latch and re-latch on switch — never cache an empty
			// or stale set (the first pass predates the latch).
			if (squadManagers == null || squadManagers.Length == 0 || squadManagers.Any(t => t.IsTraitDisabled))
				squadManagers = player.PlayerActor.TraitsImplementing<SquadManagerBotModuleCA>()
					.Where(t => !t.IsTraitDisabled).ToArray();
			threatProviders ??= player.PlayerActor.TraitsImplementing<IBotRegionThreatProvider>().ToArray();
			defenceProviders ??= player.PlayerActor.TraitsImplementing<IBotRememberedDefenceProvider>().ToArray();
			failureMemory ??= player.PlayerActor.TraitsImplementing<IBotSiegeFailureMemory>().ToArray();
			if (squadManagers.Length == 0 || defenceProviders.Length == 0)
				return;

			var defences = defenceProviders.SelectMany(p => p.RememberedDefences()).ToArray();
			var rules = world.Map.Rules;
			verdicts.Clear();
			var evaluated = 0;

			foreach (var squad in squadManagers.SelectMany(m => m.Squads))
			{
				if (!squad.IsValid || squad.Units.Count == 0 ||
					(squad.Type != SquadCAType.Rush && squad.Type != SquadCAType.Guerrilla && squad.Type != SquadCAType.Harass))
					continue;

				var squadValue = squad.Units.Sum(u => u.Actor?.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0);
				if (squadValue < Info.MinimumSquadValue)
					continue;

				evaluated++;

				var target = squad.Target;
				if (target.Type == TargetType.Invalid)
					continue;

				var targetCell = world.Map.CellContaining(target.CenterPosition);
				var squadCell = world.Map.CellContaining(squad.CenterPosition);

				var covering = defences
					.Where(d => CoversApproach(d.Cell, d.MaxRangeCells + Info.StandOffMarginCells, squadCell, targetCell))
					.ToArray();

				var rememberedThreat = threatProviders.MergedThreatAt(targetCell);
				var defenceValue = covering.Sum(d => d.Value);

				// CA-2c: regions that already beat a siege read heavier — the
				// next plan avoids the wall (§12.6 rule 5, "the next plan avoids it").
				var siegeMem = 100;
				if (Info.SiegeMemoryEnabled && failureMemory.Length > 0)
					siegeMem = failureMemory.Sum(p => p.FailedSiegeWeightPercentAt(targetCell, world.WorldTick));
				// S4: ONE measure. The commit ratio compares the squad with the remembered defences the predictor priced; the remembered
				// regional army (scalar, no composition, so the predictor cannot price it) is screened by the 6c risk gate at target
				// selection and is only logged here. Stacking it on top was the second, differently-scaled comparison of RCA C2.
				var obstacleValue = defenceValue * siegeMem / 100;

				// §12.6 rule order: hold at stand-off -> artillery works the defences ->
				// commit on cleared area or overwhelming effective value -> retreat on loss.
				// "Effective" is Versus-weighted: BotCombatPredictor.Predict with the
				// remembered defences as the enemy set — one engagement authority.
				var artilleryAttached = squadManagers.Any(m => m.Squads.Any(s =>
					s.IsValid && s.Type == SquadCAType.Artillery && s.Parent == squad && s.Units.Count > 0));
				var now = world.WorldTick;
				string verdict;
				var standOffCells = 0;
				var verdictKind = SiegeVerdict.Advance;
				var standOffCell = CPos.Zero;
				if (covering.Length == 0)
				{
					verdict = rememberedThreat > 0 ? "commit-no-defences(army-covered)" : "free-advance";
					holds.Remove(squad);
				}
				else
				{
					var own = squad.Units.Where(u => u.Actor != null)
						.GroupBy(u => u.Actor.Info)
						.SelectMany(g => Info.UseEffectiveDamageModel
							? g.Select(u => (BotUnitProfiles.Get(u.Actor, player, true), 1))
							: new[] { (BotUnitProfiles.Get(rules, g.Key, false), g.Count()) })
						.ToList();
					var enemy = covering.GroupBy(d => d.Observed)
						.Select(g => (BotUnitProfiles.Get(rules, g.Key, Info.UseEffectiveDamageModel), g.Count()))
						.ToList();
					var prediction = BotCombatPredictor.Predict(own, enemy, Info.UseEffectiveDamageModel);

					// S1: stand-off support = artillery attached, or a ground unit whose weapon can hit the longest-range covering
					// defence's type from at least that defence's range (it can work the defence from the stand-off line).
					var longest = covering.Aggregate((a, b) => a.MaxRangeCells >= b.MaxRangeCells ? a : b);
					var longestProfile = BotUnitProfiles.Get(rules, longest.Observed, Info.UseEffectiveDamageModel);
					var standOffSupport = artilleryAttached ||
						HasStandOffWeapon(own.Select(o => o.Item1), longestProfile, longest.MaxRangeCells);

					standOffCells = covering.Max(d => d.MaxRangeCells) + Info.StandOffMarginCells;
					var standOffSq = (long)standOffCells * standOffCells;
					var nearest = covering.Aggregate((a, b) =>
						(a.Cell - squadCell).LengthSquared <= (b.Cell - squadCell).LengthSquared ? a : b);
					var distToNearestDefenceSq = (nearest.Cell - squadCell).LengthSquared;
					var insideLine = distToNearestDefenceSq <= standOffSq;
					var committed = prediction.OwnWins &&
						(long)squadValue * 100 >= (long)Info.CommitRatioPercent * obstacleValue;

					// The hold point sits on the stand-off line, on the defence->squad bearing.
					if (!committed)
					{
						var delta = squadCell - nearest.Cell;
						var distCells = (int)Math.Sqrt((double)distToNearestDefenceSq);
						var hx = distCells >= standOffCells ? squadCell.X
							: nearest.Cell.X + (int)((long)delta.X * standOffCells / Math.Max(1, distCells));
						var hy = distCells >= standOffCells ? squadCell.Y
							: nearest.Cell.Y + (int)((long)delta.Y * standOffCells / Math.Max(1, distCells));
						standOffCell = new CPos(hx, hy);
					}

					if (!holds.TryGetValue(squad, out var hold))
						holds[squad] = hold = new SiegeHold();

					var wasRetreating = hold.LastRaw == SiegeVerdict.Retreat;
					var raw = Decide(prediction.OwnWins, committed, insideLine, standOffSupport, squadValue, now,
						StandOffMaxTicks, SiegeReengagePct, wasRetreating, hold, out verdict, out var newRetreat);
					hold.LastRaw = raw;

					// S3: the served verdict follows the raw one only after two agreeing evaluations or a material change of the squad's value;
					// a hold that just ended by timeout (S2) is a decision and is served at once.
					verdictKind = Hysteresis(raw, squadValue, HysteresisBandPct, raw == SiegeVerdict.Advance && hold.TimedOut, hold);
					if (verdictKind != raw)
						verdict += "~held(" + verdictKind + ")";

					// The failed siege writes the loss into the wall's region
					// memory — once per retreat transition, not per pass spent
					// pinned (a standing-off squad does not stack the count).
					if (newRetreat && Info.SiegeMemoryEnabled)
						foreach (var p in failureMemory)
							p.RecordFailedSiege(nearest.Enemy, nearest.Cell, now);
				}

				verdicts[squad] = (verdictKind, standOffCell);

				// Log.Write direct: AIUtils.BotDebug is gated on Game.Settings.Debug.BotDebug,
				// which the match harness does not set — the FransBotLog pattern.
#if CAMEO_DEV_LOGGING
				Log.Write("debug", string.Format("[SIEGE-EVAL][WT {0}] AI {1} {2} squad v={3} units={4} at {5} -> {6}: defences={7} v={8} maxRange={9} threat={10} verdict={11} artillery={12} siegeMem={13}",
					world.WorldTick, player.ClientIndex, squad.Type, squadValue, squad.Units.Count, squadCell, targetCell,
					covering.Length, defenceValue, standOffCells, rememberedThreat, verdict,
					artilleryAttached ? "attached" : "none", siegeMem));
#endif
			}

			// Drop transition entries for squads that left evaluation (dissolved
			// or below value floor) — the dict must not grow with squad churn.
			foreach (var stale in holds.Keys.Where(k => !verdicts.ContainsKey(k)).ToArray())
				holds.Remove(stale);

			// Heartbeat: distinguishes "module silent" from "no assault squads
			// qualified" — a whole batch of silence means formation starves.
#if CAMEO_DEV_LOGGING
			Log.Write("debug", string.Format("[SIEGE-EVAL][WT {0}] AI {1} pass: squads={2} evaluated={3} defences={4}",
				world.WorldTick, player.ClientIndex,
				squadManagers.Sum(m => m.Squads.Count(s => s.IsValid)), evaluated, defences.Length));
#endif
		}

		/// <summary>S2/S6 memory of one squad's siege: when the current stand-off hold began, whether it ended by timeout, and the value at the last retreat.</summary>
		internal sealed class SiegeHold
		{
			public int StandOffSince = -1;
			public bool TimedOut;
			public int ReengageBase = -1;

			// S3: the verdict being served (-1 none), the squad value when it began, and the changed raw verdict awaiting a second vote.
			public int Served = -1;
			public int ServedValue;
			public int Pending = -1;
			public int PendingCount;

			// What Decide said last pass (not what was served): the retreat-transition test must not depend on the held verdict.
			public SiegeVerdict? LastRaw;
		}

		/// <summary>
		/// S3 hysteresis (pure). Serve <paramref name="raw"/> at once on the first evaluation, when <paramref name="bypass"/> is set, or when the
		/// squad's value moved by at least <paramref name="bandPct"/> percent since the served verdict began; otherwise a changed verdict
		/// is served only on its second consecutive vote. A new squad is a new <see cref="SiegeHold"/> (reset on squad change).
		/// </summary>
		internal static SiegeVerdict Hysteresis(SiegeVerdict raw, int squadValue, int bandPct, bool bypass, SiegeHold hold)
		{
			if (hold.Served < 0 || bypass || (int)raw == hold.Served)
				return Serve(raw, squadValue, hold);

			var moved = hold.ServedValue > 0 &&
				Math.Abs((long)squadValue - hold.ServedValue) * 100 >= (long)bandPct * hold.ServedValue;
			if (moved)
				return Serve(raw, squadValue, hold);

			hold.PendingCount = hold.Pending == (int)raw ? hold.PendingCount + 1 : 1;
			hold.Pending = (int)raw;
			return hold.PendingCount >= 2 ? Serve(raw, squadValue, hold) : (SiegeVerdict)hold.Served;
		}

		static SiegeVerdict Serve(SiegeVerdict raw, int squadValue, SiegeHold hold)
		{
			if ((int)raw != hold.Served)
				hold.ServedValue = squadValue;

			hold.Served = (int)raw;
			hold.Pending = -1;
			hold.PendingCount = 0;
			return raw;
		}

		/// <summary>S1 (pure): a weapon reaches a defence from the stand-off line when its range is at least the defence's range.</summary>
		internal static bool Outranges(WDist weaponRange, int defenceRangeCells) => weaponRange.Length >= WDist.FromCells(defenceRangeCells).Length;

		/// <summary>S1 (pure): some unit has a weapon that can target the defence's type and Outranges it.</summary>
		internal static bool HasStandOffWeapon(IEnumerable<BotUnitProfile> units, BotUnitProfile defence, int defenceRangeCells)
		{
			foreach (var u in units)
				foreach (var w in u.Weapons)
					if (w.CanTarget(defence.TargetTypes) && Outranges(w.Range, defenceRangeCells))
						return true;

			return false;
		}

		/// <summary>The retreat-side pull-back hold ends once it has run <paramref name="capTicks"/> (0 or less = uncapped).</summary>
		internal static bool HoldExpired(int now, int startTick, int capTicks) => capTicks > 0 && now - startTick >= capTicks;

		/// <summary>
		/// S5 (pure): true when a defence of effective reach <paramref name="reachCells"/> (its range plus the stand-off margin) can hit the
		/// approach, i.e. the squad-to-target segment passes within that reach of it. Integer math, no sqrt.
		/// </summary>
		internal static bool CoversApproach(CPos defence, int reachCells, CPos from, CPos to)
		{
			return DistanceSqToSegment(defence, from, to) <= (long)reachCells * reachCells;
		}

		internal static long DistanceSqToSegment(CPos p, CPos a, CPos b)
		{
			long dx = b.X - a.X, dy = b.Y - a.Y;
			long px = p.X - a.X, py = p.Y - a.Y;
			var len2 = dx * dx + dy * dy;
			if (len2 == 0)
				return px * px + py * py;

			var t = px * dx + py * dy;
			if (t <= 0)
				return px * px + py * py;

			if (t >= len2)
			{
				long qx = p.X - b.X, qy = p.Y - b.Y;
				return qx * qx + qy * qy;
			}

			var cross = px * dy - py * dx;
			return cross * cross / len2;
		}

		/// <summary>
		/// The verdict ladder for a squad that has remembered defences covering its target (pure, headless-testable).
		/// Retreat: the fight is predicted lost, or the re-engage cooldown still holds (SiegeReengagePct growth since the retreat; an
		/// overwhelming commit bypasses it). Advance: committed; or no stand-off support (S1: a stand-off is held only while artillery is
		/// attached or a ground unit can work the covering defence from its own range); or the hold timed out (S2: the predicted-won fight is taken, and stays taken until a Retreat).
		/// StandOff: artillery attached, fight predicted won, hold not yet timed out.
		/// </summary>
		internal static SiegeVerdict Decide(bool ownWins, bool committed, bool insideLine, bool standOffSupport, int squadValue, int now,
			int standOffMaxTicks, int reengagePct, bool wasRetreating, SiegeHold hold, out string label, out bool newRetreat)
		{
			newRetreat = false;
			var cooling = !committed && hold.ReengageBase >= 0 &&
				(long)squadValue * 100 < (long)hold.ReengageBase * (100 + reengagePct);

			if (!ownWins || cooling)
			{
				label = ownWins ? "retreat(reengage-cooldown)" : "retreat(predicted-loss)";
				hold.StandOffSince = -1;
				hold.TimedOut = false;
				if (!wasRetreating)
				{
					hold.ReengageBase = squadValue;
					newRetreat = true;
				}

				return SiegeVerdict.Retreat;
			}

			if (committed)
			{
				hold.StandOffSince = -1;
				hold.TimedOut = false;
				hold.ReengageBase = -1;
				label = insideLine ? "overrun-line(commit anyway)" : "commit";
				return SiegeVerdict.Advance;
			}

			hold.ReengageBase = -1;
			if (hold.TimedOut)
			{
				label = "commit(stand-off-timeout)";
				return SiegeVerdict.Advance;
			}

			if (!standOffSupport)
			{
				hold.StandOffSince = -1;
				label = "advance(no-standoff-support)";
				return SiegeVerdict.Advance;
			}

			if (hold.StandOffSince < 0)
				hold.StandOffSince = now;

			if (now - hold.StandOffSince >= standOffMaxTicks)
			{
				hold.StandOffSince = -1;
				hold.TimedOut = true;
				label = "commit(stand-off-timeout)";
				return SiegeVerdict.Advance;
			}

			label = insideLine ? "inside-defence-range(HOLD-FAIL)" : "stand-off";
			return SiegeVerdict.StandOff;
		}

		// IBotSiegeAdvisor — serves the cached evaluation. A disabled trait or a squad
		// without a cached evaluation answers Advance (no objection). There is no
		// behaviour switch (SIEGE-FIX-01 S8): the verdict is always served, graded on the ladder.
		// standOffCell is valid for StandOff and Retreat (the pull-back point on the stand-off line).
		public SiegeVerdict VerdictFor(SquadCA squad, out CPos standOffCell)
		{
			standOffCell = CPos.Zero;
			if (IsTraitDisabled || !verdicts.TryGetValue(squad, out var cached))
				return SiegeVerdict.Advance;

			standOffCell = cached.StandOff;
			return cached.Verdict;
		}
	}
}
