#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>ENGDROP stage 2: scouted-memory area intel (visibility stamps, threat summary) and the hard exclusion geometry. No World.</summary>
	[TestFixture]
	public sealed class EngDropAreaIntelTest
	{
		static readonly CPos Target = new(50, 50);
		static readonly IReadOnlyList<CPos> Corridor = VisibilityStamps.Corridor(new CPos(10, 10), Target, 4, 64);
		static readonly IReadOnlyList<CPos> Ring = VisibilityStamps.Ring(Target, 6, 24);

		[Test]
		public void SampleSetIsDeterministicBoundedAndStartsAtTheCenter()
		{
			var (a, truncatedA) = VisibilityStamps.SampleCells(Target, 6, Corridor, Ring);
			var (b, _) = VisibilityStamps.SampleCells(Target, 6, Corridor, Ring);
			Assert.That(a, Is.EqualTo(b));
			Assert.That(a.Length, Is.LessThanOrEqualTo(VisibilityStamps.MaxSamples));
			Assert.That(a[0], Is.EqualTo(Target));
			Assert.That(a.Distinct().Count(), Is.EqualTo(a.Length), "every sample distinct");
			Assert.That(truncatedA, Is.False);
		}

		[Test]
		public void TooManyApproachCellsTruncateAndTheAreaIsNeverKnown()
		{
			var many = Enumerable.Range(0, 400).Select(i => new CPos(i, 3)).ToList();
			var (samples, truncated) = VisibilityStamps.SampleCells(Target, 6, many, Ring);
			Assert.That(truncated, Is.True);
			Assert.That(samples.Length, Is.LessThanOrEqualTo(VisibilityStamps.MaxSamples));

			var stamps = new VisibilityStamps();
			stamps.Observe(Target, 6, many, Ring, _ => true, 100);
			Assert.That(stamps.IsKnown(Target, 6, many, Ring, 100, 2500, out _), Is.False, "clipped coverage is unknown, not safe");
		}

		[Test]
		public void KnownNeedsEverySampleSeenWithinTheFreshnessWindow()
		{
			var stamps = new VisibilityStamps();
			stamps.Observe(Target, 6, Corridor, Ring, _ => true, 1000);
			Assert.That(stamps.IsKnown(Target, 6, Corridor, Ring, 1000, 2500, out var oldest), Is.True);
			Assert.That(oldest, Is.EqualTo(1000));
			Assert.That(stamps.IsKnown(Target, 6, Corridor, Ring, 3500, 2500, out _), Is.True, "exactly at the window edge");
			Assert.That(stamps.IsKnown(Target, 6, Corridor, Ring, 3501, 2500, out _), Is.False, "stale");
		}

		[Test]
		public void OneUnseenSampleMakesTheWholeAreaUnknown()
		{
			var (samples, _) = VisibilityStamps.SampleCells(Target, 6, Corridor, Ring);
			var hole = samples[samples.Length / 2];
			var stamps = new VisibilityStamps();
			stamps.Observe(Target, 6, Corridor, Ring, c => c != hole, 1000);
			Assert.That(stamps.IsKnown(Target, 6, Corridor, Ring, 1000, 2500, out _), Is.False);

			// Seeing the hole later completes the picture; older stamps of the other cells still count inside the window.
			stamps.Observe(Target, 6, Corridor, Ring, c => c == hole, 1200);
			Assert.That(stamps.IsKnown(Target, 6, Corridor, Ring, 1200, 2500, out var oldest), Is.True);
			Assert.That(oldest, Is.EqualTo(1000));
		}

		[Test]
		public void AChangedCorridorResetsTheStamps()
		{
			var stamps = new VisibilityStamps();
			stamps.Observe(Target, 6, Corridor, Ring, _ => true, 1000);
			var other = VisibilityStamps.Corridor(new CPos(90, 10), Target, 4, 64);
			Assert.That(stamps.IsKnown(Target, 6, other, Ring, 1000, 2500, out _), Is.False, "no table for this sample set");
		}

		[Test]
		public void TheTableKeepsAtMostSixteenTargets()
		{
			var stamps = new VisibilityStamps();
			for (var i = 0; i < 20; i++)
				stamps.Observe(new CPos(20 + i * 10, 40), 3, [], [], _ => true, 10);

			Assert.That(stamps.IsKnown(new CPos(20, 40), 3, [], [], 10, 2500, out _), Is.False, "the oldest four were evicted");
			Assert.That(stamps.IsKnown(new CPos(20 + 19 * 10, 40), 3, [], [], 10, 2500, out _), Is.True);
		}

		[Test]
		public void ThreatSummaryCountsTheAreaTheCorridorAndTheLandingButNotFarAway()
		{
			var (samples, _) = VisibilityStamps.SampleCells(Target, 6, Corridor, Ring);
			var inArea = new AreaThreat(new CPos(52, 51), 500, 4, true, false, false, false, 0);
			var onCorridor = new AreaThreat(Corridor[Corridor.Count / 2], 300, 0, false, true, false, false, 0);
			var antiAir = new AreaThreat(new CPos(49, 53), 700, 8, true, false, true, false, 0);
			var far = new AreaThreat(new CPos(200, 200), 9999, 5, true, true, true, false, 0);
			var detector = new AreaThreat(new CPos(60, 50), 100, 0, false, false, false, true, 12);

			var intel = VisibilityStamps.Summarize(true, 1000, Target, 6, samples, [inArea, onCorridor, antiAir, far, detector]);
			Assert.That(intel.DefenceValue, Is.EqualTo(1200));
			Assert.That(intel.ArmyValue, Is.EqualTo(300));
			Assert.That(intel.AntiAirValue, Is.EqualTo(700));
			Assert.That(intel.MaxDefenceRangeCells, Is.EqualTo(8));
			Assert.That(intel.Detectors, Is.EqualTo(1));
			Assert.That(intel.Undefended, Is.False);
		}

		[Test]
		public void UndefendedNeedsKnownCoverageAndZeroThreat()
		{
			var (samples, _) = VisibilityStamps.SampleCells(Target, 6, Corridor, Ring);
			Assert.That(VisibilityStamps.Summarize(true, 1000, Target, 6, samples, []).Undefended, Is.True);
			Assert.That(VisibilityStamps.Summarize(false, 0, Target, 6, samples, []).Undefended, Is.False, "unknown is never undefended");
			Assert.That(VisibilityStamps.Summarize(true, 1000, Target, 6, samples,
				[new AreaThreat(Target, 50, 0, false, true, false, false, 0)]).Undefended, Is.False);
		}

		// Two hidden worlds with equal observation give equal answers: the summary reads only the observation inputs.
		[Test]
		public void EqualObservationGivesEqualAssessment()
		{
			var (samples, _) = VisibilityStamps.SampleCells(Target, 6, Corridor, Ring);
			var a = VisibilityStamps.Summarize(true, 900, Target, 6, samples, [new AreaThreat(new CPos(120, 5), 400, 3, true, false, false, false, 0)]);
			var b = VisibilityStamps.Summarize(true, 900, Target, 6, samples, [new AreaThreat(new CPos(5, 120), 800, 3, true, false, false, false, 0)]);
			Assert.That(a, Is.EqualTo(b));
		}

		static readonly WPos Origin = new(0, 0, 0);

		[Test]
		public void AntiAirZonesBarAircraftOnlyAndGroundZonesBarEveryLanding()
		{
			var aa = new DropExclusion(new WPos(10000, 0, 0), 2000, DropExclusionKind.AntiAir);
			var ground = new DropExclusion(new WPos(0, 10000, 0), 2000, DropExclusionKind.Ground);
			var constraints = new DropConstraints([ground, aa], 500);

			Assert.That(constraints.RejectsPoint(new WPos(10500, 0, 0), air: true), Is.True);
			Assert.That(constraints.RejectsPoint(new WPos(10500, 0, 0), air: false), Is.False, "ground craft ignore anti-air");
			Assert.That(constraints.RejectsPoint(new WPos(0, 10500, 0), air: false), Is.True, "a ground zone bars every landing");
			Assert.That(constraints.RejectsPoint(new WPos(0, 10500, 0), air: true), Is.True);
			Assert.That(constraints.RejectsPoint(new WPos(30000, 30000, 0), air: true), Is.False);
		}

		[Test]
		public void AnAirCorridorCrossingAnAntiAirZoneIsRejected()
		{
			var constraints = new DropConstraints([new DropExclusion(new WPos(10000, 500, 0), 1000, DropExclusionKind.AntiAir)], 0);
			Assert.That(constraints.RejectsSegment(Origin, new WPos(20000, 0, 0), air: true), Is.True, "passes within the radius");
			Assert.That(constraints.RejectsSegment(Origin, new WPos(20000, 0, 0), air: false), Is.False, "ground routes are the provider's own search");
			Assert.That(constraints.RejectsSegment(Origin, new WPos(20000, 5000, 0), air: true), Is.False, "clears the zone");
			Assert.That(constraints.RejectsSegment(new WPos(12000, 5000, 0), new WPos(20000, 5000, 0), air: true), Is.False, "starts beyond the zone");
		}

		[Test]
		public void ZonesAreStoredInAFixedOrderAndNoneRejectsNothing()
		{
			var z1 = new DropExclusion(new WPos(5, 9, 0), 100, DropExclusionKind.Ground);
			var z2 = new DropExclusion(new WPos(1, 1, 0), 100, DropExclusionKind.AntiAir);
			var z3 = new DropExclusion(new WPos(1, 1, 0), 50, DropExclusionKind.Ground);
			Assert.That(new DropConstraints([z1, z2, z3], 0).Zones, Is.EqualTo(new DropConstraints([z3, z1, z2], 0).Zones));
			Assert.That(DropConstraints.None.RejectsPoint(Origin, true), Is.False);
			Assert.That(DropConstraints.None.RejectsSegment(Origin, new WPos(9, 9, 0), true), Is.False);
		}
	}
}
