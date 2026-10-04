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
	}
}
