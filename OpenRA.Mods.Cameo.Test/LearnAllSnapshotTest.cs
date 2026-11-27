#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Dev;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// LEARN-ALL-02 P1b: the accepted snapshot (deterministic text, damaged = neutral), the provider switch (off by default, inert on master)
	// and the ablation row. The consumer code is untouched: only a new IBotMixLearning provider exists.
	[TestFixture]
	public sealed class LearnAllSnapshotTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");
		static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", name)).Replace("\r\n", "\n");

		static LearnTable Sample()
		{
			var table = new LearnTable();
			var enemies = new EnemyObservation[] { new("nod", "boom"), new("allies", null, 2) };
			LearnAllTrainer.ApplyGame(table, "g1", "wave_role.frontline", [new SeatObservation("rush", "gdi", enemies, "nod", 1.0)]);
			LearnAllTrainer.ApplyGame(table, "g2", "wave_role.antiair", [new SeatObservation("turtle", "gdi", [new EnemyObservation("nod", null)], "nod", -1.0)]);
			return table;
		}

		[Test]
		public void TheSnapshotTextRoundTripsEveryField()
		{
			var table = Sample();
			var text = LearnAllSnapshotWriter.Write(table);
			var back = LearnAllSnapshot.ParseText(text);
			Assert.That(LearnAllSnapshotWriter.Write(back), Is.EqualTo(text), "write -> parse -> write is stable");
			Assert.That(back.AppliedGames, Is.EqualTo(table.AppliedGames));
			var a = table.Find("rush", "wave_role.frontline");
			var b = back.Find("rush", "wave_role.frontline");
			Assert.That(b.Global, Is.EqualTo(a.Global));
			Assert.That(b.OwnFaction, Is.EqualTo(a.OwnFaction));
			Assert.That(b.EnemyFaction, Is.EqualTo(a.EnemyFaction));
			Assert.That(b.Style, Is.EqualTo(a.Style));
			Assert.That(back.Resolve("rush", "wave_role.frontline", "gdi", ["nod", "allies"], "nod", "boom"),
				Is.EqualTo(table.Resolve("rush", "wave_role.frontline", "gdi", ["nod", "allies"], "nod", "boom")));
		}

		[Test]
		public void TheWriterIsDeterministicAndSorted()
		{
			var text = LearnAllSnapshotWriter.Write(Sample());
			Assert.That(LearnAllSnapshotWriter.Write(Sample()), Is.EqualTo(text));
			Assert.That(text, Does.StartWith("BotLearnAll:\n\tVersion: 1\n"));
			Assert.That(text.IndexOf("Knob@any__wave_role.antiair", StringComparison.Ordinal), Is.LessThan(text.IndexOf("Knob@any__wave_role.frontline", StringComparison.Ordinal)));
			Assert.That(text.IndexOf("Knob@any__", StringComparison.Ordinal), Is.LessThan(text.IndexOf("Knob@rush__", StringComparison.Ordinal)));
			Assert.That(LearnAllSnapshotWriter.Write(Sample(), includeApplied: false), Does.Not.Contain("Applied"), "the release copy carries no bookkeeping");
		}

		[Test]
		public void ADamagedForeignOrOtherVersionFileIsNeutral()
		{
			foreach (var text in new[] { null, "", "   ", "Other:\n\tVersion: 1\n", "BotLearnAll:\n\tVersion: 2\n\tKnob@any__k:\n\t\tGlobal: 1500\n", "BotLearnAll:\n\tKnob@any__k:\n\t\tGlobal: 1500\n" })
			{
				var table = LearnAllSnapshot.ParseText(text);
				Assert.That(table.Personalities, Is.Empty, "text: " + text);
				Assert.That(table.Resolve("any", "k", "gdi", ["nod"], "nod", null), Is.EqualTo(1000));
			}
		}

		[Test]
		public void AnUnreadableOrOutOfDomainEstimateIsDroppedOrClampedNeverZero()
		{
			var table = LearnAllSnapshot.ParseText("BotLearnAll:\n\tVersion: 1\n\tKnob@any__k:\n\t\tGlobal: banana\n\t\tOwn@gdi: 0\n\t\tEnemy@nod: -5\n\t\tStyle@boom: 99999999\n\t\tEnemy@allies: 30\n");
			var axes = table.Find("any", "k");
			Assert.That(axes.Global, Is.EqualTo(1000), "unreadable = the neutral default");
			Assert.That(axes.OwnFaction, Is.Empty, "zero is dropped, not stored");
			Assert.That(axes.EnemyFaction.ContainsKey("nod"), Is.False);
			Assert.That(axes.Style["boom"], Is.EqualTo(LearnAllMath.StoreMax));
			Assert.That(axes.EnemyFaction["allies"], Is.EqualTo(LearnAllMath.StoreMin));
		}

		[Test]
		public void ThePersonalityTableWinsAndAnUnknownPersonalityReadsTheGlobalOne()
		{
			var table = LearnAllSnapshot.ParseText("BotLearnAll:\n\tVersion: 1\n\tKnob@any__k:\n\t\tGlobal: 1200\n\tKnob@rush__k:\n\t\tGlobal: 1500\n");
			Assert.That(table.Find("rush", "k").Global, Is.EqualTo(1500));
			Assert.That(table.Find("turtle", "k").Global, Is.EqualTo(1200));
			Assert.That(table.Find(null, "k").Global, Is.EqualTo(1200));
			Assert.That(table.Find("rush", "other"), Is.Null);
		}

		[Test]
		public void TheCommittedSnapshotIsEmptyAndTheModuleIsOffAndInertOnMaster()
		{
			var committed = Source("packaging/learn_all.yaml");
			Assert.That(LearnAllSnapshot.ParseText(committed).Personalities, Is.Empty);

			var ai = Fixture("mcv_deploy_ai.yaml");
			Assert.That(ai, Does.Contain("\tGrantConditionOnBotOwner@learnall:\n\t\tCondition: learn_all\n\t\tBots:\n"), "an empty Bots list = inert on master, the arm sets it; no fransbot grant");
			Assert.That(ai, Does.Not.Contain("Condition: learn_all\n\t\tBots: fransbot"));
			Assert.That(ai, Does.Contain("\tLearnAllBotModule:\n\t\tRequiresCondition: genericbot && learn_all\n"));
			Assert.That(new LearnAllBotModuleInfo().LearnedFile, Is.EqualTo("ai/learned/learn_all.yaml"));
		}

		[Test]
		public void TheGenericReadSeamLivesInTheCaAssemblyAndSwitchOffIsNeutral()
		{
			Assert.That(typeof(OpenRA.Mods.CA.Traits.IBotLearnedKnobs).Assembly, Is.SameAs(typeof(OpenRA.Mods.CA.Traits.IBotMixLearning).Assembly), "visible to the squad manager");

			var code = Source("mods/Cameo/Traits/BotModules/LearnAll/LearnAllBotModule.cs");
			Assert.That(code, Does.Contain("class LearnAllBotModule : ConditionalTrait<LearnAllBotModuleInfo>, IBotMixLearning, IBotLearnedKnobs"));
			Assert.That(code, Does.Contain("if (IsTraitDisabled || string.IsNullOrEmpty(knob) || ablateLearnAll.Off(player, AblationIds.LearnAll))\n\t\t\t\treturn false;"), "off, ablated or empty = false: the consumer keeps its prior");
			Assert.That(Fixture("mcv_deploy_ai.yaml"), Does.Contain("\tGrantConditionOnBotOwner@learnall:\n\t\tCondition: learn_all\n\t\tBots:\n"), "the module is off by default");
		}

		[Test]
		public void TheGenericReadSeamResolvesExactlyLikeTheTableAndRefusesAnUnknownKnob()
		{
			var table = LearnAllSnapshot.ParseText("BotLearnAll:\n\tVersion: 1\n\tKnob@any__raid.k:\n\t\tGlobal: 1200\n\t\tOwn@gdi: 900\n\t\tEnemy@nod: 1500\n\t\tEnemy@allies: 700\n\t\tStyle@boom: 1100\n\tKnob@rush__raid.k:\n\t\tGlobal: 1300\n");
			string[] enemies = ["nod", "allies"];
			foreach (var personality in new[] { "any", "rush", "turtle", null })
			{
				Assert.That(LearnAllBotModule.TryResolve(table, personality, "raid.k", "gdi", enemies, "nod", out var v), Is.True, personality);
				Assert.That(v, Is.EqualTo(table.Resolve(personality, "raid.k", "gdi", enemies, "nod", null)), "parity with LearnTable.Resolve, personality " + personality);
			}

			Assert.That(LearnAllBotModule.TryResolve(table, "any", "raid.k", "gdi", [], "", out var noEnemies), Is.True);
			Assert.That(noEnemies, Is.EqualTo(table.Resolve("any", "raid.k", "gdi", [], "", null)));

			Assert.That(LearnAllBotModule.TryResolve(table, "any", "no.such.knob", "gdi", enemies, "nod", out var unknown), Is.False, "unknown knob");
			Assert.That(unknown, Is.EqualTo(1000), "neutral out value");
			Assert.That(LearnAllBotModule.TryResolve(table, "any", "", "gdi", enemies, "nod", out _), Is.False);
			Assert.That(LearnAllBotModule.TryResolve(table, "any", null, "gdi", enemies, "nod", out _), Is.False);
			Assert.That(LearnAllBotModule.TryResolve(null, "any", "raid.k", "gdi", enemies, "nod", out _), Is.False);
			Assert.That(LearnAllBotModule.TryResolve(new LearnTable(), "any", "raid.k", "gdi", enemies, "nod", out _), Is.False, "nothing learned = false, not 1000");
		}

		[Test]
		public void TheProviderIsTheOnlyNewSeamAndItAnswersNothingWhenOffAblatedOrUnlearned()
		{
			var code = Source("mods/Cameo/Traits/BotModules/LearnAll/LearnAllBotModule.cs");
			Assert.That(code, Does.Contain("if (IsTraitDisabled || role == null || ablateLearnAll.Off(player, AblationIds.LearnAll))"));
			Assert.That(code, Does.Contain("if (axes == null)\n\t\t\t\treturn false;"));
			Assert.That(code, Does.Contain("class LearnAllBotModule : ConditionalTrait<LearnAllBotModuleInfo>, IBotMixLearning"));
			Assert.That(code, Does.Not.Contain("Apply("), "the release reader never steps a table");
			Assert.That(code, Does.Not.Contain("Write("), "and never writes the snapshot");
			Assert.That(OpenRA.Mods.Cameo.Dev.AblationSpec.RegistryIds(), Does.Contain("learn_all"));
		}
	}
}
