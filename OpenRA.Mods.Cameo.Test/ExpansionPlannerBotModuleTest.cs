#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class ExpansionPlannerBotModuleTest
	{
		// AI_ARCHITECTURE §12.13: score = V x S / (T + tau), S = 1 / (1 + threat / max(guard, 1)).
		static double Score(int value, int threat = 0, int guard = 0, int cost = 2000, int income = 10, int hops = 0)
		{
			return ExpansionPlannerBotModule.Score(value, threat, guard, cost, income, hops, 300, 900, 750, out _);
		}

		[Test]
		public void MoreValueRanksHigher()
		{
			Assert.That(Score(80), Is.GreaterThan(Score(40)));
		}

		[Test]
		public void ShorterDistanceRanksHigher()
		{
			Assert.That(Score(60, hops: 0), Is.GreaterThan(Score(60, hops: 3)));
		}

		[Test]
		public void FewerRememberedEnemiesRankSafer()
		{
			Assert.That(Score(60, threat: 0), Is.GreaterThan(Score(60, threat: 3000)));

			// Our own guard at the field offsets the threat.
			Assert.That(Score(60, threat: 3000, guard: 6000), Is.GreaterThan(Score(60, threat: 3000, guard: 0)));
		}

		[Test]
		public void PaybackTurnsCostIntoTicksAtTheCurrentIncome()
		{
			ExpansionPlannerBotModule.Score(60, 0, 0, 2000, 10, 2, 300, 900, 750, out var payback);

			// 2000 / 10 per tick + 2 links x 300 + the refinery's 900.
			Assert.That(payback, Is.EqualTo(200 + 600 + 900));
		}

		[Test]
		public void NoIncomeMakesEveryFieldSlowButStillRanksThem()
		{
			Assert.That(Score(80, income: 0), Is.GreaterThan(Score(40, income: 0)));
		}

		[Test]
		public void HopsCountTheBuildingsNeededToReachAField()
		{
			Assert.That(ExpansionPlannerBotModule.Hops(10, 12, 4), Is.EqualTo(0));
			Assert.That(ExpansionPlannerBotModule.Hops(13, 12, 4), Is.EqualTo(1));
			Assert.That(ExpansionPlannerBotModule.Hops(20, 12, 4), Is.EqualTo(2));
			Assert.That(ExpansionPlannerBotModule.Hops(21, 12, 4), Is.EqualTo(3));
		}

		[Test]
		public void ARefineryWithinTheClaimRadiusClaimsTheField()
		{
			var field = new CPos(16, 36);
			Assert.That(ExpansionPlannerBotModule.Claimed(field, new[] { new CPos(20, 40) }, 8), Is.True);
			Assert.That(ExpansionPlannerBotModule.Claimed(field, new[] { new CPos(30, 36) }, 8), Is.False);
			Assert.That(ExpansionPlannerBotModule.Claimed(field, new CPos[0], 8), Is.False);
		}

		[Test]
		public void RefineriesBuiltWithoutClaimingTheFieldParkItAfterTheLimit()
		{
			// Start wanting field 6 with 1 refinery owned.
			var (state, attempts, park) = ExpansionPlannerBotModule.TrackClaim((-1, 0), 6, 1, 0, 2);
			Assert.That(state, Is.EqualTo((6, 1)));
			Assert.That(park, Is.False);

			// A refinery appears elsewhere, the field is still unclaimed: one missed attempt.
			(state, attempts, park) = ExpansionPlannerBotModule.TrackClaim(state, 6, 2, attempts, 2);
			Assert.That(attempts, Is.EqualTo(1));
			Assert.That(park, Is.False);

			// A second miss reaches the limit: park the field instead of building refineries forever.
			(_, attempts, park) = ExpansionPlannerBotModule.TrackClaim(state, 6, 3, attempts, 2);
			Assert.That(attempts, Is.EqualTo(2));
			Assert.That(park, Is.True);
		}

		[Test]
		public void ANewTargetStartsItsOwnCount()
		{
			var (state, attempts, park) = ExpansionPlannerBotModule.TrackClaim((6, 3), 7, 5, 0, 2);
			Assert.That(state, Is.EqualTo((7, 5)));
			Assert.That(attempts, Is.EqualTo(0));
			Assert.That(park, Is.False);
		}

		static ExpansionPlannerBotModule.FieldScore Field(int index, int x, int y, int value, int hops, double safety = 1)
		{
			return new ExpansionPlannerBotModule.FieldScore(index, new CPos(x, y), value, hops, 0, 0, 0, safety);
		}

		[Test]
		public void TheMcvGoesToAFarFieldNeverOneTheBuildingLineReaches()
		{
			var fields = new[] { Field(1, 12, 12, 200, 1), Field(2, 40, 10, 60, 5) };
			var site = ExpansionPlannerBotModule.McvSite(fields, new CPos(10, 10), 3, 10);
			Assert.That(site?.Index, Is.EqualTo(2));
			Assert.That(ExpansionPlannerBotModule.McvSite(new[] { Field(1, 12, 12, 200, 1) }, new CPos(10, 10), 3, 10), Is.Null);
		}

		[Test]
		public void TheMcvWeighsValueSafetyAndDistance()
		{
			var mcv = new CPos(10, 10);

			// Same distance: the richer field wins.
			Assert.That(ExpansionPlannerBotModule.McvSite(new[] { Field(1, 40, 10, 60, 4), Field(2, 10, 40, 90, 4) }, mcv, 3, 10)?.Index, Is.EqualTo(2));

			// Same value: the safer field wins.
			Assert.That(ExpansionPlannerBotModule.McvSite(new[] { Field(1, 40, 10, 60, 4, 0.25), Field(2, 10, 40, 60, 4, 1) }, mcv, 3, 10)?.Index, Is.EqualTo(2));

			// Same value and safety: the nearer field wins.
			Assert.That(ExpansionPlannerBotModule.McvSite(new[] { Field(1, 60, 10, 60, 4), Field(2, 30, 10, 60, 4) }, mcv, 3, 10)?.Index, Is.EqualTo(2));
		}

		[Test]
		public void AnIneligibleFieldFallsOutAndTheNextBestIsOffered()
		{
			// LC3: an unreachable or parked field is skipped, never handed to the MCV again.
			var fields = new[] { Field(1, 40, 10, 90, 4), Field(2, 10, 40, 60, 4) };
			var mcv = new CPos(10, 10);
			Assert.That(ExpansionPlannerBotModule.McvSite(fields, mcv, 3, 10)?.Index, Is.EqualTo(1));
			Assert.That(ExpansionPlannerBotModule.McvSite(fields, mcv, 3, 10, f => f.Index != 1)?.Index, Is.EqualTo(2));
			Assert.That(ExpansionPlannerBotModule.McvSite(fields, mcv, 3, 10, f => false), Is.Null);
		}

		[Test]
		public void AFieldHandedOutTooOftenInARowIsParked()
		{
			// LC3: the MCV module only asks again for an idle MCV, so a repeat means the last attempt failed.
			var streak = (Field: -1, Count: 0);
			for (var i = 1; i <= 3; i++)
			{
				(streak, var park) = ExpansionPlannerBotModule.TrackMcvHandout(streak, 7, 3);
				Assert.That(park, Is.False);
				Assert.That(streak.Count, Is.EqualTo(i));
			}

			(streak, var parked) = ExpansionPlannerBotModule.TrackMcvHandout(streak, 7, 3);
			Assert.That(parked, Is.True);
			Assert.That(streak, Is.EqualTo((-1, 0)));

			// A different field restarts the streak; 0 disables parking.
			Assert.That(ExpansionPlannerBotModule.TrackMcvHandout((7, 3), 8, 3), Is.EqualTo(((8, 1), false)));
			Assert.That(ExpansionPlannerBotModule.TrackMcvHandout((7, 50), 7, 0).Park, Is.False);
		}

		[Test]
		public void BevSendsOnlyBaseBuildingVehiclesHome()
		{
			// Construction MCVs found bases at fields (EX-3); Yuri's slave miner and Japan's core refinery deploy into
			// refineries and go to fields too; Japan's other cores are base buildings and deploy at home.
			Assert.That(ExpansionPlannerBotModule.ClassifyMcv(constructionMcv: true, deploysIntoRefinery: false), Is.EqualTo(McvRole.Expansion));
			Assert.That(ExpansionPlannerBotModule.ClassifyMcv(constructionMcv: true, deploysIntoRefinery: true), Is.EqualTo(McvRole.Expansion),
				"a StarCraft command centre is a construction MCV that also accepts resources: still an expansion");
			Assert.That(ExpansionPlannerBotModule.ClassifyMcv(constructionMcv: false, deploysIntoRefinery: true), Is.EqualTo(McvRole.FieldRefinery));
			Assert.That(ExpansionPlannerBotModule.ClassifyMcv(constructionMcv: false, deploysIntoRefinery: false), Is.EqualTo(McvRole.BaseBuilding));
		}

		[Test]
		public void AFieldStaysInPlayWhileLiveOrRemembered()
		{
			Assert.That(ExpansionPlannerBotModule.FieldPresent(false, 50, true), Is.False, "no indice at all");
			Assert.That(ExpansionPlannerBotModule.FieldPresent(true, 50, false), Is.True);
			Assert.That(ExpansionPlannerBotModule.FieldPresent(true, 0, false), Is.False);
			Assert.That(ExpansionPlannerBotModule.FieldPresent(true, 0, true), Is.True,
				"a depleted but remembered field stays in play: resources regrow, the location does not move");
		}

		[Test]
		public void AFieldIsTakenOnceOursOrWorthless()
		{
			Assert.That(ExpansionPlannerBotModule.FieldTaken(0, false, 40), Is.False);
			Assert.That(ExpansionPlannerBotModule.FieldTaken(1, false, 40), Is.True, "our refinery sits in the indice");
			Assert.That(ExpansionPlannerBotModule.FieldTaken(0, true, 40), Is.True, "claimed by a refinery in radius");
			Assert.That(ExpansionPlannerBotModule.FieldTaken(0, false, 0), Is.True, "never worth anything");
		}

		[Test]
		public void UnderTheLawAnIndiceWithAnUnservedAnchorIsNotTaken()
		{
			const bool Law = true;
			const bool Unserved = true;

			// F1: one refinery per ANCHOR — a refinery in the indice takes it only when no anchor
			// there is still unserved; otherwise the second spreader of a single-indice field could
			// never be served from its own field.
			Assert.That(ExpansionPlannerBotModule.FieldTaken(1, false, 40, Law, Unserved), Is.False,
				"refinery present but an anchor in the indice is still unserved");
			Assert.That(ExpansionPlannerBotModule.FieldTaken(0, true, 40, Law, Unserved), Is.False,
				"claimed-by-radius also yields to an unserved anchor");
			Assert.That(ExpansionPlannerBotModule.FieldTaken(1, false, 40, Law, !Unserved), Is.True,
				"every anchor in the indice served — taken as before");
			Assert.That(ExpansionPlannerBotModule.FieldTaken(0, true, 40, Law, !Unserved), Is.True);
			Assert.That(ExpansionPlannerBotModule.FieldTaken(0, false, 40, Law, Unserved), Is.False,
				"a free field stays free, law or not");

			// Classic shape is unchanged: without the law the unserved flag cannot rescue anything.
			Assert.That(ExpansionPlannerBotModule.FieldTaken(1, false, 40, !Law, Unserved), Is.True);
			Assert.That(ExpansionPlannerBotModule.FieldTaken(0, true, 40, !Law, Unserved), Is.True);

			// A worthless indice stays out of the scores even under the law.
			Assert.That(ExpansionPlannerBotModule.FieldTaken(1, false, 0, Law, Unserved), Is.True);
		}

		[Test]
		public void OnlyAnOutrankingAllyClaimSteersUsOffAField()
		{
			var me = 3;
			var field = new WPos(5120, 5120, 0);
			var near = new WPos(5120 + 4096, 5120, 0);
			Assert.That(ExpansionPlannerBotModule.YieldToAllyClaim(null, field, me, 10), Is.False, "claims off");
			Assert.That(ExpansionPlannerBotModule.YieldToAllyClaim(new List<(int, WPos)>(), field, me, 10), Is.False, "no claims");
			Assert.That(ExpansionPlannerBotModule.YieldToAllyClaim(new List<(int, WPos)> { (1, near) }, field, me, 10), Is.True,
				"lower ClientIndex outranks us in radius");
			Assert.That(ExpansionPlannerBotModule.YieldToAllyClaim(new List<(int, WPos)> { (5, near) }, field, me, 10), Is.False,
				"a higher ClientIndex never outranks us");
			Assert.That(ExpansionPlannerBotModule.YieldToAllyClaim(new List<(int, WPos)> { (1, new WPos(5120 + 40960, 5120, 0)) }, field, me, 10), Is.False,
				"out of the claim radius");
		}

		[Test]
		public void ScoreModifiersApplyOnlyTheOnesPresent()
		{
			Assert.That(ExpansionPlannerBotModule.ApplyFieldScoreModifiers(100, null, null, null), Is.EqualTo(100));
			Assert.That(ExpansionPlannerBotModule.ApplyFieldScoreModifiers(100, 60, null, null), Is.EqualTo(60));
			Assert.That(ExpansionPlannerBotModule.ApplyFieldScoreModifiers(100, null, 1.5, null), Is.EqualTo(150));
			Assert.That(ExpansionPlannerBotModule.ApplyFieldScoreModifiers(100, null, null, 0.25), Is.EqualTo(25));
			Assert.That(ExpansionPlannerBotModule.ApplyFieldScoreModifiers(100, 50, 2.0, 0.5), Is.EqualTo(50));
		}

		[Test]
		public void AGoneOrForeignSiteHolderReleasesItsClaim()
		{
			Assert.That(ExpansionPlannerBotModule.SiteHolderGone(true, false, true, true), Is.False);
			Assert.That(ExpansionPlannerBotModule.SiteHolderGone(false, false, true, true), Is.True, "deployed or sold: the id no longer resolves");
			Assert.That(ExpansionPlannerBotModule.SiteHolderGone(true, true, true, true), Is.True, "dead");
			Assert.That(ExpansionPlannerBotModule.SiteHolderGone(true, false, false, true), Is.True, "left the world");
			Assert.That(ExpansionPlannerBotModule.SiteHolderGone(true, false, true, false), Is.True, "captured: a foreign MCV holds no claim of ours");
		}

		[Test]
		public void APendingCommitClearsOnExpiryOrService()
		{
			Assert.That(ExpansionPlannerBotModule.PendingCommitCleared(10, 20, false), Is.False);
			Assert.That(ExpansionPlannerBotModule.PendingCommitCleared(20, 20, false), Is.True, "the commit expired");
			Assert.That(ExpansionPlannerBotModule.PendingCommitCleared(10, 20, true), Is.True, "its refinery landed early");
		}

		[Test]
		public void ARefineryIsWantedWhileAnchorsOutnumberProduction()
		{
			Assert.That(ExpansionPlannerBotModule.AnchorRefineryWanted(2, 1), Is.True);
			Assert.That(ExpansionPlannerBotModule.AnchorRefineryWanted(1, 1), Is.False);
			Assert.That(ExpansionPlannerBotModule.AnchorRefineryWanted(0, 0), Is.False);
		}

		[Test]
		public void AStuckAnchorCountsOnlyIdleReplans()
		{
			Assert.That(ExpansionPlannerBotModule.AnchorStuckNext(false, 2, 1, 0, 3, 5).Replans, Is.EqualTo(0), "a different anchor restarts");
			Assert.That(ExpansionPlannerBotModule.AnchorStuckNext(true, 3, 2, 0, 3, 5).Replans, Is.EqualTo(0), "a refinery gained is progress");
			Assert.That(ExpansionPlannerBotModule.AnchorStuckNext(true, 2, 2, 1, 3, 5).Replans, Is.EqualTo(0), "one in production is progress");
			Assert.That(ExpansionPlannerBotModule.AnchorStuckNext(true, 2, 2, 0, 3, 5), Is.EqualTo((4, false)));
			Assert.That(ExpansionPlannerBotModule.AnchorStuckNext(true, 2, 2, 0, 4, 5), Is.EqualTo((5, true)));
			Assert.That(ExpansionPlannerBotModule.AnchorStuckNext(true, 2, 2, 0, 9, 0).Park, Is.False, "0 disables parking");
		}

		[Test]
		public void TheLawDisablesFieldClaimsAndKeepsItsOwnWant()
		{
			var best = Field(1, 10, 10, 80, 0);
			var (claim, want) = ExpansionPlannerBotModule.ClaimSelection(true, true, best);
			Assert.That(claim, Is.Null, "the law claims anchors, not fields");
			Assert.That(want, Is.True, "the law's own want passes through");

			(claim, want) = ExpansionPlannerBotModule.ClaimSelection(true, false, best);
			Assert.That(claim, Is.Null);
			Assert.That(want, Is.False);

			(claim, want) = ExpansionPlannerBotModule.ClaimSelection(false, true, best);
			Assert.That(claim, Is.EqualTo(best), "classic claims the best in-reach field");
			Assert.That(want, Is.True);

			(claim, want) = ExpansionPlannerBotModule.ClaimSelection(false, true, null);
			Assert.That(claim, Is.Null);
			Assert.That(want, Is.False, "no in-reach field: classic wants nothing, whatever the law last said");
		}

		[Test]
		public void TheCrawlAimsAtTheFieldCellNearestOurFrontier()
		{
			var frontier = new List<CPos> { new CPos(30, 10) };
			var target = Field(0, 40, 10, 90, 1);
			var fields = new List<RefineryField>
			{
				new(0, new List<CPos> { new(40, 10), new(41, 10) }),
				new(1, new List<CPos> { new(10, 40) })
			};
			Assert.That(ExpansionPlannerBotModule.LinkTargetEdge(fields, target, frontier), Is.EqualTo(new CPos(40, 10)),
				"field 0 is the target's field; its cell (40,10) is nearest the frontier");

			Assert.That(ExpansionPlannerBotModule.LinkTargetEdge(null, target, frontier), Is.Null);
			Assert.That(ExpansionPlannerBotModule.LinkTargetEdge(new List<RefineryField>(), target, frontier), Is.Null);
			Assert.That(ExpansionPlannerBotModule.LinkTargetEdge(fields, target, new List<CPos>()), Is.Null);
			Assert.That(ExpansionPlannerBotModule.LinkTargetEdge(new List<RefineryField> { new(0, new List<CPos>()) }, target, frontier), Is.Null,
				"the nearest field has no usable cells: the centre stays the aim, no fall-through to the next field");
		}

		// F4: the scores sort orders by score desc, then by indice index — List.Sort is unstable, so
		// a bare score comparison could rank tied fields differently across runtimes in MP.

		static ExpansionPlannerBotModule.FieldScore FieldScore(int index, double score) =>
			new(index, new CPos(index, 0), 40, 2, 100, 0, score);

		[Test]
		public void HigherScoreSortsFirstRegardlessOfIndex()
		{
			Assert.That(ExpansionPlannerBotModule.CompareFieldScore(FieldScore(9, 2.0), FieldScore(1, 1.0)), Is.LessThan(0));
			Assert.That(ExpansionPlannerBotModule.CompareFieldScore(FieldScore(1, 1.0), FieldScore(9, 2.0)), Is.GreaterThan(0));
		}

		[Test]
		public void AnExactScoreTieSortsTheLowestIndexFirst()
		{
			Assert.That(ExpansionPlannerBotModule.CompareFieldScore(FieldScore(3, 1.5), FieldScore(7, 1.5)), Is.LessThan(0));
			Assert.That(ExpansionPlannerBotModule.CompareFieldScore(FieldScore(7, 1.5), FieldScore(3, 1.5)), Is.GreaterThan(0));
			Assert.That(ExpansionPlannerBotModule.CompareFieldScore(FieldScore(5, 1.5), FieldScore(5, 1.5)), Is.EqualTo(0));
		}

		[Test]
		public void TheSortedListRanksTiedFieldsDeterministically()
		{
			var scores = new List<ExpansionPlannerBotModule.FieldScore>
			{
				FieldScore(7, 1.5), FieldScore(1, 2.0), FieldScore(3, 1.5)
			};
			scores.Sort(ExpansionPlannerBotModule.CompareFieldScore);
			Assert.That(scores[0].Index, Is.EqualTo(1));
			Assert.That(scores[1].Index, Is.EqualTo(3));
			Assert.That(scores[2].Index, Is.EqualTo(7));
		}

		// TF-1 (FINDINGS_2026-10-11_turtle_regression increment 1): the optional crawl-link episode —
		// a queue's StartProduction intent is a pending claim that becomes paid only when the item is
		// seen in its production queue, admission is atomic per owner, the route lives only while the
		// frontier keeps closing on the aim, and a changed target opens a fresh episode.

		static ExpansionPlannerBotModule.CrawlLinkAllowance Episode(CPos target, CPos aim, int distSq, int maxNonprogress = 4)
		{
			var ep = new ExpansionPlannerBotModule.CrawlLinkAllowance();
			ep.ForTarget(target);
			ep.NoteDistance(distSq, aim, maxNonprogress);
			return ep;
		}

		static Dictionary<object, HashSet<string>> InProduction(object owner, string item) =>
			new() { { owner, new HashSet<string> { item } } };

		/// <summary>The real admission path: the queue claims, then its queue shows the item in production.</summary>
		static void AdmitPaid(ExpansionPlannerBotModule.CrawlLinkAllowance ep, object owner, int cost, string item = "link")
		{
			Assert.That(ep.TryClaim(owner, item, cost, false, 0, 0), Is.True, "the admission gate claims the link");
			ep.Reconcile(InProduction(owner, item), 2);
		}

		[Test]
		public void ATruePowerDeficitAdmitsALinkTheAllowanceWouldRefuse()
		{
			// Case 1: the episode's cost headroom is spent, but mandatory power skips the allowance.
			var owner = new object();
			var target = new CPos(40, 10);
			var aim = new CPos(40, 10);
			var ep = Episode(target, aim, 400);
			AdmitPaid(ep, owner, 600);
			ep.NoteDistance(300, aim, 4); // the paid link advanced the frontier — the claim settles
			AdmitPaid(ep, owner, 400);
			ep.NoteDistance(200, aim, 4); // advanced again: only the paid-cost bound can refuse now

			Assert.That(ep.OptionalLinkAdmissible(200, 1000, 4, 200), Is.False,
				"paid cost at the allowance refuses the OPTIONAL link");
			Assert.That(ExpansionPlannerBotModule.OptionalLinkAdmitted(false, false), Is.False);
			Assert.That(ExpansionPlannerBotModule.OptionalLinkAdmitted(true, false), Is.True,
				"a real power deficit admits anyway — mandatory power never consults the allowance");
		}

		[Test]
		public void AnAdvancingAffordableLinkIsAdmittedSerially()
		{
			// Case 2: supply flows one outstanding claim at a time, each progress step re-arms it.
			var owner = new object();
			var aim = new CPos(40, 10);
			var ep = Episode(new CPos(40, 10), aim, 400);

			Assert.That(ep.OptionalLinkAdmissible(500, 6000, 4, 400), Is.True,
				"a measured frontier and headroom admit the first link");
			Assert.That(ep.TryClaim(owner, "link", 500, false, 6000, 4), Is.True);
			Assert.That(ep.OptionalLinkAdmissible(500, 6000, 4, 400), Is.False,
				"the outstanding claim must advance before the next is supplied");
			ep.Reconcile(InProduction(owner, "link"), 2); // accepted — now PAID, still outstanding
			Assert.That(ep.PaidCost, Is.EqualTo(500));
			Assert.That(ep.OptionalLinkAdmissible(500, 6000, 4, 400), Is.False,
				"the accepted-but-unadvanced link still holds the window");

			ep.NoteDistance(300, aim, 4); // the link landed forward — progress
			Assert.That(ep.AwaitingProgress, Is.EqualTo(0));
			Assert.That(ep.OptionalLinkAdmissible(500, 6000, 4, 300), Is.True,
				"progress re-arms the supply for the same episode");
		}

		[Test]
		public void ANonadvancingRouteParksAndThePaidCountBoundRefuses()
		{
			// Case 3: a link that never advances the frontier counts stalled re-plans to the park.
			var owner = new object();
			var aim = new CPos(40, 10);
			var ep = Episode(new CPos(40, 10), aim, 400);
			AdmitPaid(ep, owner, 500);
			for (var i = 0; i < 4; i++)
				ep.NoteDistance(400, aim, 4);
			Assert.That(ep.Parked, Is.True, "four measured re-plans without progress park the route");
			Assert.That(ep.OptionalLinkAdmissible(500, 6000, 4, 400), Is.False);

			// The paid-count bound: progressing links still exhaust the episode's allowance.
			var ep2 = Episode(new CPos(40, 10), aim, 400);
			for (var i = 0; i < 4; i++)
			{
				Assert.That(ep2.OptionalLinkAdmissible(500, 6000, 4, 400 - 100 * i), Is.True);
				AdmitPaid(ep2, owner, 500);
				ep2.NoteDistance(300 - 100 * i, aim, 4); // each link advances — progress, not stalls
			}

			Assert.That(ep2.PaidCount, Is.EqualTo(4));
			Assert.That(ep2.Parked, Is.False);
			Assert.That(ep2.OptionalLinkAdmissible(500, 6000, 4, 100), Is.False,
				"the fourth paid link spent the count allowance even with steady progress");
		}

		[Test]
		public void AChangedTargetOpensAFreshEpisode()
		{
			// Case 4: the parked route retires — a new target field gets a clean ledger.
			var owner = new object();
			var aim = new CPos(40, 10);
			var ep = Episode(new CPos(40, 10), aim, 400);
			AdmitPaid(ep, owner, 500);
			for (var i = 0; i < 4; i++)
				ep.NoteDistance(400, aim, 4);
			Assert.That(ep.Parked, Is.True);

			Assert.That(ep.ForTarget(new CPos(10, 40)), Is.True, "a different field resets the episode");
			Assert.That(ep.Parked, Is.False);
			Assert.That(ep.PaidCount, Is.EqualTo(0));
			Assert.That(ep.ForTarget(new CPos(10, 40)), Is.False, "the same target keeps the episode");

			ep.NoteDistance(500, new CPos(10, 40), 4);
			Assert.That(ep.OptionalLinkAdmissible(500, 6000, 4, 500), Is.True,
				"the new episode supplies once its own frontier is measured");
		}

		[Test]
		public void MissingProgressEvidenceRefusesAnOptionalLink()
		{
			// Case 5: no measured frontier distance — the want may not be supplied at all.
			var ep = new ExpansionPlannerBotModule.CrawlLinkAllowance();
			ep.ForTarget(new CPos(40, 10));
			Assert.That(ep.OptionalLinkAdmissible(500, 6000, 4, -1), Is.False,
				"an unmeasurable frontier is missing evidence, not a free pass");

			ep.NoteDistance(-1, null, 4); // a replan that could not measure counts nothing
			Assert.That(ep.NonprogressReplans, Is.EqualTo(0));
			Assert.That(ep.OptionalLinkAdmissible(500, 6000, 4, -1), Is.False);
		}

		[Test]
		public void TheFrontierAnchorIsTheBuildingCellNearestTheAim()
		{
			var aim = new CPos(40, 10);
			var frontier = new List<CPos> { new CPos(10, 10), new CPos(30, 12), new CPos(20, 30) };
			Assert.That(ExpansionPlannerBotModule.FrontierAnchor(frontier, aim), Is.EqualTo(new CPos(30, 12)),
				"the bounded search centres on the frontier cell nearest the aim, not the yard");

			Assert.That(ExpansionPlannerBotModule.FrontierAnchor(frontier, null), Is.Null);
			Assert.That(ExpansionPlannerBotModule.FrontierAnchor(new List<CPos>(), aim), Is.Null);
		}

		// TF-1 repair (Sol review of 5bedabace): admission is an atomic CLAIM at the queue's order,
		// not a published want; a claim charges only once its item is observed in production.

		[Test]
		public void TwoQueuesCompetingForTheWantOnlyOneClaimsIt()
		{
			// Both queues see the published want in the same window: the first TryClaim wins, the
			// second is refused — the want alone admitted nothing to anyone.
			var queueA = new object();
			var queueB = new object();
			var ep = Episode(new CPos(40, 10), new CPos(40, 10), 400);

			Assert.That(ep.TryClaim(queueA, "link", 500, false, 6000, 4), Is.True);
			Assert.That(ep.TryClaim(queueB, "link", 500, false, 6000, 4), Is.False,
				"a second queue in the same window is refused — no double admission");
			Assert.That(ep.TryClaim(queueB, "link", 500, false, 6000, 4), Is.False,
				"and it stays refused while the first claim is outstanding");
			Assert.That(ep.PaidCount, Is.EqualTo(0), "intents charged nothing");

			ep.Reconcile(InProduction(queueA, "link"), 2); // queue A's item accepted
			Assert.That(ep.PaidCount, Is.EqualTo(1));
			Assert.That(ep.TryClaim(queueB, "link", 500, false, 6000, 4), Is.False,
				"queue B stays refused while queue A's paid link awaits progress");

			ep.NoteDistance(300, new CPos(40, 10), 4); // the paid link advanced — claims settle
			Assert.That(ep.TryClaim(queueB, "link", 500, false, 6000, 4), Is.True,
				"progress re-opens the supply for the next claimant");
		}

		[Test]
		public void ARejectedIntentDropsUnchargedAndReopensSupply()
		{
			// The queue's order never makes it into production: the pending claim ages out, the
			// episode spends nothing, and the window re-opens for a fresh claimant.
			var owner = new object();
			var ep = Episode(new CPos(40, 10), new CPos(40, 10), 400);

			Assert.That(ep.TryClaim(owner, "link", 500, false, 6000, 4), Is.True);
			Assert.That(ep.AwaitingProgress, Is.EqualTo(1));
			Assert.That(ep.OptionalLinkAdmissible(500, 6000, 4, 400), Is.False,
				"the pending intent serializes the supply while it may still land");

			var nothing = new Dictionary<object, HashSet<string>>();
			ep.Reconcile(nothing, 2); // first unseen re-plan: still pending
			Assert.That(ep.AwaitingProgress, Is.EqualTo(1));
			ep.Reconcile(nothing, 2); // aged out: dropped
			Assert.That(ep.AwaitingProgress, Is.EqualTo(0));
			Assert.That(ep.PaidCost, Is.EqualTo(0));
			Assert.That(ep.PaidCount, Is.EqualTo(0),
				"a rejected intent never charges the episode");
			Assert.That(ep.OptionalLinkAdmissible(500, 6000, 4, 400), Is.True);
		}

		[Test]
		public void AnAcceptedLinkThatLeavesProductionKeepsItsCharge()
		{
			// A paid item that leaves the queue — completed or cancelled — consumed its spend
			// window: the charge stands and the claim clears for the next admission.
			var owner = new object();
			var ep = Episode(new CPos(40, 10), new CPos(40, 10), 400);
			AdmitPaid(ep, owner, 500);
			Assert.That(ep.PaidCost, Is.EqualTo(500));

			ep.Reconcile(new Dictionary<object, HashSet<string>>(), 2); // it left production
			Assert.That(ep.AwaitingProgress, Is.EqualTo(0));
			Assert.That(ep.PaidCost, Is.EqualTo(500), "accepted spend stays charged");
			Assert.That(ep.PaidCount, Is.EqualTo(1));
			Assert.That(ep.TryClaim(owner, "link", 500, false, 6000, 4), Is.True,
				"the cleared window admits the next link — the count bound is the real cap");
		}

		[Test]
		public void AMandatoryClaimBypassesEveryBound()
		{
			// Mandatory power outranks a spent allowance and even an outstanding optional claim.
			var queueA = new object();
			var queueB = new object();
			var ep = Episode(new CPos(40, 10), new CPos(40, 10), 400);
			Assert.That(ep.TryClaim(queueA, "link", 500, false, 6000, 4), Is.True);

			Assert.That(ep.TryClaim(queueB, "link", 500, true, 6000, 4), Is.True,
				"a real deficit claims even while an optional claim is outstanding");

			var ep2 = Episode(new CPos(40, 10), new CPos(40, 10), 400);
			Assert.That(ep2.TryClaim(queueA, "link", 500, false, 6000, 4), Is.True);
			for (var i = 0; i < 4; i++)
				ep2.NoteDistance(400, new CPos(40, 10), 4);
			Assert.That(ep2.Parked, Is.True);
			Assert.That(ep2.TryClaim(queueB, "link", 500, false, 6000, 4), Is.False);
			Assert.That(ep2.TryClaim(queueB, "link", 500, true, 6000, 4), Is.True,
				"mandatory power claims on a parked route");
		}

		[Test]
		public void PaidCountersSaturateInsteadOfWrapping()
		{
			var owner = new object();
			var ep = Episode(new CPos(40, 10), new CPos(40, 10), 400);
			AdmitPaid(ep, owner, int.MaxValue - 10);
			ep.NoteDistance(300, new CPos(40, 10), 4);
			AdmitPaid(ep, owner, int.MaxValue - 10);
			Assert.That(ep.PaidCost, Is.EqualTo(int.MaxValue), "the bound saturates, never wraps negative");
			Assert.That(ep.PaidCount, Is.EqualTo(2));
		}

		// CRAWL-SAFETY (FINDINGS_2026-10-12 S3): the front ledger — the corridor watch diffs own
		// building cells re-plan to re-plan, lost cells pool their counts within the merge radius
		// (a link rebuilt on the same farmed spot), the scout hold bounds an unexplored aim, and a
		// dead corridor diffs from nothing so it can never phantom-loss.

		[Test]
		public void TheCorridorDistanceIsToTheSegmentNotItsEnds()
		{
			var a = new CPos(10, 10);
			var b = new CPos(30, 10);
			Assert.That(ExpansionPlannerBotModule.CrawlFrontSafety.PointToSegmentDistanceSq(new CPos(20, 13), a, b),
				Is.EqualTo(9L), "a point three cells off the route measures three");
			Assert.That(ExpansionPlannerBotModule.CrawlFrontSafety.PointToSegmentDistanceSq(new CPos(22, 10), a, b),
				Is.EqualTo(0L), "a point on the route measures zero");
			Assert.That(ExpansionPlannerBotModule.CrawlFrontSafety.PointToSegmentDistanceSq(new CPos(40, 10), a, b),
				Is.EqualTo(100L), "beyond the far end it clamps to the endpoint");
			Assert.That(ExpansionPlannerBotModule.CrawlFrontSafety.PointToSegmentDistanceSq(new CPos(5, 5), a, b),
				Is.EqualTo(50L), "behind the frontier it clamps to the near endpoint");
		}

		[Test]
		public void TheFirstWatchIsABaselineAndLostCellsCount()
		{
			var safety = new ExpansionPlannerBotModule.CrawlFrontSafety();
			var cell = new CPos(20, 10);
			var watch = new List<CPos> { cell, new CPos(24, 10) };

			Assert.That(safety.NoteWatch(watch, 4, out _), Is.EqualTo(0),
				"the first set establishes the baseline — nothing is a loss yet");

			watch.RemoveAt(0);
			Assert.That(safety.NoteWatch(watch, 4, out var hottest), Is.EqualTo(1),
				"the vanished cell counts its first loss");
			Assert.That(hottest, Is.EqualTo(cell));
			Assert.That(safety.TotalLosses, Is.EqualTo(1));
		}

		[Test]
		public void LostCellsInsideTheMergeRadiusPoolTheirCounts()
		{
			// The S3 farm: the same ground rebuilt and lost again and again — losses at cells
			// within the merge radius sum into one farmed-cell count that trips the park bound.
			var safety = new ExpansionPlannerBotModule.CrawlFrontSafety();
			var first = new List<CPos> { new CPos(20, 10) };
			var second = new List<CPos> { new CPos(21, 11) };
			var third = new List<CPos> { new CPos(19, 12) };

			safety.NoteWatch(first, 4, out _);          // baseline
			safety.NoteWatch(second, 4, out _);          // (20,10) lost
			safety.NoteWatch(third, 4, out _);           // (21,11) lost
			var worst = safety.NoteWatch(new List<CPos>(), 4, out var hottest); // (19,12) lost

			Assert.That(worst, Is.EqualTo(3), "three same-spot losses pool through the merge radius");
			Assert.That(hottest, Is.EqualTo(new CPos(19, 12)));
			Assert.That(safety.LossesNear(new CPos(20, 10), 4), Is.EqualTo(3));
			Assert.That(safety.LossesNear(new CPos(50, 50), 4), Is.EqualTo(0),
				"a far cell sees none of the farm");
		}

		[Test]
		public void ACellSlidOutOfTheCorridorIsNotALoss()
		{
			// The frontier advancing past a watched building must not count it lost — only a cell
			// that is still inside the corridor when it disappears is a real loss.
			var safety = new ExpansionPlannerBotModule.CrawlFrontSafety();
			Predicate<CPos> nearFront = c => c.X >= 15; // the corridor's leading edge moved forward
			safety.NoteWatch(new List<CPos> { new CPos(20, 10), new CPos(8, 10) }, 4, out _, nearFront);

			Assert.That(safety.NoteWatch(new List<CPos> { new CPos(20, 10) }, 4, out _, nearFront),
				Is.EqualTo(0), "the cell left behind by the advance slid out — not lost");
			Assert.That(safety.TotalLosses, Is.EqualTo(0));

			Assert.That(safety.NoteWatch(new List<CPos>(), 4, out _, nearFront), Is.EqualTo(1),
				"the cell still inside the corridor that vanished counts");
		}

		[Test]
		public void ADeadCorridorCannotPhantomLoss()
		{
			var safety = new ExpansionPlannerBotModule.CrawlFrontSafety();
			safety.NoteWatch(new List<CPos> { new CPos(20, 10) }, 4, out _);
			safety.ResetCorridor(); // the want or frontier blinked off — the watch is void

			Assert.That(safety.NoteWatch(new List<CPos>(), 4, out _), Is.EqualTo(0),
				"the next watch re-baselines instead of counting the old cells as lost");
			Assert.That(safety.TotalLosses, Is.EqualTo(0));
		}

		[Test]
		public void TheScoutWaitBoundsAnUnexploredAim()
		{
			var safety = new ExpansionPlannerBotModule.CrawlFrontSafety();
			Assert.That(safety.ScoutHolds(3), Is.True, "the first unexplored re-plans hold");
			Assert.That(safety.ScoutHolds(3), Is.True);
			Assert.That(safety.ScoutHolds(3), Is.False,
				"the bound spent — a stubbornly unscouted aim must not freeze the crawl");

			safety.ResetScoutWait();
			Assert.That(safety.ScoutHolds(3), Is.True, "explored ground reset the wait");

			var unbounded = new ExpansionPlannerBotModule.CrawlFrontSafety();
			for (var i = 0; i < 20; i++)
				Assert.That(unbounded.ScoutHolds(0), Is.True, "a 0 bound holds until explored, forever");
		}

		[Test]
		public void TheParityGateAdmitsOnlyAtOrAboveTheFloor()
		{
			Assert.That(ExpansionPlannerBotModule.ArmyParityAdmits(30, 60), Is.False,
				"well below the remembered enemy — the crawl starves the army it needs");
			Assert.That(ExpansionPlannerBotModule.ArmyParityAdmits(60, 60), Is.True, "at parity the gate opens");
			Assert.That(ExpansionPlannerBotModule.ArmyParityAdmits(10000, 60), Is.True,
				"an unobserved enemy scores MaxRatio — no unseen army ever blocks");
			Assert.That(ExpansionPlannerBotModule.ArmyParityAdmits(0, 0), Is.True,
				"a disabled floor never gates");
		}

		[Test]
		public void TheEscortGateStillCrawlsIntoAContestedFrontWhenStronger()
		{
			// The maintainer's constraint: a contested crawl proceeds when the local force is
			// stronger — the gate refuses only an UNDERpowered escort, never the march itself.
			Assert.That(ExpansionPlannerBotModule.FrontGuardAdmits(2200, 2000, 110), Is.True,
				"guard at 110% of the remembered threat — the escorted crawl goes");
			Assert.That(ExpansionPlannerBotModule.FrontGuardAdmits(1000, 2000, 110), Is.False,
				"half the threat — the link waits for defenders instead");
			Assert.That(ExpansionPlannerBotModule.FrontGuardAdmits(0, 0, 110), Is.True,
				"an uncontested front never needs an escort");
			Assert.That(ExpansionPlannerBotModule.FrontGuardAdmits(0, 2000, 110), Is.False,
				"a bare frontier into a remembered army holds");
		}
	}
}
