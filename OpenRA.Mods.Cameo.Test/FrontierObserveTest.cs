#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class FrontierObserveTest
	{
		// CB_frontier_observe (P3a): the shadow stage machine's pure helpers against
		// DESIGN_2026-10-12_protected_frontier §2/§6 — deadlines, the backoff ladder and
		// censor labels. Observation only: these helpers never decide anything.

		[Test]
		public void ClaimAndArmRunAtHalfTheStageDeadline()
		{
			var d = ExpansionPlannerBotModule.FrontierObserveCA.StageDeadlineTicks;
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.StageDeadline("claim", d), Is.EqualTo(d / 2));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.StageDeadline("arm", d), Is.EqualTo(d / 2));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.StageDeadline("extend", d), Is.EqualTo(d));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.StageDeadline("cover", d), Is.EqualTo(d));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.StageDeadline("hold", d), Is.EqualTo(d));
		}

		[Test]
		public void BackoffLadderDoublesThenCapsAtFour()
		{
			var b = ExpansionPlannerBotModule.FrontierObserveCA.BackoffTicks;
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.BackoffTicksFor(1, b), Is.EqualTo(b));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.BackoffTicksFor(2, b), Is.EqualTo(b * 2));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.BackoffTicksFor(3, b), Is.EqualTo(b * 4));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.BackoffTicksFor(9, b), Is.EqualTo(b * 4));
		}

		[Test]
		public void DeadlineExpiryIsInclusiveOfTheDeadline()
		{
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.DeadlineExpired(100, 9099, 9000), Is.False);
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.DeadlineExpired(100, 9100, 9000), Is.True);
		}

		[Test]
		public void StageCompletionFollowsTheObservedEvidenceContract()
		{
			// §2: claim wanted advances EXTEND, a completed refinery advances CLAIM,
			// met cover advances COVER, a completed producer advances ARM; HOLD never advances.
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.StageCompleted("extend", claimWanted: true, false, false, false), Is.True);
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.StageCompleted("extend", claimWanted: false, true, true, true), Is.False);
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.StageCompleted("claim", false, refineryClaimed: true, false, false), Is.True);
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.StageCompleted("cover", false, false, coverMet: true, false), Is.True);
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.StageCompleted("arm", false, false, false, producerCompleted: true), Is.True);
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.StageCompleted("hold", true, true, true, true), Is.False);
		}

		[Test]
		public void CensorReasonPrefersTheHardBoundThenTheCapThenTheWatch()
		{
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.CensorReason(true, true, true), Is.EqualTo("record_cap_full"));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.CensorReason(false, true, true), Is.EqualTo("max_active"));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.CensorReason(false, false, true), Is.EqualTo("watch_overflow"));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.CensorReason(false, false, false), Is.Null);
		}

		[Test]
		public void ShadowConstantsMatchTheDesignBounds()
		{
			// §6/§7 flat values — a drift here silently changes what the shadow measures.
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.StageDeadlineTicks, Is.EqualTo(9000));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.MaxActive, Is.EqualTo(3));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.CoverMinValue, Is.EqualTo(450));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.RadiusCells, Is.EqualTo(14));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.RecordCap, Is.EqualTo(32));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.WatchCap, Is.EqualTo(64));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.FreezeReplans, Is.EqualTo(12));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.MaxBackoffEpisodes, Is.EqualTo(3));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.ReentryReplans, Is.EqualTo(4));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.LossesToPark, Is.EqualTo(2));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.LossCellRadiusCells, Is.EqualTo(4));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.ParityResumeMarginPct, Is.EqualTo(10));
			Assert.That(ExpansionPlannerBotModule.FrontierObserveCA.MaxStalls, Is.EqualTo(2));
		}
	}
}
