using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class M13TempoTest
	{
		static void Eligible(AttackLivenessEvalCA state, int tick, long floor = 1000,
			int noLaunch = 7500, int lead = 150, int firstWave = 0) =>
			state.Observe(tick, true, true, true, true, floor, floor, noLaunch, lead,
				AttackSoftRestraintCA.None, firstWave);

		[Test]
		public void FirstWaveCapOffIsByteIdentical()
		{
			Assert.That(AttackLivenessEvalCA.IntervalCap(7500, 0, 0), Is.EqualTo(7500));
			Assert.That(AttackLivenessEvalCA.IntervalCap(7500, -1, 0), Is.EqualTo(7500));
			var clock = new AttackLivenessEvalCA();
			Eligible(clock, 100);
			Assert.That(clock.DeadlineTick, Is.EqualTo(7600));
		}

		[Test]
		public void FirstWaveCapShortensOnlyBeforeTheFirstAcceptedWave()
		{
			Assert.That(AttackLivenessEvalCA.IntervalCap(7500, 3750, 0), Is.EqualTo(3750));
			Assert.That(AttackLivenessEvalCA.IntervalCap(7500, 3750, 1), Is.EqualTo(7500));
			Assert.That(AttackLivenessEvalCA.IntervalCap(7500, 9000, 0), Is.EqualTo(7500),
				"the cap can never exceed the tier's no-launch interval");
		}

		[Test]
		public void FirstWaveDeadlineKeepsTheDispatchLead()
		{
			var clock = new AttackLivenessEvalCA();
			Eligible(clock, 100, firstWave: 3750);
			Assert.That(clock.DeadlineTick, Is.EqualTo(3850));
			Assert.That(clock.Phase, Is.EqualTo(AttackLivenessPhaseCA.EligibleWaiting));
			Eligible(clock, 3699, firstWave: 3750);
			Assert.That(clock.Phase, Is.EqualTo(AttackLivenessPhaseCA.EligibleWaiting));
			Eligible(clock, 3700, firstWave: 3750);
			Assert.That(clock.Phase, Is.EqualTo(AttackLivenessPhaseCA.DispatchDue));
			Assert.That(clock.DeadlineTick, Is.EqualTo(3850));
		}

		[Test]
		public void FirstWaveNeverRenewsAnExistingIntervalDeadline()
		{
			var clock = new AttackLivenessEvalCA();
			Eligible(clock, 0, firstWave: 3750);
			Assert.That(clock.DeadlineTick, Is.EqualTo(3750));
			Eligible(clock, 100, noLaunch: 9500, firstWave: 9000);
			Assert.That(clock.DeadlineTick, Is.EqualTo(3750), "later observes never move the bar");
		}

		[Test]
		public void AfterTheFirstAcceptedWaveIntervalsUseTheFullCap()
		{
			var clock = new AttackLivenessEvalCA();
			Eligible(clock, 0, firstWave: 3750);
			Assert.That(clock.DeadlineTick, Is.EqualTo(3750));
			Assert.That(clock.ObserveOffensiveActivity(100, 1, 1, true, true, true, true), Is.True);
			Assert.That(clock.ObservedOffensiveCount, Is.EqualTo(1));
			Eligible(clock, 200, firstWave: 3750);
			Assert.That(clock.DeadlineTick, Is.EqualTo(7700), "post-first-wave intervals keep the tier's full no-launch cap");
		}

		[Test]
		public void FirstWaveShorteningStillDispatchesAtDeadline()
		{
			var clock = new AttackLivenessEvalCA();
			Eligible(clock, 100, firstWave: 3750);
			Eligible(clock, 3700, firstWave: 3750);
			var calls = 0;
			Assert.That(M13LaunchPolicyCA.TryCommit(true, 3, 1000, 36, true, clock, () => calls++), Is.True);
			Assert.That(calls, Is.EqualTo(1));
		}

		[Test]
		public void MinWaveFloorOffIsByteIdentical()
		{
			Assert.That(AttackForceEvalCA.RequiredSize(5, 0), Is.EqualTo(5));
			Assert.That(AttackForceEvalCA.RequiredSize(5, 8), Is.EqualTo(8));
			Assert.That(AttackForceEvalCA.RequiredSize(9, 8), Is.EqualTo(9));
		}

		[Test]
		public void MinWaveFloorOnlyRaisesTheOrdinaryCountGate()
		{
			// 5 units / plenty of value, floor 8: held.
			Assert.That(AttackForceEvalCA.ShouldLaunch(5, 12, 999999, 2500,
				AttackForceEvalCA.RequiredSize(3, 8), false, 2500), Is.False);
			// MaxIdleUnits overflow still launches the same small pool regardless of the floor.
			Assert.That(AttackForceEvalCA.ShouldLaunch(12, 12, 0, 2500,
				AttackForceEvalCA.RequiredSize(3, 8), false, 2500), Is.True);
			// ValueOnlyAttackLaunch keeps waiving the count gate entirely.
			Assert.That(AttackForceEvalCA.ShouldLaunch(1, 12, 999999, 2500,
				AttackForceEvalCA.RequiredSize(3, 8), true, 2500), Is.True);
		}

		[Test]
		public void MinWaveFloorNeverDefersTheLivenessDeadline()
		{
			// The urgent path takes no min-wave argument at all: DispatchDue commits a draft
			// smaller than any tier's TempoMinWaveUnits (hard = 8) as long as the value floor holds.
			var clock = new AttackLivenessEvalCA();
			Eligible(clock, 100, firstWave: 3750);
			Eligible(clock, 3700, firstWave: 3750);
			Assert.That(clock.Phase, Is.EqualTo(AttackLivenessPhaseCA.DispatchDue));
			var calls = 0;
			Assert.That(M13LaunchPolicyCA.TryCommit(true, 1, 1000, 36, true, clock, () => calls++), Is.True,
				"a 1-unit draft still dispatches at the first-wave deadline — the floor cannot defer it");
			Assert.That(calls, Is.EqualTo(1));
		}
	}
}
