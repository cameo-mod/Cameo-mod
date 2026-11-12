#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>BOTPASSIVE IMPL01: the pure breaker admission core (design _06 §1, _07 M1/M2, _08 §4/§5/N1/N3/N9, _09 M1/M2/LD; Sonnet D09 L2).</summary>
	[TestFixture]
	public sealed class StalemateBreakerTest
	{
		static StalemateDraftUnitCA U(uint id, int x, int y, long value = 800, string loco = "wheeled", string type = "tank") =>
			new(id, loco, type, x, y, value);

		/// <summary>Fake engine: per-candidate behaviour by raw X; everything else reachable with L = 40, speed 100.</summary>
		sealed class FakeOracle : IStalemateRouteOracleCA
		{
			public readonly Dictionary<int, string> FailByRawX = new();
			public int Paths;
			public int RouteL = 40;
			public int Speed = 100;
			public Func<StalemateDraftUnitCA, bool> Reachable = _ => true;

			public (int X, int Y)? ResolveTarget(StalemateGroupCA group, int rawX, int rawY) =>
				FailByRawX.TryGetValue(rawX, out var r) && r == "target_unenterable" ? null : (rawX + 1, rawY);

			public bool Connected(StalemateDraftUnitCA member, (int X, int Y) tc) => Reachable(member);

			public StalemateRouteCA Route(StalemateGroupCA group, (int X, int Y) tc, IReadOnlyList<uint> typeRepresentatives)
			{
				Paths++;
				FailByRawX.TryGetValue(tc.X - 1, out var r);
				return r switch
				{
					"unreachable" => new StalemateRouteCA(false, 0, 0),
					"route_too_long" => new StalemateRouteCA(true, 2049, 100),
					"speed_unknown" => new StalemateRouteCA(true, RouteL, 0),
					"e_over_cap" => new StalemateRouteCA(true, 1501, 128),
					_ => new StalemateRouteCA(true, RouteL, Speed)
				};
			}
		}

		static List<StalemateDraftUnitCA> Army(int n, int x = 100, int y = 100, uint firstId = 100, long value = 800) =>
			Enumerable.Range(0, n).Select(i => U((uint)(firstId + i), x + (i % 3), y + (i / 3 % 3), value)).ToList();

		[Test]
		public void OutlierLowestIdNoLongerCollapsesTheDraft()
		{
			// _09 M2: 24 members at the rally, the lowest-ActorID one 50 cells away.
			var draft = new List<StalemateDraftUnitCA> { U(1, 150, 100) };
			draft.AddRange(Enumerable.Range(0, 23).Select(i => U((uint)(10 + i), 100, 100)));
			var groups = StalemateBreakerCA.BuildGroups(draft, out var excluded, out var stragglers, out _);

			Assert.That(groups, Has.Count.EqualTo(1));
			Assert.That(groups[0].CenX, Is.EqualTo(102), "floor(2450 / 24)");
			Assert.That(groups[0].RepId, Is.Not.EqualTo(1u));
			Assert.That(groups[0].Drafted, Has.Count.EqualTo(23));
			Assert.That(stragglers, Is.EqualTo(1));
			Assert.That(excluded, Is.EqualTo(0));

			// The rally-end 60 % test still passes against N0 = 24.
			Assert.That(23, Is.GreaterThanOrEqualTo(StalemateBreakerPolicyCA.RequiredUnits(24)));
		}

		[Test]
		public void BimodalGroupEmptiesTheDraft()
		{
			// Sonnet D09 L2: two clusters 70 cells apart; the centroid lies 35 cells from both, so every member is a straggler.
			var draft = Army(12, 0, 0, 1).Select(u => u with { X = 0, Y = 0 }).Concat(Army(12, 70, 0, 50).Select(u => u with { X = 70, Y = 0 })).ToList();
			var groups = StalemateBreakerCA.BuildGroups(draft, out _, out var stragglers, out _);
			Assert.That(groups, Is.Empty, "the attempt holds draft_empty");
			Assert.That(stragglers, Is.EqualTo(24));
		}

		[Test]
		public void KeepsThreeGroupsRankedByValueThenLocomotorName()
		{
			var draft = new List<StalemateDraftUnitCA>();
			draft.AddRange(Army(3, value: 100, firstId: 1).Select(u => u with { Locomotor = "foot" }));
			draft.AddRange(Army(3, value: 900, firstId: 10).Select(u => u with { Locomotor = "tracked" }));
			draft.AddRange(Army(3, value: 500, firstId: 20).Select(u => u with { Locomotor = "wheeled" }));
			draft.AddRange(Army(3, value: 500, firstId: 30).Select(u => u with { Locomotor = "hover" }));
			var groups = StalemateBreakerCA.BuildGroups(draft, out var excluded, out _, out _);
			Assert.That(groups.Select(g => g.Locomotor), Is.EqualTo(new[] { "tracked", "hover", "wheeled" }));
			Assert.That(excluded, Is.EqualTo(3));
		}

		[Test]
		public void MoreThanSixteenTypesDropsTheGroup()
		{
			var draft = Enumerable.Range(0, 17).Select(i => U((uint)(1 + i), 100, 100, type: "t" + i)).ToList();
			var groups = StalemateBreakerCA.BuildGroups(draft, out var excluded, out _, out var tooMany);
			Assert.That(groups, Is.Empty);
			Assert.That(tooMany, Is.EqualTo(1));
			Assert.That(excluded, Is.EqualTo(17));
			Assert.That(StalemateBreakerCA.BuildGroups(draft.Take(16), out _, out _, out _), Has.Count.EqualTo(1));
		}

		[Test]
		public void CandidateOrderIsVisibleThenDistanceThenOwnerThenId()
		{
			var c = new[]
			{
				new StalemateCandidateCA(9, 2, false, 1, 0),
				new StalemateCandidateCA(8, 3, true, 30, 0),
				new StalemateCandidateCA(7, 1, true, 30, 0),
				new StalemateCandidateCA(6, 1, true, 10, 0),
				new StalemateCandidateCA(5, 1, true, 30, 0)
			};
			var ordered = StalemateBreakerCA.OrderCandidates(c, (0, 0));
			Assert.That(ordered.Select(x => x.ActorId), Is.EqualTo(new uint[] { 6, 5, 7, 8, 9 }));
		}

		[Test]
		public void AtMostSixtyFourCandidatesPerOwner()
		{
			var c = Enumerable.Range(0, 70).Select(i => new StalemateCandidateCA((uint)(1 + i), 1, true, i, 0))
				.Append(new StalemateCandidateCA(500, 2, true, 1000, 0));
			var ordered = StalemateBreakerCA.OrderCandidates(c, (0, 0));
			Assert.That(ordered.Count(x => x.OwnerIndex == 1), Is.EqualTo(64));
			Assert.That(ordered.Last().ActorId, Is.EqualTo(500u));
		}

		[Test]
		public void ForceLowOwnerIsFilteredBeforeTheFourCut()
		{
			// _09 M1: candidates 1-5 belong to owner 1 (fails force), clock false, candidate 6 is owner 2 (passes).
			var ordered = Enumerable.Range(1, 5).Select(i => new StalemateCandidateCA((uint)i, 1, true, i, 0))
				.Append(new StalemateCandidateCA(6, 2, true, 6, 0)).ToList();
			var forceLow = new List<int>();
			var exclusion = new StalemateExclusionSetCA();
			var admissible = StalemateBreakerCA.FilterByTrigger(ordered, id => exclusion.IsExcluded(id, 0), false, owner => owner == 2, forceLow);

			Assert.That(admissible.Select(x => x.ActorId), Is.EqualTo(new uint[] { 6 }));
			Assert.That(forceLow, Is.EqualTo(new[] { 1 }), "exactly one force_low row, one owner");

			var groups = StalemateBreakerCA.BuildGroups(Army(12), out _, out _, out _);
			var attempt = StalemateBreakerCA.RunAttempt(admissible, groups, 12, 9600, new FakeOracle());
			Assert.That(attempt.Admitted?.Candidate.ActorId, Is.EqualTo(6u), "the first attempt admits candidate 6");
			Assert.That(exclusion.Count, Is.Zero, "force_low never enters the exclusion set");

			// The clock makes the owner irrelevant.
			Assert.That(StalemateBreakerCA.FilterByTrigger(ordered, _ => false, true, _ => false, new List<int>()), Has.Count.EqualTo(6));
		}

		[Test]
		public void PerCandidateFailuresAndOnlyLdReasonsEnterExclusion()
		{
			var oracle = new FakeOracle();
			oracle.FailByRawX[1] = "target_unenterable";
			oracle.FailByRawX[2] = "unreachable";
			oracle.FailByRawX[3] = "route_too_long";
			oracle.FailByRawX[4] = "speed_unknown";
			oracle.FailByRawX[5] = "e_over_cap";
			var candidates = Enumerable.Range(1, 5).Select(i => new StalemateCandidateCA((uint)i, 1, true, i, 0)).ToList();
			var groups = StalemateBreakerCA.BuildGroups(Army(12), out _, out _, out _);
			var exclusion = new StalemateExclusionSetCA();

			var attempt = StalemateBreakerCA.RunAttempt(candidates, groups, 12, 9600, oracle);
			Assert.That(attempt.Admitted, Is.Null);
			Assert.That(attempt.CandidatesTried, Is.EqualTo(4));
			Assert.That(attempt.Failures.Select(f => f.Result),
				Is.EqualTo(new[] { "target_unenterable", "unreachable", "route_too_long", "speed_unknown" }));
			Assert.That(attempt.Paths, Is.EqualTo(3), "an unenterable candidate costs no path; one path per group otherwise");
			foreach (var f in attempt.Failures)
				exclusion.Add(f.Candidate.ActorId, 1000, f.Result);
			Assert.That(exclusion.Count, Is.EqualTo(3));
			Assert.That(exclusion.IsExcluded(4, 1000), Is.False, "speed_unknown can change as units change");

			// Next attempt: the excluded ones are skipped, so candidate 4 and 5 are reached.
			var next = StalemateBreakerCA.RunAttempt(candidates.Where(c => !exclusion.IsExcluded(c.ActorId, 1150)).ToList(), groups, 12, 9600, oracle);
			Assert.That(next.Failures.Select(f => f.Result), Is.EqualTo(new[] { "speed_unknown", "e_over_cap" }));
		}

		[Test]
		public void OneOwnerHoldingTheFirstFourRotatesToOtherOwners()
		{
			// _08 §4 test: owner 1 holds the first 4 candidates, all unreachable; the next attempt reaches owner 2.
			var oracle = new FakeOracle();
			for (var i = 1; i <= 4; i++)
				oracle.FailByRawX[i] = "unreachable";
			var candidates = Enumerable.Range(1, 4).Select(i => new StalemateCandidateCA((uint)i, 1, true, i, 0))
				.Append(new StalemateCandidateCA(9, 2, true, 50, 0)).ToList();
			var groups = StalemateBreakerCA.BuildGroups(Army(12), out _, out _, out _);
			var exclusion = new StalemateExclusionSetCA();
			var first = StalemateBreakerCA.RunAttempt(candidates, groups, 12, 9600, oracle);
			Assert.That(first.Admitted, Is.Null);
			foreach (var f in first.Failures)
				exclusion.Add(f.Candidate.ActorId, 0, f.Result);
			var second = StalemateBreakerCA.RunAttempt(candidates.Where(c => !exclusion.IsExcluded(c.ActorId, 750)).ToList(), groups, 12, 9600, oracle);
			Assert.That(second.Admitted?.Candidate.OwnerIndex, Is.EqualTo(2));
		}

		[Test]
		public void UnreachableMembersStayInTheDenominators()
		{
			// 12 eligible, 7 reachable: 7 < ceil(60 % of 12) = 8, so the candidate fails "unreachable".
			var oracle = new FakeOracle { Reachable = m => m.ActorId < 107 };
			var groups = StalemateBreakerCA.BuildGroups(Army(12), out _, out _, out _);
			var paths = 0;
			var checks = 0;
			var eval = StalemateBreakerCA.EvaluateCandidate(new StalemateCandidateCA(1, 1, true, 50, 0), groups, 12, 9600, oracle, ref paths, ref checks);
			Assert.That(eval.Result, Is.EqualTo("unreachable"));
			Assert.That(eval.UnreachableUnits, Is.EqualTo(5));

			oracle.Reachable = m => m.ActorId < 108;
			eval = StalemateBreakerCA.EvaluateCandidate(new StalemateCandidateCA(1, 1, true, 50, 0), groups, 12, 9600, oracle, ref paths, ref checks);
			Assert.That(eval.Result, Is.EqualTo("admit"));
			Assert.That(eval.ReachableCount, Is.EqualTo(8));
		}

		[TestCase(1500, 128, "admit", 18000)]
		[TestCase(0, 128, "admit", 0)]
		public void TimingBoundsFromTheGroupRoute(int l, int s, string expected, int e)
		{
			var oracle = new FakeOracle { RouteL = l, Speed = s };
			var groups = StalemateBreakerCA.BuildGroups(Army(12), out _, out _, out _);
			var paths = 0;
			var checks = 0;
			var eval = StalemateBreakerCA.EvaluateCandidate(new StalemateCandidateCA(1, 1, true, 50, 0), groups, 12, 9600, oracle, ref paths, ref checks);
			Assert.That(eval.Result, Is.EqualTo(expected));
			Assert.That(eval.Timing.EstimatedTicks, Is.EqualTo(e));
			if (l == 0)
			{
				// A 0-length route keeps the _04 minimum bounds.
				Assert.That(eval.Timing.RallyDuration, Is.EqualTo(750));
				Assert.That(eval.Timing.NoProgressDuration, Is.EqualTo(1500));
				Assert.That(eval.Timing.AbsoluteDuration, Is.EqualTo(3000));
			}
		}

		[Test]
		public void EOverEighteenThousandIsECap()
		{
			var oracle = new FakeOracle();
			oracle.FailByRawX[7] = "e_over_cap";
			var groups = StalemateBreakerCA.BuildGroups(Army(12), out _, out _, out _);
			var paths = 0;
			var checks = 0;
			var eval = StalemateBreakerCA.EvaluateCandidate(new StalemateCandidateCA(1, 1, true, 7, 0), groups, 12, 9600, oracle, ref paths, ref checks);
			Assert.That(eval.Result, Is.EqualTo("e_over_cap"), "L 1501 at speed 128 gives E = 18012 > 18000");
		}

		[Test]
		public void LeaderSuccessionFollowsGroupRank()
		{
			var groups = new List<(uint RepId, IReadOnlyList<uint> Members)>
			{
				(10, new uint[] { 10, 11, 12 }), (20, new uint[] { 20, 21 }), (30, new uint[] { 30 })
			};
			Assert.That(StalemateBreakerCA.NextLeader(groups, id => id != 10), Is.EqualTo(11u), "top group's lowest surviving id");
			Assert.That(StalemateBreakerCA.NextLeader(groups, id => id >= 20), Is.EqualTo(20u));
			Assert.That(StalemateBreakerCA.NextLeader(groups, id => id == 21), Is.EqualTo(21u));
			Assert.That(StalemateBreakerCA.NextLeader(groups, _ => false), Is.Zero, "no survivor: the wave ends on casualties");
		}

		[Test]
		public void ExclusionExpiresAtThreeThousandCapsAndPersists()
		{
			var set = new StalemateExclusionSetCA();
			Assert.That(set.Add(5, 100, "route_too_long"), Is.True);
			Assert.That(set.IsExcluded(5, 3099), Is.True);
			Assert.That(set.IsExcluded(5, 3100), Is.False);
			for (uint i = 1000; i < 1000 + StalemateBreakerCA.ExclusionMax + 10; i++)
				set.Add(i, 200, "unreachable");
			Assert.That(set.Count, Is.EqualTo(StalemateBreakerCA.ExclusionMax));

			var text = new List<MiniYamlNode> { new("X", "", set.Save()) }.WriteToString();
			var loaded = StalemateExclusionSetCA.Load(MiniYaml.FromString(text, "test").Single().Value);
			Assert.That(new List<MiniYamlNode> { new("X", "", loaded.Save()) }.WriteToString(), Is.EqualTo(text));
		}

		[Test]
		public void WorldRotationServesEverySeatWithinSeatCountTicksIndependentOfTickOrder()
		{
			// _08 N9: P_world rotation and atomic deferral over 8 seats x 2 runs (forward and reverse seat tick order).
			List<(int Tick, int Seat)> Run(bool reverse)
			{
				var rotation = new StalemateRotationCA();
				var served = new List<(int, int)>();
				for (var s = 0; s < 8; s++)
					rotation.MarkPending(s, 100);
				for (var t = 101; t <= 120; t++)
				{
					var order = Enumerable.Range(0, 8);
					if (reverse)
						order = order.Reverse();
					foreach (var s in order)
						if (rotation.IsPending(s) && rotation.IsWinner(s, t, 8))
						{
							Assert.That(t - rotation.PendingSince(s) - 1, Is.LessThanOrEqualTo(7), "deferral at most seatCount - 1");
							rotation.Clear(s);
							served.Add((t, s));
						}
				}

				return served;
			}

			var forward = Run(false);
			var backward = Run(true);
			Assert.That(forward, Is.EqualTo(backward));
			Assert.That(forward.Select(x => x.Seat).OrderBy(s => s), Is.EqualTo(Enumerable.Range(0, 8)));
			Assert.That(forward.GroupBy(x => x.Tick).All(g => g.Count() == 1), Is.True, "12 of P_world 16: one admission per tick");
			Assert.That(forward.Max(x => x.Tick), Is.LessThanOrEqualTo(108));
		}

		[Test]
		public void ASeatPendingThisTickWaitsForTheNextElection()
		{
			var rotation = new StalemateRotationCA();
			rotation.MarkPending(3, 500);
			Assert.That(rotation.IsWinner(3, 500, 8), Is.False);
			Assert.That(rotation.IsWinner(3, 501, 8), Is.True);
		}

		[Test]
		public void StuckSquadNeedsSixSamplesAndLessThanTwoCells()
		{
			// Opus ruling (H12): versus the leader cell 6 health samples (1500 ticks) ago, only with 6 samples of history.
			var tracker = new StalemateStuckTrackerCA<string>();
			var counts = new List<int>();
			for (var k = 0; k < 9; k++)
				counts.Add(tracker.Sample(new[]
				{
					("osc", 10 + (k % 2), 10),        // oscillates within 1 cell: stuck
					("march", 10 + (2 * k), 40)       // 2 cells per sample: 12 cells over the window, not stuck
				}));

			Assert.That(counts.Take(6), Is.All.EqualTo(0), "no count before 6 samples of history");
			Assert.That(counts.Skip(6), Is.All.EqualTo(1));

			// A squad that moved 2 cells across the window (but not per sample) is not stuck.
			var slow = new StalemateStuckTrackerCA<string>();
			var last = 0;
			for (var k = 0; k < 7; k++)
				last = slow.Sample(new[] { ("slow", k == 6 ? 12 : 10, 10) });
			Assert.That(last, Is.Zero);

			// Leaving attack/move drops the ring.
			slow.Sample(Array.Empty<(string, int, int)>());
			Assert.That(slow.Tracked, Is.Zero);
		}

		[Test]
		public void SeatStateRoundTripsByteIdentically()
		{
			var seat = new StalemateBreakerSeatCA { LastKillTick = 7300, NextAttemptTick = 9150, CooldownUntil = 8000, WaveCounter = 2 };
			seat.Exclusion.Add(44, 9000, "target_unenterable");
			seat.Sightings[71] = new StalemateSightingCA(2, 900, 8950, false, 40, 41);
			seat.Sightings[72] = new StalemateSightingCA(2, 1500, 8950, true, 55, 58);
			seat.KillWatch[90] = (60, 61);
			seat.Wave = new StalemateBreakerWaveCA(9000, Enumerable.Range(1, 10).Select(i => new StalemateWaveMemberCA((uint)i, 1, 700)),
				new StalemateBreakerPolicyCA.TimingCA(600, 750, 1500, 3000), 12, 8400, 40, 3);
			seat.WaveId = 2;
			seat.TargetId = 90;
			seat.TargetOwnerIndex = 2;
			seat.TargetX = 60;
			seat.TargetY = 61;
			seat.RallyX = 20;
			seat.RallyY = 21;
			seat.Groups.Add(new StalemateWaveGroupCA { Locomotor = "tracked", RepId = 3, TcX = 61, TcY = 61, CenX = 20, CenY = 21, L = 40, S = 100,
				Members = Enumerable.Range(1, 10).Select(i => (uint)i).ToList() });

			var text = new List<MiniYamlNode> { new("B", "", seat.Save()) }.WriteToString();
			var loaded = StalemateBreakerSeatCA.Load(MiniYaml.FromString(text, "test").Single().Value);
			Assert.That(new List<MiniYamlNode> { new("B", "", loaded.Save()) }.WriteToString(), Is.EqualTo(text));
			Assert.That(loaded.DenL(2, 9000), Is.EqualTo(900));
			Assert.That(loaded.DenL(2, 9701), Is.Zero, "older than 750 ticks");
			Assert.That(loaded.DenDefenceFresh(2, 60, 61, 9000), Is.EqualTo(1500));
		}
	}
}
