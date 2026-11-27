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
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	// FOG-MINELAYER-01 (Opus 11-26 BINDING no-omniscience; Haiku FOG_SCAN_03 3ec93acd, Terra RM_04_MINELAYER_FOG_DESIGN_01 aabe98b8). The engine MinelayerBotModule reads hidden enemies four ways (a world scan with
	// the enemy's location, a nearby-enemy test, the live target-type state, the attacker's location). The generic bot now runs ObservedMinelayerBotModule, which knows only what it observes: an enemy actor in sight,
	// a frozen-layer record, its own actors. Row fog_minelayer off = no generic minelaying at all, never the old hidden read; classicbot keeps the engine module unchanged.
	[TestFixture]
	public sealed class FogMinelayerTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		// Whitespace runs collapse to one space so the pins do not depend on indentation or line breaks.
		static string Module() => System.Text.RegularExpressions.Regex.Replace(Source("mods/Cameo/Traits/BotModules/ObservedMinelayerBotModule.cs"), @"\s+", " ");

		static string RepoRoot()
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return dir.FullName;
		}

		static int Count(string text, string needle) => text.Split(new[] { needle }, StringSplitOptions.None).Length - 1;

		static string Between(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start + from.Length, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text[start..end];
		}

		static bool[] Bits(int n, int count) => Enumerable.Range(0, count).Select(i => (n & (1 << i)) != 0).ToArray();

		[Test]
		public void PreferredVisibleEnemyNeedsEveryConditionAndTheInSightTestFirst()
		{
			for (var n = 0; n < 64; n++)
			{
				var b = Bits(n, 6);
				var expected = b[0] && b[1] && !b[2] && !b[3] && !b[4] && !b[5];
				Assert.That(ObservedMine.PreferredVisibleEnemy(b[0], b[1], b[2], b[3], b[4], b[5]), Is.EqualTo(expected), "mask " + n);
			}

			Assert.That(ObservedMine.PreferredVisibleEnemy(false, true, false, false, false, false), Is.False, "an enemy we do not see is never a target");
		}

		[Test]
		public void ARememberedBuildingNeedsAValidShownUnhiddenEnemyBuildingThatIsNoHusk()
		{
			for (var n = 0; n < 128; n++)
			{
				var b = Bits(n, 7);
				var expected = b[0] && b[1] && !b[2] && b[3] && b[4] && !b[5] && !b[6];
				Assert.That(ObservedMine.RememberedCandidate(b[0], b[1], b[2], b[3], b[4], b[5], b[6]), Is.EqualTo(expected), "mask " + n);
			}

			Assert.That(ObservedMine.RememberedCandidate(true, true, false, true, true, false, true), Is.False, "a record wholly in shroud is no memory of the place");
		}

		[Test]
		public void ARememberedEnemyCountsNearbyOnlyWhenValidShownUnhiddenAndEnemyOwned()
		{
			for (var n = 0; n < 32; n++)
			{
				var b = Bits(n, 5);
				Assert.That(ObservedMine.RememberedEnemyNearby(b[0], b[1], b[2], b[3], b[4]), Is.EqualTo(b[0] && b[1] && !b[2] && b[3] && !b[4]), "mask " + n);
			}

			Assert.That(ObservedMine.RememberedEnemyNearby(true, true, false, true, true), Is.False, "a record wholly in shroud never counts as an enemy nearby");
		}

		[Test]
		public void AnAttackCountsWhenTheAttackerIsAnEnemyAndAnAttackerInSightAlsoPassesThePreferredTest()
		{
			for (var n = 0; n < 8; n++)
			{
				var b = Bits(n, 3);
				var expected = b[1] && (!b[0] || b[2]);
				Assert.That(ObservedMine.AttackCounts(b[0], b[1], b[2]), Is.EqualTo(expected), "mask " + n);
			}

			Assert.That(ObservedMine.AttackCounts(false, true, false), Is.True, "a hidden attacker is judged by its owner relationship only");
			Assert.That(ObservedMine.AttackCounts(true, true, false), Is.False, "an attacker in sight that fails the preferred test does not count");
			Assert.That(ObservedMine.AttackCounts(false, false, true), Is.False);
		}

		[Test]
		public void TheAttackersCellIsTheSiteOnlyWhenTheAttackerIsInSightAndTheVictimAsksForIt()
		{
			Assert.That(ObservedMine.UsesAttackerCell(true, true), Is.True);
			Assert.That(ObservedMine.UsesAttackerCell(true, false), Is.False);
			Assert.That(ObservedMine.UsesAttackerCell(false, true), Is.False, "a hidden attacker's place is never read");
			Assert.That(ObservedMine.UsesAttackerCell(false, false), Is.False);
		}

		[Test]
		public void StallAndParkArithmetic()
		{
			Assert.That(ObservedMine.Stalled(1749, 1000, 750), Is.False);
			Assert.That(ObservedMine.Stalled(1750, 1000, 750), Is.True);
			Assert.That(ObservedMine.ParkedActive(4999, 5000), Is.True);
			Assert.That(ObservedMine.ParkedActive(5000, 5000), Is.False);
			Assert.That(ObservedMine.ParkUntil(1000, 3000, 1, 2), Is.EqualTo(4000), "first stall: one park window");
			Assert.That(ObservedMine.ParkUntil(1000, 3000, 2, 3), Is.EqualTo(7000), "linear back-off");
			Assert.That(ObservedMine.ParkUntil(1000, 3000, 2, 2), Is.EqualTo(int.MaxValue), "at MaxStallAttempts the site is parked for the rest of the game");
		}

		[Test]
		public void EveryEnemyReadIsBehindAnObservation()
		{
			var src = Module();

			// the one world scan reads only what IsPreferredEnemyUnit lets through, and that test checks the sight first
			Assert.That(Count(src, "world.Actors)"), Is.EqualTo(1));
			Assert.That(Count(src, "world.Actors "), Is.EqualTo(0));
			Assert.That(src, Does.Contain("foreach (var a in world.Actors) if (IsPreferredEnemyUnit(a)) observedCells.Add(a.Location);"));
			var preferred = Between(src, "bool IsPreferredEnemyUnit(Actor actor)", "// Allies as in the engine");
			Assert.That(preferred.IndexOf("CanBeViewedByPlayer(player)", StringComparison.Ordinal), Is.GreaterThan(0));
			Assert.That(preferred.IndexOf("CanBeViewedByPlayer(player)", StringComparison.Ordinal), Is.LessThan(preferred.IndexOf("RelationshipWith(actor.Owner)", StringComparison.Ordinal)),
				"a hidden actor's owner, type, state and place are not read");
			Assert.That(preferred.IndexOf("CanBeViewedByPlayer(player)", StringComparison.Ordinal), Is.LessThan(preferred.IndexOf("IsDead", StringComparison.Ordinal)));
			Assert.That(preferred.IndexOf("CanBeViewedByPlayer(player)", StringComparison.Ordinal), Is.LessThan(preferred.IndexOf("GetEnabledTargetTypes", StringComparison.Ordinal)));

			// the nearby-enemy test: an enemy actor counts only when seen, plus the frozen records
			Assert.That(Count(src, "world.FindActorsInCircle"), Is.EqualTo(1));
			Assert.That(src, Does.Contain("else if (relationship == PlayerRelationship.Enemy && actor.CanBeViewedByPlayer(player))"));
			Assert.That(src, Does.Contain("frozenLayer.FrozenActorsInCircle(world, pos, dist)"));
			Assert.That(src, Does.Contain("ObservedMine.RememberedEnemyNearby(fa.IsValid, fa.Visible, fa.Hidden, enemyOwner, fa.Shrouded)"));

			// own minelayers only
			Assert.That(Count(src, "world.ActorsWithTrait<Minelayer>()"), Is.EqualTo(1));
			Assert.That(src, Does.Contain("a.Owner != player"));

			// remembered buildings come from the frozen layer's record, never from the live actor behind it
			Assert.That(src, Does.Contain("ObservedMine.RememberedCandidate(fa.IsValid, fa.Visible, fa.Hidden, enemyOwner, fa.Info.HasTraitInfo<BuildingInfo>(), fa.Info.HasTraitInfo<HuskInfo>(), fa.Shrouded)"));
			Assert.That(src, Does.Contain("world.Map.CellContaining(fa.CenterPosition)"));
			Assert.That(src, Does.Not.Contain("fa.Actor"));
			Assert.That(src, Does.Not.Contain("FrozenActor.Actor"));
		}

		[Test]
		public void AHiddenAttackerIsJudgedByItsOwnerOnlyAndItsLocationIsReadOnlyInSight()
		{
			var src = Module();
			Assert.That(src, Does.Not.Contain("e.Attacker.Location"));
			Assert.That(Count(src, "attacker.Location"), Is.EqualTo(1));
			Assert.That(src, Does.Contain("var pos = ObservedMine.UsesAttackerCell(attackerInSight, victimWantsEnemyLocation) ? attacker.Location : self.Location;"));
			Assert.That(src, Does.Contain("var attackerInSight = attacker.CanBeViewedByPlayer(player);"));
			Assert.That(src, Does.Contain("var preferredWhenSeen = attackerInSight && IsPreferredEnemyUnit(attacker);"));
			Assert.That(src, Does.Contain("if (!ObservedMine.AttackCounts(attackerInSight, attackerEnemy, preferredWhenSeen))"));
		}

		[Test]
		public void PathsAreTerrainOnlySoHiddenActorsCannotBendThem()
		{
			var src = Module();
			Assert.That(Count(src, "FindPathToTargetCell("), Is.EqualTo(2));
			Assert.That(Count(src, "BlockedByActor.None"), Is.EqualTo(2));
			foreach (var other in new[] { "BlockedByActor.All", "BlockedByActor.Immovable", "BlockedByActor.Stationary" })
				Assert.That(src, Does.Not.Contain(other), other);
		}

		[Test]
		public void TheRowGatesTheTickAndTheAttackResponseAndThereIsNoFallbackToTheEngineModule()
		{
			var src = Module();
			Assert.That(src, Does.Contain("bool FogMinelayerOn => Info.FogMinelayer && !ablateFogMinelayer.Off(player, AblationIds.FogMinelayer);"));
			Assert.That(src, Does.Contain("public readonly bool FogMinelayer = false;"));
			Assert.That(src, Does.Contain("void IBotTick.BotTick(IBot bot) { if (!FogMinelayerOn) return;"));
			Assert.That(src, Does.Contain("void IBotRespondToAttack.RespondToAttack(IBot bot, Actor self, AttackInfo e) { if (!FogMinelayerOn) return;"));
			Assert.That(src, Does.Not.Contain(": MinelayerBotModule"), "no inheritance from the engine module");
			Assert.That(src, Does.Not.Contain("new MinelayerBotModule"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("fog_minelayer"));
		}

		[Test]
		public void TimersRunOnWorldTicksAndTheStallRuleSendsOneStopAndParksTheSite()
		{
			var src = Module();
			Assert.That(src, Does.Contain("nextScanTick = now + Info.ScanTick;"));
			Assert.That(src, Does.Contain("if (now < nextScanTick)"));
			Assert.That(src, Does.Contain("bot.QueueOrder(new Order(\"Stop\", actor, false));"));
			Assert.That(Count(src, "new Order(\"Stop\""), Is.EqualTo(1));
			Assert.That(src, Does.Contain("ObservedMine.Stalled(now, miner.LastProgressTick, Info.StallTicks)"));
			Assert.That(src, Does.Contain("ObservedMine.ParkUntil(now, Info.ParkTicks, failures, Info.MaxStallAttempts)"));
			Assert.That(src, Does.Contain("public readonly int StallTicks = 750;"));
			Assert.That(src, Does.Contain("public readonly int StallCheckTicks = 50;"));
			Assert.That(src, Does.Contain("public readonly int ParkTicks = 3000;"));
			Assert.That(src, Does.Contain("public readonly int MaxStallAttempts = 2;"));
		}

		[Test]
		public void OrdersAreTheEnginesOwnMinefieldOrders()
		{
			var src = Module();
			Assert.That(src, Does.Contain("\"PlaceMinefield\","));
			Assert.That(src, Does.Contain("{ ExtraLocation = minelayingPosition - vec });"));
			Assert.That(src, Does.Contain("Target.FromCell(world, minelayingPosition + vec)"));
		}

		[Test]
		public void TheEngineBlockRunsForClassicbotOnlyAndTheGenericBlockCarriesTheRow()
		{
			var yaml = Source("packaging/ai.yaml");
			Assert.That(yaml, Does.Contain("\tMinelayerBotModule:\n\t\tRequiresCondition: classicbot\n"));
			Assert.That(yaml, Does.Not.Contain("\tMinelayerBotModule:\n\t\tRequiresCondition: genericbot || classicbot"));
			Assert.That(yaml, Does.Contain("\tObservedMinelayerBotModule:\n\t\tRequiresCondition: genericbot\n\t\tFogMinelayer: true\n"));
			Assert.That(Count(yaml, "\tObservedMinelayerBotModule:"), Is.EqualTo(1));

			// the same config keys as the engine block, so classic and generic place the same fields when they place them
			foreach (var key in new[] { "MinelayingActorTypes: ra1_allies_minelayer", "IgnoredEnemyTargetTypes: Structure, Defense, Air, Obstacle", "UseEnemyLocationTargetTypes: Structure, Defense",
				"AwayFromAlliedTargetTypes: Structure, Defense", "AwayFromEnemyTargetTypes: Structure, Defense" })
			{
				var engine = Between(yaml, "\tMinelayerBotModule:\n", "\tObservedMinelayerBotModule:\n");
				var observed = yaml[yaml.IndexOf("\tObservedMinelayerBotModule:\n", StringComparison.Ordinal)..];
				observed = observed[..Math.Min(observed.Length, 900)];
				Assert.That(engine, Does.Contain(key), "engine block " + key);
				Assert.That(observed, Does.Contain(key), "observed block " + key);
			}
		}

		[Test]
		public void TheModuleIsInTheFogHonestyManifestWithItsThreeReviewedSites()
		{
			var manifest = File.ReadAllText(Path.Combine(RepoRoot(), "tools", "audit", "fog_honesty_manifest.json"));
			Assert.That(manifest, Does.Contain("\"OpenRA.Mods.Cameo/Traits/BotModules/ObservedMinelayerBotModule.cs\": 3,"));
		}
	}
}
