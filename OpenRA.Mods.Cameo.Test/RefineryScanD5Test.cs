#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// REFSCAN D-5 (design _12 3be85dba + ERRATUM_01 9c33a59b + IMPL_DOC_NOTES_01): the World-free half of the
	/// site-first refinery search — the score order (T-12 field order), the K-best list, the reservation mask
	/// (F3), lints L-R and L-C, the two-verdict gate (T-5b), the v_last ordering-visit rule, the sec 4.4 class
	/// precedence with ruling C3 (T-14 (a)-(e)), and the Peek streak filter (T-12b). The world fixtures
	/// (T-1..T-4, T-6..T-11, T-15b, T-16) need a running world and are measured by the smoke game's metric.
	/// </summary>
	[TestFixture]
	public sealed class RefineryScanD5Test
	{
		static RefinerySiteScore S(int cls, int tier, int gap, long d2, int x, int y) => new(cls, tier, gap, d2, x, y);

		// ---- sec 3.2: S = (class, tier, gap, d2, X, Y), each field dominating every later one ----

		[Test]
		public void TheScoreIsLexicographicInTheDesignFieldOrder()
		{
			Assert.That(S(0, 9, 9, 999, 99, 99).CompareTo(S(1, 0, 0, 0, 0, 0)), Is.LessThan(0), "class first");
			Assert.That(S(0, 0, 9, 999, 99, 99).CompareTo(S(0, 1, 0, 0, 0, 0)), Is.LessThan(0), "then tier");
			Assert.That(S(0, 0, 0, 999, 99, 99).CompareTo(S(0, 0, 1, 0, 0, 0)), Is.LessThan(0), "then gap");
			Assert.That(S(0, 0, 0, 1, 99, 99).CompareTo(S(0, 0, 0, 2, 0, 0)), Is.LessThan(0), "then d2");
			Assert.That(S(0, 0, 0, 1, 1, 99).CompareTo(S(0, 0, 0, 1, 2, 0)), Is.LessThan(0), "then X");
			Assert.That(S(0, 0, 0, 1, 1, 1).CompareTo(S(0, 0, 0, 1, 1, 2)), Is.LessThan(0), "then Y");
			Assert.That(S(0, 0, 0, 1, 1, 1).CompareTo(S(0, 0, 0, 1, 1, 1)), Is.Zero);
		}

		// ---- sec 3.4: the K = 4 best-so-far list ----

		sealed class C
		{
			public CPos Cell;
			public RefinerySiteScore Score;
		}

		static int Put(List<C> best, int x, RefinerySiteScore s) =>
			RefinerySiteSearchLogic.InsertBest(best, new C { Cell = new CPos(x, 0), Score = s }, c => c.Score, c => c.Cell);

		[Test]
		public void TheBestListKeepsTheFourLowestScoresInOrderWithUniqueCells()
		{
			var best = new List<C>();
			Assert.That(Put(best, 1, S(1, 0, 5, 0, 1, 0)), Is.EqualTo(0));
			Assert.That(Put(best, 2, S(0, 2, 0, 4, 2, 0)), Is.EqualTo(0), "class 0 goes ahead of class 1");
			Assert.That(Put(best, 3, S(0, 0, 1, 9, 3, 0)), Is.EqualTo(0));
			Assert.That(Put(best, 4, S(0, 0, 0, 9, 4, 0)), Is.EqualTo(0));
			Assert.That(Put(best, 5, S(1, 0, 9, 0, 5, 0)), Is.EqualTo(-1), "worse than the fourth with the list full");
			Assert.That(Put(best, 6, S(0, 1, 0, 0, 6, 0)), Is.EqualTo(2), "a better one pushes the worst out");
			Assert.That(best.Select(c => c.Cell.X), Is.EqualTo(new[] { 4, 3, 6, 2 }));

			Assert.That(Put(best, 3, S(0, 0, 1, 99, 3, 0)), Is.EqualTo(-1), "the same cell rescored worse keeps the better score");
			Assert.That(Put(best, 2, S(0, 0, 0, 0, 2, 0)), Is.EqualTo(0), "the same cell rescored better moves up, no duplicate");
			Assert.That(best.Select(c => c.Cell.X), Is.EqualTo(new[] { 2, 4, 3, 6 }));
			Assert.That(best.Count, Is.EqualTo(RefinerySiteSearchLogic.K));
		}

		// ---- sec 3.4 F2 / F3: the reservation mask ----

		[Test]
		public void TheOwnerIsExemptFromItsOwnMaskAndEveryOtherCallerIsBlocked()
		{
			var mask = new RefineryReservationMask();
			var a = new[] { new CPos(1, 1), new CPos(2, 1) };
			mask.Set(7, a);
			Assert.That(mask.Blocks(new[] { new CPos(2, 1), new CPos(3, 1) }, 7), Is.False, "F3: the owning episode is exempt");
			Assert.That(mask.Blocks(new[] { new CPos(2, 1) }, 8), Is.True, "another episode is blocked");
			Assert.That(mask.Blocks(new[] { new CPos(1, 1) }, RefineryReservationMask.None), Is.True, "a non-episode caller is blocked");
			Assert.That(mask.Blocks(new[] { new CPos(5, 5) }, RefineryReservationMask.None), Is.False, "an unreserved cell is free");

			mask.Set(8, new[] { new CPos(2, 1), new CPos(9, 9) });
			Assert.That(mask.TryOwner(new CPos(2, 1), out var o) && o == 7, Is.True, "a held cell is never taken over");
			mask.Release(8);
			Assert.That(mask.TryOwner(new CPos(2, 1), out o) && o == 7, Is.True, "a release frees only the releaser's own cells");
			Assert.That(mask.Blocks(new[] { new CPos(9, 9) }, RefineryReservationMask.None), Is.False);

			mask.Set(7, new[] { new CPos(4, 4) });
			Assert.That(mask.Blocks(a, RefineryReservationMask.None), Is.False, "re-reserving replaces the old site");
			mask.Release(7);
			Assert.That(mask.IsEmpty, Is.True, "the end of an episode releases its reservation");
		}

		// ---- sec 4.4: lint L-R, and sec 3.6: lint L-C and the two-verdict gate ----

		[Test]
		public void LintLRRefusesEveryFailingBotCountByName()
		{
			Assert.That(RefinerySiteSearchLogic.LatencyLintFailures(9), Is.Empty, "the design example: 42 and 64 both pass");
			Assert.That(RefinerySiteSearchLogic.LatencyLintFailures(14), Is.Empty, "2 * (23 + 14) = 74");
			Assert.That(RefinerySiteSearchLogic.LatencyLintFailures(15), Is.EqualTo(new[] { "re-pack V=23" }));
			Assert.That(RefinerySiteSearchLogic.LatencyLintFailures(26), Is.EqualTo(new[] { "N=12", "re-pack V=23" }),
				"2 * (12 + 26) = 76 fails, 2 * (11 + 26) = 74 passes");
		}

		[Test]
		public void LintLCAndTheTwoVerdictRuleHaveTheirOwnKnob()
		{
			Assert.That(RefinerySiteSearchLogic.CancelKnobLintFails(true, false), Is.True, "search on, two-verdict off: refused");
			Assert.That(RefinerySiteSearchLogic.CancelKnobLintFails(true, true), Is.False);
			Assert.That(RefinerySiteSearchLogic.CancelKnobLintFails(false, false), Is.False);

			Assert.That(RefinerySiteSearchLogic.CancelRequiresTwoVerdicts(false, 1500), Is.True, "CONTROL keeps today's gate");
			Assert.That(RefinerySiteSearchLogic.CancelRequiresTwoVerdicts(true, 0), Is.True, "TREATMENT: force off, rule kept");
			Assert.That(RefinerySiteSearchLogic.CancelRequiresTwoVerdicts(false, 0), Is.False, "neither: the single-verdict gate");
		}

		// ---- sec 4.3 / E-2: v_last from the real visit schedule ----

		[Test]
		public void VLastIsTheLastVisitWhoseOrderCanLandByTheDeadline()
		{
			const int tR = 1000;
			const int lo = 9;
			var visits = Enumerable.Range(0, 7).Select(i => tR + 12 * i).ToList();
			var ordering = visits.Where(v => RefinerySiteSearchLogic.IsOrderingVisit(v, tR, lo)).ToList();
			Assert.That(ordering.Last(), Is.EqualTo(tR + 60), "E-2: 60 + 9 <= 75 < 72 + 9");
			Assert.That(RefinerySiteSearchLogic.IsOrderingVisit(tR + 66, tR, lo), Is.True, "a re-pack moves v_last with the real schedule");
			Assert.That(RefinerySiteSearchLogic.IsOrderingVisit(tR + 67, tR, lo), Is.False);
		}

		// ---- sec 4.4 precedence, ruling C3 / D1, T-14 ----

		static RefineryEpisodeFacts Placed(bool afterVLast = false, bool held = false, int rej = 0, bool mcv = false,
			RefineryPlacedVia via = RefineryPlacedVia.ReservedHit) => new()
		{
			ReadyTick = 1000, EndTick = 1040, End = RefineryEpisodeEnd.Placed, EverFound = true,
			OrderIssuedAfterVLast = afterVLast, HeldAtVLast = held, Rejections = rej, McvOverlap = mcv, Via = via,
		};

		static RefineryEpisodeFacts Ended(RefineryEpisodeEnd end, int endTick, bool found, int? ready = 1000) => new()
		{
			ReadyTick = ready, EndTick = endTick, End = end, EverFound = found,
		};

		static string Cls(in RefineryEpisodeFacts f) => RefinerySiteSearchLogic.Classify(f);

		[Test]
		public void T14ATheRejectionOfAnOrderHeldAtVLastIsLateSite()
		{
			Assert.That(Cls(Placed(afterVLast: true, held: true, rej: 1)), Is.EqualTo("late-site"));
			Assert.That(Cls(Placed(rej: 1)), Is.EqualTo("rejected-retry"), "a rejection retried in time meets the full bound");
		}

		[Test]
		public void T14BNoReservedSiteAtVLastAndAFindAfterItIsLateFirst()
		{
			Assert.That(Cls(Placed(afterVLast: true, held: false, via: RefineryPlacedVia.DeadlinePhase)), Is.EqualTo("late-first"));
			Assert.That(Cls(Placed(afterVLast: false, via: RefineryPlacedVia.DeadlinePhase)), Is.EqualTo("deadline-phase"),
				"made available at v_last itself: placed by the deadline, not late-first");
			Assert.That(RefinerySiteSearchLogic.GateFailureClass("late-first"), Is.True);
		}

		[Test]
		public void T14CNeverFoundIsSitelessOnly()
		{
			Assert.That(Cls(Ended(RefineryEpisodeEnd.CancelledNoSite, 1500, false)), Is.EqualTo("SITELESS"));
			Assert.That(Cls(Ended(RefineryEpisodeEnd.ProducerLost, 1500, false)), Is.EqualTo("SITELESS"));
			Assert.That(RefinerySiteSearchLogic.GateFailureClass("SITELESS"), Is.True);
		}

		[Test]
		public void T14DFoundButNeverPlacedIsUnplacedAndAnEarlyGameEndIsCensored()
		{
			foreach (var end in new[] { RefineryEpisodeEnd.Cancelled, RefineryEpisodeEnd.ProducerLost, RefineryEpisodeEnd.LawOff })
				Assert.That(Cls(Ended(end, 1100, true)), Is.EqualTo("UNPLACED"), end.ToString());

			Assert.That(Cls(Ended(RefineryEpisodeEnd.GameEnd, 1075, true)), Is.EqualTo("UNPLACED"), "game end at t_R' + 75");
			Assert.That(Cls(Ended(RefineryEpisodeEnd.GameEnd, 1074, true)), Is.EqualTo("censored"), "deadline never reached");
			Assert.That(Cls(Ended(RefineryEpisodeEnd.GameEnd, 1074, false)), Is.EqualTo("censored"), "censored ranks above SITELESS (C2)");
			Assert.That(RefinerySiteSearchLogic.GateFailureClass("UNPLACED"), Is.True);
			Assert.That(RefinerySiteSearchLogic.GateFailureClass("censored"), Is.False);
		}

		[Test]
		public void T14ETheErratumScenarioIsLateFirstAndItsCompanionIsLateSite()
		{
			// A ordered at t_R', rejection seen at t_R' + 12, nothing legal up to v_last = t_R' + 60, B found at t_R' + 72.
			Assert.That(Cls(Placed(afterVLast: true, held: false, rej: 1, via: RefineryPlacedVia.DeadlinePhase)), Is.EqualTo("late-first"),
				"late-first, not late-site, although an earlier order was rejected");

			// The order issued at v_last is rejected after v_last and re-ordered after it.
			Assert.That(Cls(Placed(afterVLast: true, held: true, rej: 1)), Is.EqualTo("late-site"));
		}

		[Test]
		public void RulingC3EndsBeforeReadyArePreReadyUnlessTheNoSiteCancelEndedThem()
		{
			Assert.That(Cls(Ended(RefineryEpisodeEnd.Cancelled, 900, true)), Is.EqualTo("PRE_READY_END"));
			Assert.That(Cls(Ended(RefineryEpisodeEnd.ProducerLost, 900, false, null)), Is.EqualTo("PRE_READY_END"));
			Assert.That(Cls(Ended(RefineryEpisodeEnd.CancelledNoSite, 900, false, null)), Is.EqualTo("SITELESS"));
			Assert.That(RefinerySiteSearchLogic.GateFailureClass("PRE_READY_END"), Is.False);
		}

		[Test]
		public void ThePlacedClassesFollowThePrecedence()
		{
			Assert.That(Cls(Placed(mcv: true, rej: 1, via: RefineryPlacedVia.DeadlinePhase)), Is.EqualTo("mcv-overlap"));
			Assert.That(Cls(Placed(rej: 2, via: RefineryPlacedVia.NextBest)), Is.EqualTo("rejected-retry"));
			Assert.That(Cls(Placed(via: RefineryPlacedVia.DeadlinePhase)), Is.EqualTo("deadline-phase"));
			Assert.That(Cls(Placed(via: RefineryPlacedVia.NextBest)), Is.EqualTo("next-best"));
			Assert.That(Cls(Placed()), Is.EqualTo("reserved-hit"));
			Assert.That(Cls(default), Is.EqualTo("open"));
		}

		// ---- sec 3.3 (iii), R1: T-12b, the Peek streak filter ----

		static readonly CPos A = new(1, 0);
		static readonly CPos B = new(2, 0);
		static readonly CPos Cc = new(3, 0);
		static readonly CPos D = new(4, 0);

		// The ladder's own rule (ComputeClaim): offers = streak + 1, park and remove when offers > attempts.
		static int? LadderOffer(List<int> order, IReadOnlyList<CPos> anchors, Dictionary<CPos, int> streaks, int attempts)
		{
			var o = order.ToList();
			while (o.Count > 0)
			{
				var offers = (streaks.TryGetValue(anchors[o[0]], out var s) ? s : 0) + 1;
				if (offers > attempts)
				{
					o.RemoveAt(0);
					continue;
				}

				return o[0];
			}

			return null;
		}

		[TestCase(1)]
		[TestCase(2)]
		public void T12bPeekKeepsTheStreakEdgeBelowAndDropsTheEdgeAtAttempts(int attempts)
		{
			var anchors = new[] { A, B, Cc, D };

			// D is parked: AnchorBlockedForClaims already keeps it out of ClaimOrder, so the order lacks it.
			var order = new List<int> { 2, 0, 1 };
			var streaks = new Dictionary<CPos, int> { [B] = attempts - 1, [Cc] = attempts };
			var before = streaks.ToDictionary(kv => kv.Key, kv => kv.Value);

			var kept = ExpansionPlannerBotModule.PeekFilter(order, anchors, streaks, attempts);
			Assert.That(kept, Is.EqualTo(new[] { 0, 1 }), "C (streak = attempts) dropped; A (no entry) and B (attempts - 1) kept, in order");
			Assert.That(streaks, Is.EqualTo(before), "Peek writes nothing");
			Assert.That(kept.First(), Is.EqualTo(LadderOffer(order, anchors, new Dictionary<CPos, int>(streaks), attempts)),
				"the first Peek member is the claim the ladder offers");
		}
	}

	/// <summary>REFSCAN T-5b (F1): the cancel gate on its own knob, on the real scan's verdict chain.</summary>
	public sealed partial class StallFix01Test
	{
		[Test]
		public void T5bTreatmentKnobsOneExhaustedDoesNotCancelTheSecondOneAnEpochLaterDoes()
		{
			var treatment = RefinerySiteSearchLogic.CancelRequiresTwoVerdicts(true, 0);
			var control = RefinerySiteSearchLogic.CancelRequiresTwoVerdicts(false, Bound);
			var w = new FakeWorld();
			var scan = NewScan();
			Assert.That(Force(scan, w, 1000), Is.EqualTo(RefineryFallbackScan.Verdict.Exhausted));
			Assert.That(scan.CertificateCurrent(), Is.True);
			Assert.That(scan.CancelCertificateCurrent(treatment), Is.False, "TREATMENT: one Exhausted with a current certificate holds");
			Assert.That(scan.CancelCertificateCurrent(control), Is.EqualTo(scan.CancelCertificateCurrent(Bound)), "CONTROL is today's gate");
			Assert.That(scan.CancelCertificateCurrent(false), Is.True, "a gate derived from the force knob alone would cancel here");

			Assert.That(Force(scan, w, 1000 + Epoch), Is.EqualTo(RefineryFallbackScan.Verdict.Exhausted));
			Assert.That(scan.CancelCertificateCurrent(treatment), Is.True, "the second verdict one relist epoch later cancels");
			Assert.That(scan.CancelCertificateCurrent(control), Is.EqualTo(scan.CancelCertificateCurrent(Bound)));
		}
	}
}
