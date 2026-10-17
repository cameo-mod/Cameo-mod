using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class DefenceClaimEpisodeTest
	{
		[Test]
		public void PublishedAdmissionStaysStableAcrossPulsesAndEndsOnWithdrawal()
		{
			var publisher = new BotDefenceClaimEpisodes("Multi0", 0);
			Assert.That(publisher.Observe(false).IsKnown, Is.False);
			var first = publisher.Observe(true);
			Assert.That(first.IsKnown, Is.True);
			for (var pulse = 0; pulse < 100; pulse++)
				Assert.That(publisher.Observe(true), Is.EqualTo(first));
			Assert.That(publisher.Observe(false).IsKnown, Is.False);
			Assert.That(publisher.Observe(false).IsKnown, Is.False);
			var second = publisher.Observe(true);
			Assert.That(second.OwnerId, Is.EqualTo(first.OwnerId));
			Assert.That(second.Sequence, Is.EqualTo(first.Sequence + 1));
		}

		[TestCase("Multi1", 0)]
		[TestCase("Multi0", 1)]
		public void ParticipantsAndProviderInstancesCannotCollide(string participant, int instance)
		{
			var first = new BotDefenceClaimEpisodes("Multi0", 0).Observe(true);
			var other = new BotDefenceClaimEpisodes(participant, instance).Observe(true);
			Assert.That(other.Sequence, Is.EqualTo(first.Sequence));
			Assert.That(other, Is.Not.EqualTo(first));
		}

		[Test]
		public void RestorationKeepsHistoryMissingUnknownThroughReadmission()
		{
			var publisher = new BotDefenceClaimEpisodes("Multi0", 0);
			Assert.That(publisher.Observe(true).IsKnown, Is.True);
			publisher.Invalidate();
			Assert.That(publisher.Observe(true).IsKnown, Is.False);
			publisher.Observe(false);
			Assert.That(publisher.Observe(true).IsKnown, Is.False);
		}

		[TestCase(null, 0)]
		[TestCase("Multi0", -1)]
		public void UnsupportedProviderNeverAdmitsKnownIdentity(string participant, int instance)
		{
			var publisher = new BotDefenceClaimEpisodes(participant, instance);
			Assert.That(publisher.Observe(true).IsKnown, Is.False);
			publisher.Observe(false);
			Assert.That(publisher.Observe(true).IsKnown, Is.False);
		}
	}
}
