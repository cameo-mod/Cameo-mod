using System;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class SquadAttackLivenessTest
	{
		static void Eligible(AttackLivenessEvalCA state, int tick, long floor = 1000,
			int delay = 7500, int lead = 150, AttackSoftRestraintCA restraint = AttackSoftRestraintCA.None,
			bool complete = true, long value = 1000) =>
			state.Observe(tick, complete, true, true, true, value, floor, delay, lead, restraint);

		[Test]
		public void RotatingEverySoftHoldAndPersonalityCannotRenewDeadlineOrFloor()
		{
			var state = new AttackLivenessEvalCA();
			Eligible(state, 0);
			for (var tick = 1; tick < 7350; tick++)
				Eligible(state, tick, 9000, 9500, 1, (AttackSoftRestraintCA)(1 << (tick % 11)));
			Assert.That(state.Phase, Is.EqualTo(AttackLivenessPhaseCA.EligibleWaiting));
			Eligible(state, 7350, 9000, 9500, 1);
			Assert.That(state.Phase, Is.EqualTo(AttackLivenessPhaseCA.DispatchDue));
			Assert.That(state.DeadlineTick, Is.EqualTo(7500));
			Assert.That(state.ValueFloor, Is.EqualTo(1000));
		}

		[Test]
		public void MissingEvidenceCannotRestartAnExistingInterval()
		{
			var state = new AttackLivenessEvalCA();
			Eligible(state, 0);
			Eligible(state, 7000, complete: false);
			Assert.That(state.Phase, Is.EqualTo(AttackLivenessPhaseCA.Unsupported));
			Eligible(state, 7500);
			Assert.That(state.Phase, Is.EqualTo(AttackLivenessPhaseCA.DispatchDue));
			Assert.That(state.EligibleSinceTick, Is.Zero);
		}

		[TestCase(false, true, true, 1000)]
		[TestCase(true, false, true, 1000)]
		[TestCase(true, true, false, 1000)]
		[TestCase(true, true, true, 999)]
		public void GenuineIneligibilityClosesInterval(bool active, bool target, bool orderable, long value)
		{
			var state = new AttackLivenessEvalCA();
			Eligible(state, 0);
			state.Observe(1, true, active, target, orderable, value, 1000, 7500, 150, 0);
			Assert.That(state.Phase, Is.EqualTo(AttackLivenessPhaseCA.Ineligible));
			Assert.That(state.EligibleSinceTick, Is.EqualTo(-1));
		}

		[TestCase(0, true, true, true, true)]
		[TestCase(1, false, true, true, true)]
		[TestCase(1, true, false, true, true)]
		[TestCase(1, true, true, false, true)]
		[TestCase(1, true, true, true, false)]
		public void EmptyEscortNoActivityNoTargetOrUnknownCannotVerifyWave(int members,
			bool owned, bool activity, bool target, bool complete)
		{
			var state = new AttackLivenessEvalCA();
			Eligible(state, 0);
			Assert.That(state.NoteDispatchIntent(1), Is.True);
			Assert.That(state.ObserveDispatch(1, 1, members, owned, activity, target, complete), Is.False);
			Assert.That(state.VerifiedLaunchCount, Is.Zero);
			Assert.That(state.EligibleSinceTick, Is.Zero);
		}

		[Test]
		public void FailedOrderKeepsDeadlineAndAllowsNewWaveVerifiedExactlyOnce()
		{
			var state = new AttackLivenessEvalCA();
			Eligible(state, 0);
			Eligible(state, 7350);
			Assert.That(state.NoteDispatchIntent(1), Is.True);
			Assert.That(state.NoteDispatchFailed(1), Is.True);
			Assert.That(state.NoteDispatchIntent(1), Is.False);
			Assert.That(state.DeadlineTick, Is.EqualTo(7500));
			Assert.That(state.NoteDispatchIntent(2), Is.True);
			Assert.That(state.ObserveDispatch(7500, 1, 1, true, true, true, true), Is.False);
			Assert.That(state.ObserveDispatch(7500, 2, 1, true, true, true, true), Is.True);
			Assert.That(state.ObserveDispatch(7500, 2, 1, true, true, true, true), Is.False);
			Assert.That(state.VerifiedLaunchCount, Is.EqualTo(1));
			Assert.That(state.LastVerifiedLaunchTick, Is.EqualTo(7500));
		}

		[TestCase(7000)]
		[TestCase(7500)]
		public void FailedIntentCannotRestoreEligibilityAfterEvidenceBecomesUnsupported(int tick)
		{
			var state = new AttackLivenessEvalCA();
			Eligible(state, 0);
			Assert.That(state.NoteDispatchIntent(1), Is.True);
			Eligible(state, tick, complete: false);
			Assert.That(state.Phase, Is.EqualTo(AttackLivenessPhaseCA.Unsupported));
			Assert.That(state.NoteDispatchFailed(1), Is.True);
			Assert.That(state.NoteDispatchFailed(1), Is.False);
			Assert.That(state.Phase, Is.EqualTo(AttackLivenessPhaseCA.Unsupported));
			Assert.That(state.NoteDispatchIntent(2), Is.False);
			Assert.That(state.EligibleSinceTick, Is.Zero);
			Assert.That(state.DeadlineTick, Is.EqualTo(7500));
			Assert.That(state.VerifiedLaunchCount, Is.Zero);
			Eligible(state, tick + 1);
			Assert.That(state.Phase, Is.EqualTo(tick < 7350
				? AttackLivenessPhaseCA.EligibleWaiting : AttackLivenessPhaseCA.DispatchDue));
			Assert.That(state.DeadlineTick, Is.EqualTo(7500));
			Assert.That(state.NoteDispatchIntent(2), Is.True);
		}

		[Test]
		public void PauseAndInvalidConfigurationCannotAdvanceOrCreateZeroFloorGuarantee()
		{
			var state = new AttackLivenessEvalCA();
			Eligible(state, 0, floor: 0);
			Assert.That(state.Phase, Is.EqualTo(AttackLivenessPhaseCA.Unsupported));
			Assert.That(state.EligibleSinceTick, Is.EqualTo(-1));
			Eligible(state, 1);
			Eligible(state, 1);
			Assert.That(state.DeadlineTick, Is.EqualTo(7501));
			Assert.Throws<ArgumentOutOfRangeException>(() => Eligible(state, 0));
		}

		[TestCase(35, false)]
		[TestCase(36, true)]
		[TestCase(360, true)]
		public void AbsoluteValveHasNoValueScaleParameter(int count, bool overflow)
		{
			Assert.That(AttackLivenessEvalCA.AbsoluteOverflow(count, 36), Is.EqualTo(overflow));
			Assert.That(AttackLivenessEvalCA.AbsoluteOverflow(count, 0), Is.False);
		}
	}
}
