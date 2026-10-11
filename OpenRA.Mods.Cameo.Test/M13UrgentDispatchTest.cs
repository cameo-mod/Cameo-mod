using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class M13UrgentDispatchTest
	{
		static AttackLivenessEvalCA Waiting(AttackSoftRestraintCA holds = AttackSoftRestraintCA.None)
		{
			var clock = new AttackLivenessEvalCA();
			clock.Observe(100, true, true, true, true, 1000, 1000, 7500, 151, holds);
			return clock;
		}

		[TestCase(AttackSoftRestraintCA.ValueBar)]
		[TestCase(AttackSoftRestraintCA.CountBar)]
		[TestCase(AttackSoftRestraintCA.AttackDelay)]
		[TestCase(AttackSoftRestraintCA.Composition)]
		[TestCase(AttackSoftRestraintCA.Director)]
		[TestCase(AttackSoftRestraintCA.DefendMission)]
		[TestCase(AttackSoftRestraintCA.TeamResponse)]
		[TestCase(AttackSoftRestraintCA.Staging)]
		[TestCase(AttackSoftRestraintCA.FormationSync)]
		[TestCase(AttackSoftRestraintCA.CombatPolicy)]
		[TestCase(AttackSoftRestraintCA.Scheduler)]
		[TestCase((AttackSoftRestraintCA)2047)]
		public void AbsoluteCountCommitsOnceDespiteSoftHoldsWithoutRenewingDeadline(AttackSoftRestraintCA hold)
		{
			var clock = Waiting(hold);
			var calls = 0;
			Assert.That(M13LaunchPolicyCA.TryCommit(true, 35, 1000, 36, true, clock, () => calls++), Is.False);
			Assert.That(M13LaunchPolicyCA.TryCommit(true, 36, 999, 36, true, clock, () => calls++), Is.True);
			Assert.That(calls, Is.EqualTo(1));
			Assert.That(clock.DeadlineTick, Is.EqualTo(7600));
			Assert.That(clock.ObservedOffensiveCount, Is.Zero);
		}

		[Test]
		public void DeadlineNeedsLegalDraftFloorAndIntentDoesNotProveProgress()
		{
			var clock = Waiting();
			clock.Observe(7449, true, true, true, true, 1000, 9000, 9000, 200, (AttackSoftRestraintCA)2047);
			var calls = 0;
			Assert.That(clock.Phase, Is.EqualTo(AttackLivenessPhaseCA.DispatchDue));
			Assert.That(M13LaunchPolicyCA.TryCommit(true, 1, 999, 36, true, clock, () => calls++), Is.False);
			Assert.That(M13LaunchPolicyCA.TryCommit(true, 1, 1000, 36, true, clock,
				() => { calls++; clock.NoteDispatchIntent(1); }), Is.True);
			Assert.That(calls, Is.EqualTo(1));
			Assert.That(clock.DeadlineTick, Is.EqualTo(7600));
			Assert.That(clock.ObservedOffensiveCount, Is.Zero);
			Assert.That(M13LaunchPolicyCA.TryCommit(true, 1, 1000, 36, true, clock, () => calls++), Is.False);
			clock.NoteDispatchFailed(1);
			Assert.That(M13LaunchPolicyCA.TryCommit(true, 1, 1000, 36, true, clock, () => calls++), Is.True);
			Assert.That(clock.DeadlineTick, Is.EqualTo(7600));
		}

		[TestCase(false, true, 36)]
		[TestCase(true, false, 36)]
		[TestCase(true, true, 0)]
		public void OffOrHardRefusalDoesNotInvokeTheRealCommitBoundary(bool enabled, bool legal, int count)
		{
			Assert.That(M13LaunchPolicyCA.TryCommit(enabled, count, 1000, 36, legal, Waiting(),
				() => Assert.Fail("Rejected draft reached the commit boundary")), Is.False);
		}

		[Test]
		public void UnsupportedEvidenceCannotUseAbsoluteOverflowToDispatch()
		{
			var clock = Waiting();
			clock.Observe(200, false, true, true, true, 1000, 1000, 7500, 151, 0);
			Assert.That(M13LaunchPolicyCA.TryCommit(true, 36, 1000, 36, true, clock,
				() => Assert.Fail("Unsupported evidence dispatched")), Is.False);
		}

		[Test]
		public void SharedPlayerClockKeepsOriginalPersonalityFloorAndWaveSequence()
		{
			var player = new M13PlayerStateCA();
			var firstManager = player;
			var secondManager = player;
			firstManager.Liveness.Observe(0, true, true, true, true, 1000, 1000, 7500, 151, 0);
			secondManager.Liveness.Observe(7000, true, true, true, true, 1000, 5000, 9500, 151, 0);
			Assert.That(secondManager.Liveness.DeadlineTick, Is.EqualTo(7500));
			Assert.That(secondManager.Liveness.ValueFloor, Is.EqualTo(1000));
			Assert.That(firstManager.AllocateWave(), Is.EqualTo(1));
			Assert.That(secondManager.AllocateWave(), Is.EqualTo(2));
			Assert.That(new M13PlayerStateCA().AllocateWave(), Is.EqualTo(1));
		}

		[Test]
		public void UnknownRestoreEvidenceStaysUnsupportedAndDangerExpires()
		{
			var player = new M13PlayerStateCA();
			player.NoteBaseDamage(100, 50);
			Assert.That(player.BaseDangerUntilTick, Is.EqualTo(150));
			player.Invalidate();
			Assert.That(player.EvidenceComplete, Is.False);
			player.Liveness.Observe(200, player.EvidenceComplete, true, true, true, 1000, 1000, 7500, 151, 0);
			Assert.That(player.Liveness.Phase, Is.EqualTo(AttackLivenessPhaseCA.Unsupported));
			Assert.That(200 > player.BaseDangerUntilTick, Is.True);
		}
	}
}
