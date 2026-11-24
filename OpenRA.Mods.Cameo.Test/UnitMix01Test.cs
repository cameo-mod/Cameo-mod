#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// UNIT-MIX-01 (USER 11-24 via Opus): the army keeps a minimum share of VALUE in infantry (20%) and aircraft (10%, airfield present). Below the floor that category's queue is
	// served first on the next pass, inside the existing cash and unit rules, and an idle queue never waits. Solo and team alike (the rule reads only the bot's own army).
	[TestFixture]
	public sealed class UnitMix01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static Dictionary<UnitMixCategory, long> Army(long infantry, long aircraft, long other)
		{
			var census = new Dictionary<UnitMixCategory, long>();
			UnitMixFloorCA.Add(census, UnitMixCategory.Infantry, (int)infantry);
			UnitMixFloorCA.Add(census, UnitMixCategory.Aircraft, (int)aircraft);
			UnitMixFloorCA.Add(census, UnitMixCategory.Other, (int)other);
			return census;
		}

		[TestCase(200, 1000, 20, false, false)] // exactly 20%: met
		[TestCase(199, 1000, 20, false, true)]
		[TestCase(0, 1000, 0, false, false)] // floor 0 = off
		[TestCase(0, 0, 20, false, true)] // empty army: the first barracks pick is not left waiting
		[TestCase(0, 0, 10, true, false)] // ... but no aircraft priority before there is an army
		[TestCase(99, 1000, 10, true, true)]
		[TestCase(100, 1000, 10, true, false)]
		public void TheFloorIsAShareOfTheArmyValue(long category, long total, int pct, bool needsArmy, bool below) =>
			Assert.That(UnitMixFloorCA.BelowFloor(category, total, pct, needsArmy), Is.EqualTo(below));

		[Test]
		public void AMostlyTankArmyPutsTheInfantryQueueFirst()
		{
			// V05 picture: 9000 of tanks, 500 of infantry (5%), no aircraft, a barracks and an airfield exist.
			var d = UnitMixFloorCA.Deficits(Army(500, 0, 9000), true, 20, true, 10);
			Assert.That(d, Is.EqualTo(new[] { UnitMixCategory.Infantry, UnitMixCategory.Aircraft }), "infantry first, then aircraft");
		}

		[Test]
		public void ACategoryWithoutItsQueueIsNeverServedAndAMetFloorIsLeftAlone()
		{
			Assert.That(UnitMixFloorCA.Deficits(Army(0, 0, 9000), false, 20, false, 10), Is.Empty, "no barracks and no airfield yet");
			Assert.That(UnitMixFloorCA.Deficits(Army(3000, 1000, 6000), true, 20, true, 10), Is.Empty, "30% infantry, 10% aircraft: both met");
			Assert.That(UnitMixFloorCA.Deficits(Army(3000, 0, 7000), true, 20, false, 10), Is.Empty, "no airfield: no aircraft floor");
		}

		[Test]
		public void TheFloorsAreTunablePerPersonality()
		{
			var strict = UnitMixFloorCA.Deficits(Army(2500, 1500, 6000), true, 30, true, 20);
			var lax = UnitMixFloorCA.Deficits(Army(2500, 1500, 6000), true, 10, true, 5);
			Assert.That(strict, Is.EqualTo(new[] { UnitMixCategory.Infantry, UnitMixCategory.Aircraft }));
			Assert.That(lax, Is.Empty);

			var squad = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.cs");
			Assert.That(squad, Does.Contain("public readonly int UnitMixInfantryFloorPct = 20;"));
			Assert.That(squad, Does.Contain("public readonly int UnitMixAircraftFloorPct = 10;"));
		}

		[Test]
		public void TheDeficitQueuesAreServedBeforeTheRotationAndAnEmptyOneNeverWaits()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			var first = u.IndexOf("foreach (var queueType in UnitMixDeficitQueues().ToList())", StringComparison.Ordinal);
			Assert.That(first, Is.GreaterThan(0));
			Assert.That(first, Is.LessThan(u.IndexOf("for (var i = 0; i < Info.UnitQueues.Length; i++)", StringComparison.Ordinal)), "served first on the same batch, before the rotation");

			// an empty deficit queue the normal pick left without a pick takes the best buildable unit (the same idle-floor pick: delays, limits, caps, cash and reserve still apply)
			Assert.That(u, Does.Contain("if (unit == null && held == 0 && UnitMixOn && UnitMixDeficitQueues().Contains(queue.Info.Type))"));
			Assert.That(u, Does.Contain("unit = ChooseIdleFloorUnit(queue, shares, skip);"));

			// the pass-level cash floor and the per-unit afford/reserve checks stay in front of / behind it
			Assert.That(u, Does.Contain("if (!batch.Affords(cost) || !ReserveAdmits(batch, cost)"));
			Assert.That(u.IndexOf("UnitMixDeficitQueues().ToList())", StringComparison.Ordinal),
				Is.GreaterThan(u.IndexOf("Don't produce if we don't have enough cash", StringComparison.Ordinal)));
		}

		[Test]
		public void TheCensusCountsOnlyTheBotsOwnArmedUnitsAndWhatIsInProduction()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			var body = u.Substring(u.IndexOf("List<string> UnitMixDeficitQueues()", StringComparison.Ordinal));
			body = body.Substring(0, body.IndexOf("\n\t\t}\n", StringComparison.Ordinal));
			Assert.That(body, Does.Contain("a.Owner == player"));
			Assert.That(body, Does.Contain("q.AllQueued()"));
			Assert.That(body, Does.Not.Contain("RelationshipWith"), "no enemy or ally state is read: solo and team behave alike");
		}

		[Test]
		public void OnlyGenericbotArmsTheRowAndTheRowIsRegistered()
		{
			Assert.That(Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.EarlyDef.cs"), Does.Contain("\"um_category_floor\""));

			var player = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			var generic = player.Single(n => n.Key == "UnitBuilderBotModuleCA@generic").Value.Nodes;
			Assert.That(generic.Single(n => n.Key == "SwitchCondition").Value.Value, Is.EqualTo("genericbot"));
			Assert.That(generic.Single(n => n.Key == "UnitMixCategoryFloor").Value.Value, Is.EqualTo("true"));
			foreach (var o in player.Where(n => n.Key.StartsWith("UnitBuilderBotModuleCA", StringComparison.Ordinal) && n.Key != "UnitBuilderBotModuleCA@generic"))
				Assert.That(o.Value.Nodes.Any(n => n.Key == "UnitMixCategoryFloor"), Is.False, o.Key);
		}
	}
}
