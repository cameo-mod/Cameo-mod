#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// Hotspot #7 (ORDERS_2026-10-04b): TickContests' decision seams extracted pure — every branch covered,
	// and the laziness that protects the heartbeat TryClaim and the null claims set is asserted.
	[TestFixture]
	public sealed class GarrisonContestRulesTest
	{
		[Test]
		public void NoClaimSetMeansNoSupersedeProbe()
		{
			var probed = false;
			Assert.That(GarrisonContestRules.SupersedesClaim(false, true, () => { probed = true; return true; }), Is.False);
			Assert.That(probed, Is.False);
		}

		[Test]
		public void AClaimWithoutARecordedCellIsNotSuperseded()
		{
			var probed = false;
			Assert.That(GarrisonContestRules.SupersedesClaim(true, false, () => { probed = true; return true; }), Is.False);
			Assert.That(probed, Is.False);
		}

		[Test]
		public void OnlyAnOutrankedCellSupersedesTheClaim()
		{
			Assert.That(GarrisonContestRules.SupersedesClaim(true, true, () => false), Is.False);
			Assert.That(GarrisonContestRules.SupersedesClaim(true, true, () => true), Is.True);
		}

		[Test]
		public void AClaimWithoutAStartTickCannotTimeOut()
		{
			Assert.That(GarrisonContestRules.ClaimTimedOut(false, int.MaxValue, 500), Is.False);
		}

		[Test]
		public void AClaimTimesOutOnlyStrictlyPastTheLimit()
		{
			Assert.That(GarrisonContestRules.ClaimTimedOut(true, 500, 500), Is.False);
			Assert.That(GarrisonContestRules.ClaimTimedOut(true, 501, 500), Is.True);
		}

		[Test]
		public void InsideDeadGoneOrStolenWalkersAreDone()
		{
			Assert.That(GarrisonContestRules.WalkerDone(true, false, true, true, () => false), Is.True);
			Assert.That(GarrisonContestRules.WalkerDone(false, true, true, true, () => false), Is.True);
			Assert.That(GarrisonContestRules.WalkerDone(false, false, false, true, () => false), Is.True);
			Assert.That(GarrisonContestRules.WalkerDone(false, false, true, false, () => false), Is.True);
		}

		[Test]
		public void TheLeaseHeartbeatNeverRunsForADoneWalker()
		{
			var probed = false;
			Assert.That(GarrisonContestRules.WalkerDone(true, false, true, true, () => { probed = true; return true; }), Is.True);
			Assert.That(probed, Is.False, "TryClaim is a renewal side effect — a done walker must not heartbeats it");
		}

		[Test]
		public void ALiveWalkerIsDoneOnlyWhenItsLeaseIsLost()
		{
			Assert.That(GarrisonContestRules.WalkerDone(false, false, true, true, () => false), Is.False);
			Assert.That(GarrisonContestRules.WalkerDone(false, false, true, true, () => true), Is.True);
		}

		[Test]
		public void EveryContestabilityGateHolds()
		{
			Assert.That(GarrisonContestRules.IsContestable(true, true, true, false, false, false, true, true), Is.True);
		}

		[Test]
		public void EachContestGateRejectsOnItsOwn()
		{
			Assert.That(GarrisonContestRules.IsContestable(false, true, true, false, false, false, true, true), Is.False, "dead");
			Assert.That(GarrisonContestRules.IsContestable(true, false, true, false, false, false, true, true), Is.False, "not neutral");
			Assert.That(GarrisonContestRules.IsContestable(true, true, false, false, false, false, true, true), Is.False, "full");
			Assert.That(GarrisonContestRules.IsContestable(true, true, true, true, false, false, true, true), Is.False, "claimed by us");
			Assert.That(GarrisonContestRules.IsContestable(true, true, true, false, true, false, true, true), Is.False, "blocked window");
			Assert.That(GarrisonContestRules.IsContestable(true, true, true, false, false, true, true, true), Is.False, "claimed by ally");
			Assert.That(GarrisonContestRules.IsContestable(true, true, true, false, false, false, false, true), Is.False, "unexplored - fog-honest");
			Assert.That(GarrisonContestRules.IsContestable(true, true, true, false, false, false, true, false), Is.False, "outside radius");
		}

		[Test]
		public void AContestWalkerMustBeAliveIdleAbleToFightAndFree()
		{
			Assert.That(GarrisonContestRules.IsContestWalker(true, true, true, false), Is.True);
			Assert.That(GarrisonContestRules.IsContestWalker(false, true, true, false), Is.False);
			Assert.That(GarrisonContestRules.IsContestWalker(true, false, true, false), Is.False);
			Assert.That(GarrisonContestRules.IsContestWalker(true, true, false, false), Is.False);
			Assert.That(GarrisonContestRules.IsContestWalker(true, true, true, true), Is.False);
		}

		[Test]
		public void CapacityStopsOnClaimsLeasesOrPool()
		{
			Assert.That(GarrisonContestRules.CapacityReached(4, 4, 0, 8, 3), Is.True, "claims full");
			Assert.That(GarrisonContestRules.CapacityReached(0, 4, 8, 8, 3), Is.True, "lease budget out");
			Assert.That(GarrisonContestRules.CapacityReached(0, 4, 0, 8, 0), Is.True, "pool empty");
			Assert.That(GarrisonContestRules.CapacityReached(3, 4, 7, 8, 1), Is.False);
		}

		[Test]
		public void ClaimsFullIsTheEarlyOutForm()
		{
			Assert.That(GarrisonContestRules.ClaimsFull(4, 4), Is.True);
			Assert.That(GarrisonContestRules.ClaimsFull(3, 4), Is.False);
		}

		[Test]
		public void DesiredWalkersClampsToTheBuildingAndTheFloor()
		{
			Assert.That(GarrisonContestRules.DesiredWalkers(6, 4), Is.EqualTo(4));
			Assert.That(GarrisonContestRules.DesiredWalkers(3, 8), Is.EqualTo(3));
			Assert.That(GarrisonContestRules.DesiredWalkers(6, 0), Is.EqualTo(1));
			Assert.That(GarrisonContestRules.DesiredWalkers(6, -2), Is.EqualTo(1));
		}

		[Test]
		public void ContestRankNormalizesDistanceByCapacity()
		{
			Assert.That(GarrisonContestRules.ContestRankKey(100, 4), Is.EqualTo(25));
			Assert.That(GarrisonContestRules.ContestRankKey(100, 0), Is.EqualTo(100), "weight 0 never divides by zero");
			Assert.That(GarrisonContestRules.ContestRankKey(0, 4), Is.EqualTo(0));
			Assert.That(GarrisonContestRules.ContestRankKey(100, 8), Is.LessThan(GarrisonContestRules.ContestRankKey(100, 2)),
				"a bigger building justifies a longer walk");
		}
	}
}
