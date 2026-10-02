#region Copyright & License Information
/*
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// TC-3 (AI_ARCHITECTURE.md §12.18): the coalition fold. Every member computes
	// the identical directive from the same broadcast set, so the tests pin the
	// arithmetic: target votes, phase precedence, rescue election determinism,
	// and the Voronoi sector score.
	[TestFixture]
	public sealed class CoalitionFoldTest
	{
		// Uninitialised Players stand in for real ones — only reference identity
		// matters to the vote (same fixture trick as TeamBlackboardTest).
		static OpenRA.Player FakePlayer() =>
			(OpenRA.Player)RuntimeHelpers.GetUninitializedObject(typeof(OpenRA.Player));

		static TeamBroadcast Broadcast(int clientIndex, int ownArmyValue = 0, int urgencyLevel = 0,
			int directorTension = 0, DirectorPhase directorPhase = DirectorPhase.BuildUp,
			OpenRA.Player mainTarget = null, bool requestsDefence = false, WPos defendPosition = default,
			WPos armyCentroid = default, WPos spawnPoint = default) =>
			new(1500, ownArmyValue, urgencyLevel, directorTension, directorPhase, mainTarget,
				requestsDefence, defendPosition, clientIndex, default, armyCentroid, default, spawnPoint);

		[Test]
		public void EmptyInputYieldsEmptyDirective()
		{
			var d = CoalitionFold.Compute(null, null);
			Assert.That(d.MainTarget, Is.Null);
			Assert.That(d.Phase, Is.EqualTo(CoalitionPhase.BuildUp));
			Assert.That(d.RescueAssignments, Is.Empty);
			Assert.That(d.SectorAnchors, Is.Empty);
		}

		[Test]
		public void SoloBroadcastDegradesToOwn()
		{
			var t = FakePlayer();
			var own = Broadcast(3, ownArmyValue: 5000, mainTarget: t);
			var d = CoalitionFold.Compute(own, new List<TeamBroadcast>());
			Assert.That(d.MainTarget, Is.SameAs(t));
			Assert.That(d.RescueAssignments, Is.Empty);
		}

		[Test]
		public void MainTargetVotePicksTheMostSharedTarget()
		{
			var a = FakePlayer();
			var b = FakePlayer();
			var own = Broadcast(0, mainTarget: a);
			var allies = new List<TeamBroadcast>
			{
				Broadcast(1, mainTarget: b),
				Broadcast(2, mainTarget: b, directorTension: 90),
			};

			Assert.That(CoalitionFold.Compute(own, allies).MainTarget, Is.SameAs(b));
		}

		[Test]
		public void MainTargetTieBreaksToHigherTensionThenLowerIndex()
		{
			var a = FakePlayer();
			var b = FakePlayer();
			var allies = new List<TeamBroadcast>
			{
				Broadcast(5, mainTarget: a, directorTension: 10),
				Broadcast(4, mainTarget: b, directorTension: 80),
			};

			Assert.That(CoalitionFold.Compute(null, allies).MainTarget, Is.SameAs(b));

			// Equal tension: lowest ClientIndex publisher wins.
			var lowIndex = new List<TeamBroadcast>
			{
				Broadcast(5, mainTarget: a, directorTension: 10),
				Broadcast(4, mainTarget: b, directorTension: 10),
			};
			Assert.That(CoalitionFold.Compute(null, lowIndex).MainTarget, Is.SameAs(b));
		}

		[Test]
		public void PushNeedsTargetAndClimax_DefendOverrides()
		{
			var t = FakePlayer();
			var quiet = new List<TeamBroadcast> { Broadcast(1, mainTarget: t) };
			Assert.That(CoalitionFold.Compute(null, quiet).Phase, Is.EqualTo(CoalitionPhase.BuildUp));

			var wave = new List<TeamBroadcast>
			{
				Broadcast(1, mainTarget: t),
				Broadcast(2, directorPhase: DirectorPhase.Climax),
			};
			Assert.That(CoalitionFold.Compute(null, wave).Phase, Is.EqualTo(CoalitionPhase.Push));

			var emergency = new List<TeamBroadcast>(wave)
			{
				Broadcast(3, urgencyLevel: 2, requestsDefence: true, defendPosition: new WPos(1000, 1000, 0)),
			};
			Assert.That(CoalitionFold.Compute(null, emergency).Phase, Is.EqualTo(CoalitionPhase.Defend));
		}

		[Test]
		public void RescueElectionPicksNearestFreeAlly()
		{
			var defendAt = new WPos(1000, 1000, 0);
			var requester = Broadcast(0, urgencyLevel: 2, requestsDefence: true, defendPosition: defendAt);
			var near = Broadcast(1, armyCentroid: new WPos(1100, 1000, 0));
			var far = Broadcast(2, armyCentroid: new WPos(9000, 9000, 0));

			var d = CoalitionFold.Compute(requester, new List<TeamBroadcast> { near, far });
			Assert.That(d.RescueAssignments.Count, Is.EqualTo(1));
			Assert.That(d.RescueAssignments[0].ResponderClientIndex, Is.EqualTo(1));
			Assert.That(d.RescueAssignments[0].RequesterClientIndex, Is.EqualTo(0));

			// Every member computes the same election — swap own/allies roles.
			var d2 = CoalitionFold.Compute(near, new List<TeamBroadcast> { requester, far });
			Assert.That(d2.RescueAssignments[0].ResponderClientIndex, Is.EqualTo(1));
		}

		[Test]
		public void RequesterNeverRescuesAndZeroCentroidIsNeverElected()
		{
			var defendAt = new WPos(1000, 1000, 0);
			var allies = new List<TeamBroadcast>
			{
				Broadcast(1, urgencyLevel: 2, requestsDefence: true, defendPosition: defendAt,
					armyCentroid: new WPos(1001, 1001, 0)),
				Broadcast(2), // zero centroid — no army
				Broadcast(3, armyCentroid: new WPos(5000, 5000, 0)),
			};

			var d = CoalitionFold.Compute(null, allies);
			Assert.That(d.RescueAssignments.Count, Is.EqualTo(1));
			Assert.That(d.RescueAssignments[0].ResponderClientIndex, Is.EqualTo(3));
		}

		[Test]
		public void SectorAnchorsFoldDeterministically()
		{
			var allies = new List<TeamBroadcast>
			{
				Broadcast(7, spawnPoint: new WPos(512, 512, 0)),
				Broadcast(2, spawnPoint: new WPos(8000, 512, 0)),
			};
			var d = CoalitionFold.Compute(Broadcast(9, spawnPoint: new WPos(512, 8000, 0)), allies);
			Assert.That(d.SectorAnchors.Count, Is.EqualTo(3));
			Assert.That(d.SectorAnchors[7], Is.EqualTo(new WPos(512, 512, 0)));
			Assert.That(d.SectorAnchors[2], Is.EqualTo(new WPos(8000, 512, 0)));
			Assert.That(d.SectorAnchors[9], Is.EqualTo(new WPos(512, 8000, 0)));
		}

		[Test]
		public void SectorScorePercentOwnsNearestAnchor()
		{
			var anchors = new Dictionary<int, WPos>
			{
				[0] = new WPos(0, 0, 0),
				[1] = new WPos(10000, 0, 0),
			};

			Assert.That(OpenRA.Mods.Cameo.Traits.BotModules.ExpansionPlannerBotModule.SectorScorePercent(
				new WPos(100, 0, 0), anchors, 0, 35), Is.EqualTo(100));
			Assert.That(OpenRA.Mods.Cameo.Traits.BotModules.ExpansionPlannerBotModule.SectorScorePercent(
				new WPos(100, 0, 0), anchors, 1, 35), Is.EqualTo(35));

			// No anchors: everything scores full.
			Assert.That(OpenRA.Mods.Cameo.Traits.BotModules.ExpansionPlannerBotModule.SectorScorePercent(
				new WPos(100, 0, 0), new Dictionary<int, WPos>(), 0, 35), Is.EqualTo(100));
		}
	}
}
