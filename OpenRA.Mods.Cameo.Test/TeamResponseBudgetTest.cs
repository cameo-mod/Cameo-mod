using System;
using System.Collections.Generic;
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
		[Test]
		public void RestorePreservesTheNonrenewingLeaseAndOwnerAcrossManagers()
		{
			var source = new TeamResponseBudgetCA();
			source.TryAcquire(100, "managerA", "ally:claim:assist", 1500, true);
			var restored = new TeamResponseBudgetCA();
			Assert.That(restored.TryRestore(1000, "playerA", source.Capture("playerA")), Is.True);
			Assert.That(restored.StartedTick, Is.EqualTo(100));
			Assert.That(restored.ExpiresTick, Is.EqualTo(1600));
			Assert.That(restored.TryAcquire(1000, "managerB", "other", 1500, true), Is.False);
			Assert.That(restored.Release(1000, "managerB", 750), Is.False);
			Assert.That(restored.Evaluate(1600, true, true, true, true, false), Is.EqualTo(TeamResponseReleaseCA.Expired));
			Assert.That(restored.Release(1600, "managerA", 750), Is.True);
			Assert.That(restored.TryAcquire(2349, "managerB", "ally:claim:assist", 1500, true), Is.False);
			Assert.That(restored.TryAcquire(2350, "managerB", "ally:claim:assist", 1500, true), Is.True);
		}

		[Test]
		public void RestoreAtOrAfterExpiryKeepsMembershipReclaimRequired()
		{
			var source = new TeamResponseBudgetCA();
			source.TryAcquire(0, "manager", "claim", 1500, true);
			var restored = new TeamResponseBudgetCA();
			Assert.That(restored.TryRestore(2000, "player", source.Capture("player")), Is.True);
			Assert.That(restored.TryAcquire(2000, "other", "new", 1500, true), Is.False);
			Assert.That(restored.Evaluate(2000, true, true, true, true, false), Is.EqualTo(TeamResponseReleaseCA.Expired));
		}

		[Test]
		public void CooldownRoundTripDoesNotResetOrRotateAwayHistory()
		{
			var source = new TeamResponseBudgetCA();
			for (var i = 0; i < 128; i++)
			{
				source.TryAcquire(10, "manager", "claim:" + i, 100, true);
				source.Release(10, "manager", 750);
			}
			var restored = new TeamResponseBudgetCA();
			Assert.That(restored.TryRestore(700, "player", source.Capture("player")), Is.True);
			Assert.That(restored.TryAcquire(759, "other", "fresh", 100, true), Is.False);
			Assert.That(restored.TryAcquire(759, "other", "claim:0", 100, true), Is.False);
			Assert.That(restored.TryAcquire(760, "other", "claim:0", 100, true), Is.True);
		}

		[Test]
		public void CaptureIsCanonicalAndRestoreCopiesUntrustedTransport()
		{
			var source = new TeamResponseBudgetCA();
			foreach (var key in new[] { "z", "a" })
			{
				source.TryAcquire(0, "manager", key, 100, true);
				source.Release(0, "manager", 750);
			}
			var snapshot = source.Capture("player");
			Assert.That(snapshot.Cooldowns[0].Key, Is.EqualTo("a"));
			var entries = (KeyValuePair<string, long>[])snapshot.Cooldowns;
			var restored = new TeamResponseBudgetCA();
			Assert.That(restored.TryRestore(1, "player", snapshot), Is.True);
			entries[0] = new KeyValuePair<string, long>("a", 0);
			Assert.That(restored.TryAcquire(2, "other", "a", 100, true), Is.False);
			Assert.That(source.TryAcquire(2, "other", "a", 100, true), Is.False);
		}

		[TestCase("null")]
		[TestCase("version")]
		[TestCase("player")]
		[TestCase("future")]
		[TestCase("clock")]
		[TestCase("idle-owner")]
		[TestCase("idle-start")]
		[TestCase("idle-expiry")]
		[TestCase("active-owner")]
		[TestCase("active-start")]
		[TestCase("active-expiry")]
		[TestCase("overflow")]
		[TestCase("duplicates")]
		[TestCase("negative-cooldown")]
		[TestCase("null-cooldowns")]
		[TestCase("too-many")]
		[TestCase("large-key")]
		public void InvalidOrMissingSaveEvidenceCannotRearmAdmission(string defect)
		{
			var entries = new List<KeyValuePair<string, long>>();
			var version = defect == "version" ? 2 : 1;
			var player = defect == "player" ? "foreign" : "player";
			var last = defect == "future" ? 101 : defect == "clock" ? -2 : 0;
			string key = null, owner = null;
			var start = -1;
			long expiry = -1;
			if (defect.StartsWith("active") || defect == "overflow")
			{
				key = "claim";
				owner = defect == "active-owner" ? null : "manager";
				start = defect == "active-start" ? 1 : 0;
				expiry = defect == "active-expiry" ? 0 : defect == "overflow" ? long.MaxValue : 1000;
			}
			if (defect == "idle-owner") owner = "manager";
			if (defect == "idle-start") start = 0;
			if (defect == "idle-expiry") expiry = 0;
			if (defect == "duplicates")
				entries.AddRange(new[] { new KeyValuePair<string, long>("a", 750), new KeyValuePair<string, long>("a", 750) });
			if (defect == "negative-cooldown") entries.Add(new KeyValuePair<string, long>("a", -1));
			if (defect == "large-key") entries.Add(new KeyValuePair<string, long>(new string('a', 513), 750));
			if (defect == "too-many")
				for (var i = 0; i < 129; i++) entries.Add(new KeyValuePair<string, long>("claim:" + i, 750));
			var snapshot = defect == "null" ? null : new TeamResponseBudgetSnapshotCA(version, player, last,
				key, owner, start, expiry, defect == "null-cooldowns" ? null : entries);
			var restored = new TeamResponseBudgetCA();
			Assert.That(restored.TryRestore(100, "player", snapshot), Is.False);
			Assert.That(restored.TryAcquire(100, "manager", "claim", 1500, true), Is.False);
			Assert.That(() => restored.Capture("player"), Throws.InvalidOperationException);
		}

		[Test]
		public void ASecondRestoreCannotEraseActiveStateOrCooldowns()
		{
			var empty = new TeamResponseBudgetCA().Capture("player");
			var source = new TeamResponseBudgetCA();
			source.TryAcquire(0, "manager", "claim", 1500, true);
			var restored = new TeamResponseBudgetCA();
			Assert.That(restored.TryRestore(100, "player", source.Capture("player")), Is.True);
			Assert.That(restored.TryRestore(100, "player", empty), Is.False);
			Assert.That(restored.ActiveKey, Is.EqualTo("claim"));
			Assert.That(restored.TryAcquire(100, "other", "fresh", 1500, true), Is.False);
		}

		[Test]
		public void ExpiredCooldownsPruneAtRestoreWithoutDroppingUnexpiredOnes()
		{
			var source = new TeamResponseBudgetCA();
			source.TryAcquire(0, "manager", "expired", 100, true);
			source.Release(0, "manager", 100);
			source.TryAcquire(1, "manager", "waiting", 100, true);
			source.Release(1, "manager", 750);
			var restored = new TeamResponseBudgetCA();
			Assert.That(restored.TryRestore(100, "player", source.Capture("player")), Is.True);
			Assert.That(restored.Capture("player").Cooldowns.Count, Is.EqualTo(1));
			Assert.That(restored.TryAcquire(100, "other", "waiting", 100, true), Is.False);
			Assert.That(restored.TryAcquire(100, "other", "expired", 100, true), Is.True);
		}

		[Test]
		public void MalformedRestoreCannotBeRetriedWithAnEmptySnapshot()
		{
			var budget = new TeamResponseBudgetCA();
			Assert.That(budget.TryRestore(100, "player", null), Is.False);
			Assert.That(budget.TryRestore(100, "player", new TeamResponseBudgetCA().Capture("player")), Is.False);
			Assert.That(budget.TryAcquire(100, "manager", "claim", 1500, true), Is.False);
		}

		[Test]
		public void LongDeadlinesSurviveWithoutIntegerWrapAndRestoreRejectsTimeRollback()
		{
			var source = new TeamResponseBudgetCA();
			Assert.That(source.TryAcquire(int.MaxValue - 1, "manager", "claim", int.MaxValue, true), Is.True);
			var snapshot = source.Capture("player");
			Assert.That(snapshot.ExpiresTick, Is.EqualTo(2L * int.MaxValue - 1));
			var restored = new TeamResponseBudgetCA();
			Assert.That(restored.TryRestore(int.MaxValue, "player", snapshot), Is.True);
			Assert.That(restored.ExpiresTick, Is.EqualTo(snapshot.ExpiresTick));
			Assert.That(() => restored.Evaluate(int.MaxValue - 1, true, true, true, true, false), Throws.InstanceOf<ArgumentOutOfRangeException>());
			Assert.That(new TeamResponseBudgetCA().TryRestore(1, "player", snapshot), Is.False);
		}

	}
}
