using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class ExpansionMcvLeaseTest
	{
		// Actor-independent adapter uses the production lease table and order gate. No world/game is created.
		sealed class Leases : IBotUnitLeases
		{
			public readonly BotLeaseTable<int> Table = new();
			public int Tick;
			public int Key = 1;
			public int Transfers;
			public bool Live = true;
			public bool TryClaim(Actor actor, string owner, BotLeasePurpose purpose, int ticks) =>
				Live && Table.TryClaim(Key, owner, purpose, Tick, ticks);
			public BotLease? LeaseOf(Actor actor) => Table.LeaseOf(Key, Tick);
			public void Release(Actor actor, string owner) => Table.Release(Key, owner);
			public bool IsClaimedByOther(Actor actor, string owner) => Table.IsClaimedByOther(Key, owner, Tick);
			public bool Preempt(Actor actor, string owner, BotLeasePurpose purpose, int ticks)
			{
				if (!Live)
					return false;
				Table.Preempt(Key, owner, purpose, Tick, ticks);
				return true;
			}
			public bool Transfer(Actor actor, string owner, BotLeasePurpose purpose, int ticks)
			{
				if (!Live)
					return false;
				Transfers++;
				Table.Transfer(Key, owner, purpose, Tick, ticks);
				return true;
			}
		}

		[TestCase(false)]
		[TestCase(true)]
		public void PrebuildAcquisitionAndHeartbeatPermitEveryExpansionOrder(bool oldDemandLease)
		{
			var leases = new Leases();
			if (oldDemandLease)
				leases.TryClaim(null, nameof(BaseBuilderBotModuleCA), BotLeasePurpose.McvExpansion, 100);
			var gate = new BotOrderGate<int>();
			for (var i = 0; i < 75; i++)
			{
				leases.Tick = i * 25;
				Assert.That(ExpansionMcvLease.Acquire(leases, null, 100), Is.True);
				// Both Move and queued DeployTransform traverse the same ownership verdict.
				for (var order = 0; order < 2; order++)
					Assert.That(gate.Judge("McvExpansionManagerBotModule@0", leases.LeaseOf(null)?.Owner,
						true, false, null).Verdict, Is.EqualTo(BotOrderVerdict.Allow));
			}
			Assert.That(gate.Refused, Is.Zero);
			Assert.That(leases.Transfers, Is.EqualTo(oldDemandLease ? 1 : 0));
			ExpansionMcvLease.Release(leases, null);
			Assert.That(leases.LeaseOf(null), Is.Null);
		}

		[Test]
		public void OldBaseBuilderOwnerReproducesTheRefusal()
		{
			var leases = new Leases();
			leases.TryClaim(null, nameof(BaseBuilderBotModuleCA), BotLeasePurpose.McvExpansion, 100);
			Assert.That(BotOrderGate<int>.Decide("McvExpansionManagerBotModule@0", leases.LeaseOf(null)?.Owner,
				true, false, null), Is.EqualTo(BotOrderVerdict.Refuse));
		}

		[TestCase("SquadManagerBotModuleCA", BotLeasePurpose.Emergency)]
		[TestCase("ScoutBotModule", BotLeasePurpose.McvExpansion)]
		[TestCase("BaseBuilderBotModuleCA", BotLeasePurpose.Emergency)]
		[TestCase("McvExpansionManagerBotModule", BotLeasePurpose.Emergency)]
		public void UnrelatedOrEmergencyLeaseCannotBeTransferredRenewedOrReleased(string owner, BotLeasePurpose purpose)
		{
			var leases = new Leases();
			leases.TryClaim(null, owner, purpose, 500);
			var before = leases.LeaseOf(null);
			Assert.That(ExpansionMcvLease.Acquire(leases, null, 100), Is.False);
			ExpansionMcvLease.Release(leases, null);
			Assert.That(leases.LeaseOf(null), Is.EqualTo(before));
			Assert.That(leases.Transfers, Is.Zero);
		}

		[Test]
		public void RelocationReleasesOldActorAndAcquiresReplacementForIssuer()
		{
			var leases = new Leases();
			Assert.That(ExpansionMcvLease.Acquire(leases, null, 100), Is.True);
			ExpansionMcvLease.Release(leases, null);
			leases.Key = 2;
			Assert.That(ExpansionMcvLease.Acquire(leases, null, 100), Is.True);
			Assert.That(leases.Table.LeaseOf(1, 0), Is.Null);
			Assert.That(leases.LeaseOf(null)?.Owner, Is.EqualTo(ExpansionMcvLease.IssuerOwner));
		}

		[Test]
		public void DeadTravellerCannotAcquireOrTransfer()
		{
			var leases = new Leases();
			leases.TryClaim(null, nameof(BaseBuilderBotModuleCA), BotLeasePurpose.McvExpansion, 100);
			leases.Live = false;
			Assert.That(ExpansionMcvLease.Acquire(leases, null, 100), Is.False);
			Assert.That(leases.Transfers, Is.Zero);
		}

		[Test]
		public void MissingRegistryPreservesUnleasedBehavior()
		{
			Assert.That(ExpansionMcvLease.Acquire(null, null, 100), Is.True);
			Assert.DoesNotThrow(() => ExpansionMcvLease.Release(null, null));
		}
	}
}
