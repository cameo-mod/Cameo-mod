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

using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class PrepositionDecisionEvalTest
	{
		// Hotspot #8 (NOTE_2026-10-04_nova_hotspot8_tree): the pure decisions of
		// PrepositionDefenceTick. All pins are against the pre-extraction literals.

		[Test]
		public void SelectChannelFollowsThePrecedenceLadder()
		{
			Assert.That(PrepositionDecisionEvalCA.SelectChannel(true, true, true, true), Is.EqualTo(PrepositionChannelCA.Threat));
			Assert.That(PrepositionDecisionEvalCA.SelectChannel(false, true, true, true), Is.EqualTo(PrepositionChannelCA.Request));
			Assert.That(PrepositionDecisionEvalCA.SelectChannel(false, false, true, true), Is.EqualTo(PrepositionChannelCA.DefendAnswer));
			Assert.That(PrepositionDecisionEvalCA.SelectChannel(false, false, false, true), Is.EqualTo(PrepositionChannelCA.AssistAnswer));
			Assert.That(PrepositionDecisionEvalCA.SelectChannel(false, false, false, false), Is.EqualTo(PrepositionChannelCA.None));
		}

		[Test]
		public void RallyForPicksTheGuardedPointOrTheDefenceSnap()
		{
			var request = new BotProtectionRequest(new CPos(40, 50), 3000, 6000);
			var threat = new BotPredictedThreat(new CPos(90, 90), 200, 5000, 100);

			// Request path: the guarded point verbatim — no building snap.
			Assert.That(PrepositionDecisionEvalCA.RallyFor(request, null, new CPos(1, 2)), Is.EqualTo(new CPos(40, 50)));

			// Threat path: the caller's nearest-defence-or-target result verbatim.
			Assert.That(PrepositionDecisionEvalCA.RallyFor(null, threat, new CPos(88, 89)), Is.EqualTo(new CPos(88, 89)));
			Assert.That(PrepositionDecisionEvalCA.RallyFor(null, threat, threat.Target), Is.EqualTo(new CPos(90, 90)));
		}

		[Test]
		public void HoldUntilTickBoundsTheRollingWindow()
		{
			var request = new BotProtectionRequest(new CPos(40, 50), 3000, 5000);
			var threat = new BotPredictedThreat(new CPos(90, 90), 200, 5000, 100);

			// request: min(expires, now + 10*interval)
			Assert.That(PrepositionDecisionEvalCA.HoldUntilTick(request, null, 1000, 300), Is.EqualTo(4000), "now+10*300=4000 < expires 5000");

			var farExpiry = new BotProtectionRequest(new CPos(40, 50), 3000, 9000);
			Assert.That(PrepositionDecisionEvalCA.HoldUntilTick(farExpiry, null, 1000, 300), Is.EqualTo(4000));

			var nearExpiry = new BotProtectionRequest(new CPos(40, 50), 3000, 3500);
			Assert.That(PrepositionDecisionEvalCA.HoldUntilTick(nearExpiry, null, 1000, 300), Is.EqualTo(3500), "expires bounds the window");

			// threat: now + eta + 10*interval
			Assert.That(PrepositionDecisionEvalCA.HoldUntilTick(null, threat, 1000, 300), Is.EqualTo(1000 + 200 + 3000));
		}

		[Test]
		public void IsEmergencyRallyReadsTheDoorstepRing()
		{
			var center = new CPos(100, 100);

			Assert.That(PrepositionDecisionEvalCA.IsEmergencyRally(new CPos(105, 100), center, 10), Is.True, "5 cells < radius 10");
			Assert.That(PrepositionDecisionEvalCA.IsEmergencyRally(new CPos(110, 100), center, 10), Is.True, "on the boundary is inside");
			Assert.That(PrepositionDecisionEvalCA.IsEmergencyRally(new CPos(111, 100), center, 10), Is.False);
			Assert.That(PrepositionDecisionEvalCA.IsEmergencyRally(new CPos(107, 107), center, 10), Is.True, "diagonal inside (49+49<=100)");
		}

		[Test]
		public void IsElectedResponderMatchesIdThenClientFallback()
		{
			Assert.That(PrepositionDecisionEvalCA.IsElectedResponder("me", 0, "me", 7), Is.True);
			Assert.That(PrepositionDecisionEvalCA.IsElectedResponder("other", 7, "me", 7), Is.False, "id mismatch wins over client match");
			Assert.That(PrepositionDecisionEvalCA.IsElectedResponder(null, 7, "me", 7), Is.True, "pre-id broadcast falls back to client index");
			Assert.That(PrepositionDecisionEvalCA.IsElectedResponder(null, 3, "me", 7), Is.False);
		}
	}
}
