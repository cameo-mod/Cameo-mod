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
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// RM-03 pin 2 (design RM_03_YARD_RAMP_DESIGN_02 49BFE42A, Opus 11-26): the pure construction-yard demand floor and its planner seam (row rm03_yard_ramp, off by default).
	// Pin 1 (one MCV requester across the engine managers) and the lease/stall recovery wait for P8 on master (RM-03b).
	[TestFixture]
	public sealed class YardRamp01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		// Illustrative fixture only: NOT an approved difficulty table (the design: values need designer/Opus approval).
		static readonly string[] Table = { "0:easiest:1", "6000:easy:2", "12000:hard:3", "24000:brutal:4" };

		[Test]
		public void TiersAreTheDifficultyLadderAndUnrankedTypesHaveNone()
		{
			Assert.That(YardRamp.RankForBotType("noob"), Is.EqualTo(BotDifficultyLadder.NoobRank));
			Assert.That(YardRamp.RankForBotType("easiest"), Is.EqualTo(0));
			Assert.That(YardRamp.RankForBotType("HARD"), Is.EqualTo(4), "case-insensitive like the ladder");
			Assert.That(YardRamp.RankForBotType("cameogod"), Is.EqualTo(BotDifficultyLadder.Difficulties.Length - 1));
			Assert.That(YardRamp.RankForBotType("custom_bot"), Is.Null, "an unlisted type has no tier");
			Assert.That(YardRamp.RankForBotType(""), Is.Null);
			Assert.That(YardRamp.RankForBotType(null), Is.Null);
			Assert.That(YardRamp.RankFor(null), Is.Null);
		}

		[Test]
		public void ClassicIsNeverRampedAlthoughTheLadderAliasesItToHard()
		{
			Assert.That(BotDifficultyLadder.DefaultAliases["classic"], Is.EqualTo("hard"), "the alias exists");
			Assert.That(YardRamp.RankForBotType("classic"), Is.Null, "the A/B reference is never ramped");
			Assert.That(YardRamp.RankForBotType("Classic"), Is.Null);
			Assert.That(YardRamp.Floor(int.MaxValue, YardRamp.RankForBotType("classic"), YardRamp.Parse(Table, out _)), Is.EqualTo(0));
		}

		[Test]
		public void FransbotIsNeverRampedWhileFrozen()
		{
			Assert.That(BotDifficultyLadder.DefaultAliases["fransbot"], Is.EqualTo("hard"), "the alias exists");
			Assert.That(YardRamp.RankForBotType("fransbot"), Is.Null, "frozen until RETIRE-FRANSBOT-01");
			Assert.That(YardRamp.Floor(int.MaxValue, YardRamp.RankForBotType("fransbot"), YardRamp.Parse(Table, out _)), Is.EqualTo(0));
		}

		[TestCase("exploit_rush")]
		[TestCase("exploit_turtle")]
		[TestCase("exploit_guerrilla")]
		[TestCase("EXPLOIT_RUSH")]
		public void ExploitBotsFollowTheirHardAlias(string botType)
		{
			Assert.That(YardRamp.RankForBotType(botType), Is.EqualTo(YardRamp.RankForBotType("hard")), "one straight ladder");
			var rows = YardRamp.Parse(Table, out _);
			Assert.That(YardRamp.Floor(12000, YardRamp.RankForBotType(botType), rows), Is.EqualTo(YardRamp.Floor(12000, 4, rows)));
			Assert.That(YardRamp.Floor(12000, YardRamp.RankForBotType(botType), rows), Is.EqualTo(3));
		}

		[Test]
		public void AValidTableParsesInAnyOrder()
		{
			var rows = YardRamp.Parse(Table.Reverse().ToArray(), out var error);
			Assert.That(error, Is.Null);
			Assert.That(rows.Count, Is.EqualTo(4));
			Assert.That(rows.Any(r => r.MinTick == 12000 && r.MinRank == 4 && r.Desired == 3), Is.True);
			Assert.That(YardRamp.Parse(new[] { " 0 : noob : 1 " }, out error).Single().MinRank, Is.EqualTo(-1), "whitespace around the fields is fine");

			Assert.That(YardRamp.Parse(null, out error), Is.Empty);
			Assert.That(error, Is.Null);
			Assert.That(YardRamp.Parse(Array.Empty<string>(), out error), Is.Empty);
			Assert.That(error, Is.Null);
		}

		[TestCase("100:hard")]
		[TestCase("100:hard:2:9")]
		[TestCase("-1:hard:2")]
		[TestCase("+5:hard:2")]
		[TestCase("1e3:hard:2")]
		[TestCase("99999999999:hard:2")]
		[TestCase("100:classic:2")]
		[TestCase("100:4:2")]
		[TestCase("100:hard:-1")]
		[TestCase("100:hard:17")]
		[TestCase("")]
		public void OneBadEntryRejectsTheWholeTable(string bad)
		{
			var rows = YardRamp.Parse(new[] { "0:easiest:2", bad }, out var error);
			Assert.That(rows, Is.Empty, "fail-safe: floor 0 everywhere");
			Assert.That(error, Is.EqualTo(bad));
			Assert.That(YardRamp.Floor(int.MaxValue, 9, rows), Is.EqualTo(0));
		}

		[Test]
		public void TheFloorIsMonotoneInTickAndTier()
		{
			var rows = YardRamp.Parse(Table, out _);
			for (var rank = BotDifficultyLadder.NoobRank; rank < BotDifficultyLadder.Difficulties.Length; rank++)
			{
				var last = 0;
				for (var tick = 0; tick <= 30000; tick += 250)
				{
					var f = YardRamp.Floor(tick, rank, rows);
					Assert.That(f, Is.GreaterThanOrEqualTo(last), $"tick {tick} rank {rank}");
					if (rank > BotDifficultyLadder.NoobRank)
						Assert.That(f, Is.GreaterThanOrEqualTo(YardRamp.Floor(tick, rank - 1, rows)), $"tier step at tick {tick} rank {rank}");

					last = f;
				}
			}
		}

		[Test]
		public void NoobEasyAndHardFixturesAndInclusiveBoundaries()
		{
			var rows = YardRamp.Parse(Table, out _);

			// noob is below every row's tier here
			Assert.That(YardRamp.Floor(30000, -1, rows), Is.EqualTo(0));

			// easy (rank 2): 1 from tick 0, 2 from tick 6000 inclusive, never the hard/brutal rows
			Assert.That(YardRamp.Floor(0, 2, rows), Is.EqualTo(1));
			Assert.That(YardRamp.Floor(5999, 2, rows), Is.EqualTo(1));
			Assert.That(YardRamp.Floor(6000, 2, rows), Is.EqualTo(2));
			Assert.That(YardRamp.Floor(int.MaxValue, 2, rows), Is.EqualTo(2));

			// hard (rank 4): 3 from 12000; the brutal row (rank 6) stays out
			Assert.That(YardRamp.Floor(11999, 4, rows), Is.EqualTo(2));
			Assert.That(YardRamp.Floor(12000, 4, rows), Is.EqualTo(3));
			Assert.That(YardRamp.Floor(int.MaxValue, 4, rows), Is.EqualTo(3));
			Assert.That(YardRamp.Floor(24000, 6, rows), Is.EqualTo(4));

			// no tier, no table, no row admitted: 0, never negative
			Assert.That(YardRamp.Floor(30000, null, rows), Is.EqualTo(0));
			Assert.That(YardRamp.Floor(30000, 9, null), Is.EqualTo(0));
			Assert.That(YardRamp.Floor(int.MinValue, 9, rows), Is.EqualTo(0));
		}

		[Test]
		public void TheDisclosureSaysWhenTheRampIsImmaterial()
		{
			Assert.That(YardRamp.Material(3, 2, 0), Is.True, "CoverAllFields off: the floor decides");
			Assert.That(YardRamp.Material(3, 4, 0), Is.False, "the scale/utility target already asks for more");
			Assert.That(YardRamp.Material(3, 2, 3), Is.False, "CoverAllFields active + inflight already reaches it (genericbot: target 4, 3 inflight)");
			Assert.That(YardRamp.Material(4, 2, 3), Is.True);
			Assert.That(YardRamp.Material(0, 0, 0), Is.False, "off/empty: never material");
		}

		[Test]
		public void TheSeamIsAFloorAppliedOnceAndOffByDefault()
		{
			var planner = Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.cs");
			Assert.That(planner, Does.Contain("public readonly bool UseRampedYardDemand = false;"), "off by default");
			Assert.That(planner, Does.Contain("public readonly string[] YardRampRows = Array.Empty<string>();"), "no table by default");
			Assert.That(planner, Does.Contain("bool YardRampOn => Info.UseRampedYardDemand && !IsTraitDisabled && !ablateYardRamp.Off(player, AblationIds.YardRamp);"));
			Assert.That(planner, Does.Contain("rampFloor = YardRamp.Floor(world.WorldTick, YardRamp.RankFor(player), yardRampRows);\n\t\t\t\tif (rampFloor > targetCount)\n\t\t\t\t\ttargetCount = rampFloor;"),
				"Max only: the ramp raises, never caps");

			// applied after the scale/utility result (which holds the personality lean) and before CoverAllFields, McvDue unchanged
			var scale = planner.IndexOf("if (scaleTargetProviders.TryTarget(\"conyard\", out var scaledYards))", StringComparison.Ordinal);
			var ramp = planner.IndexOf("rampFloor = YardRamp.Floor(", StringComparison.Ordinal);
			var cover = planner.IndexOf("coverFloor = active + inflightCap;", StringComparison.Ordinal);
			var due = planner.IndexOf("if (!McvDue(farFieldFree, active + queued, targetCount))", StringComparison.Ordinal);
			Assert.That(scale, Is.GreaterThan(0));
			Assert.That(ramp, Is.GreaterThan(scale));
			Assert.That(cover, Is.GreaterThan(ramp));
			Assert.That(due, Is.GreaterThan(cover), "the free-far-field gate still applies to the ramped target");
			Assert.That(planner.Substring(ramp, cover - ramp), Does.Not.Contain("EffectiveMcvTargetCount"), "no second personality multiplier");

			Assert.That(AblationIds.YardRamp, Is.EqualTo("rm03_yard_ramp"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("rm03_yard_ramp"));

			var node = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == "ExpansionPlannerBotModule");
			Assert.That(node.Value.Nodes.Any(n => n.Key == "UseRampedYardDemand"), Is.False, "not armed until the table is approved (RM-03b)");
		}
	}
}
