#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// AR-5/AR-7 (ai_arch_audit R7): the declared merges for multi-provider seams.
	// IBotRegionThreatProvider merges by Max across ENABLED providers — the providers
	// publish overlapping estimates of the same strength, so Sum double-counts.
	// IBotMissionProvider merges all enabled providers' cards on one ordering —
	// Priority desc, RequiredValue asc, publish order — instead of yielding to the
	// provider that happens to sit earlier in the trait list.
	[TestFixture]
	public class ProviderMergeTest
	{
		sealed class StubThreatProvider : IBotRegionThreatProvider
		{
			public int Threat;
			public int RememberedEnemyThreatAt(CPos cell) => Threat;
		}

		sealed class DisabledThreatProvider : IBotRegionThreatProvider, IDisabledTrait
		{
			public bool IsTraitDisabled => true;
			public int RememberedEnemyThreatAt(CPos cell) => 9999;
		}

		sealed class StubMissionProvider : IBotMissionProvider
		{
			public IReadOnlyList<BotMission> Missions { get; set; } = Array.Empty<BotMission>();
			public void MissionTaken(BotMission mission) { }
		}

		sealed class DisabledMissionProvider : IBotMissionProvider, IDisabledTrait
		{
			public bool IsTraitDisabled => true;
			public IReadOnlyList<BotMission> Missions { get; set; } = Array.Empty<BotMission>();
			public void MissionTaken(BotMission mission) { }
		}

		static BotMission Raid(int requiredValue, int priority, int region = 0)
		{
			return new BotMission
			{
				Type = BotMissionType.Raid,
				RequiredValue = requiredValue,
				Priority = priority,
				RegionIndex = region
			};
		}

		// ---- IBotRegionThreatProvider: declared merge is max over enabled ----

		[Test]
		public void NullOrEmptyThreatProvidersYieldZero()
		{
			Assert.That(((IEnumerable<IBotRegionThreatProvider>)null).MergedThreatAt(default), Is.EqualTo(0));
			Assert.That(Array.Empty<IBotRegionThreatProvider>().MergedThreatAt(default), Is.EqualTo(0));
			Assert.That(new IBotRegionThreatProvider[] { null }.MergedThreatAt(default), Is.EqualTo(0));
		}

		[Test]
		public void StrongestReadingWins_NeverSummed()
		{
			// Both providers estimate the same underlying strength from different
			// memories — summing would double-count a region both observed.
			var providers = new IBotRegionThreatProvider[]
			{
				new StubThreatProvider { Threat = 400 },
				new StubThreatProvider { Threat = 150 }
			};

			Assert.That(providers.MergedThreatAt(default), Is.EqualTo(400));
		}

		[Test]
		public void DisabledThreatProviderContributesNothing()
		{
			var providers = new IBotRegionThreatProvider[]
			{
				new DisabledThreatProvider(),
				new StubThreatProvider { Threat = 60 }
			};

			Assert.That(providers.MergedThreatAt(default), Is.EqualTo(60));
		}

		[Test]
		public void ZeroStaysUnknown()
		{
			var providers = new IBotRegionThreatProvider[]
			{
				new StubThreatProvider { Threat = 0 },
				new StubThreatProvider { Threat = 0 }
			};

			Assert.That(providers.MergedThreatAt(default), Is.EqualTo(0));
		}

		// ---- IBotMissionProvider: declared merge is one sorted ordering ----

		[Test]
		public void HigherPriorityWinsAcrossProviders()
		{
			var master = Raid(500, priority: 40, region: 1);
			var garrison = Raid(500, priority: 80, region: 2);
			var providers = new IBotMissionProvider[]
			{
				new StubMissionProvider { Missions = new[] { master } },
				new StubMissionProvider { Missions = new[] { garrison } }
			};

			// The garrison card outranks the master card regardless of which provider
			// iterates first — the old code would have returned `master` here.
			Assert.That(SquadManagerBotModuleCA.BestAffordableMission(providers, 1000), Is.SameAs(garrison));

			var reversed = new IBotMissionProvider[] { providers[1], providers[0] };
			Assert.That(SquadManagerBotModuleCA.BestAffordableMission(reversed, 1000), Is.SameAs(garrison));
		}

		[Test]
		public void AffordableGateStillAppliesBeforeRanking()
		{
			var dear = Raid(9000, priority: 90, region: 1);
			var cheap = Raid(500, priority: 10, region: 2);
			var providers = new IBotMissionProvider[]
			{
				new StubMissionProvider { Missions = new[] { dear } },
				new StubMissionProvider { Missions = new[] { cheap } }
			};

			Assert.That(SquadManagerBotModuleCA.BestAffordableMission(providers, 1000), Is.SameAs(cheap));
		}

		[Test]
		public void PublishOrderBreaksFullTies()
		{
			var first = Raid(500, priority: 60, region: 1);
			var second = Raid(500, priority: 60, region: 2);
			var providers = new IBotMissionProvider[]
			{
				new StubMissionProvider { Missions = new[] { first } },
				new StubMissionProvider { Missions = new[] { second } }
			};

			Assert.That(SquadManagerBotModuleCA.BestAffordableMission(providers, 1000), Is.SameAs(first));
		}

		[Test]
		public void DisabledMissionProviderPublishesNothing()
		{
			var live = Raid(500, priority: 10, region: 1);
			var providers = new IBotMissionProvider[]
			{
				new DisabledMissionProvider { Missions = new[] { Raid(100, priority: 99, region: 2) } },
				new StubMissionProvider { Missions = new[] { live } }
			};

			Assert.That(SquadManagerBotModuleCA.BestAffordableMission(providers, 1000), Is.SameAs(live));
		}

		[Test]
		public void SteeringPicksIgnoreDisabledProvidersToo()
		{
			var live = Raid(500, priority: 10, region: 1);
			var providers = new IBotMissionProvider[]
			{
				new DisabledMissionProvider { Missions = new[] { Raid(100, priority: 99, region: 2) } },
				new StubMissionProvider { Missions = new[] { live } }
			};

			Assert.That(SquadManagerBotModuleCA.BestRaidForSteering(providers, 1000), Is.SameAs(live));
		}
	}
}
