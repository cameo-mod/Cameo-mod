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
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class MasterAiEvalTest
	{
		static OpenRA.Player Player() =>
			(OpenRA.Player)RuntimeHelpers.GetUninitializedObject(typeof(OpenRA.Player));

		[TestCase(BotUrgency.Emergency, false, 0, 0, BotUrgency.Emergency)]
		[TestCase(BotUrgency.Emergency, true, 9999, 0, BotUrgency.Emergency)]
		[TestCase(BotUrgency.Pressured, true, 0, 0, BotUrgency.Pressured)]
		[TestCase(BotUrgency.Normal, true, 0, 0, BotUrgency.Pressured)]
		[TestCase(BotUrgency.Normal, false, 1000, 500, BotUrgency.Pressured)]
		[TestCase(BotUrgency.Normal, false, 1000, 700, BotUrgency.Normal)]
		[TestCase(BotUrgency.Normal, false, 0, 0, BotUrgency.Normal)]
		[TestCase(BotUrgency.Pressured, false, 0, 0, BotUrgency.Normal)]
		public void ClassifyUrgencyCoversLatchPressureAndRatio(
			BotUrgency current, bool anyPressure, int enemyArmy, int ownArmy, BotUrgency expected)
		{
			// PressuredArmyRatio=60: own*100 < enemy*60 -> own < 60% of enemy.
			Assert.That(MasterAiEval.ClassifyUrgency(current, anyPressure, enemyArmy, ownArmy, 60),
				Is.EqualTo(expected));
		}

		[Test]
		public void CoalitionBiasTargetRequiresFlagTargetAndCandidate()
		{
			var mt = Player();
			var candidate = new EnemyProfile { Player = mt, Alive = true, NearestCells = 3 };
			var other = new EnemyProfile { Alive = true, NearestCells = 3 };

			Assert.That(MasterAiEval.CoalitionBiasTarget(false, mt, new[] { candidate }), Is.Null);
			Assert.That(MasterAiEval.CoalitionBiasTarget(true, null, new[] { candidate }), Is.Null);
			Assert.That(MasterAiEval.CoalitionBiasTarget(true, mt, new[] { other }), Is.Null);
			Assert.That(MasterAiEval.CoalitionBiasTarget(true, mt, new[] { other, candidate }),
				Is.SameAs(candidate));
		}

		[Test]
		public void NemesisOverrideNeedsScoreWeightAndFreshProfile()
		{
			var nemesis = Player();
			var incumbent = Player();
			var profile = new EnemyProfile { Player = nemesis, Alive = true, NearestCells = 5 };
			var dead = new EnemyProfile { Player = nemesis, Alive = false, NearestCells = 5 };
			var unreached = new EnemyProfile { Player = nemesis, Alive = true, NearestCells = -1 };

			Assert.That(MasterAiEval.NemesisOverrideTarget(null, 100f, 60, incumbent, new[] { profile }), Is.Null);
			Assert.That(MasterAiEval.NemesisOverrideTarget(nemesis, 100f, 60, nemesis, new[] { profile }), Is.Null);
			Assert.That(MasterAiEval.NemesisOverrideTarget(nemesis, 59f, 60, incumbent, new[] { profile }), Is.Null);
			Assert.That(MasterAiEval.NemesisOverrideTarget(nemesis, 100f, 60, incumbent, new[] { dead }), Is.Null);
			Assert.That(MasterAiEval.NemesisOverrideTarget(nemesis, 100f, 60, incumbent, new[] { unreached }), Is.Null);
			Assert.That(MasterAiEval.NemesisOverrideTarget(nemesis, 60f, 60, incumbent, new[] { dead, profile }),
				Is.SameAs(profile));
		}

		[TestCase(BotUrgency.Emergency, false, false, true)]
		[TestCase(BotUrgency.Emergency, true, false, false)]
		[TestCase(BotUrgency.Emergency, false, true, false)]
		[TestCase(BotUrgency.Pressured, false, false, false)]
		public void EmergencyTransitionFiresOncePerUnkeptEmergency(
			BotUrgency urgency, bool handled, bool keepsPersonality, bool expected)
		{
			Assert.That(MasterAiEval.EmergencyTransition(urgency, handled, keepsPersonality),
				Is.EqualTo(expected));
		}

		[Test]
		public void HeldDemandsPrefersLiveSetAndFallsBackToIssued()
		{
			var active = new HashSet<string>(System.StringComparer.Ordinal) { "a" };
			var issued = new[] { "b", "c" };
			Assert.That(MasterAiEval.HeldDemands(active, issued), Is.SameAs(active));
			Assert.That(MasterAiEval.HeldDemands(
				new HashSet<string>(System.StringComparer.Ordinal), issued), Is.SameAs(issued));
		}

		[Test]
		public void DemandOrderNeedsControllerAndAChangedSet()
		{
			var active = new HashSet<string>(System.StringComparer.Ordinal) { "a", "b" };
			Assert.That(MasterAiEval.ShouldIssueDemandOrder(false, active, new[] { "a" }), Is.False);
			Assert.That(MasterAiEval.ShouldIssueDemandOrder(true, active, new[] { "a", "b" }), Is.False);
			Assert.That(MasterAiEval.ShouldIssueDemandOrder(true, active, new[] { "a" }), Is.True);
			Assert.That(MasterAiEval.ShouldIssueDemandOrder(true, active, System.Array.Empty<string>()), Is.True);
		}

		[Test]
		public void CountRegionIntelUnionsFreshAndPresenceAcrossEnemies()
		{
			static RegionMemory.Region Region(bool seen, int lastSeen, int army = 0, int econ = 0) =>
				new() { EverSeen = seen, LastSeenTick = lastSeen, ArmyValue = army, EconomyValue = econ };

			var enemyA = new[]
			{
				Region(true, 900, army: 100),   // cell0: fresh + presence
				null,                            // cell1: unobserved
				Region(true, 100),               // cell2: stale
			};
			var enemyB = new[]
			{
				Region(true, 100, army: 50),     // cell0: stale but has presence
				Region(true, 950),               // cell1: fresh, no presence
			};
			var byEnemy = new Dictionary<OpenRA.Player, RegionMemory.Region[]>
			{
				{ Player(), enemyA },
				{ Player(), enemyB },
			};

			// cell0 fresh(A) + presence(A|B); cell1 fresh(B), no presence; cell2 nothing fresh,
			// nothing present (B's table is shorter — out-of-range entries are skipped).
			var (fresh, presence) = MasterAiEval.CountRegionIntel(byEnemy, cellCount: 3, tick: 1000,
				staleAfterTicks: 200);
			Assert.That(fresh, Is.EqualTo(2));
			Assert.That(presence, Is.EqualTo(1));

			var empty = MasterAiEval.CountRegionIntel(
				new Dictionary<OpenRA.Player, RegionMemory.Region[]>(), 3, 1000, 200);
			Assert.That(empty, Is.EqualTo((0, 0)));
		}

		[TestCase(-1L, 500L, 0L)]
		[TestCase(500L, -1L, 0L)]
		[TestCase(100L, 500L, 400L)]
		[TestCase(500L, 100L, 0L)]
		[TestCase(100L, 100L, 0L)]
		public void LedgerWindowDeltaHonoursSentinelsAndFloor(long prev, long cur, long expected)
		{
			Assert.That(MasterAiEval.LedgerWindowDelta(prev, cur), Is.EqualTo(expected));
		}

		[Test]
		public void WindowDeltaFloorsAtZero()
		{
			Assert.That(MasterAiEval.WindowDelta(10, 25), Is.EqualTo(15));
			Assert.That(MasterAiEval.WindowDelta(25, 10), Is.EqualTo(0));
			Assert.That(MasterAiEval.WindowDelta(10L, 25L), Is.EqualTo(15L));
			Assert.That(MasterAiEval.WindowDelta(25L, 10L), Is.EqualTo(0L));
		}

		[Test]
		public void PerGameMinScalesByDeltaAndYieldsZeroOnFirstSnapshot()
		{
			Assert.That(MasterAiEval.PerGameMin(1200, 1500, 1500), Is.EqualTo(1200));
			Assert.That(MasterAiEval.PerGameMin(1200, 750, 1500), Is.EqualTo(2400));
			Assert.That(MasterAiEval.PerGameMin(1200, 0, 1500), Is.EqualTo(0));
		}

		[TestCase(BotUrgency.Emergency, 80)]
		[TestCase(BotUrgency.Pressured, 55)]
		[TestCase(BotUrgency.Normal, 30)]
		public void DefenceFractionHintFollowsUrgency(BotUrgency urgency, int expected)
		{
			Assert.That(MasterAiEval.DefenceFractionHint(urgency), Is.EqualTo(expected));
		}

		[TestCase(BotUrgency.Normal, 500, 60)]
		[TestCase(BotUrgency.Normal, 0, 20)]
		[TestCase(BotUrgency.Pressured, 500, 20)]
		[TestCase(BotUrgency.Emergency, 500, 20)]
		public void ExpansionAppetiteHintNeedsNormalUrgencyAndAnArmy(
			BotUrgency urgency, int ownArmy, int expected)
		{
			Assert.That(MasterAiEval.ExpansionAppetiteHint(urgency, ownArmy), Is.EqualTo(expected));
		}

		[TestCase(BotUrgency.Normal, false)]
		[TestCase(BotUrgency.Pressured, true)]
		[TestCase(BotUrgency.Emergency, true)]
		public void RequestsDefenceIsPressuredOrWorse(BotUrgency urgency, bool expected)
		{
			Assert.That(MasterAiEval.RequestsDefence(urgency), Is.EqualTo(expected));
		}
	}
}
