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

		[Desc("Remembered defence this close to the squad's target (cells) counts as covering the approach.")]
		public readonly int RelevantDefenceRadiusCells = 24;

		[Desc("Commit when the squad's summed unit value reaches this percent of the remembered defence+army value around the target (rule 4's R, x100). Personality moves this later.")]
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
		readonly Dictionary<SquadCA, SiegeVerdict> lastVerdictBySquad = new();

		// S2/S6 per-squad memory (hold timer, latch, re-engage base value); dropped with the squad.
		readonly Dictionary<SquadCA, SiegeHold> holds = new();
		int lastEvaluationTick = -1;

		// QW-B: the evaluation tick within the interval; ClientIndex % interval (shared by map-side bots) unless the bot phase-offsets its timers.
		int evaluationPhase = -1;

		int StandOffMaxTicks => BotDifficultyLadder.Scale(Info.StandOffMaxTicksEasy, Info.StandOffMaxTicksHard, player, Info.StandOffMaxTicksHard);
		int SiegeReengagePct => BotDifficultyLadder.Scale(Info.SiegeReengagePctEasy, Info.SiegeReengagePctHard, player, Info.SiegeReengagePctHard);

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

				var radiusSq = (long)Info.RelevantDefenceRadiusCells * Info.RelevantDefenceRadiusCells;
				var covering = defences
					.Where(d => (d.Cell - targetCell).LengthSquared <= radiusSq)
					.ToArray();

				var rememberedThreat = threatProviders.MergedThreatAt(targetCell);
				var defenceValue = covering.Sum(d => d.Value);

				// CA-2c: regions that already beat a siege read heavier — the
				// next plan avoids the wall (§12.6 rule 5, "the next plan avoids it").
				var siegeMem = 100;
				if (Info.SiegeMemoryEnabled && failureMemory.Length > 0)
					siegeMem = failureMemory.Sum(p => p.FailedSiegeWeightPercentAt(targetCell, world.WorldTick));
				var obstacleValue = (defenceValue + rememberedThreat) * siegeMem / 100;

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
					verdict = obstacleValue > 0 ? "commit-no-defences(army-covered)" : "free-advance";
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

					var wasRetreating = lastVerdictBySquad.TryGetValue(squad, out var prev) && prev == SiegeVerdict.Retreat;
					verdictKind = Decide(prediction.OwnWins, committed, insideLine, artilleryAttached, squadValue, now,
						StandOffMaxTicks, SiegeReengagePct, wasRetreating, hold, out verdict, out var newRetreat);

					// The failed siege writes the loss into the wall's region
					// memory — once per retreat transition, not per pass spent
					// pinned (a standing-off squad does not stack the count).
					if (newRetreat && Info.SiegeMemoryEnabled)
						foreach (var p in failureMemory)
							p.RecordFailedSiege(nearest.Enemy, nearest.Cell, now);
				}

				verdicts[squad] = (verdictKind, standOffCell);
				lastVerdictBySquad[squad] = verdictKind;

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
			foreach (var stale in lastVerdictBySquad.Keys.Where(k => !verdicts.ContainsKey(k)).ToArray())
			{
				lastVerdictBySquad.Remove(stale);
				holds.Remove(stale);
			}

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
		}

		/// <summary>
		/// The verdict ladder for a squad that has remembered defences covering its target (pure, headless-testable).
		/// Retreat: the fight is predicted lost, or the re-engage cooldown still holds (SiegeReengagePct growth since the retreat; an
		/// overwhelming commit bypasses it). Advance: committed; or no artillery is attached (S1: a stand-off is held only while artillery
		/// works the defences); or the hold timed out (S2: the predicted-won fight is taken, and stays taken until a Retreat).
		/// StandOff: artillery attached, fight predicted won, hold not yet timed out.
		/// </summary>
		internal static SiegeVerdict Decide(bool ownWins, bool committed, bool insideLine, bool artilleryAttached, int squadValue, int now,
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

			if (!artilleryAttached)
			{
				hold.StandOffSince = -1;
				label = "advance(no-artillery)";
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
