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
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// TC-2e (AI_ARCHITECTURE.md §12.17): capture-claim arbitration on the team
	// blackboard. ClaimsAheadOf is the pure half — the union of every allied
	// CaptureClaims position whose publisher outranks the caller (strictly lower
	// ClientIndex wins). The module ticks that consume it stay integration-side.
	[TestFixture]
	public sealed class TeamCaptureClaimsTest
	{
		static readonly WPos ClaimA = new(1024, 2048, 0);
		static readonly WPos ClaimB = new(5120, 4096, 0);

		static TeamBroadcast Broadcast(int clientIndex, params WPos[] captureClaims) =>
			new(1500, 0, 0, 0, DirectorPhase.BuildUp, null, false, WPos.Zero,
				clientIndex, default, default, default, default, captureClaims);

		[Test]
		public void NullOrEmptyInputYieldsNoClaims()
		{
			Assert.That(TeamBlackboard.ClaimsAheadOf(null, 3), Is.Empty);
			Assert.That(TeamBlackboard.ClaimsAheadOf(new List<TeamBroadcast>(), 3), Is.Empty);
		}

		[Test]
		public void LowerIndexAllyClaimsAhead()
		{
			var claims = TeamBlackboard.ClaimsAheadOf(new[] { Broadcast(1, ClaimA) }, 3);
			Assert.That(claims, Does.Contain(ClaimA));
			Assert.That(claims, Has.Count.EqualTo(1));
		}

		[Test]
		public void HigherIndexAllyNeverOutranks()
		{
			var claims = TeamBlackboard.ClaimsAheadOf(new[] { Broadcast(5, ClaimA) }, 3);
			Assert.That(claims, Is.Empty,
				"an ally that publishes the same cell but indexes below ours keeps its claim unchallenged");
		}

		[Test]
		public void SameIndexIsNotAhead()
		{
			// Two broadcasts can never share a ClientIndex in a live match — CollectBroadcasts
			// reads one member per Player — but the helper itself must still hold strictly-lower.
			var claims = TeamBlackboard.ClaimsAheadOf(new[] { Broadcast(3, ClaimA) }, 3);
			Assert.That(claims, Is.Empty);
		}

		[Test]
		public void MultipleClaimantsOnOnePositionStillResolveToLowest()
		{
			// Two allies claim the same cell: index 1 outranks index 3, so member 4 and
			// member 3 both read the cell as claimed-ahead while the winner (1) reads none.
			var broadcasts = new[] { Broadcast(3, ClaimA), Broadcast(1, ClaimA) };
			Assert.That(TeamBlackboard.ClaimsAheadOf(broadcasts, 4), Does.Contain(ClaimA));
			Assert.That(TeamBlackboard.ClaimsAheadOf(broadcasts, 2), Does.Contain(ClaimA));
			Assert.That(TeamBlackboard.ClaimsAheadOf(broadcasts, 1), Is.Empty,
				"the lowest-index claimant sees nothing ahead — it is the winner by construction");
		}

		[Test]
		public void ClaimsUnionAcrossBroadcastsAndSkipNulls()
		{
			var broadcasts = new TeamBroadcast[] { null, Broadcast(0, ClaimA), Broadcast(1, ClaimB) };
			var claims = TeamBlackboard.ClaimsAheadOf(broadcasts, 2);
			Assert.That(claims, Is.EquivalentTo(new[] { ClaimA, ClaimB }));
		}

		[Test]
		public void BroadcastWithoutClaimsContributesNothing()
		{
			// captureClaims left at the ctor default stores an empty list, never null.
			var claims = TeamBlackboard.ClaimsAheadOf(new[] { Broadcast(0) }, 2);
			Assert.That(claims, Is.Empty);
		}
	}
}
