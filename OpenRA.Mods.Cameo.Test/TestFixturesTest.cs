#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Test.TestFixtures;

namespace OpenRA.Mods.Cameo.Test
{
	// AR-T2: the shared fixtures are load-bearing for every seam test that follows,
	// so their own contracts get pinned — reference identity (Uninitialized),
	// order capture (RecordingBot), the two nearest-region semantics
	// (FakeZoneTopology), and the publish-only provider stubs.
	[TestFixture]
	public sealed class TestFixturesTest
	{
		[Test]
		public void UninitializedGivesDistinctReferences()
		{
			var a = Uninitialized.Player();
			var b = Uninitialized.Player();
			Assert.That(a, Is.Not.Null.And.Not.SameAs(b));
			Assert.That(Uninitialized.Actor(), Is.Not.SameAs(Uninitialized.Actor()));
		}

		[Test]
		public void RecordingBotCapturesQueuedOrdersInOrder()
		{
			var bot = new RecordingBot();
			var first = Uninitialized.Of<Order>();
			var second = Uninitialized.Of<Order>();

			bot.QueueOrder(first);
			bot.QueueOrder(second);

			Assert.That(bot.QueuedOrders, Has.Count.EqualTo(2));
			Assert.That(bot.QueuedOrders[0], Is.SameAs(first));
			Assert.That(bot.LastOrder, Is.SameAs(second));
		}

		[Test]
		public void RecordingBotActivatesWithPlayer()
		{
			var bot = new RecordingBot();
			var player = Uninitialized.Player();
			bot.Activate(player);
			Assert.That(bot.Player, Is.SameAs(player));
		}

		[Test]
		public void FakeZoneTopologyRingSearchResolvesNeighbours()
		{
			var topology = new FakeZoneTopology();
			topology.AddZone(new[] { new CPos(0, 0) });

			Assert.That(topology.NearestRegionId(new CPos(0, 0)), Is.EqualTo(0));
			Assert.That(topology.NearestRegionId(new CPos(2, -1)), Is.EqualTo(0),
				"within the default radius-3 ring");
			Assert.That(topology.NearestRegionId(new CPos(30, 30)), Is.EqualTo(-1));
		}

		[Test]
		public void FakeZoneTopologyRadiusZeroIsExactOnly()
		{
			var topology = new FakeZoneTopology { NearestSearchRadius = 0 };
			topology.AddZone(new[] { new CPos(0, 0) });

			Assert.That(topology.NearestRegionId(new CPos(0, 0)), Is.EqualTo(0));
			Assert.That(topology.NearestRegionId(new CPos(1, 0)), Is.EqualTo(-1));
		}

		[Test]
		public void FakeZoneTopologyRecutClearsTheMap()
		{
			var topology = new FakeZoneTopology();
			topology.AddZone(new[] { new CPos(0, 0) });
			topology.Recut();

			Assert.That(topology.Regions, Is.Empty);
			Assert.That(topology.RegionIdAt(new CPos(0, 0)), Is.EqualTo(-1));
		}

		[Test]
		public void StubMissionProviderPublishesItsList()
		{
			var mission = new BotMission();
			var provider = new StubMissionProvider { Missions = new[] { mission } };
			Assert.That(provider.Missions, Has.Count.EqualTo(1));
			Assert.DoesNotThrow(() => provider.MissionTaken(mission));
		}

		[Test]
		public void StubUtilityAxesDefaultsToNeutralAndHonoursArgs()
		{
			var neutral = new StubUtilityAxes();
			Assert.That(neutral.UtilityTurtleRush, Is.EqualTo(IBotUtilityAxes.Neutral));
			Assert.That(neutral.UtilitySteamrollerGuerrilla, Is.EqualTo(IBotUtilityAxes.Neutral));
			Assert.That(neutral.UtilityTechRushExpansion, Is.EqualTo(IBotUtilityAxes.Neutral));

			var axes = new StubUtilityAxes(80, 20, 60);
			Assert.That(axes.UtilityTurtleRush, Is.EqualTo(80));
			Assert.That(axes.UtilitySteamrollerGuerrilla, Is.EqualTo(20));
			Assert.That(axes.UtilityTechRushExpansion, Is.EqualTo(60));
		}
	}
}
