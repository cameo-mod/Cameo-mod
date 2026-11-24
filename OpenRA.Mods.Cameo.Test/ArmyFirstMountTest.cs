#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
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
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OpenRA.Mods.Cameo.Test
{
	// DEFENCE-ZERO-FIX (Opus ruling 2026-11-22, user): the PROD-IDLE activator (364f31ca5) mounted ArmyFirstBotModule on all 11 tier bots,
	// which gave every tier a building pause vote that held every defence, radar and tech building (DEFENCE_ZERO_RCA_01B). The fix
	// restores the 837a13915 gate: `genericbot && army_first`, army_first granted to fransbot only. The PR-3 / reserve code stays,
	// dormant. Army-first for tiers returns only as a redesign (a unit-side push, never a building veto).
	[TestFixture]
	public class ArmyFirstMountTest
	{
		static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
			"tools", "tests", "fixtures", name)).Replace("\r\n", "\n");

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string WithoutDevRegions(string text) => Regex.Replace(text, @"#if CAMEO_DEV_LOGGING.*?#endif", string.Empty, RegexOptions.Singleline);

		static List<MiniYamlNode> TopLevel() => MiniYaml.FromString(Fixture("mcv_deploy_ai.yaml"), "ai").ToList();

		// Every GrantConditionOnBotOwner@x of ^AIDifficulties as (condition, bots).
		static List<(string Condition, string[] Bots)> Grants() =>
			TopLevel().Single(n => n.Key == "^AIDifficulties").Value.Nodes
				.Where(n => n.Key.StartsWith("GrantConditionOnBotOwner@", StringComparison.Ordinal))
				.Select(n => (
					(n.Value.Nodes.SingleOrDefault(c => c.Key == "Condition")?.Value.Value ?? string.Empty).Trim(),
					(n.Value.Nodes.SingleOrDefault(c => c.Key == "Bots")?.Value.Value ?? string.Empty)
						.Split(',').Select(b => b.Trim()).Where(b => b.Length > 0).ToArray()))
				.Where(g => g.Item1.Length > 0)
				.ToList();

		static HashSet<string> ConditionsOf(string bot) =>
			Grants().Where(g => g.Bots.Contains(bot)).Select(g => g.Condition).ToHashSet();

		// The 17 bot types the INC-3 grant lists: the 11 tiers and the six exploit personalities.
		static string[] IncThreeTypes() => Grants().Single(g => g.Condition == "inc3_frans_services").Bots;

		static MiniYamlNode ArmyFirstModule() =>
			TopLevel().Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == "ArmyFirstBotModule");

		[Test]
		public void TheArmyFirstModuleIsGatedOnArmyFirstAgain()
		{
			var module = ArmyFirstModule();
			Assert.That(module.Value.Nodes.Single(n => n.Key == "RequiresCondition").Value.Value.Trim(), Is.EqualTo("genericbot && army_first"));

			// The 837a13915 block carried no field override: the flat-vote values G1 wrote (0/0/0) are gone with the mount.
			Assert.That(module.Value.Nodes.Select(n => n.Key), Is.EquivalentTo(new[] { "RequiresCondition" }));
		}

		[Test]
		public void ArmyFirstIsGrantedToFransbotOnly()
		{
			var grants = Grants().Where(g => g.Condition == "army_first").ToList();

			Assert.That(grants, Has.Count.EqualTo(1));
			Assert.That(grants[0].Bots, Is.EqualTo(new[] { "fransbot" }));
			Assert.That(ConditionsOf("fransbot"), Does.Contain("army_first"), "fransbot still has the grant");
		}

		[Test]
		public void NoTierBotTypeCanMountTheModule()
		{
			var types = IncThreeTypes();
			Assert.That(types, Has.Length.EqualTo(17));
			Assert.That(types, Is.SupersetOf(new[]
			{
				"noob", "easiest", "veryeasy", "easy", "medium", "hard", "veryhard", "brutal", "challenger", "unbeatable", "cameogod",
				"exploit_rush", "exploit_turtle", "exploit_guerrilla", "exploit_expansion", "exploit_steamroller", "exploit_tech",
			}));

			foreach (var type in types)
			{
				var conditions = ConditionsOf(type);
				Assert.That(conditions, Does.Not.Contain("army_first"), type + " carries no army_first grant");
				Assert.That(conditions.Contains("genericbot") && conditions.Contains("army_first"), Is.False,
					type + " does not satisfy `genericbot && army_first`");
			}
		}

		[Test]
		public void TheParallelProductionGrantStaysRemoved()
		{
			Assert.That(Grants().Select(g => g.Condition), Does.Not.Contain("parallel_production"));
			Assert.That(TopLevel().Single(n => n.Key == "Player").Value.Nodes.Select(n => n.Key),
				Has.None.StartsWith("ParallelProductionBotModule"), "the removed module is not mounted again");
		}

		[Test]
		public void ThePr3AndReserveCodeStaysInTheModuleDormant()
		{
			var module = Source("mods/Cameo/Traits/BotModules/ArmyFirstBotModule.cs");
			Assert.That(module, Does.Contain("public static bool ShareHolds("));
			Assert.That(module, Does.Contain("public static bool ReserveHolds("));
			Assert.That(module, Does.Contain("ArmyFirstEval.ShareHolds(essential, admission, share, cost, ShareTarget)"));
			Assert.That(module, Does.Contain("ArmyFirstEval.ReserveHolds(essential, admission"));
			Assert.That(Source("mods/CA/Traits/BotModules/BotModuleLogic/ArmyShareCA.cs"), Does.Contain("public bool BuildingAdmits("));
		}

		[Test]
		public void TheShareHoldCounterExistsOnlyInTheDevBuild()
		{
			var module = Source("mods/Cameo/Traits/BotModules/ArmyFirstBotModule.cs");

			Assert.That(module, Does.Contain("SHARE-HOLD share_hold class="));
			Assert.That(WithoutDevRegions(module), Does.Not.Contain("Log.Write"));
			Assert.That(WithoutDevRegions(module), Does.Not.Contain("shareHolds"));
			Assert.That(WithoutDevRegions(module), Does.Not.Contain("CountShareHold"), "the call site is inside the DEV region too");
		}
	}
}
