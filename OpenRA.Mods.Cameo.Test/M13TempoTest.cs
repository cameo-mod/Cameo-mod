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

	}
}
