using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class StalemateHomePressureTest
	{
		[Test]
		public void RequiresTwoQualifyingObservationsAndHoldsForFiveHundredTicks()
		{
			var latch = new StalemateHomePressureCA();
			Assert.That(latch.Observe(100, true, true), Is.True);
			Assert.That(latch.Observe(249, true, true), Is.True);
			Assert.That(latch.Active, Is.False);
			Assert.That(latch.Observe(250, true, true), Is.True);
			Assert.That(latch.Active, Is.True);
			Assert.That(latch.Observe(749, true, false), Is.True);
			Assert.That(latch.Active, Is.True);
			Assert.That(latch.Observe(750, true, false), Is.True);
			Assert.That(latch.Active, Is.False);
		}

		[Test]
		public void ClearBetweenSamplesResetsEntryAndUnknownCannotQualify()
		{
			var latch = new StalemateHomePressureCA();
			latch.Observe(100, true, true);
			latch.Observe(200, true, false);
			latch.Observe(250, true, true);
			Assert.That(latch.Active, Is.False);
			Assert.That(latch.Observe(400, false, true), Is.False);
			Assert.That(latch.Active, Is.False);
			Assert.That(latch.Observe(399, true, true), Is.True);
			Assert.That(latch.Active, Is.False);
			Assert.That(latch.Observe(400, true, true), Is.True);
			Assert.That(latch.Active, Is.True);
			Assert.That(latch.Observe(399, true, false), Is.False);
		}

		[Test]
		public void OnTheSnapshotGridReleaseLandsSixHundredTicksAfterTheLastQualifyingObservation()
		{
			// Design _08 N4: observations every 150 ticks; release at the first one with t - lastQualifying >= 500.
			var latch = new StalemateHomePressureCA();
			latch.Observe(1500, true, true);
			latch.Observe(1650, true, true);
			Assert.That(latch.Active, Is.True);
			latch.Observe(1800, true, false);
			latch.Observe(1950, true, false);
			latch.Observe(2100, true, false);
			Assert.That(latch.Active, Is.True, "450 ticks after the last qualifying observation");
			latch.Observe(2250, true, false);
			Assert.That(latch.Active, Is.False, "600 ticks after the last qualifying observation");
		}

		[Test]
		public void SaveLoadRoundTripsEveryField()
		{
			var latch = new StalemateHomePressureCA();
			latch.Observe(1500, true, true);
			latch.Observe(1650, true, true);
			latch.Observe(1800, true, false);
			var loaded = StalemateHomePressureCA.Load(new MiniYaml("", latch.Save()));
			Assert.That(loaded.Active, Is.EqualTo(latch.Active));
			Assert.That(loaded.FirstQualifyingTick, Is.EqualTo(latch.FirstQualifyingTick));
			Assert.That(loaded.LastQualifyingTick, Is.EqualTo(latch.LastQualifyingTick));
			Assert.That(loaded.LastObservedTick, Is.EqualTo(latch.LastObservedTick));
			loaded.Observe(2250, true, false);
			latch.Observe(2250, true, false);
			Assert.That(loaded.Active, Is.EqualTo(latch.Active));
		}
	}
}
