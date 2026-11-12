using System;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class StalemateBreakerPolicyTest
	{
		[Test]
		public void ClockStartsAt5000AndConfirmedKillResetsIt()
		{
			Assert.That(StalemateBreakerPolicyCA.ClockTrigger(true, 10999, -1), Is.False);
			Assert.That(StalemateBreakerPolicyCA.ClockTrigger(true, 11000, -1), Is.True);
			Assert.That(StalemateBreakerPolicyCA.ClockTrigger(true, 11999, 6000), Is.False);
			Assert.That(StalemateBreakerPolicyCA.ClockTrigger(true, 12000, 6000), Is.True);
			Assert.That(StalemateBreakerPolicyCA.ClockTrigger(false, 12000, 6000), Is.False);
			Assert.That(StalemateBreakerPolicyCA.ClockTrigger(true, 12000, 12001), Is.False, "kill after now = unknown provenance");
			Assert.That(StalemateBreakerPolicyCA.ClockTrigger(true, 12000, -2), Is.False);
		}

		[Test]
		public void ForceTriggerIsTheIntegerFormOfGeometricMeanAtLeastOnePointFive()
		{
			// 4 * numL * numT >= 9 * denL * denT; boundary 4*3*3 = 36 = 9*2*2.
			Assert.That(StalemateBreakerPolicyCA.ForceTrigger(true, 3, 2, 3, 2), Is.True);
			Assert.That(StalemateBreakerPolicyCA.ForceTrigger(true, 3, 2, 3, 2) == (4L * 3 * 3 >= 9L * 2 * 2), Is.True);

			// One below the boundary: 4*numL*numT = 9*denL*denT - 1 is impossible in small ints, so use 4*35 = 140 < 144 = 9*16.
			Assert.That(StalemateBreakerPolicyCA.ForceTrigger(true, 5, 4, 7, 4), Is.False);
			Assert.That(StalemateBreakerPolicyCA.ForceTrigger(true, 6, 4, 6, 4), Is.True);

			// Asymmetric: a strong local ratio compensates a weak team ratio (r_local 4, r_team 0.5625 => sqrt = 1.5).
			Assert.That(StalemateBreakerPolicyCA.ForceTrigger(true, 40000, 10000, 9000, 16000), Is.True);
			Assert.That(StalemateBreakerPolicyCA.ForceTrigger(true, 40000, 10000, 8999, 16000), Is.False);
			Assert.That(StalemateBreakerPolicyCA.ForceTrigger(false, 40000, 10000, 9000, 16000), Is.False);
		}

		[Test]
		public void ForceTriggerNeverFiresOnZeroOrUnknownDenominator()
		{
			Assert.That(StalemateBreakerPolicyCA.ForceTrigger(true, 50000, 0, 50000, 1000), Is.False, "stale/unseen target owner");
			Assert.That(StalemateBreakerPolicyCA.ForceTrigger(true, 50000, 1000, 50000, 0), Is.False, "no remembered enemy army");
			Assert.That(StalemateBreakerPolicyCA.ForceTrigger(true, 50000, -5, 50000, 1000), Is.False, "negative clamps to zero");
			Assert.That(StalemateBreakerPolicyCA.ScoreMilli(50000, 0, 50000, 1000), Is.EqualTo(-1));

			// The clock still can fire when the force test cannot.
			Assert.That(StalemateBreakerPolicyCA.ClockTrigger(true, 11000, -1), Is.True);
		}

		[Test]
		public void ForceInputsClampAtTwoToTheTwentyEightAndNeverOverflow()
		{
			var cap = StalemateBreakerPolicyCA.ForceInputCap;
			Assert.That(cap, Is.EqualTo((1L << 28) - 1));
			Assert.That(StalemateBreakerPolicyCA.ClampForceInput(long.MaxValue), Is.EqualTo(cap));
			Assert.That(() => StalemateBreakerPolicyCA.ForceTrigger(true, long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue), Throws.Nothing);
			Assert.That(StalemateBreakerPolicyCA.ForceTrigger(true, long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue), Is.False, "equal clamped ratio 1 < 1.5");
			Assert.That(StalemateBreakerPolicyCA.ForceTrigger(true, long.MaxValue, 1, long.MaxValue, 1), Is.True);
		}

		[Test]
		public void OneVersusOneIdentityWhenInputsCoincide()
		{
			// When eligible ground = own army and fresh seen = remembered, rLocal = rTeam = r and the trigger is r >= 1.5.
			foreach (var (own, enemy) in new[] { (15000L, 10000L), (14999L, 10000L), (30000L, 20000L), (29999L, 20000L), (1L, 1L) })
			{
				var expected = 2 * own >= 3 * enemy;
				Assert.That(StalemateBreakerPolicyCA.ForceTrigger(true, own, enemy, own, enemy), Is.EqualTo(expected), $"{own}/{enemy}");
				Assert.That(StalemateBreakerPolicyCA.ScoreMilli(own, enemy, own, enemy), Is.EqualTo(1000 * own / enemy), $"score {own}/{enemy}");
			}

			// When they do not coincide (air units in own army, old sightings in memory) the score is not r.
			Assert.That(StalemateBreakerPolicyCA.ScoreMilli(12000, 10000, 15000, 10000), Is.Not.EqualTo(1500));
		}

		[Test]
		public void MinimumShareRoundsUpAndRefusesIncompleteDraft()
		{
			Assert.That(StalemateBreakerPolicyCA.RequiredUnits(8), Is.EqualTo(5));
			Assert.That(StalemateBreakerPolicyCA.RequiredUnits(11), Is.EqualTo(7));
			Assert.Throws<ArgumentOutOfRangeException>(() => StalemateBreakerPolicyCA.RequiredUnits(-1));
			Assert.That(StalemateBreakerPolicyCA.Admit(true, true, false, 11, 7, 10000, 6000), Is.True);
			Assert.That(StalemateBreakerPolicyCA.Admit(true, true, false, 11, 6, 10000, 6000), Is.False);
			Assert.That(StalemateBreakerPolicyCA.Admit(true, true, false, 11, 7, 10000, 5999), Is.False);
			Assert.That(StalemateBreakerPolicyCA.Admit(true, true, true, 11, 7, 10000, 6000), Is.False);
			Assert.That(StalemateBreakerPolicyCA.Admit(false, true, false, 11, 7, 10000, 6000), Is.False);
			Assert.That(StalemateBreakerPolicyCA.Arrived(11, 10000, 7, 6000), Is.True);
			Assert.That(StalemateBreakerPolicyCA.Arrived(11, 10000, 7, 5999), Is.False);
			Assert.That(StalemateBreakerPolicyCA.MeetsValueShare(long.MaxValue, long.MaxValue), Is.True);
		}

		[Test]
		public void RetreatExemptionEndsOnSafetyOrTimeBoundaries()
		{
			bool Check(int now, int progress, long loss, bool targetDestroyed = false, bool emergency = false, bool complete = true) =>
				StalemateBreakerPolicyCA.DeferRetreat(true, complete, true, targetDestroyed, emergency, now, 100, progress, 1000, loss, 1500, 3000);
			Assert.That(Check(1599, 100, 499), Is.True);
			Assert.That(Check(1600, 100, 499), Is.False);
			Assert.That(Check(3099, 3000, 499), Is.True);
			Assert.That(Check(3100, 3000, 499), Is.False);
			Assert.That(Check(200, 100, 500), Is.False);
			Assert.That(Check(200, 100, 0, targetDestroyed: true), Is.False);
			Assert.That(Check(200, 100, 0, emergency: true), Is.False);
			Assert.That(Check(200, 100, 0, complete: false), Is.False);
		}

		[Test]
		public void DwellAndClampPreserveOffAndRespectBoundaries()
		{
			Assert.That(StalemateBreakerPolicyCA.MaySwitchPersonality(false, false, 2, 1), Is.True);
			Assert.That(StalemateBreakerPolicyCA.MaySwitchPersonality(true, false, 2999, 0), Is.False);
			Assert.That(StalemateBreakerPolicyCA.MaySwitchPersonality(true, false, 3000, 0), Is.True);
			Assert.That(StalemateBreakerPolicyCA.MaySwitchPersonality(true, true, 1, 0), Is.True);
			Assert.That(StalemateBreakerPolicyCA.SteamrollerValueBar(true, 10000), Is.EqualTo(10000));
			Assert.That(StalemateBreakerPolicyCA.SteamrollerValueBar(true, 20000), Is.EqualTo(15000));
			Assert.That(StalemateBreakerPolicyCA.SteamrollerOverflow(false, 60), Is.EqualTo(60));
			Assert.That(StalemateBreakerPolicyCA.SteamrollerOverflow(true, 60), Is.EqualTo(24));
		}

		[Test]
		public void LongRoutesScaleDurationsAndUnsupportedRoutesHold()
		{
			var route = new StalemateBreakerPolicyCA.RouteCA(2000, 256);
			Assert.That(StalemateBreakerPolicyCA.TryTiming(new[] { route }, out var timing), Is.True);
			Assert.That(timing.EstimatedTicks, Is.EqualTo(12000));
			Assert.That(timing.RallyDuration, Is.EqualTo(4000));
			Assert.That(timing.NoProgressDuration, Is.EqualTo(5000));
			Assert.That(timing.AbsoluteDuration, Is.EqualTo(28000));
			Assert.That(StalemateBreakerPolicyCA.TryTiming(new[] { new StalemateBreakerPolicyCA.RouteCA(2049, 1) }, out _), Is.False);
			Assert.That(StalemateBreakerPolicyCA.TryTiming(new[] { new StalemateBreakerPolicyCA.RouteCA(2048, 1) }, out _), Is.False);
			Assert.That(StalemateBreakerPolicyCA.TryTiming(new[] { new StalemateBreakerPolicyCA.RouteCA(1, 0) }, out _), Is.False);
			Assert.That(StalemateBreakerPolicyCA.TryTiming(Array.Empty<StalemateBreakerPolicyCA.RouteCA>(), out _), Is.False);
		}
	}
}
