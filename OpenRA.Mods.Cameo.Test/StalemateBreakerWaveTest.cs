using System;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class StalemateBreakerWaveTest
	{
		static readonly StalemateBreakerPolicyCA.TimingCA Short = new(1000, 750, 1500, 30000);

		static StalemateWaveMemberCA[] Members(int count, int cost = 100) =>
			Enumerable.Range(1, count).Select(i => new StalemateWaveMemberCA((uint)i, 1, cost)).ToArray();

		static StalemateBreakerWaveCA Wave(int c = 0, StalemateBreakerPolicyCA.TimingCA? timing = null,
			int count = 10, int n0 = 10, long v0 = 1000, int routeEstL = 1000) =>
			new(c, Members(count), timing ?? Short, n0, v0, routeEstL, 1);

		/// <summary>Real TryTiming output for a single route whose estimate is exactly e.</summary>
		static StalemateBreakerPolicyCA.TimingCA TimingFor(int e)
		{
			for (var l = 1; l <= 2048; l++)
			{
				var s = (int)((1536L * l + e - 1) / e);
				if (s <= 0 || (1536L * l + s - 1) / s != e)
					continue;
				Assert.That(StalemateBreakerPolicyCA.TryTiming(new[] { new StalemateBreakerPolicyCA.RouteCA(l, s) },
					out var timing), Is.True);
				Assert.That(timing.EstimatedTicks, Is.EqualTo(e));
				return timing;
			}

			throw new InvalidOperationException("No route gives E = " + e);
		}

		/// <summary>
		/// Per-tick driver: deadlines every tick, rally end (all members assembled) at the rally deadline,
		/// and a sample on every grid tick. Returns the tick the wave ended at, or -1.
		/// </summary>
		static int Run(StalemateBreakerWaveCA wave, int from, int to, Func<int, int?> remaining,
			Func<int, bool> damage = null)
		{
			for (var tick = from; tick <= to; tick++)
			{
				wave.CheckDeadlines(tick);
				if (tick == wave.RallyDeadline && wave.IsActive)
					wave.RallyEnd(tick, wave.InitialCount, wave.InitialValue);
				if (wave.IsActive && wave.IsSampleTick(tick))
					wave.ObserveSample(tick, remaining(tick), damage != null && damage(tick));
				if (!wave.IsActive)
					return tick;
			}

			return -1;
		}

		static string Saved(StalemateBreakerWaveCA wave) => wave.Save().WriteToString();

		static StalemateBreakerWaveCA RoundTrip(StalemateBreakerWaveCA wave)
		{
			var text = Saved(wave);
			var nodes = MiniYaml.FromString(text, "wave-test");
			var loaded = StalemateBreakerWaveCA.Load(new MiniYaml("", nodes));
			Assert.That(Saved(loaded), Is.EqualTo(text));
			return loaded;
		}

		[Test]
		public void RallyPassesAtExactSixtyPercentOfN0AndV0()
		{
			// 8 committed of N0 = 10 (2 unreachable), V0 = 1000: 6 units and 600 credits are both required.
			var pass = Wave(count: 8, n0: 10, v0: 1000);
			Assert.That(pass.RallyEnd(750, 6, 600), Is.True);
			Assert.That(pass.Phase, Is.EqualTo(StalemateWavePhaseCA.Advancing));

			var shortCount = Wave(count: 8, n0: 10, v0: 1000);
			Assert.That(shortCount.RallyEnd(750, 5, 800), Is.False);
			Assert.That(shortCount.End, Is.EqualTo(StalemateWaveEndCA.RallyFailed));

			var shortValue = Wave(count: 8, n0: 10, v0: 1000);
			Assert.That(shortValue.RallyEnd(750, 6, 599), Is.False);
			Assert.That(shortValue.End, Is.EqualTo(StalemateWaveEndCA.RallyFailed));

			// ceil(0.6 x 1001) = 601.
			var odd = Wave(count: 8, n0: 10, v0: 1001);
			Assert.That(odd.RallyEnd(750, 8, 600), Is.False);
			var oddPass = Wave(count: 8, n0: 10, v0: 1001);
			Assert.That(oddPass.RallyEnd(750, 7, 601), Is.True);
		}

		[Test]
		public void RallyEndOffDeadlineOrMissedIsMissingEvidence()
		{
			var early = Wave();
			Assert.That(early.RallyEnd(749, 10, 1000), Is.False);
			Assert.That(early.End, Is.EqualTo(StalemateWaveEndCA.MissingEvidence));

			var missed = Wave();
			Assert.That(missed.CheckDeadlines(750), Is.True);
			Assert.That(missed.CheckDeadlines(751), Is.False);
			Assert.That(missed.End, Is.EqualTo(StalemateWaveEndCA.MissingEvidence));

			var overCount = Wave(count: 8);
			Assert.That(overCount.RallyEnd(750, 9, 800), Is.False);
			Assert.That(overCount.End, Is.EqualTo(StalemateWaveEndCA.MissingEvidence));
		}

		[TestCase(750, 850)]
		[TestCase(3334, 3475)]
		[TestCase(5000, 5125)]
		public void ArmTickIsFirstGridTickAtOrAfterRallyEnd(int rally, int expectedArm)
		{
			var wave = Wave(100, new StalemateBreakerPolicyCA.TimingCA(1000, rally, 1500, 30000));
			Assert.That(wave.RallyDeadline, Is.EqualTo(100 + rally));
			Assert.That(wave.ArmTick, Is.EqualTo(expectedArm));
			Assert.That(wave.AbsoluteDeadline, Is.EqualTo(30100));
		}

		[Test]
		public void HealthyProgressNeverTimesOutAndDeadlineIsExact()
		{
			var wave = Wave(routeEstL: 2000);
			var end = Run(wave, 1, 40000, t => 2000 - 2 * (t / 75));
			Assert.That(end, Is.EqualTo(30000));
			Assert.That(wave.End, Is.EqualTo(StalemateWaveEndCA.Deadline));
			Assert.That(wave.LastProgressTick, Is.EqualTo(29925));
		}

		[TestCase(1500, 3000)]
		[TestCase(1550, 3075)]
		public void StallTimesOutExactlyOnTheGrid(int noProgress, int expectedEnd)
		{
			var wave = Wave(timing: new StalemateBreakerPolicyCA.TimingCA(1000, 750, noProgress, 30000));
			Func<int, int?> l = t => t <= 1500 ? 500 - 2 * (t / 75) : 480;
			Assert.That(Run(wave, 1, expectedEnd - 75, l), Is.EqualTo(-1));
			Assert.That(wave.LastProgressTick, Is.EqualTo(1500));
			Assert.That(wave.IsActive, Is.True);
			Assert.That(Run(wave, expectedEnd - 74, expectedEnd, l), Is.EqualTo(expectedEnd));
			Assert.That(wave.End, Is.EqualTo(StalemateWaveEndCA.NoProgress));
			Assert.That(expectedEnd, Is.EqualTo(1500 + 75 * ((noProgress + 74) / 75)));
		}

		[TestCase(12000, 4000, 28000, 4050)]
		[TestCase(15000, 5000, 30000, 5025)]
		public void LongRouteTimingArmsAtRallyEndAndTimesOutOnGrid(int e, int rally, int absolute, int arm)
		{
			var timing = TimingFor(e);
			Assert.That(timing.RallyDuration, Is.EqualTo(rally));
			Assert.That(timing.NoProgressDuration, Is.EqualTo(5000));
			Assert.That(timing.AbsoluteDuration, Is.EqualTo(absolute));

			const int c = 300;
			var wave = Wave(c, timing);
			Assert.That(wave.ArmTick, Is.EqualTo(c + arm));

			// No progress at all after the arm point: timeout exactly at R + 75 x ceil(5000 / 75) = R + 5025.
			Assert.That(Run(wave, c + 1, wave.ArmTick + 4950, _ => 900), Is.EqualTo(-1));
			Assert.That(wave.LastProgressTick, Is.EqualTo(wave.ArmTick));
			Assert.That(wave.BestL, Is.EqualTo(900));
			Assert.That(Run(wave, wave.ArmTick + 4951, wave.ArmTick + 5025, _ => 900), Is.EqualTo(wave.ArmTick + 5025));
			Assert.That(wave.End, Is.EqualTo(StalemateWaveEndCA.NoProgress));
		}

		// LA: the absolute clamp. E = 12500 is below the clamp (rally 4167 + 2E = 29167); 13000 and 18000 clamp to 30000.
		[TestCase(12500, 4167, 29167)]
		[TestCase(12856, 4286, 29998)]    // Sonnet D09 L1: just below the clamp, R = C + 4350
		[TestCase(12858, 4286, 30000)]    // Sonnet D09 L1: first clamped value, R = C + 4350
		[TestCase(13000, 4334, 30000)]
		[TestCase(18000, 5000, 30000)]    // Sonnet D09 L1: R = C + 5025
		public void AbsoluteDeadlineIsExactIncludingTheClamp(int e, int rally, int absolute)
		{
			var timing = TimingFor(e);
			Assert.That(timing.RallyDuration, Is.EqualTo(rally));
			Assert.That(timing.AbsoluteDuration, Is.EqualTo(absolute));
			Assert.That(absolute, Is.LessThanOrEqualTo(rally + 2 * e));

			const int c = 1000;
			var wave = Wave(c, timing, routeEstL: 2000);
			Assert.That(wave.ArmTick, Is.EqualTo(c + 75 * ((rally + 74) / 75)));
			Func<int, int?> l = t => 2000 - 2 * ((t - c) / 75);
			Assert.That(Run(wave, c + 1, c + absolute - 1, l), Is.EqualTo(-1));
			Assert.That(Run(wave, c + absolute, c + absolute + 100, l), Is.EqualTo(c + absolute));
			Assert.That(wave.End, Is.EqualTo(StalemateWaveEndCA.Deadline));
		}

		[Test]
		public void PostArrivalDamageKeepsAliveAndSilenceTimesOut()
		{
			var all = Members(10).Select(m => (m.ActorId, m.Generation)).ToArray();

			var fighting = Wave();
			Assert.That(Run(fighting, 1, 900, t => 100 - 2 * (t / 75)), Is.EqualTo(-1));
			Assert.That(fighting.ObserveArrival(all), Is.True);
			Assert.That(Run(fighting, 901, 29999, _ => 0, _ => true), Is.EqualTo(-1));
			Assert.That(Run(fighting, 30000, 30000, _ => 0, _ => true), Is.EqualTo(30000));
			Assert.That(fighting.End, Is.EqualTo(StalemateWaveEndCA.Deadline));

			var idle = Wave();
			Assert.That(Run(idle, 1, 900, t => 100 - 2 * (t / 75)), Is.EqualTo(-1));
			Assert.That(idle.LastProgressTick, Is.EqualTo(900));
			Assert.That(idle.ObserveArrival(all), Is.True);

			// Arrived: even a 2-cell improvement is not progress, only damage or kills are.
			Assert.That(Run(idle, 901, 2400, t => 76 - 2 * ((t - 900) / 75)), Is.EqualTo(2400));
			Assert.That(idle.End, Is.EqualTo(StalemateWaveEndCA.NoProgress));
		}

		static StalemateBreakerWaveCA ProgressedTo1500()
		{
			var wave = Wave();
			Assert.That(Run(wave, 1, 1500, t => 500 - 2 * (t / 75)), Is.EqualTo(-1));
			Assert.That(wave.LastProgressTick, Is.EqualTo(1500));
			Assert.That(wave.BestL, Is.EqualTo(460));
			return wave;
		}

		[Test]
		public void RetargetToFartherTargetKeepsTheOriginalClock()
		{
			var wave = ProgressedTo1500();
			Assert.That(wave.TryRetarget(1520, 900, 700), Is.True);
			Assert.That(wave.BestL, Is.EqualTo(700));
			Assert.That(wave.LastProgressTick, Is.EqualTo(1500));
			Assert.That(wave.AbsoluteDeadline, Is.EqualTo(30000));

			// A one-cell improvement is not progress.
			Assert.That(Run(wave, 1521, 2925, _ => 699), Is.EqualTo(-1));
			Assert.That(wave.BestL, Is.EqualTo(700));
			Assert.That(Run(wave, 2926, 3000, _ => 699), Is.EqualTo(3000));
			Assert.That(wave.End, Is.EqualTo(StalemateWaveEndCA.NoProgress));
		}

		[Test]
		public void RetargetThenTwoCellImprovementIsProgress()
		{
			var wave = ProgressedTo1500();
			Assert.That(wave.TryRetarget(1500, 1000, 700), Is.True);
			Assert.That(wave.ObserveSample(1575, 698, false), Is.True);
			Assert.That(wave.LastProgressTick, Is.EqualTo(1575));
			Assert.That(wave.BestL, Is.EqualTo(698));
		}

		[Test]
		public void SecondRetargetOrLargerEFails()
		{
			var twice = ProgressedTo1500();
			Assert.That(twice.TryRetarget(1500, 500, 600), Is.True);
			Assert.That(twice.TryRetarget(1510, 500, 600), Is.False);
			Assert.That(twice.End, Is.EqualTo(StalemateWaveEndCA.RetargetFailed));

			var farther = ProgressedTo1500();
			Assert.That(farther.TryRetarget(1500, 1001, 600), Is.False);
			Assert.That(farther.End, Is.EqualTo(StalemateWaveEndCA.RetargetFailed));
		}

		[Test]
		public void RetargetClearsArrival()
		{
			var wave = ProgressedTo1500();
			Assert.That(wave.ObserveArrival(Members(10).Select(m => (m.ActorId, m.Generation))), Is.True);
			Assert.That(wave.TryRetarget(1500, 1000, 700), Is.True);
			Assert.That(wave.Arrived, Is.False);
			Assert.That(wave.RetargetUsed, Is.True);
		}

		[Test]
		public void LeaderChangeRebasesBestLWithoutResettingTheClock()
		{
			var wave = ProgressedTo1500();
			Assert.That(wave.LeaderChange(1510, 2, 50), Is.True);
			Assert.That(wave.LeaderId, Is.EqualTo(2u));
			Assert.That(wave.BestL, Is.EqualTo(50));
			Assert.That(wave.LastProgressTick, Is.EqualTo(1500));
			Assert.That(wave.ObserveSample(1575, 49, false), Is.True);
			Assert.That(wave.LastProgressTick, Is.EqualTo(1500));
			Assert.That(wave.ObserveSample(1650, 48, false), Is.True);
			Assert.That(wave.LastProgressTick, Is.EqualTo(1650));

			var deadLeader = ProgressedTo1500();
			deadLeader.RecordDeath(3, 1, true);
			Assert.That(deadLeader.LeaderChange(1510, 3, 50), Is.False);
			Assert.That(deadLeader.End, Is.EqualTo(StalemateWaveEndCA.IdentityChanged));
		}

		[Test]
		public void OffGridOrNonContiguousSampleIsMissingEvidence()
		{
			var offGrid = Wave(100);
			Assert.That(offGrid.ObserveSample(175, 900, false), Is.True);
			Assert.That(offGrid.ObserveSample(251, 900, false), Is.False);
			Assert.That(offGrid.End, Is.EqualTo(StalemateWaveEndCA.MissingEvidence));

			var skipped = Wave(100);
			Assert.That(skipped.ObserveSample(250, 900, false), Is.False);
			Assert.That(skipped.End, Is.EqualTo(StalemateWaveEndCA.MissingEvidence));

			var nullAtArm = Wave();
			Assert.That(Run(nullAtArm, 1, 749, _ => 900), Is.EqualTo(-1));
			nullAtArm.RallyEnd(750, 10, 1000);
			Assert.That(nullAtArm.ObserveSample(750, null, false), Is.False);
			Assert.That(nullAtArm.End, Is.EqualTo(StalemateWaveEndCA.MissingEvidence));
		}

		[Test]
		public void NullRemainingBeforeArrivalIsNoProgress()
		{
			var wave = Wave();
			Assert.That(Run(wave, 1, 750, _ => 900), Is.EqualTo(-1));
			Assert.That(Run(wave, 751, 2175, _ => null), Is.EqualTo(-1));
			Assert.That(wave.BestL, Is.EqualTo(900));
			Assert.That(Run(wave, 2176, 2250, _ => null), Is.EqualTo(2250));
			Assert.That(wave.End, Is.EqualTo(StalemateWaveEndCA.NoProgress));
		}

		[Test]
		public void ProgressBeforeArmIsIgnored()
		{
			var wave = Wave(timing: new StalemateBreakerPolicyCA.TimingCA(1000, 3334, 1500, 30000), routeEstL: 3000);
			Assert.That(Run(wave, 1, 3374, t => 3000 - 10 * (t / 75)), Is.EqualTo(-1));
			Assert.That(wave.LastProgressTick, Is.EqualTo(0));
			Assert.That(wave.Phase, Is.EqualTo(StalemateWavePhaseCA.Advancing));
			Assert.That(Run(wave, 3375, 3375, _ => 2600), Is.EqualTo(-1));
			Assert.That(wave.Phase, Is.EqualTo(StalemateWavePhaseCA.Armed));
			Assert.That(wave.LastProgressTick, Is.EqualTo(3375));
			Assert.That(wave.BestL, Is.EqualTo(2600));
			Assert.That(wave.RouteBestL, Is.EqualTo(2560));
		}

		[Test]
		public void CasualtiesEndAtMoreThanHalfOfInitialValue()
		{
			var wave = new StalemateBreakerWaveCA(0, new[]
			{
				new StalemateWaveMemberCA(1, 1, 249),
				new StalemateWaveMemberCA(2, 1, 1),
				new StalemateWaveMemberCA(3, 1, 250)
			}, Short, 3, 500, 10, 1);
			Assert.That(wave.RecordDeath(1, 1, true), Is.True);
			Assert.That(wave.End, Is.EqualTo(StalemateWaveEndCA.None));
			Assert.That(wave.DeadCost, Is.EqualTo(249));
			Assert.That(wave.RecordDeath(2, 1, true), Is.True);
			Assert.That(wave.DeadCost, Is.EqualTo(250));
			Assert.That(wave.End, Is.EqualTo(StalemateWaveEndCA.Casualties));
		}

		[Test]
		public void VerifiedDeathIsIdempotentAndUnknownIdentityFailsClosed()
		{
			var wave = Wave();
			Assert.That(wave.RecordDeath(1, 1, true), Is.True);
			Assert.That(wave.RecordDeath(1, 1, true), Is.True);
			Assert.That(wave.ConfirmedDeadValue, Is.EqualTo(100));
			Assert.That(wave.RecordDeath(2, 2, true), Is.False);
			Assert.That(wave.End, Is.EqualTo(StalemateWaveEndCA.IdentityChanged));

			var unverified = Wave();
			Assert.That(unverified.RecordDeath(2, 1, false), Is.False);
			Assert.That(unverified.End, Is.EqualTo(StalemateWaveEndCA.IdentityChanged));
		}

		[Test]
		public void ArrivalUsesTheCommitRoster()
		{
			var wave = Wave(count: 5, n0: 8, v0: 900);
			Assert.That(wave.RecordDeath(1, 1, true), Is.True);
			Assert.That(wave.ObserveArrival(new[] { (2u, 1u), (3u, 1u) }), Is.False);
			Assert.That(wave.ObserveArrival(new[] { (2u, 1u), (3u, 1u), (4u, 1u) }), Is.True);
			Assert.That(wave.InitialCount, Is.EqualTo(5));
			Assert.That(wave.InitialValue, Is.EqualTo(500));
			Assert.That(wave.Continue(200, true, true, false, true, false), Is.True);
		}

		[Test]
		public void ContinueHardAbortOrdering()
		{
			var emergency = Wave();
			Assert.That(emergency.Continue(101, true, true, true, true, true), Is.False);
			Assert.That(emergency.End, Is.EqualTo(StalemateWaveEndCA.Emergency));

			var lost = Wave();
			Assert.That(lost.Continue(101, true, true, false, false, true), Is.False);
			Assert.That(lost.End, Is.EqualTo(StalemateWaveEndCA.TargetLost));

			var deadline = Wave();
			Assert.That(deadline.Continue(29999, true, true, false, true, false), Is.True);
			Assert.That(deadline.Continue(30000, true, true, false, true, true), Is.False);
			Assert.That(deadline.End, Is.EqualTo(StalemateWaveEndCA.Deadline));

			var complete = Wave();
			Assert.That(complete.Continue(500, true, true, false, true, true), Is.False);
			Assert.That(complete.End, Is.EqualTo(StalemateWaveEndCA.Complete));

			var incomplete = Wave();
			Assert.That(incomplete.Continue(500, false, true, true, true, true), Is.False);
			Assert.That(incomplete.End, Is.EqualTo(StalemateWaveEndCA.MissingEvidence));
		}

		[Test]
		public void EndedWaveCannotBeRevived()
		{
			var wave = Wave();
			wave.RallyEnd(750, 1, 100);
			Assert.That(wave.End, Is.EqualTo(StalemateWaveEndCA.RallyFailed));
			Assert.That(wave.ObserveSample(75, 900, true), Is.False);
			Assert.That(wave.CheckDeadlines(30000), Is.False);
			Assert.That(wave.End, Is.EqualTo(StalemateWaveEndCA.RallyFailed));
		}

		[Test]
		public void RouteStatistics()
		{
			var wave = Wave(routeEstL: 300);
			Assert.That(wave.ObserveLeaderCell(0, 10, 10), Is.True);
			Assert.That(wave.ObserveLeaderCell(75, 10, 10), Is.True);
			Assert.That(wave.ObserveLeaderCell(150, 11, 10), Is.True);
			Assert.That(wave.ObserveLeaderCell(151, 12, 10), Is.False);
			Assert.That(wave.RouteActualCells, Is.EqualTo(2));
			Assert.That(wave.IsActive, Is.True);

			Assert.That(Run(wave, 1, 1500, t => t == 825 ? 200 : 290), Is.EqualTo(-1));
			Assert.That(wave.RouteEstL, Is.EqualTo(300));
			Assert.That(wave.RouteBestL, Is.EqualTo(200));
		}

		[Test]
		public void WireNamesAreSnakeCase()
		{
			var expected = new[]
			{
				"none", "complete", "missing_evidence", "identity_changed", "emergency", "target_lost", "casualties",
				"no_progress", "deadline", "rally_failed", "retarget_failed"
			};
			var values = (StalemateWaveEndCA[])Enum.GetValues(typeof(StalemateWaveEndCA));
			Assert.That(values.Select(StalemateBreakerWaveCA.WireName), Is.EqualTo(expected));
		}

		[Test]
		public void ConstructorValidatesDenominatorsLeaderAndTiming()
		{
			Assert.Throws<ArgumentException>(() => new StalemateBreakerWaveCA(0, Members(5), Short, 4, 500, 0, 1));
			Assert.Throws<ArgumentException>(() => new StalemateBreakerWaveCA(0, Members(5), Short, 5, 499, 0, 1));
			Assert.Throws<ArgumentException>(() => new StalemateBreakerWaveCA(0, Members(5), Short, 5, 500, 0, 6));
			Assert.Throws<ArgumentException>(() => new StalemateBreakerWaveCA(0, Members(5), Short, 5, 500, -1, 1));
			Assert.Throws<ArgumentException>(() => new StalemateBreakerWaveCA(0, Members(5),
				new StalemateBreakerPolicyCA.TimingCA(1000, 749, 1500, 30000), 5, 500, 0, 1));
			Assert.Throws<ArgumentException>(() => new StalemateBreakerWaveCA(0, Members(5),
				new StalemateBreakerPolicyCA.TimingCA(18001, 750, 1500, 30000), 5, 500, 0, 1));
			Assert.Throws<ArgumentException>(() => new StalemateBreakerWaveCA(0, new[]
			{
				new StalemateWaveMemberCA(1, 1, 1), new StalemateWaveMemberCA(1, 2, 1)
			}, Short, 2, 2, 0, 1));
		}

		[Test]
		public void SaveLoadRoundTripInRally()
		{
			var wave = Wave(50, n0: 12, v0: 1300);
			wave.ObserveSample(125, 990, false);
			wave.ObserveLeaderCell(125, 3, 4);
			wave.RecordDeath(4, 1, true);
			var loaded = RoundTrip(wave);
			Assert.That(loaded.Phase, Is.EqualTo(StalemateWavePhaseCA.Rally));
			Assert.That(loaded.DeadCost, Is.EqualTo(100));
			Assert.That(loaded.N0, Is.EqualTo(12));
			Assert.That(loaded.V0, Is.EqualTo(1300));
			Assert.That(loaded.RouteBestL, Is.EqualTo(990));

			// The loaded wave behaves identically: a second death of the same identity is idempotent.
			Assert.That(loaded.RecordDeath(4, 1, true), Is.True);
			Assert.That(loaded.DeadCost, Is.EqualTo(100));
		}

		[Test]
		public void SaveLoadRoundTripArmedArrivedAndEnded()
		{
			var armed = ProgressedTo1500();
			armed.LeaderChange(1500, 7, 455);
			armed.ObserveLeaderCell(1500, 9, 9);
			armed.ObserveLeaderCell(1425, 8, 9);
			var loadedArmed = RoundTrip(armed);
			Assert.That(loadedArmed.Phase, Is.EqualTo(StalemateWavePhaseCA.Armed));
			Assert.That(loadedArmed.LeaderId, Is.EqualTo(7u));
			Assert.That(loadedArmed.RouteActualCells, Is.EqualTo(2));

			// Both continue to the same timeout tick.
			Assert.That(Run(armed, 1501, 4000, _ => 455), Is.EqualTo(3000));
			Assert.That(Run(loadedArmed, 1501, 4000, _ => 455), Is.EqualTo(3000));

			var arrived = ProgressedTo1500();
			arrived.ObserveArrival(Members(10).Select(m => (m.ActorId, m.Generation)));
			arrived.TryRetarget(1500, 800, 600);
			arrived.ObserveArrival(Members(10).Select(m => (m.ActorId, m.Generation)));
			var loadedArrived = RoundTrip(arrived);
			Assert.That(loadedArrived.Arrived, Is.True);
			Assert.That(loadedArrived.RetargetUsed, Is.True);
			Assert.That(loadedArrived.TryRetarget(1500, 800, 600), Is.False);

			var ended = ProgressedTo1500();
			Run(ended, 1501, 4000, _ => 460);
			Assert.That(ended.End, Is.EqualTo(StalemateWaveEndCA.NoProgress));
			var loadedEnded = RoundTrip(ended);
			Assert.That(loadedEnded.End, Is.EqualTo(StalemateWaveEndCA.NoProgress));
			Assert.That(loadedEnded.IsActive, Is.False);
		}
	}
}
