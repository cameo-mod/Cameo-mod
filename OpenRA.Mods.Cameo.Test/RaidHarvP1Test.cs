#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// RAID-HARV-01 P1 (design 01 48E9BC72, design 02 A9436EE3, delta 01 C8849FD7, delta 02 5087797E): the pure classes only, nothing wired, nothing learns. The package prefix rule against a brute-force reference, the fail-closed allied
	/// branch (never Solo with an active ally), the routed committed group value (a nominally large group whose other seats cannot reach the target), the explored-only route contract, the phase machine with the release order, the nine key
	/// specs and their read quantizer, the observed signals, the target-owner-only style axis, and source pins (no private learning arithmetic, no actor dereference, switch OFF in code, classic and all six blocks).
	/// </summary>
	[TestFixture]
	public class RaidHarvP1Test
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";

		static readonly string[] RaidFiles =
		{
			Bots + "Squads/RaidHarvKeys.cs",
			Bots + "Squads/RaidHarvSignals.cs",
			Bots + "Squads/RaidHarvPackage.cs",
			Bots + "Squads/RaidHarvGroup.cs",
			Bots + "Squads/RaidRoute.cs",
			Bots + "Squads/RaidRecon.cs",
		};

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		static int Count(string text, string term) => text.Split(new[] { term }, StringSplitOptions.None).Length - 1;

		// ---- the nine keys -------------------------------------------------------------------------------------------------------

		[Test]
		public void TheNineKeysDeclareTheirPriorClampAndQuantumInPositiveMilliUnits()
		{
			var expected = new (RaidKey Key, string Id, int Prior, int Min, int Max)[]
			{
				(RaidKey.LostRecon, "raid.lost_recon", 400, 100, 1200),
				(RaidKey.MoveRefresh, "raid.move_refresh", 125, 25, 300),
				(RaidKey.ReqMargin, "raid.req_margin", 150, 100, 300),
				(RaidKey.ReconRadius, "raid.recon_radius", 8, 3, 16),
				(RaidKey.IncidentLoss, "raid.incident_loss", 25, 10, 60),
				(RaidKey.IncidentWindow, "raid.incident_window", 300, 100, 900),
				(RaidKey.IncidentDecay, "raid.incident_decay", 3000, 600, 9000),
				(RaidKey.WaypointSpacing, "raid.waypoint_spacing", 8, 4, 16),
				(RaidKey.DefenceRadius, "raid.defence_radius", 6, 3, 12),
			};

			Assert.That(RaidHarvKeys.All.Count, Is.EqualTo(expected.Length));
			Assert.That(Enum.GetValues(typeof(RaidKey)).Length, Is.EqualTo(expected.Length));
			foreach (var e in expected)
			{
				var spec = RaidHarvKeys.Spec(e.Key);
				Assert.That(spec.Key, Is.EqualTo(e.Key));
				Assert.That(spec.Id, Is.EqualTo(e.Id));
				Assert.That(spec.PriorMilli, Is.EqualTo(e.Prior * 1000), e.Id);
				Assert.That(spec.MinMilli, Is.EqualTo(e.Min * 1000), e.Id);
				Assert.That(spec.MaxMilli, Is.EqualTo(e.Max * 1000), e.Id);
				Assert.That(spec.QuantumMilli, Is.EqualTo(1000), e.Id);
				Assert.That(spec.MinMilli, Is.GreaterThan(0), "positive encoding: never zero");
				Assert.That(spec.PriorMilli, Is.InRange(spec.MinMilli, spec.MaxMilli));
			}

			Assert.That(RaidHarvKeys.All.Select(k => k.Id).Distinct().Count(), Is.EqualTo(9));

			// The declared learner parameters (delta 02 section 1): gain 1/8, step capped at 5 percent of the CURRENT value, error clip +-300, categorical error 200.
			Assert.That(RaidHarvKeys.Milli, Is.EqualTo(1000));
			Assert.That(RaidHarvKeys.GainNumerator, Is.EqualTo(1));
			Assert.That(RaidHarvKeys.GainDenominator, Is.EqualTo(8));
			Assert.That(RaidHarvKeys.StepMaxPermille, Is.EqualTo(50));
			Assert.That(RaidHarvKeys.ErrorClipPermille, Is.EqualTo(300));
			Assert.That(RaidHarvKeys.CategoricalPermille, Is.EqualTo(200));
		}

		[Test]
		public void TheReadRoundsToTheNearestQuantumSoARepeatedSmallStepIsNeverLost()
		{
			// Half up, and clamped to the key's clamp.
			Assert.That(RaidHarvKeys.Read(RaidKey.DefenceRadius, 6000), Is.EqualTo(6));
			Assert.That(RaidHarvKeys.Read(RaidKey.DefenceRadius, 6499), Is.EqualTo(6));
			Assert.That(RaidHarvKeys.Read(RaidKey.DefenceRadius, 6500), Is.EqualTo(7));
			Assert.That(RaidHarvKeys.Read(RaidKey.DefenceRadius, 6600), Is.EqualTo(7));
			Assert.That(RaidHarvKeys.Read(RaidKey.DefenceRadius, 1), Is.EqualTo(3), "clamped up");
			Assert.That(RaidHarvKeys.Read(RaidKey.DefenceRadius, 99000), Is.EqualTo(12), "clamped down");
			Assert.That(RaidHarvKeys.ClampMilli(RaidKey.LostRecon, -5), Is.EqualTo(100000));
			Assert.That(RaidHarvKeys.ClampMilli(RaidKey.LostRecon, long.MaxValue), Is.EqualTo(1200000));

			// The failure Sol named: the defence radius has a 9-cell span, so 5 percent of the SPAN is under one cell and a whole-cell delta divided by 16 is zero. The declared step is 5 percent of the CURRENT value in milli-units
			// (a test-local fixed-point walk; the real arithmetic is the shared core's): 6.000 -> 6.300 -> 6.615 reads 7 after two games, and never freezes.
			long milli = 6000;
			var reads = new List<int>();
			for (var game = 0; game < 4; game++)
			{
				milli += milli * RaidHarvKeys.StepMaxPermille / 1000;
				reads.Add(RaidHarvKeys.Read(RaidKey.DefenceRadius, milli));
			}

			Assert.That(reads[0], Is.EqualTo(6));
			Assert.That(reads[1], Is.EqualTo(7), "sub-cell progress becomes visible at the read");
			Assert.That(reads.Distinct().Count(), Is.GreaterThan(1), "a repeated signal always becomes visible");
		}

		[Test]
		public void TheP1ValueSourceIsTheClampedQuantizedPriors()
		{
			var defaults = RaidKeyPriors.FromInfo(new SquadManagerBotModuleCAInfo());
			foreach (var spec in RaidHarvKeys.All)
				Assert.That(defaults.Read(spec.Key), Is.EqualTo(spec.PriorMilli / 1000), spec.Id);

			// Out of range priors are clamped; a short list takes the declared prior for the rest.
			var odd = new RaidKeyPriors(new[] { 5, 9999 });
			Assert.That(odd.Read(RaidKey.LostRecon), Is.EqualTo(100));
			Assert.That(odd.Read(RaidKey.MoveRefresh), Is.EqualTo(300));
			Assert.That(odd.Read(RaidKey.ReconRadius), Is.EqualTo(8));
			Assert.That(new RaidKeyPriors(null).Read(RaidKey.DefenceRadius), Is.EqualTo(6));
		}

		// ---- the observed signals ------------------------------------------------------------------------------------------------

		[Test]
		public void TheSignalsAreObservedRelativeErrorsWithNoSampleMeaningNoStep()
		{
			// raid.lost_recon: current 400, two reacquires (300, 500: median 400, target 500, error +250 each) and one budget used up (-200): (250 + 250 - 200) / 3 = 100.
			Assert.That(RaidHarvSignals.LostRecon(400, new[] { 300, 500 }, 1), Is.EqualTo(new RaidSignal(true, 100)));
			Assert.That(RaidHarvSignals.LostRecon(400, Array.Empty<int>(), 2), Is.EqualTo(new RaidSignal(true, -200)));
			Assert.That(RaidHarvSignals.LostRecon(100, new[] { 1000 }, 0), Is.EqualTo(new RaidSignal(true, 300)), "clipped");
			Assert.That(RaidHarvSignals.LostRecon(400, Array.Empty<int>(), 0), Is.EqualTo(RaidSignal.None));
			Assert.That(RaidHarvSignals.LostRecon(0, new[] { 300 }, 0), Is.EqualTo(RaidSignal.None), "an undefined denominator is no sample, never zero");

			// raid.move_refresh: drift cells of the eligible refreshes.
			Assert.That(RaidHarvSignals.MoveRefresh(Array.Empty<int>()), Is.EqualTo(RaidSignal.None));
			Assert.That(RaidHarvSignals.MoveRefresh(new[] { 5, 4, 3, 1 }), Is.EqualTo(new RaidSignal(true, -200)), "late share 75 percent");
			Assert.That(RaidHarvSignals.MoveRefresh(new[] { 0, 1, 1, 1, 3 }), Is.EqualTo(new RaidSignal(true, 200)), "wasted share 80 percent");
			Assert.That(RaidHarvSignals.MoveRefresh(new[] { 2, 2, 5, 0 }), Is.EqualTo(new RaidSignal(true, 0)));
			Assert.That(RaidHarvSignals.MoveRefresh(new[] { 3, 1 }), Is.EqualTo(new RaidSignal(true, 0)), "exactly 50 percent late is not over 50");

			// raid.req_margin: only raids that killed their target.
			Assert.That(RaidHarvSignals.ReqMargin(new[] { new RaidResult(false, 900) }), Is.EqualTo(RaidSignal.None));
			Assert.That(RaidHarvSignals.ReqMargin(new[] { new RaidResult(true, 700), new RaidResult(true, 650) }), Is.EqualTo(new RaidSignal(true, 200)));
			Assert.That(RaidHarvSignals.ReqMargin(new[] { new RaidResult(true, 100), new RaidResult(true, 299), new RaidResult(false, 900) }), Is.EqualTo(new RaidSignal(true, -200)));
			Assert.That(RaidHarvSignals.ReqMargin(new[] { new RaidResult(true, 450) }), Is.EqualTo(new RaidSignal(true, 0)));

			// raid.recon_radius: reacquire distance 8 (median 8, target 10, error +250) with one far sighting after a used-up budget (+200): (250 + 200) / 2 = 225.
			Assert.That(RaidHarvSignals.ReconRadius(8, new[] { 8 }, 1), Is.EqualTo(new RaidSignal(true, 225)));
			Assert.That(RaidHarvSignals.ReconRadius(8, Array.Empty<int>(), 0), Is.EqualTo(RaidSignal.None));

			// raid.incident_loss / incident_decay / waypoint_spacing / defence_radius: categorical.
			Assert.That(RaidHarvSignals.IncidentLoss(new[] { new RetreatOutcome(true, false), new RetreatOutcome(false, true) }), Is.EqualTo(new RaidSignal(true, 0)));
			Assert.That(RaidHarvSignals.IncidentLoss(new[] { new RetreatOutcome(true, true) }), Is.EqualTo(new RaidSignal(true, -200)), "lost half wins over a clean re-strike");
			Assert.That(RaidHarvSignals.IncidentLoss(new[] { new RetreatOutcome(false, false) }), Is.EqualTo(RaidSignal.None));
			Assert.That(RaidHarvSignals.IncidentDecay(new[] { new DecayOutcome(true, false) }), Is.EqualTo(new RaidSignal(true, 200)));
			Assert.That(RaidHarvSignals.IncidentDecay(new[] { new DecayOutcome(false, true) }), Is.EqualTo(new RaidSignal(true, -200)));
			Assert.That(RaidHarvSignals.IncidentDecay(new[] { new DecayOutcome(false, false) }), Is.EqualTo(RaidSignal.None));
			Assert.That(RaidHarvSignals.WaypointSpacing(new[] { false, false }), Is.EqualTo(new RaidSignal(true, 200)));
			Assert.That(RaidHarvSignals.WaypointSpacing(new[] { false, true }), Is.EqualTo(new RaidSignal(true, -200)));
			Assert.That(RaidHarvSignals.WaypointSpacing(Array.Empty<bool>()), Is.EqualTo(RaidSignal.None));
			Assert.That(RaidHarvSignals.DefenceRadius(new[] { true, true, false }), Is.EqualTo(new RaidSignal(true, 66)), "(200 + 200 - 200) / 3");
			Assert.That(RaidHarvSignals.DefenceRadius(Array.Empty<bool>()), Is.EqualTo(RaidSignal.None));

			// raid.incident_window: the target is 1.5 x the median span (200, 400 -> 300 -> 450): +500 clipped to 300.
			Assert.That(RaidHarvSignals.IncidentWindow(300, new[] { 200, 400 }), Is.EqualTo(new RaidSignal(true, 300)));
			Assert.That(RaidHarvSignals.IncidentWindow(300, new[] { 200 }), Is.EqualTo(new RaidSignal(true, 0)), "1.5 x 200 = 300 = current");
			Assert.That(RaidHarvSignals.IncidentWindow(300, Array.Empty<int>()), Is.EqualTo(RaidSignal.None));

			// The median of an even count is the average of the middle two, toward zero.
			Assert.That(RaidHarvSignals.Median(new[] { 4, 1, 3, 2 }), Is.EqualTo(2));
			Assert.That(RaidHarvSignals.Median(new[] { 7 }), Is.EqualTo(7));
			Assert.That(RaidHarvSignals.Median(Array.Empty<int>()), Is.EqualTo(0));
			Assert.That(RaidHarvSignals.RefreshSettleTicks, Is.EqualTo(25));
		}

		// ---- the package ---------------------------------------------------------------------------------------------------------

		[Test]
		public void TheRequiredValueIsTheLargerOfTheTargetMarginAndTheObservedDefenceWithCeilings()
		{
			Assert.That(RaidHarvPackage.Required(1000, 150, 0, 20), Is.EqualTo(1500));
			Assert.That(RaidHarvPackage.Required(1000, 150, 2000, 20), Is.EqualTo(2400), "the observed defence x (100 + risk margin) dominates");
			Assert.That(RaidHarvPackage.Required(1001, 150, 0, 20), Is.EqualTo(1502), "ceiling");
			Assert.That(RaidHarvPackage.Required(0, 150, 0, 20), Is.EqualTo(0));
			Assert.That(RaidHarvPackage.Required(1000, 150, 1, 0), Is.EqualTo(1500));
		}

		[Test]
		public void ThePackageIsTheSmallestEtaOrderedPrefixAndMatchesABruteForceReference()
		{
			var seed = 12345u;
			int Next(int n)
			{
				seed = (seed * 1664525u) + 1013904223u;
				return (int)((seed >> 8) % (uint)n);
			}

			for (var round = 0; round < 300; round++)
			{
				var n = 1 + Next(8);
				var offers = new List<RaidOffer>();
				for (var i = 0; i < n; i++)
					offers.Add(new RaidOffer(i + 1, Next(8) == 0 ? 0 : 100 + (Next(10) * 100), Next(9) == 0 ? 0 : 1 + Next(4), Next(10) == 0 ? int.MaxValue : 50 + (Next(20) * 10)));

				var required = Next(5) == 0 ? 0 : 200 + (Next(12) * 150);
				var minUnits = Next(4);
				var found = RaidHarvPackage.TrySelect(offers, required, minUnits, out var package);

				// Brute force: the cheapest achievable group ETA over every subset of the usable offers that satisfies the value and the actor count.
				var usable = offers.Where(o => o.Value > 0 && o.Units > 0 && o.EtaTicks > 0 && o.EtaTicks != int.MaxValue).ToList();
				var best = int.MaxValue;
				for (var mask = 1; mask < 1 << usable.Count; mask++)
				{
					long value = 0;
					long units = 0;
					var eta = 0;
					for (var i = 0; i < usable.Count; i++)
					{
						if ((mask & (1 << i)) == 0)
							continue;

						value += usable[i].Value;
						units += usable[i].Units;
						eta = Math.Max(eta, usable[i].EtaTicks);
					}

					if (value >= required && units >= minUnits && required > 0)
						best = Math.Min(best, eta);
				}

				Assert.That(found, Is.EqualTo(best != int.MaxValue), $"round {round}");
				if (!found)
				{
					Assert.That(package, Is.Empty);
					continue;
				}

				Assert.That(RaidHarvPackage.PackageEta(package), Is.EqualTo(best), $"round {round}: the prefix reaches the best group ETA");
				Assert.That(package.Sum(o => (long)o.Value), Is.GreaterThanOrEqualTo(required));
				Assert.That(package.Sum(o => (long)o.Units), Is.GreaterThanOrEqualTo(minUnits));

				// Minimal: dropping the last (slowest) offer breaks the value or the actor count.
				var shorter = package.Take(package.Count - 1).ToList();
				Assert.That(shorter.Sum(o => (long)o.Value) >= required && shorter.Sum(o => (long)o.Units) >= minUnits, Is.False, $"round {round}: a smaller prefix suffices");
				for (var i = 1; i < package.Count; i++)
					Assert.That(package[i].EtaTicks, Is.GreaterThanOrEqualTo(package[i - 1].EtaTicks), "ETA order");
			}
		}

		[Test]
		public void TheJoinMinimumIsAnActorCountNeverAValueAndNoPrefixMeansNoRaid()
		{
			// One squad with an enormous value but one actor does not satisfy a minimum of two actors.
			var one = new[] { new RaidOffer(1, 100000, 1, 100) };
			Assert.That(RaidHarvPackage.TrySelect(one, 500, 2, out var p1), Is.False);
			Assert.That(p1, Is.Empty);
			Assert.That(RaidHarvPackage.TrySelect(one, 500, 1, out _), Is.True);

			// Two small squads reach the count and the value together.
			var two = new[] { new RaidOffer(1, 300, 1, 100), new RaidOffer(2, 300, 1, 120) };
			Assert.That(RaidHarvPackage.TrySelect(two, 500, 2, out var p2), Is.True);
			Assert.That(p2.Count, Is.EqualTo(2));

			// A requirement that is not positive is no requirement: no raid.
			Assert.That(RaidHarvPackage.TrySelect(two, 0, 0, out _), Is.False);
			Assert.That(RaidHarvPackage.TrySelect(null, 500, 0, out _), Is.False);
			Assert.That(RaidHarvPackage.PackageEta(null), Is.EqualTo(0));
		}

		// ---- the group: fail closed with allies, routed committed value -----------------------------------------------------------

		[Test]
		public void WithAnActiveAllyNothingEverLaunchesSolo()
		{
			foreach (var allies in new[] { false, true })
			foreach (var joint in new[] { false, true })
			foreach (var approved in new[] { false, true })
			foreach (var ready in new[] { false, true })
			foreach (var routed in new[] { 0L, 899L, 900L, 5000L })
			{
				var d = RaidHarvGroup.Decide(allies, joint, approved, routed, 900, ready);
				var label = $"allies={allies} joint={joint} approved={approved} ready={ready} routed={routed}";
				if (allies)
					Assert.That(d, Is.Not.EqualTo(RaidLaunch.Solo), label);

				var enough = ready && routed >= 900;
				if (!enough)
					Assert.That(d, Is.EqualTo(RaidLaunch.Hold), label);
				else if (!allies)
					Assert.That(d, Is.EqualTo(RaidLaunch.Solo), label);
				else if (!joint)
					Assert.That(d, Is.EqualTo(RaidLaunch.Hold), label + ": the joint row off with allies fails closed");
				else
					Assert.That(d, Is.EqualTo(approved ? RaidLaunch.Join : RaidLaunch.Propose), label);
			}

			Assert.That(RaidHarvGroup.Decide(false, false, false, 1000, 0, true), Is.EqualTo(RaidLaunch.Hold), "no valid requirement");
			Assert.That(RaidHarvGroup.Decide(true, true, true, 1000, -5, true), Is.EqualTo(RaidLaunch.Hold));
		}

		[Test]
		public void ANominallyLargeGroupWhoseOtherSeatsCannotReachTheTargetHolds()
		{
			const uint target = 77;
			const int now = 1000;
			const int fresh = 500;

			// Seat A is route-ready with 600. Seats B and C hold 2000 raid value each but have no explored lane, so they published no contribution: total raid value 4600, routed value 600, requirement 900.
			var routedOnly = new[] { new RaidContribution(1, target, 600, 3, 200, 980) };
			var routed = RaidHarvGroup.RoutedValue(routedOnly, target, now, fresh, 2);
			Assert.That(routed, Is.EqualTo(600));
			Assert.That(600 + 2000 + 2000, Is.GreaterThanOrEqualTo(900), "the nominal total would have launched");
			foreach (var approved in new[] { false, true })
				Assert.That(RaidHarvGroup.Decide(true, true, approved, routed, 900, true), Is.EqualTo(RaidLaunch.Hold), "no Propose, no Join");

			// All three route-ready: 600 + 400 + 300 = 1300 >= 900: the proposer proposes, and after the leader's approval the members join.
			var all = new[]
			{
				new RaidContribution(1, target, 600, 3, 200, 980),
				new RaidContribution(2, target, 400, 2, 220, 990),
				new RaidContribution(3, target, 300, 2, 260, 995),
			};
			routed = RaidHarvGroup.RoutedValue(all, target, now, fresh, 2);
			Assert.That(routed, Is.EqualTo(1300));
			Assert.That(RaidHarvGroup.Decide(true, true, false, routed, 900, true), Is.EqualTo(RaidLaunch.Propose));
			Assert.That(RaidHarvGroup.Decide(true, true, true, routed, 900, true), Is.EqualTo(RaidLaunch.Join));

			// B retracts after the approval: the launch recheck fails and the members hold.
			var retracted = all.Where(c => c.SeatKey != 2).ToArray();
			routed = RaidHarvGroup.RoutedValue(retracted, target, now, fresh, 2);
			Assert.That(routed, Is.EqualTo(900), "600 + 300");
			Assert.That(RaidHarvGroup.Decide(true, true, true, routed, 1000, true), Is.EqualTo(RaidLaunch.Hold));
		}

		[Test]
		public void TheRoutedValueCountsOnlyFreshQualifiedContributionsForThatTargetOncePerSeat()
		{
			const uint target = 5;
			var contributions = new[]
			{
				new RaidContribution(1, target, 500, 3, 100, 900),
				new RaidContribution(1, target, 700, 3, 100, 950), // the latest of seat 1 wins
				new RaidContribution(2, target, 99999, 1, 100, 950), // fewer actors than the join minimum: not counted, whatever the value
				new RaidContribution(3, target, 400, 2, 100, 100), // stale
				new RaidContribution(4, 6, 400, 2, 100, 950), // another target
				new RaidContribution(5, target, 250, 2, 100, 1200), // from the future
				new RaidContribution(6, target, 0, 2, 100, 950), // no value
				new RaidContribution(7, target, 300, 2, 100, 960),
			};

			Assert.That(RaidHarvGroup.RoutedValue(contributions, target, 1000, 500, 2), Is.EqualTo(1000), "700 + 300");
			Assert.That(RaidHarvGroup.RoutedValue(contributions, 6, 1000, 500, 2), Is.EqualTo(400));
			Assert.That(RaidHarvGroup.RoutedValue(null, target, 1000, 500, 2), Is.EqualTo(0));
			Assert.That(RaidHarvGroup.RoutedValue(contributions, target, 1000, 500, 4), Is.EqualTo(0), "a join minimum of four actors excludes all");
		}

		[Test]
		public void OnlyTheTeamLeaderApprovesAFreshProposalThatTheRoutedValueSupports()
		{
			Assert.That(RaidHarvGroup.LeaderApproves(true, true, 900, 900), Is.True);
			Assert.That(RaidHarvGroup.LeaderApproves(false, true, 5000, 900), Is.False, "a member never writes the assignment");
			Assert.That(RaidHarvGroup.LeaderApproves(true, false, 5000, 900), Is.False, "a stale proposal");
			Assert.That(RaidHarvGroup.LeaderApproves(true, true, 899, 900), Is.False);
			Assert.That(RaidHarvGroup.LeaderApproves(true, true, 5000, 0), Is.False, "no valid requirement");
		}

		[Test]
		public void ActiveAllyIsALiveAnyAllyThatIsNotSelfASpectatorOrDefeatedAndTheRallyCodePredicateAgrees()
		{
			Assert.That(RaidHarvGroup.ActiveAlly(false, true, false, false), Is.True, "a live ally, human or bot");
			Assert.That(RaidHarvGroup.ActiveAlly(false, true, false, true), Is.False, "a defeated ally");
			Assert.That(RaidHarvGroup.ActiveAlly(false, true, true, false), Is.False, "a spectator");
			Assert.That(RaidHarvGroup.ActiveAlly(false, false, false, false), Is.False, "an enemy or neutral");
			Assert.That(RaidHarvGroup.ActiveAlly(true, true, false, false), Is.False, "this seat itself");

			// The team-leader rally code skips a player with `p.NonCombatant || p.WinState != WinState.Undefined || (p != Player && relationship != Ally)`. For every OTHER player the two agree.
			var lead = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(lead, Does.Contain("if (p.NonCombatant || p.WinState != WinState.Undefined || (p != Player && Player.RelationshipWith(p) != PlayerRelationship.Ally))"));
			foreach (var ally in new[] { false, true })
			foreach (var nonCombatant in new[] { false, true })
			foreach (var defeated in new[] { false, true })
			{
				var skippedByRally = nonCombatant || defeated || !ally;
				Assert.That(RaidHarvGroup.ActiveAlly(false, ally, nonCombatant, defeated), Is.EqualTo(!skippedByRally), $"ally={ally} nc={nonCombatant} defeated={defeated}");
			}
		}

		// ---- the explored-only route ----------------------------------------------------------------------------------------------

		static List<CPos> Line(int count) => Enumerable.Range(1, count).Select(i => new CPos(i, 0)).ToList();

		[Test]
		public void TheWaypointsAreEveryNthCellAndAlwaysTheLastOne()
		{
			var path = Line(10); // cells (1,0) .. (10,0) in travel order
			Assert.That(RaidRoute.Waypoints(path, 3).Select(c => c.X), Is.EqualTo(new[] { 3, 6, 9, 10 }));
			Assert.That(RaidRoute.Waypoints(path, 5).Select(c => c.X), Is.EqualTo(new[] { 5, 10 }));
			Assert.That(RaidRoute.Waypoints(path, 10).Select(c => c.X), Is.EqualTo(new[] { 10 }));
			Assert.That(RaidRoute.Waypoints(path, 99).Select(c => c.X), Is.EqualTo(new[] { 10 }), "a spacing past the path is the target alone");
			Assert.That(RaidRoute.Waypoints(path, 1).Count, Is.EqualTo(10));
			Assert.That(RaidRoute.Waypoints(path, 0).Count, Is.EqualTo(10), "a spacing under one is one");
			Assert.That(RaidRoute.Waypoints(new List<CPos>(), 8), Is.Empty);
			Assert.That(RaidRoute.Waypoints(null, 8), Is.Empty);
		}

		[Test]
		public void TheExploredVerifierAndTheSearchCostExcludeEveryUnexploredCell()
		{
			var explored = new HashSet<CPos>(Line(10).Where(c => c.X != 6));
			bool IsExplored(CPos c) => explored.Contains(c);

			Assert.That(RaidRoute.FirstUnexplored(Line(10), IsExplored), Is.EqualTo(5));
			Assert.That(RaidRoute.AllExplored(Line(10), IsExplored), Is.False);
			Assert.That(RaidRoute.AllExplored(Line(5), IsExplored), Is.True);
			Assert.That(RaidRoute.AllExplored(new List<CPos>(), IsExplored), Is.False, "an empty path is no route");
			Assert.That(RaidRoute.FirstUnexplored(Line(5), IsExplored), Is.EqualTo(-1));

			var cost = RaidRoute.ExploredOnlyCost(IsExplored, int.MaxValue);
			Assert.That(cost(new CPos(2, 0)), Is.EqualTo(0));
			Assert.That(cost(new CPos(6, 0)), Is.EqualTo(int.MaxValue), "the engine's invalid-cost value excludes the cell");
			Assert.That(cost(new CPos(50, 50)), Is.EqualTo(int.MaxValue));
		}

		// A test-local breadth-first stand-in for the engine search with the explored-only cost (the engine excludes a cell whose custom cost is the invalid value).
		static List<CPos> Search(CPos from, CPos to, Func<CPos, int> cost, int size)
		{
			var prev = new Dictionary<CPos, CPos>();
			var queue = new Queue<CPos>();
			queue.Enqueue(from);
			prev[from] = from;
			while (queue.Count > 0)
			{
				var c = queue.Dequeue();
				if (c == to)
				{
					var path = new List<CPos>();
					for (var p = c; p != from; p = prev[p])
						path.Add(p);

					path.Reverse();
					return path;
				}

				for (var dx = -1; dx <= 1; dx++)
				for (var dy = -1; dy <= 1; dy++)
				{
					var n = new CPos(c.X + dx, c.Y + dy);
					if ((dx == 0 && dy == 0) || n.X < 0 || n.Y < 0 || n.X >= size || n.Y >= size || prev.ContainsKey(n) || cost(n) == int.MaxValue)
						continue;

					prev[n] = c;
					queue.Enqueue(n);
				}
			}

			return null;
		}

		[Test]
		public void AnExploredOnlySearchNeverCrossesUnknownTerrainAndWithoutALaneTheScoutAimIsUnexploredAndNearTheTarget()
		{
			const int size = 12;

			// Explored: the left part (x <= 4) and a corridor along y = 0 up to x = 8; the target (10, 10) lies in unexplored terrain.
			bool Explored(CPos c) => c.X >= 0 && c.Y >= 0 && c.X < size && c.Y < size && (c.X <= 4 || (c.Y == 0 && c.X <= 8));
			var cost = RaidRoute.ExploredOnlyCost(Explored, int.MaxValue);

			// A target inside the explored area has an explored-only lane.
			var lane = Search(new CPos(0, 0), new CPos(3, 11), cost, size);
			Assert.That(lane, Is.Not.Null);
			Assert.That(RaidRoute.AllExplored(lane, Explored), Is.True, "every cell of the selected route is explored");
			Assert.That(RaidRoute.Waypoints(lane, 4).All(Explored), Is.True);

			// The target behind unknown terrain has none: hold, and ask the scout.
			var target = new CPos(10, 10);
			Assert.That(Search(new CPos(0, 0), target, cost, size), Is.Null, "no explored lane to the target");

			var reachable = RaidRoute.FloodExplored(new CPos(0, 0), c => Explored(c), 500);
			Assert.That(reachable.All(Explored), Is.True);
			Assert.That(reachable[0], Is.EqualTo(new CPos(0, 0)), "the start is the first cell");
			var nearest = RaidRoute.NearestToTarget(reachable, target);
			Assert.That(nearest, Is.EqualTo(new CPos(4, 10)), "the explored-reachable cell nearest the target");

			var aim = RaidRoute.ScoutAim(reachable, target, Explored, c => c.X >= 0 && c.Y >= 0 && c.X < size && c.Y < size, 4);
			Assert.That(aim, Is.Not.Empty);
			Assert.That(aim.Count, Is.LessThanOrEqualTo(4));
			Assert.That(aim.Any(Explored), Is.False, "the scout aim cells are unexplored: a request, no vision");
			Assert.That(aim.Distinct().Count(), Is.EqualTo(aim.Count));
		}

		[Test]
		public void TheFloodIsBoundedDeterministicAndTheNearestCellBreaksTiesByCoordinates()
		{
			var flood = RaidRoute.FloodExplored(new CPos(5, 5), c => c.X >= 0 && c.Y >= 0 && c.X < 50 && c.Y < 50, 30);
			Assert.That(flood.Count, Is.EqualTo(30));
			Assert.That(flood.Distinct().Count(), Is.EqualTo(30));
			Assert.That(flood, Is.EqualTo(RaidRoute.FloodExplored(new CPos(5, 5), c => c.X >= 0 && c.Y >= 0 && c.X < 50 && c.Y < 50, 30)), "deterministic");
			Assert.That(RaidRoute.FloodExplored(new CPos(1, 1), _ => false, 10), Is.EqualTo(new[] { new CPos(1, 1) }));

			Assert.That(RaidRoute.NearestToTarget(new[] { new CPos(3, 0), new CPos(1, 0), new CPos(2, 5) }, new CPos(2, 0)), Is.EqualTo(new CPos(1, 0)), "squared distance 1 twice: the smaller X wins");
			Assert.That(RaidRoute.NearestToTarget(Array.Empty<CPos>(), new CPos(0, 0)), Is.Null);
		}

		[Test]
		public void TheEtaIsThePerStepWorldDistanceOverTheSlowestSpeedRoundedUpAndANoSpeedCellMakesItUnusable()
		{
			// Straight: 1024 per step at speed 100 = 11 ticks each (ceil 10.24).
			Assert.That(RaidRoute.Eta(new CPos(0, 0), Line(4), _ => 100), Is.EqualTo(4 * 11));

			// A diagonal step is 1448 world units: ceil(14.48) = 15 at speed 100.
			Assert.That(RaidRoute.Eta(new CPos(0, 0), new List<CPos> { new CPos(1, 1) }, _ => 100), Is.EqualTo(15));

			// The speed of the slowest member can differ per cell.
			Assert.That(RaidRoute.Eta(new CPos(0, 0), Line(2), c => c.X == 1 ? 1024 : 512), Is.EqualTo(1 + 2));
			Assert.That(RaidRoute.Eta(new CPos(0, 0), Line(3), c => c.X == 2 ? 0 : 100), Is.EqualTo(int.MaxValue));
			Assert.That(RaidRoute.Eta(new CPos(0, 0), new List<CPos>(), _ => 100), Is.EqualTo(0));
		}

		// ---- the phase machine and the release order -------------------------------------------------------------------------------

		[Test]
		public void ThePhaseMachineFollowsTheDesignTableAndReleasedIsTerminal()
		{
			Assert.That(RaidRecon.Next(RaidPhase.Travel, RaidEvent.InStrikeRange), Is.EqualTo(RaidPhase.Strike));
			Assert.That(RaidRecon.Next(RaidPhase.Strike, RaidEvent.InStrikeRange), Is.EqualTo(RaidPhase.Strike));
			Assert.That(RaidRecon.Next(RaidPhase.Travel, RaidEvent.TargetLost), Is.EqualTo(RaidPhase.LostRecon));
			Assert.That(RaidRecon.Next(RaidPhase.Strike, RaidEvent.TargetLost), Is.EqualTo(RaidPhase.LostRecon));
			Assert.That(RaidRecon.Next(RaidPhase.LostRecon, RaidEvent.TargetReacquired), Is.EqualTo(RaidPhase.Strike));
			Assert.That(RaidRecon.Next(RaidPhase.LostRecon, RaidEvent.ReconBudgetUsed), Is.EqualTo(RaidPhase.Released));
			Assert.That(RaidRecon.Next(RaidPhase.Strike, RaidEvent.ReconBudgetUsed), Is.EqualTo(RaidPhase.Strike), "does not apply");
			foreach (var p in new[] { RaidPhase.Travel, RaidPhase.Strike, RaidPhase.LostRecon })
				Assert.That(RaidRecon.Next(p, RaidEvent.Incident), Is.EqualTo(RaidPhase.Retreat));

			Assert.That(RaidRecon.Next(RaidPhase.Retreat, RaidEvent.Incident), Is.EqualTo(RaidPhase.Retreat));
			Assert.That(RaidRecon.Next(RaidPhase.Retreat, RaidEvent.RetreatDone), Is.EqualTo(RaidPhase.Released));
			Assert.That(RaidRecon.Next(RaidPhase.Strike, RaidEvent.RetreatDone), Is.EqualTo(RaidPhase.Strike));
			Assert.That(RaidRecon.Next(RaidPhase.Retreat, RaidEvent.TargetReacquired), Is.EqualTo(RaidPhase.Retreat));

			// Every release event ends the raid from every phase, and Released never leaves.
			var releases = new[] { RaidEvent.AssignmentLost, RaidEvent.JointOff, RaidEvent.ValueBelowMin, RaidEvent.AllInRecall };
			foreach (var ev in releases)
			{
				Assert.That(RaidRecon.IsRelease(ev), Is.True);
				foreach (RaidPhase p in Enum.GetValues(typeof(RaidPhase)))
					Assert.That(RaidRecon.Next(p, ev), Is.EqualTo(RaidPhase.Released), $"{p} + {ev}");
			}

			foreach (RaidEvent ev in Enum.GetValues(typeof(RaidEvent)))
				Assert.That(RaidRecon.Next(RaidPhase.Released, ev), Is.EqualTo(RaidPhase.Released), ev.ToString());

			Assert.That(RaidRecon.IsRelease(RaidEvent.Incident), Is.False);
		}

		[Test]
		public void ReleaseInvalidatesTheIntentFirstThenStopsThenReturnsToTheNormalState()
		{
			var intent = new RaidIntent(42, 7) { WaypointIndex = 3 };
			Assert.That(intent.IsCurrent(7), Is.True);
			Assert.That(intent.IsCurrent(8), Is.False);

			var order = new List<string>();
			intent.Release(
				() =>
				{
					order.Add("stop");
					Assert.That(intent.Valid, Is.False, "the intent is already invalid when the Stop is issued");
					Assert.That(intent.IsCurrent(7), Is.False, "a stale callback holding the old sequence can issue nothing");
					Assert.That(intent.Seq, Is.EqualTo(8), "the sequence was bumped first");
				},
				() =>
				{
					order.Add("normal");
					Assert.That(order, Is.EqualTo(new[] { "stop", "normal" }));
				});

			Assert.That(order, Is.EqualTo(new[] { "stop", "normal" }));
			Assert.That(intent.IsCurrent(7), Is.False, "no raid order after the release");
			Assert.That(intent.IsCurrent(8), Is.False);

			var again = 0;
			intent.Release(() => again++, () => again++);
			Assert.That(again, Is.EqualTo(0), "a second release does nothing");
			Assert.DoesNotThrow(() => new RaidIntent(1, 1).Release(null, null));
		}

		[Test]
		public void TheIncidentTriggerAndTheIncidentMemoryUseTheLearnedLossAndDecay()
		{
			// 25 percent of 1000 = 250: a loss of 250 triggers, 249 does not; no start value never triggers.
			Assert.That(RaidRecon.IncidentTriggered(1000, 750, 25), Is.True);
			Assert.That(RaidRecon.IncidentTriggered(1000, 751, 25), Is.False);
			Assert.That(RaidRecon.IncidentTriggered(0, 0, 25), Is.False);
			Assert.That(RaidRecon.IncidentTriggered(1000, 1200, 25), Is.False, "a gain is not a loss");

			Assert.That(RaidRecon.IncidentActive(100, 100, 3000), Is.True);
			Assert.That(RaidRecon.IncidentActive(100, 3099, 3000), Is.True);
			Assert.That(RaidRecon.IncidentActive(100, 3100, 3000), Is.False, "decayed");
			Assert.That(RaidRecon.IncidentActive(100, 50, 3000), Is.False, "not yet");

			var memory = new RaidIncidents();
			memory.Add(new CPos(10, 10), 100);
			memory.Add(new CPos(30, 30), 2000);
			Assert.That(memory.Near(new CPos(12, 9), 500, 3000, 3), Is.True);
			Assert.That(memory.Near(new CPos(20, 20), 500, 3000, 3), Is.False);
			Assert.That(memory.Near(new CPos(12, 9), 3200, 3000, 3), Is.False, "that incident decayed");
			memory.Prune(3200, 3000);
			Assert.That(memory.Count, Is.EqualTo(1));
		}

		// ---- the style axis ------------------------------------------------------------------------------------------------------

		[Test]
		public void TheStyleAxisIsTheTargetOwnersQualifiedStyleOnlyAndTheFallbackIsDirectlyThreeAxis()
		{
			// (a) every enemy's style unseen, (b) the target owner's style unseen while another enemy's is known, (e) the main target moves from a seen owner to an unseen owner: V3 exactly, no latch, no per-factor mixing.
			Assert.That(RaidStyle.Choose(false, false), Is.EqualTo(RaidStyleAxis.ThreeAxis), "(a) all unseen");
			Assert.That(RaidStyle.Choose(false, true), Is.EqualTo(RaidStyleAxis.ThreeAxis), "(b) the target's style unseen, an entry for another style may exist");
			Assert.That(RaidStyle.Choose(true, true), Is.EqualTo(RaidStyleAxis.FourAxis), "(c) the target's style qualified with an entry");
			Assert.That(RaidStyle.Choose(true, false), Is.EqualTo(RaidStyleAxis.SeedAtThreeAxisThenFourAxis), "(d) first sighting: the entry is created at V3");
			Assert.That(RaidStyle.Choose(false, true), Is.EqualTo(RaidStyle.Choose(false, false)), "(e) a seen owner followed by an unseen owner is V3 again");

			// The selector has no parameter for any other enemy's style: a known style of another enemy can never change the result ((f) a 1v1 reads the same).
			var parameters = typeof(RaidStyle).GetMethod(nameof(RaidStyle.Choose)).GetParameters();
			Assert.That(parameters.Select(p => p.Name), Is.EqualTo(new[] { "targetOwnerStyleQualified", "entryExists" }));
		}

		// ---- source pins ---------------------------------------------------------------------------------------------------------

		[Test]
		public void TheRaidFilesHavePrivateLearningMathNoWorldAccessNoActorDereferenceAndNoPathSearch()
		{
			foreach (var file in RaidFiles)
			{
				var text = RepoFile(file);
				foreach (var banned in new[] { "Math.Pow", "Math.Exp", "Math.Log", "Math.Sqrt", "Math.Cbrt", "Math.Round", "Random", "World", "GetActorById(", "FindPath", "BlockedByActor.Stationary", "BlockedByActor.Immovable", "double ", "float ", "decimal " })
					Assert.That(text, Does.Not.Contain(banned), file + " must not contain " + banned);
			}
		}

		[Test]
		public void TheSwitchIsOffInCodeAndInClassicAndInAllSixBlocksAndNothingIsWiredYet()
		{
			Assert.That(AblationIds.RaidHarv, Is.EqualTo("raid_harv"));
			Assert.That(Regex.IsMatch(AblationIds.RaidHarv, "^[a-z][a-z0-9]*(_[a-z0-9]+)+$"), Is.True, "safe in the lobby list");
			Assert.That(RepoFile(Bots + "BotModuleLogic/AblationIds.RaidHarv.cs"), Does.Contain("public const string RaidHarv = \"raid_harv\";"));

			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.RaidHarv.cs");
			Assert.That(info, Does.Contain("public readonly bool RaidHarvEnabled = false;"), "off in code");
			Assert.That(new SquadManagerBotModuleCAInfo().RaidHarvEnabled, Is.False);
			foreach (var line in new[]
			{
				"public readonly int RaidHarvLostReconTicks = 400;", "public readonly int RaidHarvMoveRefreshTicks = 125;", "public readonly int RaidHarvReqMarginPercent = 150;",
				"public readonly int RaidHarvReconRadiusCells = 8;", "public readonly int RaidHarvIncidentLossPercent = 25;", "public readonly int RaidHarvIncidentWindowTicks = 300;",
				"public readonly int RaidHarvIncidentDecayTicks = 3000;", "public readonly int RaidHarvWaypointSpacingCells = 8;", "public readonly int RaidHarvDefenceRadiusCells = 6;",
			})
				Assert.That(info, Does.Contain(line));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			Assert.That(yaml[classic..], Does.Not.Contain("RaidHarv"), "classic untouched");
			foreach (var key in new[]
			{
				"RaidHarvEnabled: false", "RaidHarvLostReconTicks: 400", "RaidHarvMoveRefreshTicks: 125", "RaidHarvReqMarginPercent: 150", "RaidHarvReconRadiusCells: 8",
				"RaidHarvIncidentLossPercent: 25", "RaidHarvIncidentWindowTicks: 300", "RaidHarvIncidentDecayTicks: 3000", "RaidHarvWaypointSpacingCells: 8", "RaidHarvDefenceRadiusCells: 6",
			})
				Assert.That(Count(yaml[..classic], "\n\t\t" + key + "\n"), Is.EqualTo(6), key);

			Assert.That(Count(yaml, "RaidHarvEnabled: true"), Is.EqualTo(0), "no row ships true until the A/B passes");

			// P1 wires nothing: the existing target pick, the harass cap and the team lead do not mention the raid yet (P2 replaces this pin with the single call site and the publish).
			foreach (var file in new[] { "SquadManagerBotModuleCA.TargetPick.cs", "SquadManagerBotModuleCA.HarassCap.cs", "SquadManagerBotModuleCA.TeamLead.cs", "SquadManagerBotModuleCA.cs", "SquadManagerBotModuleCA.EnemyScan.cs" })
				Assert.That(RepoFile(Bots + file), Does.Not.Contain("RaidHarv"), file);
		}
	}
}
