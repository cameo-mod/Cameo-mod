using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class ProductionSaturationTest
	{
		[Test]
		public void DisarmedThresholdIsNeverSaturated()
		{
			Assert.That(ProductionSaturationCA.Saturated(0, 0), Is.False);
			Assert.That(ProductionSaturationCA.Saturated(999999, 0), Is.False);
			Assert.That(ProductionSaturationCA.Saturated(999999, -1), Is.False);
			Assert.That(ProductionSaturationCA.Saturated(-5, 3000), Is.False);
		}

		[TestCase(2999, false)]
		[TestCase(3000, true)]
		[TestCase(3001, true)]
		public void SaturationTripsExactlyAtTheTierBar(long bank, bool expected)
		{
			Assert.That(ProductionSaturationCA.Saturated(bank, 3000), Is.EqualTo(expected));
		}

		[Test]
		public void OffKeepsTheLegacyMaximiseBreakBitIdentical()
		{
			Assert.That(ProductionSaturationCA.StopCategorySweep(false, 4999, 5000), Is.True);
			Assert.That(ProductionSaturationCA.StopCategorySweep(false, 5000, 5000), Is.False);
			Assert.That(ProductionSaturationCA.StopCategorySweep(false, 5001, 5000), Is.False);
		}

		[Test]
		public void SaturatedSweepNeverBreaksOnTheMaximiseBar()
		{
			Assert.That(ProductionSaturationCA.StopCategorySweep(true, 100, 5500), Is.False);
			Assert.That(ProductionSaturationCA.StopCategorySweep(true, 5499, 5500), Is.False);
		}

		[Test]
		public void WidthIsOneOffAndAtMostTwoOnUnlessTheProviderIsWider()
		{
			Assert.That(ProductionSaturationCA.QueuesPerCategory(false, 0), Is.EqualTo(1));
			Assert.That(ProductionSaturationCA.QueuesPerCategory(false, 1), Is.EqualTo(1));
			Assert.That(ProductionSaturationCA.QueuesPerCategory(false, 4), Is.EqualTo(4));
			Assert.That(ProductionSaturationCA.QueuesPerCategory(true, 0), Is.EqualTo(2));
			Assert.That(ProductionSaturationCA.QueuesPerCategory(true, 1), Is.EqualTo(2));
			Assert.That(ProductionSaturationCA.QueuesPerCategory(true, 2), Is.EqualTo(2));
			Assert.That(ProductionSaturationCA.QueuesPerCategory(true, 4), Is.EqualTo(4));
		}

		[Test]
		public void ShareFallbackHonoursEveryOtherGateAndKeepsOrder()
		{
			var buildable = new HashSet<string> { "a_tank", "b_tank", "c_tank" };
			var eligible = new HashSet<string> { "b_tank", "c_tank" };
			var pick = ProductionSaturationCA.PickExcludingShare(
				new[] { "z_missing", "a_tank", "b_tank" },
				buildable.Contains, eligible.Contains);
			Assert.That(pick, Is.EqualTo("b_tank"));
			Assert.That(ProductionSaturationCA.PickExcludingShare(
				new[] { "a_tank", "b_tank" }, buildable.Contains, _ => false), Is.Null);
			Assert.That(ProductionSaturationCA.PickExcludingShare(
				new[] { "z_missing" }, buildable.Contains, eligible.Contains), Is.Null);
			Assert.That(ProductionSaturationCA.PickExcludingShare(
				Enumerable.Empty<string>(), buildable.Contains, eligible.Contains), Is.Null);
		}
	}
}
