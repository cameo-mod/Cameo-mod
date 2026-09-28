#region Copyright & License Information
/*
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

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

		public override object Create(ActorInitializer init) => new SiegeEvaluatorBotModule(init.Self, this);
	}

	public class SiegeEvaluatorBotModule : ConditionalTrait<SiegeEvaluatorBotModuleInfo>, IBotTick
	{
		readonly World world;
		readonly Player player;
		SquadManagerBotModuleCA squadManager;
		IBotRegionThreatProvider[] threatProviders;
		IBotRememberedDefenceProvider[] defenceProviders;
		int lastEvaluationTick = -1;

		public SiegeEvaluatorBotModule(Actor self, SiegeEvaluatorBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (IsTraitDisabled || world.WorldTick == lastEvaluationTick ||
				world.WorldTick % Info.EvaluationInterval != player.ClientIndex % Info.EvaluationInterval)
				return;
			lastEvaluationTick = world.WorldTick;

			squadManager ??= player.PlayerActor.TraitOrDefault<SquadManagerBotModuleCA>();
			threatProviders ??= player.PlayerActor.TraitsImplementing<IBotRegionThreatProvider>().ToArray();
			defenceProviders ??= player.PlayerActor.TraitsImplementing<IBotRememberedDefenceProvider>().ToArray();
			if (squadManager == null || defenceProviders.Length == 0)
				return;

			var defences = defenceProviders.SelectMany(p => p.RememberedDefences()).ToArray();

			foreach (var squad in squadManager.Squads)
			{
				if (!squad.IsValid || squad.Units.Count == 0 ||
					(squad.Type != SquadCAType.Rush && squad.Type != SquadCAType.Guerrilla && squad.Type != SquadCAType.Harass))
					continue;

				var squadValue = squad.Units.Sum(u => u.Actor?.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0);
				if (squadValue < Info.MinimumSquadValue)
					continue;

				var target = squad.Target;
				if (target.Type == TargetType.Invalid)
					continue;

				var targetCell = world.Map.CellContaining(target.CenterPosition);
				var squadCell = world.Map.CellContaining(squad.CenterPosition);

				var radiusSq = (long)Info.RelevantDefenceRadiusCells * Info.RelevantDefenceRadiusCells;
				var covering = defences
					.Where(d => (d.Cell - targetCell).LengthSquared <= radiusSq)
					.ToArray();

				var rememberedThreat = threatProviders.Sum(p => p.RememberedEnemyThreatAt(targetCell));
				var defenceValue = covering.Sum(d => d.Value);
				var obstacleValue = defenceValue + rememberedThreat;

				// §12.6 rule order: hold at stand-off -> artillery works the defences ->
				// commit on cleared area or overwhelming effective value -> retreat on loss.
				string verdict;
				var standOffCells = 0;
				if (covering.Length == 0)
					verdict = obstacleValue > 0 ? "commit-no-defences(army-covered)" : "free-advance";
				else
				{
					standOffCells = covering.Max(d => d.MaxRangeCells) + Info.StandOffMarginCells;
					var standOffSq = (long)standOffCells * standOffCells;
					var distToNearestDefenceSq = covering.Min(d => (d.Cell - squadCell).LengthSquared);
					var committed = (long)squadValue * 100 >= (long)Info.CommitRatioPercent * obstacleValue;
					if (distToNearestDefenceSq <= standOffSq)
						verdict = committed ? "overrun-line(commit anyway)" : "inside-defence-range(HOLD-FAIL)";
					else
						verdict = committed ? "commit" : "stand-off";
				}

				var artilleryAttached = squadManager.Squads.Any(s => s.IsValid && s.Type == SquadCAType.Artillery && s.Parent == squad && s.Units.Count > 0);
				AIUtils.BotDebug("AI ({0}): SIEGE-EVAL {1} squad v={2} units={3} at {4} -> {5}: defences={6} v={7} maxRange={8} threat={9} verdict={10} artillery={11}",
					player.ClientIndex, squad.Type, squadValue, squad.Units.Count, squadCell, targetCell,
					covering.Length, defenceValue, standOffCells, rememberedThreat, verdict,
					artilleryAttached ? "attached" : "none");
			}
		}
	}
}
