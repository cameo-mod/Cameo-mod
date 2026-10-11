using System;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class AttackLivenessRestoreTest
	{
		static void Observe(AttackLivenessEvalCA clock, int tick, bool complete = true) =>
			clock.Observe(tick, complete, true, true, true, 9000, 9000, 9000, 200, AttackSoftRestraintCA.Director);

		static AttackLivenessEvalCA Waiting()
		{
			var clock = new AttackLivenessEvalCA();
			clock.Observe(100, true, true, true, true, 1000, 1000, 7500, 150, AttackSoftRestraintCA.TeamResponse);
			return clock;
		}

		[Test]
		public void LoadedClockRetainsOriginalFloorDeadlineAndLeadAcrossPersonalityChange()
		{
			var restored = new AttackLivenessEvalCA();
			Assert.That(restored.TryRestore(7450, Waiting().Capture()), Is.True);
			Observe(restored, 7450);
			Assert.That(restored.EligibleSinceTick, Is.EqualTo(100));
			Assert.That(restored.ValueFloor, Is.EqualTo(1000));
			Assert.That(restored.DeadlineTick, Is.EqualTo(7600));
			Assert.That(restored.Phase, Is.EqualTo(AttackLivenessPhaseCA.DispatchDue));
			Assert.That(restored.Capture().DispatchLead, Is.EqualTo(150));
		}

		[Test]
		public void PendingIntentSurvivesExpiredDeadlineWithoutAcceptanceOrRenewal()
		{
			var saved = Waiting();
			saved.NoteDispatchIntent(7);
			var restored = new AttackLivenessEvalCA();
			Assert.That(restored.TryRestore(8000, saved.Capture()), Is.True);
			Assert.That(restored.NoteDispatchIntent(8), Is.False);
			Assert.That(restored.NoteDispatchFailed(6), Is.False);
			Assert.That(restored.NoteDispatchFailed(7), Is.True);
			Assert.That(restored.Phase, Is.EqualTo(AttackLivenessPhaseCA.DispatchDue));
			Assert.That(restored.NoteDispatchIntent(7), Is.False);
			Assert.That(restored.NoteDispatchIntent(8), Is.True);
			Assert.That(restored.DeadlineTick, Is.EqualTo(7600));
			Assert.That(restored.ObservedOffensiveCount, Is.Zero);
		}

		[Test]
		public void IncompleteSavedEvidenceStillNeedsCompleteObserveAfterFailedIntent()
		{
			var saved = Waiting();
			saved.NoteDispatchIntent(7);
			Observe(saved, 200, false);
			var restored = new AttackLivenessEvalCA();
			Assert.That(restored.TryRestore(8000, saved.Capture()), Is.True);
			restored.NoteDispatchFailed(7);
			Assert.That(restored.Phase, Is.EqualTo(AttackLivenessPhaseCA.Unsupported));
			Assert.That(restored.NoteDispatchIntent(8), Is.False);
			Observe(restored, 8000);
			Assert.That(restored.Phase, Is.EqualTo(AttackLivenessPhaseCA.DispatchDue));
			Assert.That(restored.DeadlineTick, Is.EqualTo(7600));
		}

		[Test]
		public void ObservedWaveCannotResetAnotherIntervalAfterLoad()
		{
			var saved = Waiting();
			Assert.That(saved.ObserveOffensiveActivity(200, 9, 2, true, true, true, true), Is.True);
			var restored = new AttackLivenessEvalCA();
			Assert.That(restored.TryRestore(300, saved.Capture()), Is.True);
			Observe(restored, 300);
			Assert.That(restored.ObserveOffensiveActivity(301, 9, 2, true, true, true, true), Is.False);
			Assert.That(restored.DeadlineTick, Is.EqualTo(9300));
			Assert.That(restored.ObserveOffensiveActivity(302, 10, 2, true, true, true, true, out var receipt), Is.True);
			Assert.That(receipt.PriorDeadlineTick, Is.EqualTo(9300));
			Assert.That(receipt.ObservedCount, Is.EqualTo(2));
		}

		[TestCase(0)]
		[TestCase(1)]
		[TestCase(2)]
		[TestCase(3)]
		[TestCase(4)]
		[TestCase(5)]
		[TestCase(6)]
		[TestCase(7)]
		[TestCase(8)]
		[TestCase(9)]
		[TestCase(10)]
		[TestCase(11)]
		public void CorruptRestoreFailsClosedEvenWhenFreshEvidenceArrives(int fault)
		{
			var valid = Waiting().Capture();
			var corrupt = fault switch
			{
				0 => null,
				1 => valid with { Version = 2 },
				2 => valid with { LastTick = 301 },
				3 => valid with { Phase = (AttackLivenessPhaseCA)99 },
				4 => valid with { Restraints = (AttackSoftRestraintCA)2048 },
				5 => valid with { PendingWave = 7, LastIssuedWave = 6 },
				6 => valid with { ValueFloor = 0 },
				7 => valid with { DeadlineTick = long.MaxValue },
				8 => valid with { DispatchLead = 7500 },
				9 => valid with { Phase = AttackLivenessPhaseCA.DispatchPending },
				10 => valid with { LastObservedWave = 7 },
				_ => valid with { EligibleSinceTick = -2 },
			};
			var restored = new AttackLivenessEvalCA();
			Assert.That(restored.TryRestore(300, corrupt), Is.False);
			Assert.That(restored.TryRestore(300, valid), Is.False);
			Observe(restored, 300);
			Assert.That(restored.Phase, Is.EqualTo(AttackLivenessPhaseCA.Unsupported));
			Assert.That(restored.NoteDispatchIntent(1), Is.False);
			Assert.That(restored.ObserveOffensiveActivity(301, 1, 2, true, true, true, true), Is.False);
			Assert.Throws<InvalidOperationException>(() => restored.Capture());
		}

		[Test]
		public void RejectedActivityMayAdvanceTickWithoutReevaluatingPhaseAndStillRoundTrips()
		{
			var saved = Waiting();
			Assert.That(saved.ObserveOffensiveActivity(8000, 0, 2, true, true, true, true), Is.False);
			Assert.That(saved.Phase, Is.EqualTo(AttackLivenessPhaseCA.EligibleWaiting));
			var restored = new AttackLivenessEvalCA();
			Assert.That(restored.TryRestore(8000, saved.Capture()), Is.True);
			Observe(restored, 8000);
			Assert.That(restored.Phase, Is.EqualTo(AttackLivenessPhaseCA.DispatchDue));
			Assert.That(restored.DeadlineTick, Is.EqualTo(7600));
		}

		[Test]
		public void InitialAndClearedClocksRoundTripAndRestoreCannotOverwriteLiveState()
		{
			var initial = new AttackLivenessEvalCA();
			Assert.That(initial.TryRestore(0, initial.Capture()), Is.True);
			Assert.That(initial.TryRestore(0, Waiting().Capture()), Is.False);
			Assert.That(initial.Phase, Is.EqualTo(AttackLivenessPhaseCA.Ineligible));
			var cleared = Waiting();
			cleared.Observe(200, true, false, true, true, 1000, 1000, 7500, 150, AttackSoftRestraintCA.None);
			var loaded = new AttackLivenessEvalCA();
			Assert.That(loaded.TryRestore(300, cleared.Capture()), Is.True);
			Observe(loaded, 300);
			Assert.That(loaded.EligibleSinceTick, Is.EqualTo(300));
			Assert.That(loaded.DeadlineTick, Is.EqualTo(9300));
			Assert.That(loaded.TryRestore(300, Waiting().Capture()), Is.False);
			Assert.That(loaded.DeadlineTick, Is.EqualTo(9300));
		}
	}
}
