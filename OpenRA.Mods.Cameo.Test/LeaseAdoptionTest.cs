#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// AR-9 (§19.6): the lease-adoption seams — claim-then-order composition on DeployBotModule
	// (QueueLeased) and the shared heartbeat formulas on both adopted modules. Fabricated Actors
	// stand in where only reference identity matters (the fixture trick TeamBlackboardTest uses).
	[TestFixture]
	public sealed class LeaseAdoptionTest
	{
		sealed class FakeLeases : IBotUnitLeases
		{
			public bool Allow = true;
			public readonly List<(Actor Actor, string Owner, BotLeasePurpose Purpose, int DurationTicks)> Claims = new();
			public readonly List<(Actor Actor, string Owner)> Releases = new();

			public bool TryClaim(Actor actor, string owner, BotLeasePurpose purpose, int durationTicks)
			{
				Claims.Add((actor, owner, purpose, durationTicks));
				return Allow;
			}

			public void Release(Actor actor, string owner) => Releases.Add((actor, owner));
			public bool IsClaimedByOther(Actor actor, string owner) => false;
			public BotLease? LeaseOf(Actor actor) => null;
			public bool Preempt(Actor actor, string owner, BotLeasePurpose purpose, int durationTicks) => Allow;
			public bool Transfer(Actor actor, string newOwner, BotLeasePurpose purpose, int durationTicks) => Allow;
		}

		sealed class RecordingBot : IBot
		{
			public readonly List<Order> Queued = new();
			public Player Player { get; private set; }
			public IBotInfo Info { get; set; }
			public void Activate(Player p) => Player = p;
			public void QueueOrder(Order order) => Queued.Add(order);
		}

		static Actor FakeActor() =>
			(Actor)RuntimeHelpers.GetUninitializedObject(typeof(Actor));

		// QueueLeased — the claim precedes the order

		[Test]
		public void AnAllowedClaimQueuesTheOrderAndMarksTheUnit()
		{
			var leases = new FakeLeases();
			var bot = new RecordingBot();
			var unit = FakeActor();
			var order = new Order("Move", unit, false);
			var leased = new HashSet<Actor>();

			var queued = DeployBotModule.QueueLeased(bot, leases, unit, "deployer",
				BotLeasePurpose.Mission, 400, order, leased);

			Assert.That(queued, Is.True);
			Assert.That(bot.Queued, Is.EqualTo(new[] { order }));
			Assert.That(leased, Does.Contain(unit));
			Assert.That(leases.Claims.Count, Is.EqualTo(1));
			Assert.That(leases.Claims[0].Owner, Is.EqualTo("deployer"));
			Assert.That(leases.Claims[0].Purpose, Is.EqualTo(BotLeasePurpose.Mission));
			Assert.That(leases.Claims[0].DurationTicks, Is.EqualTo(400));
		}

		[Test]
		public void ADeniedClaimQueuesNothingAndDropsTheUnit()
		{
			var leases = new FakeLeases { Allow = false };
			var bot = new RecordingBot();
			var unit = FakeActor();
			var leased = new HashSet<Actor> { unit };

			var queued = DeployBotModule.QueueLeased(bot, leases, unit, "deployer",
				BotLeasePurpose.Mission, 400, new Order("Move", unit, false), leased);

			Assert.That(queued, Is.False);
			Assert.That(bot.Queued, Is.Empty, "a lost claim must not emit an order");
			Assert.That(leased, Does.Not.Contain(unit),
				"a denied unit leaves the leased set so the next scan re-filters it");
		}

		[Test]
		public void NullLeasesBehavesAsBeforeTheContract()
		{
			// Classicbot carries no registry: BotUnitLeases.TryClaim degrades to "always allow",
			// so the order queues exactly as the pre-lease module did.
			var bot = new RecordingBot();
			var unit = FakeActor();
			var leased = new HashSet<Actor>();

			var queued = DeployBotModule.QueueLeased(bot, null, unit, "deployer",
				BotLeasePurpose.Mission, 400, new Order("Move", unit, false), leased);

			Assert.That(queued, Is.True);
			Assert.That(bot.Queued.Count, Is.EqualTo(1));
			Assert.That(leased, Does.Contain(unit));
		}

		// Lease heartbeat formula — both adopted modules share Math.Max(200, scan * 4)

		[Test]
		public void TheHeartbeatFloorCoversTinyScanIntervals()
		{
			Assert.That(DeployBotModule.LeaseHeartbeatTicks(1), Is.EqualTo(200));
			Assert.That(DeployBotModule.LeaseHeartbeatTicks(40), Is.EqualTo(200));
			Assert.That(DeployBotModule.LeaseHeartbeatTicks(100), Is.EqualTo(400));
			Assert.That(LoadGarrisonerBotModuleCA.LeaseHeartbeatTicks(1), Is.EqualTo(200));
			Assert.That(LoadGarrisonerBotModuleCA.LeaseHeartbeatTicks(457), Is.EqualTo(1828));
		}
	}
}
