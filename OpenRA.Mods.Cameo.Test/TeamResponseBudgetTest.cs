using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class TeamResponseBudgetTest
	{
		[TestCase(1000, 10, 100, 1, true, false)]
		[TestCase(1100, 11, 100, 1, true, true)]
		[TestCase(1100, 10, 100, 1, true, false)]
		[TestCase(1100, 11, 100, 1, false, false)]
		[TestCase(500, 11, 100, 1, true, false)]
		public void DraftMustLeaveOwnValueCountAndRequiredRoles(long value, int count,
			long draftValue, int draftCount, bool roles, bool allowed)
		{
			Assert.That(TeamResponseBudgetCA.HasSpare(value, count, draftValue, draftCount,
				1000, 10, roles), Is.EqualTo(allowed));
		}

		[Test]
		public void OnePlayerSlotCannotBeRenewedByAnotherManagerOrRally()
		{
			var budget = new TeamResponseBudgetCA();
			Assert.That(budget.TryAcquire(0, "managerA", "ally:claim:assist", 1500, true), Is.True);
			Assert.That(budget.TryAcquire(100, "managerA", "ally:claim:assist", 1500, true), Is.False);
			Assert.That(budget.TryAcquire(100, "managerB", "other:claim:rescue", 1500, true), Is.False);
			Assert.That(budget.ExpiresTick, Is.EqualTo(1500));
			Assert.That(budget.Evaluate(1499, true, true, true, true, false), Is.EqualTo(TeamResponseReleaseCA.None));
			Assert.That(budget.Evaluate(1500, true, true, true, true, false), Is.EqualTo(TeamResponseReleaseCA.Expired));
			Assert.That(budget.Release(1500, "managerB", 750), Is.False);
			Assert.That(budget.Release(1500, "managerA", 750), Is.True);
			Assert.That(budget.TryAcquire(2249, "managerB", "ally:claim:assist", 1500, true), Is.False);
			Assert.That(budget.TryAcquire(2250, "managerB", "ally:claim:assist", 1500, true), Is.True);
		}

		[TestCase(false, true, true, true, false, TeamResponseReleaseCA.Stale)]
		[TestCase(true, false, true, true, false, TeamResponseReleaseCA.Cleared)]
		[TestCase(true, true, false, true, false, TeamResponseReleaseCA.RequesterInactive)]
		[TestCase(true, true, true, false, false, TeamResponseReleaseCA.ReserveLost)]
		[TestCase(true, true, true, true, true, TeamResponseReleaseCA.Emergency)]
		public void FreshClearingAndHardReleaseReasonsAreExplicit(bool fresh, bool contested,
			bool requester, bool reserve, bool emergency, TeamResponseReleaseCA reason)
		{
			var budget = new TeamResponseBudgetCA();
			budget.TryAcquire(0, "manager", "ally:claim:assist", 1500, true);
			Assert.That(budget.Evaluate(1, fresh, contested, requester, reserve, emergency), Is.EqualTo(reason));
			Assert.That(budget.Release(1, "manager", 750), Is.True);
			Assert.That(budget.Release(1, "manager", 750), Is.False);
			Assert.That(budget.ActiveKey, Is.Null);
		}

		[Test]
		public void AlternatingClaimsCannotEraseAnEarlierCooldownAndHistoryIsBounded()
		{
			var budget = new TeamResponseBudgetCA();
			for (var i = 0; i < 128; i++)
			{
				Assert.That(budget.TryAcquire(0, "manager", "claim:" + i, 1500, true), Is.True);
				Assert.That(budget.Release(0, "manager", 750), Is.True);
			}
			Assert.That(budget.TryAcquire(1, "manager", "claim:0", 1500, true), Is.False);
			Assert.That(budget.TryAcquire(1, "manager", "claim:129", 1500, true), Is.False,
				"a full bounded cooldown table refuses new admission instead of evicting evidence");
			Assert.That(budget.TryAcquire(750, "manager", "claim:0", 1500, true), Is.True);
		}
	}
}
