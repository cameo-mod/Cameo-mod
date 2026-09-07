#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class MasterAiBotModuleTest
	{
		static BotSituation Situation(BotUrgency urgency = BotUrgency.Normal, string personality = "",
			OpenRA.Player target = null, IReadOnlyDictionary<OpenRA.Player, EnemyProfile> enemies = null)
		{
			return new BotSituation
			{
				Tick = 1500,
				Urgency = urgency,
				Personality = personality,
				MainTarget = target,
				Enemies = enemies ?? new Dictionary<OpenRA.Player, EnemyProfile>(),
				Demand = new CounterDemand()
			};
		}

		[Test]
		public void SituationEmitterProducesParseableEmptyNullTargetRecord()
		{
			var b = new StringBuilder();
			AiSituationLogWriter.AppendSituation(b, "game", "", "map", "Multi0", "td_gdi", "medium", "rush",
				Situation(), 5400, 1200, 14, 4, 900, 1500);
			using var doc = JsonDocument.Parse(b.ToString());
			Assert.That(doc.RootElement.GetProperty("kind").GetString(), Is.EqualTo("situation"));
			Assert.That(doc.RootElement.GetProperty("main_target").GetString(), Is.Empty);
			Assert.That(doc.RootElement.GetProperty("enemies").GetArrayLength(), Is.Zero);
			Assert.That(doc.RootElement.EnumerateObject().Select(p => p.Name), Is.EqualTo(new[]
			{
				"schema", "kind", "record_id", "game_uid", "map_uid", "player", "faction", "bot_type",
				"tick", "urgency", "personality_current", "personality_candidate", "main_target",
				"main_target_score", "hints", "demand", "own", "enemies"
			}));
		}

		[Test]
		public void SituationEmitterProducesParseablePopulatedRecord()
		{
			var enemy = new EnemyProfile
			{
				Player = (OpenRA.Player)RuntimeHelpers.GetUninitializedObject(typeof(OpenRA.Player)),
				Alive = true,
				ArmyValue = 8100,
				InfantryValue = 2000,
				VehicleValue = 5000,
				AirValue = 1100,
				DefenceCount = 7,
				DefenceValue = 3500,
				TechBuildings = 4,
				ProductionBuildings = 3,
				ExpansionClusters = 2,
				Harvesters = 5,
				Refineries = 2,
				PressureValue = 800,
				StealthShare = 10,
				NearestCells = 42,
				LastSeenTick = 1500,
				Score = 730
			};
			var b = new StringBuilder();
			var situation = Situation(BotUrgency.Pressured, "steamroller", null,
				new Dictionary<OpenRA.Player, EnemyProfile> { [enemy.Player] = enemy });
			AiSituationLogWriter.AppendSituation(b, "game", "", "map", "Multi0", "td_gdi", "medium", "rush",
				situation, 5400, 1200, 14, 4, 900, 1500);
			using var doc = JsonDocument.Parse(b.ToString());
			var enemyJson = doc.RootElement.GetProperty("enemies")[0];
			Assert.That(enemyJson.GetProperty("army_value").GetInt32(), Is.EqualTo(8100));
			Assert.That(doc.RootElement.GetProperty("urgency").GetString(), Is.EqualTo("pressured"));
			Assert.That(doc.RootElement.GetProperty("personality_candidate").GetString(), Is.EqualTo("steamroller"));
		}

		[Test]
		public void SaturationIsMonotonicBoundedAndHandlesZeroK()
		{
			Assert.That(MasterAiBotModule.Saturate(0, 0), Is.Zero);
			Assert.That(MasterAiBotModule.Saturate(10, 20), Is.LessThanOrEqualTo(MasterAiBotModule.Saturate(20, 20)));
			Assert.That(MasterAiBotModule.Saturate(int.MaxValue, 0), Is.EqualTo(100));
			Assert.That(MasterAiBotModule.Saturate(int.MaxValue, int.MaxValue), Is.InRange(0, 100));
		}

		[Test]
		public void TargetScoreStaysBoundedForExtremeProfiles()
		{
			var info = new MasterAiBotModuleInfo();
			foreach (var profile in new[]
			{
				new EnemyProfile { ArmyValue = 0, DefenceCount = 0, NearestCells = -1 },
				new EnemyProfile { ArmyValue = int.MaxValue, DefenceValue = int.MaxValue, DefenceCount = int.MaxValue, NearestCells = int.MaxValue }
			})
				Assert.That(MasterAiBotModule.TargetScore(profile, int.MaxValue, int.MaxValue, info), Is.InRange(0, 1000));
			Assert.That(MasterAiBotModule.Momentum(new EnemyProfile { Score = 1000 }, info.IncumbentMomentum),
				Is.EqualTo(1000));
		}

		[Test]
		public void CandidatePersonalityRulesAreOrdered()
		{
			var info = new MasterAiBotModuleInfo();
			var target = new EnemyProfile { Alive = true, NearestCells = 10, ArmyValue = 1000 };
			Assert.That(MasterAiBotModule.CandidatePersonality(BotUrgency.Emergency, target, 0, new[] { target }, "", info), Is.EqualTo("turtle"));
			target.DefenceCount = 6;
			Assert.That(MasterAiBotModule.CandidatePersonality(BotUrgency.Normal, target, 3000, new[] { target }, "", info), Is.EqualTo("steamroller"));
			target.DefenceCount = 0;
			target.ExpansionClusters = 3;
			Assert.That(MasterAiBotModule.CandidatePersonality(BotUrgency.Normal, target, 0, new[] { target }, "", info), Is.EqualTo("guerrilla"));
			target.ExpansionClusters = 0;
			target.TechBuildings = 4;
			Assert.That(MasterAiBotModule.CandidatePersonality(BotUrgency.Normal, target, 0, new[] { target }, "", info), Is.EqualTo("tech"));
			target.TechBuildings = 0;
			target.ArmyValue = 1500;
			target.DefenceCount = 2;
			Assert.That(MasterAiBotModule.CandidatePersonality(BotUrgency.Normal, target, 0, new[] { target }, "", info), Is.EqualTo("rush"));
			target.ArmyValue = 5000;
			target.DefenceCount = 5;
			target.NearestCells = -1;
			Assert.That(MasterAiBotModule.CandidatePersonality(BotUrgency.Normal, target, 0, new[] { target }, "", info), Is.EqualTo("expansion"));
			target.NearestCells = 10;
			Assert.That(MasterAiBotModule.CandidatePersonality(BotUrgency.Normal, target, 0, new[] { target }, "turtle", info), Is.EqualTo("turtle"));
		}
	}
}
