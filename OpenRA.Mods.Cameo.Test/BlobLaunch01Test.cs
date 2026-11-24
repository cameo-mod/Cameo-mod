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

		[Test]
		public void LeaderHoldsOnlyOutsideAnOpenOrder()
		{
			Assert.That(BlobLaunch01CA.LeaderHolds(true, true, true, false), Is.True, "no order: the leader forms no wave of its own");
			Assert.That(BlobLaunch01CA.LeaderHolds(true, true, true, true), Is.False, "an open order is joined");
			Assert.That(BlobLaunch01CA.LeaderHolds(true, false, true, false), Is.False, "followers are TEAM-LEAD's (they wait for an order there)");
			Assert.That(BlobLaunch01CA.LeaderHolds(true, true, false, false), Is.False, "cadence off: the TEAM-LEAD-01 leader launch, as before");
			Assert.That(BlobLaunch01CA.LeaderHolds(false, true, true, false), Is.False, "switch off: as before");
		}

		[Test]
		public void ArmedOnTheSixPersonalitiesWithItsOwnRowAndGateFirst()
		{
			var yaml = Source("packaging/ai.yaml");
			Assert.That(yaml.Split("\n\t\tBlobLeaderOrderOnly: true\n").Length - 1, Is.EqualTo(6));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("bl_leader_order_only"));

			var squad = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.cs");
			var hold = squad.IndexOf("if (TeamLeadHoldsLaunch(out teamLaunch))", StringComparison.Ordinal);
			var blob = squad.IndexOf("if (BlobLaunch01CA.LeaderHolds(BlobLeaderOrderOnlyActive, teamLaunch && TeamLeadIsLeader, TeamLeadCadenceOn, TeamLeadOpenOrder() != null))",
				StringComparison.Ordinal);
			var pool = squad.IndexOf("var idleUnitsValue = 0;", blob > 0 ? blob : 0, StringComparison.Ordinal);
			Assert.That(hold, Is.GreaterThan(0));
			Assert.That(blob, Is.GreaterThan(hold), "after the TEAM-LEAD hold, before any wave is measured");
			Assert.That(pool, Is.GreaterThan(blob));

			var logic = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.Stalemate01.cs");
			Assert.That(logic, Does.Contain("Info.BlobLeaderOrderOnly && !ablateBlobLeaderOrderOnly.Off(Player, AblationIds.BlobLeaderOrderOnly)"));
		}
	}
}
