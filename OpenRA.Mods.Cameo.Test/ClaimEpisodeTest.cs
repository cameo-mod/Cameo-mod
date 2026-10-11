using System.Collections.Generic;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class ClaimEpisodeTest
	{
		static TeamBroadcast Broadcast(string owner, int tick, bool defence, WPos assist,
			BotClaimEpisode defenceEpisode = default, BotClaimEpisode assistEpisode = default) =>
			new(tick, 1000, defence ? 1 : 0, 0, DirectorPhase.BuildUp, null, defence,
				defence ? new WPos(1024, 1024, 0) : WPos.Zero, armyCentroid: new WPos(2048, 2048, 0),
				expansionAssist: assist, participantId: owner, defenceEpisode: defenceEpisode,
				expansionAssistEpisode: assistEpisode);

		[Test]
		public void AdmissionSurvivesChangingRequestLocationAndExpiryPulses()
		{
			var counter = new BotClaimEpisodeCounter("Multi0", "Engineers", 0);
			var admitted = counter.Admit();
			for (var tick = 0; tick <= 1500; tick += 50)
			{
				var request = new BotProtectionRequest(new CPos(tick, 0), 1000 + tick, tick + 100, admitted);
				Assert.That(request.Episode, Is.EqualTo(admitted));
			}
			Assert.That(admitted.IsKnown, Is.True);
			Assert.That(counter.Admit().Sequence, Is.EqualTo(admitted.Sequence + 1));
		}

		[Test]
		public void ReadmissionOfSameTargetHasNewEpisode()
		{
			var counter = new BotClaimEpisodeCounter("Multi0", "Engineers", 0);
			var first = new BotProtectionRequest(new CPos(1, 1), 1000, 100, counter.Admit());
			var second = new BotProtectionRequest(first.Location, first.Value, first.ExpiresTick, counter.Admit());
			Assert.That(second.Episode.OwnerId, Is.EqualTo(first.Episode.OwnerId));
			Assert.That(second.Episode.Sequence, Is.GreaterThan(first.Episode.Sequence));
		}

		[TestCase("Multi1", "Engineers", 0)]
		[TestCase("Multi0", "Expansion", 0)]
		[TestCase("Multi0", "Engineers", 1)]
		public void PerOwnerCounterCannotCollideWithOtherOwnerOrProvider(string owner, string provider, int instance)
		{
			var first = new BotClaimEpisodeCounter("Multi0", "Engineers", 0).Admit();
			var other = new BotClaimEpisodeCounter(owner, provider, instance).Admit();
			Assert.That(other.Sequence, Is.EqualTo(first.Sequence));
			Assert.That(other, Is.Not.EqualTo(first));
			Assert.That(other.OwnerId, Is.Not.EqualTo(first.OwnerId));
		}

		[Test]
		public void OwnerComponentsHaveUnambiguousBoundaries()
		{
			var first = new BotClaimEpisodeCounter("a", "bc", 0).Admit();
			var other = new BotClaimEpisodeCounter("ab", "c", 0).Admit();
			Assert.That(first.OwnerId, Is.Not.EqualTo(other.OwnerId));
		}

		[TestCase(null, "Engineers", 0)]
		[TestCase("", "Engineers", 0)]
		[TestCase("Multi0", null, 0)]
		[TestCase("Multi0", "Engineers", -1)]
		public void UnsupportedOwnerCannotMintKnownEpisode(string owner, string provider, int instance)
		{
			Assert.That(new BotClaimEpisodeCounter(owner, provider, instance).Admit().IsKnown, Is.False);
		}

		[Test]
		public void MissingHistoryAfterLoadCannotRecycleAdmissionIdentity()
		{
			var counter = new BotClaimEpisodeCounter("Multi0", "Engineers", 0);
			Assert.That(counter.Admit().IsKnown, Is.True);
			counter.Invalidate();
			Assert.That(counter.Admit().IsKnown, Is.False);
			Assert.That(counter.Admit().IsKnown, Is.False);
		}

		[Test]
		public void CoalitionCarriesExactDefenceEpisodeAcrossSnapshotsWithoutChangingElection()
		{
			var counter = new BotClaimEpisodeCounter("requester", "Situation", 0);
			var episode = counter.Admit();
			var responder = Broadcast("responder", 0, false, WPos.Zero);
			for (var tick = 0; tick <= 1500; tick += 50)
			{
				var request = Broadcast("requester", tick, true, WPos.Zero, episode);
				var directive = CoalitionFold.Compute(request, new List<TeamBroadcast> { responder });
				Assert.That(directive.RescueAssignments, Has.Count.EqualTo(1));
				Assert.That(directive.RescueAssignments[0].Episode, Is.EqualTo(episode));
				Assert.That(directive.RescueAssignments[0].ResponderId, Is.EqualTo("responder"));
			}
		}

		[Test]
		public void CoalitionCarriesExactAssistEpisode()
		{
			var episode = new BotClaimEpisodeCounter("requester", "Expansion", 0).Admit();
			var request = Broadcast("requester", 100, false, new WPos(1024, 1024, 0), assistEpisode: episode);
			var directive = CoalitionFold.Compute(request, new[] { Broadcast("responder", 100, false, WPos.Zero) });
			Assert.That(directive.AssistAssignments, Has.Count.EqualTo(1));
			Assert.That(directive.AssistAssignments[0].Episode, Is.EqualTo(episode));
		}

		[Test]
		public void LegacyPublishersKeepUnknownEpisodesEvenWithTickAndLocation()
		{
			Assert.That(new BotProtectionRequest(new CPos(1, 1), 1000, 100).Episode.IsKnown, Is.False);
			var request = Broadcast("requester", 9999, true, new WPos(1024, 1024, 0));
			var directive = CoalitionFold.Compute(request, new[] { Broadcast("responder", 9999, false, WPos.Zero) });
			Assert.That(directive.RescueAssignments, Has.Count.EqualTo(1));
			Assert.That(directive.RescueAssignments[0].Episode.IsKnown, Is.False);
			Assert.That(request.ExpansionAssistEpisode.IsKnown, Is.False);
		}

		[Test]
		public void ForeignOwnerAndInactiveRequestCannotPublishKnownEpisode()
		{
			var episode = new BotClaimEpisodeCounter("other", "Situation", 0).Admit();
			var foreign = Broadcast("requester", 50, true, new WPos(1024, 1024, 0), episode, episode);
			Assert.That(foreign.DefenceEpisode.IsKnown, Is.False);
			Assert.That(foreign.ExpansionAssistEpisode.IsKnown, Is.False);
			var inactive = Broadcast("other", 50, false, WPos.Zero, episode, episode);
			Assert.That(inactive.DefenceEpisode.IsKnown, Is.False);
			Assert.That(inactive.ExpansionAssistEpisode.IsKnown, Is.False);
		}
	}
}
