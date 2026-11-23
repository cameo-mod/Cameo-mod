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
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	// PERF-F1 (PERF_SCAN_BURST_01 section 5): the squad manager builds its enemy candidate lists once per bot tick. There is no
	// headless world fixture, so these pins hold what the identity argument rests on: the shared lists are never modified in place,
	// every world scan goes through the helpers, the list lives for exactly one IBotTick call, and Classic keeps the old path.
	// The behavioural proof is a same-seed order-hash compare against the parent commit (owed, DEV run).
	[TestFixture]
	public sealed class PerfF1EnemyScanTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string Squad() => Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.cs");

		static string Scan() => Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.EnemyScan.cs");

		static string Section(string text, string from, string to)
		{
			var a = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(a, Is.GreaterThanOrEqualTo(0), from);
			var b = text.IndexOf(to, a + from.Length, StringComparison.Ordinal);
			Assert.That(b, Is.GreaterThan(a), to);
			return text.Substring(a, b - a);
		}

		[Test]
		public void TheFiveWorldScanningFindersNoLongerScanTheWorldThemselves()
		{
			var squad = Squad();
			var finders = Section(squad, "internal Actor FindClosestEnemy(Actor sourceActor, SquadCA owner = null)", "internal int SquadValueOf(");
			Assert.That(finders, Does.Not.Contain("World.Actors"));
			Assert.That(finders, Does.Contain("EnemyCandidates(mainTarget)"));
			Assert.That(finders, Does.Contain("EnemyUnitsOf(targetPlayer)"));
			Assert.That(finders, Does.Contain("HighValueCandidates(EffectiveMainTarget())"));
		}

		[Test]
		public void NoFinderModifiesAListInPlace()
		{
			var finders = Section(Squad(), "internal Actor FindClosestEnemy(Actor sourceActor, SquadCA owner = null)", "internal int SquadValueOf(");

			// The location overload owns a fresh list (EnemyUnitsOf returns a new one) and keeps its RemoveAll; the two risk overloads
			// that can see the shared list use WithoutRiskyTargets, which builds a new list.
			var locationOverload = Section(finders, "internal Actor FindClosestEnemy(CPos location", "internal Actor FindHighValueTarget(WPos pos)");
			var others = finders.Replace(locationOverload, string.Empty);
			Assert.That(others, Does.Not.Contain("RemoveAll"));
			Assert.That(others.Split("WithoutRiskyTargets(units, attackerValue)").Length - 1, Is.EqualTo(2));
		}

		[Test]
		public void TheRiskGateHelperBuildsANewListAndNeverRemovesFromTheInput()
		{
			var helper = Section(Scan(), "List<Actor> WithoutRiskyTargets(", "#if CAMEO_DEV_LOGGING\n\t\tvoid VerifyEnemyScan");
			Assert.That(helper, Does.Contain("var kept = new List<Actor>(units.Count);"));
			Assert.That(helper, Does.Contain("if (PassesRiskGate(u.Location, attackerValue))"));
			Assert.That(helper, Does.Not.Contain("RemoveAll"));
			Assert.That(helper, Does.Not.Contain("units.Remove"));
		}

		[Test]
		public void TheSharedListsLiveForExactlyOneBotTick()
		{
			var squad = Squad();
			var tick = Section(squad, "void IBotTick.BotTick(IBot bot)", "void BotTickBody(IBot bot)");
			Assert.That(tick, Does.Contain("BeginEnemyScanPass();"));
			Assert.That(tick, Does.Contain("BotTickBody(bot);"));
			Assert.That(tick, Does.Contain("finally"));
			Assert.That(tick, Does.Contain("EndEnemyScanPass();"));
			Assert.That(Section(Scan(), "void EndEnemyScanPass()", "// The old sequence"), Does.Contain("enemyScan = null;"));
		}

		[Test]
		public void TheBaseListIsTheOldWorldFilterInWorldOrder()
		{
			var scan = Scan();
			Assert.That(scan, Does.Contain("World.Actors.Where(IsPreferredEnemyUnit).ToList()"));
			Assert.That(scan, Does.Contain("World.Actors.Where(IsHighValueTarget).ToList()"));
			Assert.That(scan, Does.Contain("units = FilterHiddenUnderFog(units, FoggedScans, IsNotHiddenUnit);"));
			Assert.That(scan, Does.Contain("PreferOwned(units, mainTarget == null ? null : a => a.Owner == mainTarget)"));
		}

		[Test]
		public void TheSwitchIsOnByDefaultAndAblatable()
		{
			Assert.That(Scan(), Does.Contain("public readonly bool UseSharedEnemyScan = true;"));
			Assert.That(Scan(), Does.Contain("Info.UseSharedEnemyScan && !ablateEnemyScan.Off(Player, AblationIds.EnemyScan)"));
			Assert.That(AblationIds.EnemyScan, Is.EqualTo("pf_enemy_scan"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("pf_enemy_scan"));
		}

		[Test]
		public void ClassicKeepsThePerCallScans()
		{
			var yaml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", "mcv_deploy_ai.yaml")).Replace("\r\n", "\n");
			var classic = Section(yaml, "SquadManagerBotModuleCA@classic:", "RequiresCondition: classicbot");
			Assert.That(classic, Does.Contain("UseSharedEnemyScan: false"));
			Assert.That(yaml.Split("UseSharedEnemyScan").Length - 1, Is.EqualTo(1), "only the Classic node mentions the switch; every other tier takes the default (on)");
		}
	}
}
