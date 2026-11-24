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
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	// STALEMATE-01 (USER BINDING 11-23: no stalemates): late escalation and the finish-mode hunt; pure decisions, yaml arms, rows.
	[TestFixture]
	public class Stalemate01Test
	{
		static readonly string[] Personalities = { "rush", "turtle", "tech", "expansion", "steamroller", "guerrilla" };

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static MiniYamlNode PlayerNode(string key) => MiniYaml.FromString(Source("packaging/ai.yaml"), "ai")
			.Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == key);

		static string FieldOrNull(MiniYamlNode node, string key) => node.Value.Nodes.SingleOrDefault(n => n.Key == key)?.Value.Value.Trim();

		[Test]
		public void LateArmAndScale()
		{
			Assert.That(Stalemate01CA.Late(true, 24999, 25000), Is.False);
			Assert.That(Stalemate01CA.Late(true, 25000, 25000), Is.True);
			Assert.That(Stalemate01CA.Late(false, 60000, 25000), Is.False);
			Assert.That(Stalemate01CA.Scale(2500, 50), Is.EqualTo(1250), "turtle delay reset halves");
			Assert.That(Stalemate01CA.Scale(44000, 60), Is.EqualTo(26400), "turtle late value bar");
			Assert.That(Stalemate01CA.Scale(1, 10), Is.EqualTo(1), "never below 1");
			Assert.That(Stalemate01CA.Scale(0, 50), Is.EqualTo(0), "no bar stays no bar");
			Assert.That(Stalemate01CA.Scale(100, 150), Is.EqualTo(100), "percent clamps to 100");
		}

		[Test]
		public void HuntGridAndOrder()
		{
			var grid = Stalemate01CA.Grid(0, 0, 64, 32, 16);
			Assert.That(grid, Is.EqualTo(new[] { (8, 8), (24, 8), (40, 8), (56, 8), (8, 24), (24, 24), (40, 24), (56, 24) }));
			Assert.That(Stalemate01CA.Grid(0, 0, 64, 32, 0), Is.Empty);

			var order = Stalemate01CA.HuntOrder(new[]
			{
				(false, 500, 10L), (true, -1, 1L), (false, -1, 900L), (false, -1, 100L), (false, 200, 1L),
			}).ToArray();
			Assert.That(order, Is.EqualTo(new[] { 3, 2, 4, 0 }), "never-assigned nearest first; visible cells never");

			// Frontrunner FIX: a cell marked after a failed reachability check (tick 900) goes behind every never-tried cell.
			var afterFail = Stalemate01CA.HuntOrder(new[] { (false, 900, 1L), (false, -1, 50L), (false, -1, 60L) }).ToArray();
			Assert.That(afterFail, Is.EqualTo(new[] { 1, 2, 0 }));

			Assert.That(Stalemate01CA.Hunts(SquadCAType.Rush), Is.True);
			Assert.That(Stalemate01CA.Hunts(SquadCAType.Guerrilla), Is.True);
			Assert.That(Stalemate01CA.Hunts(SquadCAType.Protection), Is.False, "base defenders never leave home to hunt");
			Assert.That(Stalemate01CA.Hunts(SquadCAType.Harass), Is.False);

			Assert.That(Stalemate01CA.Arrived(16, 4), Is.True);
			Assert.That(Stalemate01CA.Arrived(17, 4), Is.False);
		}

		[Test]
		public void ArmedOnEveryPersonalityNotClassic()
		{
			foreach (var p in Personalities)
			{
				var node = PlayerNode("SquadManagerBotModuleCA@" + p);
				Assert.That(FieldOrNull(node, "RequiresCondition"), Is.EqualTo("genericbot && personality-" + p));
				Assert.That(FieldOrNull(node, "LateEscalationEnabled"), Is.EqualTo("true"), p);
				Assert.That(FieldOrNull(node, "FinishModeEnabled"), Is.EqualTo("true"), p);
			}

			var classic = PlayerNode("SquadManagerBotModuleCA@classic");
			Assert.That(FieldOrNull(classic, "LateEscalationEnabled"), Is.Null);
			Assert.That(FieldOrNull(classic, "FinishModeEnabled"), Is.Null);
		}

		[Test]
		public void RowsAndWiring()
		{
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("st_late_escalation"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("st_finish_mode"));

			var logic = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.Stalemate01.cs");
			Assert.That(logic, Does.Contain("!ablateLateEscalation.Off(Player, AblationIds.StalemateLateEscalation)"));
			Assert.That(logic, Does.Contain("!ablateFinishMode.Off(Player, AblationIds.StalemateFinishMode)"));
			Assert.That(logic, Does.Contain("Player.Shroud.IsVisible(cell)"), "fog-honest: only the bot's own vision");
			Assert.That(logic, Does.Contain("huntRetryAt = World.WorldTick + Stalemate01CA.HuntRetryTicks;"), "a failed pick cools down");
			Assert.That(logic.Split("huntAssigned[cell] = World.WorldTick;").Length - 1, Is.EqualTo(2), "a picked and an unreachable cell are both marked");

			var squad = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.cs");
			Assert.That(squad, Does.Contain("minAttackForceDelayTicks = LateDelay("));
			Assert.That(squad, Does.Contain("requiredValue = LateBar(requiredValue);"));
			Assert.That(squad, Does.Contain("requiredSize = LateBar(requiredSize);"));

			var ground = Source("mods/CA/Traits/BotModules/Squads/States/GroundStatesCA.cs");
			Assert.That(ground, Does.Contain("owner.SquadManager.FinishHuntCell(owner.Units.First().Actor)"));
			Assert.That(ground, Does.Contain("owner.SquadManager.FinishModeActive && Stalemate01CA.Hunts(owner.Type)"));
			Assert.That(ground, Does.Contain("owner.Target.Type == TargetType.FrozenActor || owner.IsHunting"));
		}
	}
}
