#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class MasterAiBotModuleTest
	{
		sealed class EnemyProfiles : IReadOnlyDictionary<OpenRA.Player, EnemyProfile>
		{
			readonly EnemyProfile profile;

			public EnemyProfiles(EnemyProfile profile) { this.profile = profile; }
			public IEnumerable<OpenRA.Player> Keys { get { yield return null; } }
			public IEnumerable<EnemyProfile> Values { get { yield return profile; } }
			public int Count => 1;
			public EnemyProfile this[OpenRA.Player key] => profile;
			public bool ContainsKey(OpenRA.Player key) => true;
			public bool TryGetValue(OpenRA.Player key, out EnemyProfile value)
			{
				value = profile;
				return true;
			}

			public IEnumerator<KeyValuePair<OpenRA.Player, EnemyProfile>> GetEnumerator()
			{
				yield return new KeyValuePair<OpenRA.Player, EnemyProfile>(null, profile);
			}

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}

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
				Situation());
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
				Name = "Multi1",
				FactionName = "td_nod",
				Alive = true,
				ArmyValue = 8100,
				InfantryValue = 2000,
				VehicleValue = 5000,
				AirValue = 1100,
				DefenceCount = 7,
				DefenceValue = 3500,
				TechBuildings = 4,
				ProductionBuildings = 3,
				BuildingCount = 9,
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
				new EnemyProfiles(enemy));
			AiSituationLogWriter.AppendSituation(b, "game", "", "map", "Multi0", "td_gdi", "medium", "rush",
				situation);
			using var doc = JsonDocument.Parse(b.ToString());
			var enemyJson = doc.RootElement.GetProperty("enemies")[0];
			Assert.That(enemyJson.GetProperty("name").GetString(), Is.EqualTo("Multi1"));
			Assert.That(enemyJson.GetProperty("faction").GetString(), Is.EqualTo("td_nod"));
			Assert.That(enemyJson.GetProperty("army_value").GetInt32(), Is.EqualTo(8100));
			Assert.That(enemyJson.GetProperty("buildings").GetInt32(), Is.EqualTo(9));
			Assert.That(doc.RootElement.GetProperty("urgency").GetString(), Is.EqualTo("pressured"));
			Assert.That(doc.RootElement.GetProperty("personality_candidate").GetString(), Is.EqualTo("steamroller"));
			Assert.That(enemyJson.EnumerateObject().Select(p => p.Name), Is.EqualTo(new[]
			{
				"name", "faction", "alive", "army_value", "infantry_value", "vehicle_value", "air_value",
				"naval_value", "defence_count", "defence_value", "tech_buildings", "production_buildings",
				"buildings", "expansion_clusters", "harvesters", "refineries", "pressure_value",
				"stealth_share", "nearest_cells", "last_seen_tick", "score"
			}));
		}

		[TestCase(false, false, true, true)]
		[TestCase(true, false, true, false)]
		[TestCase(false, true, true, false)]
		[TestCase(false, false, false, false)]
		public void SituationLogEligibilityExcludesReplaySaveAndNonHost(bool replay, bool save, bool host, bool expected)
		{
			Assert.That(AiSituationLogWriter.Eligible(WorldType.Regular, replay, save, host), Is.EqualTo(expected));
			Assert.That(AiSituationLogWriter.Eligible(WorldType.Shellmap, replay, save, host), Is.False);
			Assert.That(AiSituationLogWriter.Eligible(WorldType.Editor, replay, save, host), Is.False);
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
				Assert.That(MasterAiBotModule.TargetScore(profile, int.MaxValue, info), Is.InRange(0, 1000));
			Assert.That(MasterAiBotModule.Momentum(new EnemyProfile { Score = 1000 }, info.IncumbentMomentum),
				Is.EqualTo(1000));
		}

		[Test]
		public void TargetChoiceHoldsIncumbentWithinMinimumHold()
		{
			var info = new MasterAiBotModuleInfo();
			var incumbent = new EnemyProfile { Name = "Multi0", Score = 100, NearestCells = 10 };
			var better = new EnemyProfile { Name = "Multi1", Score = 900, NearestCells = 10 };
			Assert.That(MasterAiBotModule.ChooseTarget(new[] { incumbent, better }, incumbent, 1000, 1000 + info.MinimumHoldTicks - 1, info),
				Is.SameAs(incumbent));
		}

		[Test]
		public void TargetChoiceSelectsBetterCandidateAfterHold()
		{
			var info = new MasterAiBotModuleInfo();
			var incumbent = new EnemyProfile { Name = "Multi0", Score = 100, NearestCells = 10 };
			var better = new EnemyProfile { Name = "Multi1", Score = 900, NearestCells = 10 };
			Assert.That(MasterAiBotModule.ChooseTarget(new[] { incumbent, better }, incumbent, 1000, 1000 + info.MinimumHoldTicks, info),
				Is.SameAs(better));
		}

		[Test]
		public void TargetChoiceSwitchesWhenIncumbentIsInvalid()
		{
			var info = new MasterAiBotModuleInfo();
			var incumbent = new EnemyProfile { Name = "Multi0", Score = 100, NearestCells = -1 };
			var better = new EnemyProfile { Name = "Multi1", Score = 900, NearestCells = 10 };
			Assert.That(MasterAiBotModule.ChooseTarget(new[] { better }, incumbent, 1000, 1000 + 1, info),
				Is.SameAs(better));
		}

		[Test]
		public void TargetChoiceDoesNotExtendHoldWhenChoiceIsUnchanged()
		{
			var info = new MasterAiBotModuleInfo();
			var incumbent = new EnemyProfile { Name = "Multi0", Score = 900, NearestCells = 10 };
			var better = new EnemyProfile { Name = "Multi1", Score = 100, NearestCells = 10 };
			var chosen = MasterAiBotModule.ChooseTarget(new[] { incumbent, better }, incumbent, 1000, 1000 + info.MinimumHoldTicks, info);
			Assert.That(chosen, Is.SameAs(incumbent));
			Assert.That(MasterAiBotModule.ChooseTarget(new[] { incumbent, better }, chosen, 1000,
				1000 + info.MinimumHoldTicks + 1, info), Is.SameAs(incumbent));
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

		[Test]
		public void PersonalityControllerMapsKnownConditionsAndIgnoresUnknownNames()
		{
			var info = new BotPersonalityControllerInfo();
			Assert.That(BotPersonalityController.PersonalityName("personality-rush", info.PersonalityPrefix), Is.EqualTo("rush"));
			Assert.That(info.Conditions.Any(c => BotPersonalityController.PersonalityName(c, info.PersonalityPrefix) == "steamroller"), Is.True);
			Assert.That(info.Conditions.Any(c => BotPersonalityController.PersonalityName(c, info.PersonalityPrefix) == "guerrilla"), Is.False);
		}

		[TestCase("rush", "turtle", 1000, 1000 + 2999, false, true, false)]
		[TestCase("rush", "turtle", 1000, 1000 + 3000, false, true, true)]
		[TestCase("rush", "turtle", 1000, 1000 + 1, true, true, true)]
		[TestCase("rush", "turtle", 1000, 1000 + 3000, false, false, false)]
		public void PersonalitySwitchPolicyRespectsHoldAndDifficulty(string current, string candidate, int lastSwitchTick,
			int tick, bool emergencyTransition, bool allowSwitching, bool expected)
		{
			Assert.That(MasterAiBotModule.ShouldSwitchPersonality(current, candidate, lastSwitchTick, tick,
				emergencyTransition, allowSwitching, new MasterAiBotModuleInfo()), Is.EqualTo(expected));
		}

		[Test]
		public void RelativeInitialAttackDelayPreservesStartAndRemovesElapsedDelay()
		{
			Assert.That(SquadManagerBotModuleCA.RemainingInitialAttackDelay(12000, 0), Is.EqualTo(12000));
			Assert.That(SquadManagerBotModuleCA.RemainingInitialAttackDelay(12000, 12001), Is.Zero);
		}
	}
}
