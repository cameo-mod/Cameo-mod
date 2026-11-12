using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class StalemateLeasePlanTest
	{
		sealed class Registry : IBotUnitLeases
		{
			public readonly BotLeaseTable<Actor> Table = new();
			public uint ThrowAfterClaim;
			public bool TryClaim(Actor actor, string owner, BotLeasePurpose purpose, int durationTicks)
			{
				var ok = Table.TryClaim(actor, owner, purpose, 100, durationTicks);
				if (actor.ActorID == ThrowAfterClaim)
					throw new InvalidOperationException("injected after claim");
				return ok;
			}

			public void Release(Actor actor, string owner) => Table.Release(actor, owner);
			public bool IsClaimedByOther(Actor actor, string owner) => Table.IsClaimedByOther(actor, owner, 100);
			public BotLease? LeaseOf(Actor actor) => Table.LeaseOf(actor, 100);
			public bool Preempt(Actor actor, string owner, BotLeasePurpose purpose, int durationTicks) =>
				throw new NotSupportedException();
			public bool Transfer(Actor actor, string owner, BotLeasePurpose purpose, int durationTicks) =>
				throw new NotSupportedException();
		}

		static Actor FakeActor(uint id)
		{
			var actor = (Actor)RuntimeHelpers.GetUninitializedObject(typeof(Actor));
			typeof(Actor).GetField("ActorID", BindingFlags.Instance | BindingFlags.Public).SetValue(actor, id);
			return actor;
		}

		[Test]
		public void ClaimsInIdOrderAndPreservesExistingSameOwnerLease()
		{
			var leases = new Registry();
			var a = FakeActor(1);
			var b = FakeActor(2);
			leases.Table.TryClaim(a, "squad", BotLeasePurpose.Squad, 90, 1000);
			Assert.That(StalemateLeasePlanCA.TryClaimAll(leases, new[] { b, a }, "squad", 200, _ => true, out var acquired), Is.True);
			Assert.That(acquired.Select(x => x.ActorID), Is.EqualTo(new uint[] { 2 }));
			Assert.That(StalemateLeasePlanCA.Rollback(leases, "squad", acquired), Is.True);
			Assert.That(leases.LeaseOf(a)?.AcquiredTick, Is.EqualTo(90));
			Assert.That(leases.LeaseOf(b), Is.Null);
		}

		[Test]
		public void ForeignClaimRollsBackOnlyNewClaims()
		{
			var leases = new Registry();
			var a = FakeActor(1);
			var b = FakeActor(2);
			leases.Table.TryClaim(b, "scout", BotLeasePurpose.Scout, 90, 1000);
			Assert.That(StalemateLeasePlanCA.TryClaimAll(leases, new[] { a, b }, "squad", 200, _ => true, out var acquired), Is.False);
			Assert.That(acquired, Is.Empty);
			Assert.That(leases.LeaseOf(a), Is.Null);
			Assert.That(leases.LeaseOf(b)?.Owner, Is.EqualTo("scout"));
		}

		[Test]
		public void ProviderExceptionAfterMutationStillRollsBack()
		{
			var leases = new Registry { ThrowAfterClaim = 2 };
			var a = FakeActor(1);
			var b = FakeActor(2);
			Assert.That(StalemateLeasePlanCA.TryClaimAll(leases, new[] { a, b }, "squad", 200, _ => true, out var acquired), Is.False);
			Assert.That(acquired, Is.Empty);
			Assert.That(leases.Table.Count, Is.Zero);
		}

		[Test]
		public void DuplicateAndInvalidIdentitiesCannotAcquireClaims()
		{
			var leases = new Registry();
			var a = FakeActor(1);
			Assert.That(StalemateLeasePlanCA.TryClaimAll(leases, new[] { a, a }, "squad", 200, _ => true, out _), Is.False);
			Assert.That(StalemateLeasePlanCA.TryClaimAll(leases, new[] { a }, "squad", 200, _ => false, out _), Is.False);
			Assert.That(leases.Table.Count, Is.Zero);
		}
	}
}
