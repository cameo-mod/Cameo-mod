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
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	// BLOB-LAUNCH-01 (USER 11-24 via Opus, V02 RCA: BIG blob pushes, not frequent trickles). V02 trickle path: the team leader's own
	// CreateAttackForce measured the bars on the team pool but sent only its own 1-6 units (Rush squads after 25k: median 3, none >= 12).
	[TestFixture]
	public class BlobLaunch01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void BlobMinimumIsWaveMinAndHalfTheTeamIdleArmy()
		{
			Assert.That(BlobLaunch01CA.MinUnits(12, 10), Is.EqualTo(12), "WAVE-MIN dominates a small team army");
			Assert.That(BlobLaunch01CA.MinUnits(12, 60), Is.EqualTo(30), "half of a large idle army");
			Assert.That(BlobLaunch01CA.MinUnits(12, 61), Is.EqualTo(31), "rounded up");
			Assert.That(BlobLaunch01CA.MinUnits(0, 0), Is.EqualTo(0));
			Assert.That(BlobLaunch01CA.MinUnits(-5, -3), Is.EqualTo(0), "negatives clamp");
		}

		static bool Holds(bool active, bool leader, bool cadence, bool order, int since = 0, int pool = 0, int min = 12) =>
			BlobLaunch01CA.LeaderHolds(active, leader, cadence, order, since, 3000, pool, min);

		[Test]
		public void LeaderHoldsOnlyOutsideAnOpenOrder()
		{
			Assert.That(Holds(true, true, true, false), Is.True, "no order: the leader forms no wave of its own");
			Assert.That(Holds(true, true, true, true), Is.False, "an open order is joined");
			Assert.That(Holds(true, false, true, false), Is.False, "followers are TEAM-LEAD's (they wait for an order there)");
			Assert.That(Holds(true, true, false, false), Is.False, "cadence off: the TEAM-LEAD-01 leader launch, as before");
			Assert.That(Holds(false, true, true, false), Is.False, "switch off: as before");
		}

		[Test]
		public void SafetyFallbackAfterThreeThousandTicksWithoutAnOrderAtTheBlobMinimum()
		{
			Assert.That(Holds(true, true, true, false, 2999, 40, 20), Is.True, "not yet 3000 ticks");
			Assert.That(Holds(true, true, true, false, 3000, 40, 20), Is.False, "3000 ticks and the pool at the minimum: the leader launches");
			Assert.That(Holds(true, true, true, false, 9000, 11, 12), Is.True, "below the blob minimum it still holds");
			Assert.That(BlobLaunch01CA.LeaderHolds(true, true, true, false, 9000, 0, 40, 12), Is.True, "0 fallback ticks = never");
			Assert.That(BlobLaunch01CA.TicksSinceOrder(10000, 4000, 3000), Is.EqualTo(6000));
			Assert.That(BlobLaunch01CA.TicksSinceOrder(10000, -1, 3000), Is.EqualTo(7000), "no order yet: since the initial attack delay");
			Assert.That(BlobLaunch01CA.TicksSinceOrder(1000, -1, 3000), Is.EqualTo(0), "never negative");
		}

		[Test]
		public void AttackGateHoldsBelowTheBlobMinimum()
		{
			Assert.That(BlobLaunch01CA.GateHolds(11, 12, 4), Is.True, "below WAVE-MIN");
			Assert.That(BlobLaunch01CA.GateHolds(12, 12, 4), Is.False);
			Assert.That(BlobLaunch01CA.GateHolds(14, 12, 40), Is.True, "below half the idle pool");
			Assert.That(BlobLaunch01CA.GateHolds(20, 12, 40), Is.False);
			Assert.That(BlobLaunch01CA.GateHolds(1, 0, 0), Is.False, "no WAVE-MIN and no pool: nothing to hold");
		}

		[Test]
		public void BreakerAndLivenessWavesAreGatedUnderTheirOwnRow()
		{
			var yaml = Source("packaging/ai.yaml");
			Assert.That(yaml.Split("\n\t\tBlobAttackGate: true\n").Length - 1, Is.EqualTo(6));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("bl_attack_gate"));

			var breaker = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.Stalemate.cs");
			var gate = breaker.IndexOf("if (BlobAttackGateHolds(roster.Count))", StringComparison.Ordinal);
			Assert.That(gate, Is.GreaterThan(0));
			Assert.That(breaker.IndexOf("StalemateLeasePlanCA.TryClaimAll(", gate, StringComparison.Ordinal), Is.GreaterThan(gate), "before any lease is claimed");

			var squad = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.cs");
			var m13 = squad.IndexOf("if (BlobAttackGateHolds(roster.Count))", StringComparison.Ordinal);
			Assert.That(m13, Is.GreaterThan(0));
			Assert.That(squad.IndexOf("return M13LaunchPolicyCA.TryCommit(", m13, StringComparison.Ordinal), Is.GreaterThan(m13));

			var logic = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.Stalemate01.cs");
			Assert.That(logic, Does.Contain("Info.BlobAttackGate && !ablateBlobAttackGate.Off(Player, AblationIds.BlobAttackGate)"));
		}

		[Test]
		public void ArmedOnTheSixPersonalitiesWithItsOwnRowAndGateFirst()
		{
			var yaml = Source("packaging/ai.yaml");
			Assert.That(yaml.Split("\n\t\tBlobLeaderOrderOnly: true\n").Length - 1, Is.EqualTo(6));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("bl_leader_order_only"));
			Assert.That(yaml.Split("\n\t\tBlobLeaderFallbackTicks: 0\n").Length - 1, Is.EqualTo(6),
				"one no-order fallback: TEAM-BLOB-01 tl_fallback_publish publishes the order itself");

			var squad = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.cs");
			var hold = squad.IndexOf("if (TeamLeadHoldsLaunch(out teamLaunch))", StringComparison.Ordinal);
			var blob = squad.IndexOf("if (BlobLeaderHolds(teamLaunch))", StringComparison.Ordinal);
			var pool = squad.IndexOf("var idleUnitsValue = 0;", blob > 0 ? blob : 0, StringComparison.Ordinal);
			Assert.That(hold, Is.GreaterThan(0));
			Assert.That(blob, Is.GreaterThan(hold), "after the TEAM-LEAD hold, before any wave is measured");
			Assert.That(pool, Is.GreaterThan(blob));

			var logic = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.Stalemate01.cs");
			Assert.That(logic, Does.Contain("Info.BlobLeaderOrderOnly && !ablateBlobLeaderOrderOnly.Off(Player, AblationIds.BlobLeaderOrderOnly)"));
		}
	}
}
