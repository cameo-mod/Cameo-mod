#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// SUPPORTPOWER-SLICE-01 (Opus 11-27): the engine's SupportPowerBotASModule scans every legal target in one tick (0.3-3.2 s calls in the V08H J3 game).
	// The mod-side successor scores a bounded number of candidates per bot tick. Pins: with an unlimited budget the scan is the engine's loop; on an unchanged
	// world any budget gives the same answer; the switch is off by default; the engine module is switched off only through the mod yaml.
	[TestFixture]
	public sealed class SupportPowerSlice01Test
	{
		static string SourceDir => Path.Combine(AppContext.BaseDirectory, "source");

		static string Source(string file) => File.ReadAllText(Path.Combine(SourceDir, file)).Replace("\r\n", "\n");

		// The engine's loop (SupportPowerBotASModule.FindAttackLocationToSupportPower), written out as the reference.
		static (bool Found, int Index, int Score) Engine(IReadOnlyList<int> scores, int minimum)
		{
			var found = false;
			var index = -1;
			var best = 0;
			for (var i = 0; i < scores.Count; i++)
			{
				var considered = scores[i];
				if (considered <= best || considered < minimum)
					continue;

				best = considered;
				index = i;
				found = true;
			}

			return (found, index, best);
		}

		static (bool Found, int Index, int Score, int Steps) Sliced(IReadOnlyList<int> scores, int minimum, int budget)
		{
			var scan = new SupportPowerSliceScan<int>(Enumerable.Range(0, scores.Count).ToList(), 0);
			var steps = 0;
			do
			{
				var examined = scan.Step(budget, _ => true, i => scores[i], minimum);
				steps++;
				Assert.That(budget <= 0 || examined <= budget, "a step never exceeds its budget");
			}
			while (!scan.Done);

			return (scan.HasBest, scan.HasBest ? scan.Best : -1, scan.BestScore, steps);
		}

		static IEnumerable<int[]> Cases()
		{
			yield return [];
			yield return [0];
			yield return [-5, -1];
			yield return [3000];
			yield return [10, 10, 10];
			yield return [5, 9, 9, 2, 9];
			yield return [1, 2, 3, 4, 5, 6, 7];
			yield return [7, 6, 5, 4, 3, 2, 1];

			// a fixed pseudo-random spread (no clock, no global RNG)
			var rng = new Random(1127);
			for (var n = 0; n < 40; n++)
				yield return Enumerable.Range(0, rng.Next(1, 200)).Select(_ => rng.Next(-400, 4000)).ToArray();
		}

		[Test]
		public void AnUnlimitedBudgetIsTheEngineLoopInOneStep()
		{
			foreach (var scores in Cases())
				foreach (var minimum in new[] { 1, 10, 3000 })
					foreach (var unlimited in new[] { 0, -1 })
					{
						var engine = Engine(scores, minimum);
						var sliced = Sliced(scores, minimum, unlimited);
						Assert.That(sliced.Steps, Is.EqualTo(1), "one step");
						Assert.That((sliced.Found, sliced.Index, sliced.Score), Is.EqualTo(engine), $"n={scores.Length} min={minimum}");
					}
		}

		[Test]
		public void OnAnUnchangedWorldEveryBudgetGivesTheEngineAnswer()
		{
			foreach (var scores in Cases())
				foreach (var minimum in new[] { 1, 10, 3000 })
					foreach (var budget in new[] { 1, 2, 7, 32, 1000 })
					{
						var engine = Engine(scores, minimum);
						var sliced = Sliced(scores, minimum, budget);
						Assert.That((sliced.Found, sliced.Index, sliced.Score), Is.EqualTo(engine), $"n={scores.Length} min={minimum} budget={budget}");
						Assert.That(sliced.Steps, Is.EqualTo(Math.Max(1, (scores.Length + budget - 1) / budget)), "the scan ends as soon as the candidates are used up");
					}
		}

		[Test]
		public void TheFirstOfEqualMaximaWinsAndACandidateThatLeftTheLegalSetIsSkipped()
		{
			var scores = new[] { 4, 9, 9, 9 };
			Assert.That(Sliced(scores, 1, 1).Index, Is.EqualTo(1));

			// index 1 is no longer legal when its turn comes: it costs budget but cannot win
			var scan = new SupportPowerSliceScan<int>([0, 1, 2, 3], 5);
			var told = new List<int>();
			Assert.That(scan.Step(2, i => i != 1, i => scores[i], 1, told.Add), Is.EqualTo(2));
			Assert.That(scan.Done, Is.False);
			Assert.That(scan.Step(0, i => i != 1, i => scores[i], 1, told.Add), Is.EqualTo(2));
			Assert.That(scan.Done);
			Assert.That(scan.Best, Is.EqualTo(2));
			Assert.That(told, Is.EqualTo(new[] { 0, 2 }), "each new best is reported once, in order");
			Assert.That(scan.StartTick, Is.EqualTo(5));
			Assert.That(scan.Count, Is.EqualTo(4));
		}

		[Test]
		public void TheSwitchIsOffByDefaultAndHasItsOwnRow()
		{
			Assert.That(AblationIds.SupportPowerSlice, Is.EqualTo("sp_slice"));
			Assert.That(typeof(SupportPowerSliceBotModuleInfo).GetField("SliceEnabled").GetValue(Activator.CreateInstance(typeof(SupportPowerSliceBotModuleInfo))), Is.EqualTo(false));

			var m = Source("mods/Cameo/Traits/BotModules/SupportPowerSliceBotModule.cs");
			Assert.That(m, Does.Contain("public readonly bool SliceEnabled = false;"));
			Assert.That(m, Does.Contain("bool Active => Info.SliceEnabled && !ablate.Off(player, AblationIds.SupportPowerSlice);"));
			Assert.That(m, Does.Contain("\t\t\tif (!Active)\n\t\t\t\treturn;\n\n\t\t\tif (activeToken == Actor.InvalidConditionToken)\n\t\t\t\tactiveToken = self.GrantCondition(Info.ActiveCondition);"),
				"nothing runs and no condition is granted while the switch or the row is off");
		}

		[Test]
		public void TheCandidateRuleAndTheOrderAreTheEngineModules()
		{
			var m = Source("mods/Cameo/Traits/BotModules/SupportPowerSliceBotModule.cs");

			// the engine's filter, term for term (fog: nothing is a candidate here that is not one there), applied at the snapshot and again when a candidate is scored
			Assert.That(m, Does.Contain("x.IsInWorld && !x.IsDead &&\n\t\t\t(decision.IgnoreVisibility || x.CanBeViewedByPlayer(player)) &&\n\t\t\tdecision.Against.HasRelationship(player.RelationshipWith(x.Owner)) &&\n\t\t\tdecision.Types.Overlaps(x.GetEnabledTargetTypes());"));
			Assert.That(m, Does.Contain("world.ActorsHavingTrait<IOccupySpace>().Where(x => Legal(x, decision)).ToList();"));
			Assert.That(m, Does.Contain("a => Legal(a, decision),\n\t\t\t\t\ta => decision.GetAttractiveness(a.CenterPosition, player),\n\t\t\t\t\tdecision.MinimumAttractiveness,"));

			// a scan that ran over several ticks is checked once more at its end; a one-tick scan is not (it is the engine's loop)
			Assert.That(m, Does.Contain("var found = run.Scan.HasBest && (run.Scan.StartTick == now || StillWorthIt(sp, run, decision));"));
			Assert.That(m, Does.Contain("Legal(run.Scan.Best, decision) &&\n\t\t\t(sp.Info.Cost == 0 || playerResource.Cash + playerResource.Resources >= sp.Info.Cost) &&\n\t\t\tdecision.GetAttractiveness(run.BestPos, player) >= decision.MinimumAttractiveness;"),
				"still a legal candidate, still affordable, still attractive");
			Assert.That(Source("mods/Cameo/Traits/ModularBot.cs"), Does.Not.Contain("\"SupportPowerSliceBotModule\""), "world-tick waits: never in the EngineReplay list");

			// the same order, the same waits
			Assert.That(m, Does.Contain("new Order(sp.Key, supportPowerManager.Self, Target.FromCell(world, world.Map.CellContaining(run.BestPos)), false)\n\t\t\t\t\t{ SuppressVisualFeedback = true, ExtraData = uint.MaxValue });"));
			Assert.That(m, Does.Contain("nextScanTick[sp] = now + 10;"));
			Assert.That(m, Does.Contain("nextScanTick[sp] = now + decision.GetNextScanTime(random);"));
			Assert.That(m, Does.Not.Contain("Stopwatch"), "the budget is a candidate count, never a clock");
			Assert.That(m, Does.Not.Contain("world.Actors."), "no second world scan");
		}
		[Test]
		public void TheEngineModuleIsSwitchedOffOnlyThroughTheModYamlAndTheYamlDoesNotArmTheSwitch()
		{
			var y = Source("packaging/ai.yaml");
			Assert.That(y, Does.Contain("\tSupportPowerSliceBotModule:\n\t\tRequiresCondition: genericbot\n\tSupportPowerBotASModule:\n\t\tRequiresCondition: (genericbot || classicbot) && !sp-slice\n\t\tDecisions:"),
				"the classic bot never has the slice module, so its engine module is as before; the decisions stay where they were");
			Assert.That(y, Does.Contain("\tSupportPowerBotASModule@wc2:\n\t\tRequiresCondition: genericbot && !sp-slice\n"));
			Assert.That(y, Does.Not.Contain("SliceEnabled"), "arming is a separate, reviewed yaml change (Wave B2)");
			Assert.That(typeof(SupportPowerSliceBotModuleInfo).GetField("ActiveCondition").GetValue(Activator.CreateInstance(typeof(SupportPowerSliceBotModuleInfo))), Is.EqualTo("sp-slice"));
		}
	}
}
