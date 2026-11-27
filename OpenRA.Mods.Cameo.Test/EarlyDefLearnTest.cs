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
using System.IO;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	// CONSOLIDATE-03 P1 (b), row early_def_learn (EARLY_DEF_LEARN_01_DESIGN.md, user 11-26): the early anti-rush count = clamp(prior x M(map) x L, 0..max) and the learned start.
	// Neutral factors give the prior exactly, so the armed row equals the old rule until data exists.
	[TestFixture]
	public sealed class EarlyDefLearnTest
	{
		const string Dir = "mods/CA/Traits/BotModules/BotModuleLogic/";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void NeutralFactorsGiveThePriorExactly()
		{
			for (var prior = 0; prior <= 4; prior++)
				Assert.That(EarlyDefencePlan.LearnedCount(prior, EarlyDefencePlan.Neutral, EarlyDefencePlan.Neutral, 4), Is.EqualTo(prior));

			Assert.That(EarlyDefencePlan.MapMilli(0, 0), Is.EqualTo(EarlyDefencePlan.Neutral), "unknown path and reference");
			Assert.That(EarlyDefencePlan.MapMilli(0, 120), Is.EqualTo(EarlyDefencePlan.Neutral), "unknown path (EDL step 2 measures it)");
			Assert.That(EarlyDefencePlan.MapMilli(80, 0), Is.EqualTo(EarlyDefencePlan.Neutral), "no reference yet (EARLY_RUSH_STATS_01)");
		}

		[Test]
		public void TheFactorsScaleRoundAndClamp()
		{
			Assert.That(EarlyDefencePlan.LearnedCount(2, 1500, 1000, 4), Is.EqualTo(3), "2 x 1.5 = 3");
			Assert.That(EarlyDefencePlan.LearnedCount(3, 1000, 500, 4), Is.EqualTo(2), "3 x 0.5 = 1.5, rounded half up = 2");
			Assert.That(EarlyDefencePlan.LearnedCount(3, 2000, 2000, 4), Is.EqualTo(4), "clamped to the max");
			Assert.That(EarlyDefencePlan.LearnedCount(2, 0, 1000, 4), Is.EqualTo(0), "zero is legal: a learned eco-first opening");
			Assert.That(EarlyDefencePlan.LearnedCount(-1, 1000, 1000, 4), Is.EqualTo(0));

			Assert.That(EarlyDefencePlan.MapMilli(60, 120), Is.EqualTo(2000), "a short path: more defences, capped at x2");
			Assert.That(EarlyDefencePlan.MapMilli(240, 120), Is.EqualTo(500), "a long path: fewer, floored at x0.5");
			Assert.That(EarlyDefencePlan.MapMilli(100, 120), Is.EqualTo(1200));
		}

		[Test]
		public void TheLearnedStartOpensAfterTheOffset()
		{
			Assert.That(EarlyDefencePlan.StartOpen(false, 100, 5000, 0), Is.False, "never before a production building");
			Assert.That(EarlyDefencePlan.StartOpen(true, -1, 5000, 0), Is.True, "offset 0 = the prior: right after the first production building");
			Assert.That(EarlyDefencePlan.StartOpen(true, 1000, 1999, 1000), Is.False);
			Assert.That(EarlyDefencePlan.StartOpen(true, 1000, 2000, 1000), Is.True);
			Assert.That(EarlyDefencePlan.StartOpen(true, 1000, 3999, 9000), Is.False, "the offset is clamped to 3000");
			Assert.That(EarlyDefencePlan.StartOpen(true, 1000, 4000, 9000), Is.True);
		}

		[Test]
		public void TheRowIsWiredBehindTheS4SetAndOnInGeneric()
		{
			var module = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			Assert.That(module, Does.Contain("public readonly bool EarlyDefLearn = false;"), "default off in code");
			Assert.That(module, Does.Contain("public bool EarlyDefLearnArmed => Info.EarlyDefLearn && AntiRushSetArmed && !ablateEarlyDefLearn.Off(player, AblationIds.EarlyDefLearn);"));

			var defence = Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01Defence.cs");
			Assert.That(defence, Does.Contain("if (baseBuilder.EarlyDefLearnArmed)\n\t\t\t\tcount = EarlyDefencePlan.LearnedCount(count, EarlyDefencePlan.MapMilli(0, b.EarlyDefReferencePathCells), EarlyDefencePlan.Neutral, b.EarlyDefCountMax);"));

			Assert.That(AblationSpec.RegistryIds(), Does.Contain(AblationIds.EarlyDefLearn));

			var yaml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", "packaging", "ai.yaml")).Replace("\r\n", "\n");
			Assert.That(yaml, Does.Contain("\t\tEarlyDefLearn: true\n\t\tEarlyDefCountMax: 4\n"));
		}
	}
}
