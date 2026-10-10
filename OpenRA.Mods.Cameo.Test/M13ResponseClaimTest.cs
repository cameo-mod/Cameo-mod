using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class M13ResponseClaimTest
	{
		static BotClaimEpisode Episode(string participant = "Multi0", long sequence = 1) =>
			new(participant, "Defence", 0, sequence);

		[Test]
		public void SynthesisRetainsLegacyPayloadAndPublishedEpisode()
		{
			var episode = Episode();
			var result = M13ResponseClaimCA.Synthesize(new CPos(12, 34), 5678, 9012, "Multi0", episode);
			Assert.That(result.Location, Is.EqualTo(new CPos(12, 34)));
			Assert.That(result.Value, Is.EqualTo(5678));
			Assert.That(result.ExpiresTick, Is.EqualTo(9012));
			Assert.That(result.Episode, Is.EqualTo(episode));
		}

		[Test]
		public void ElectedPathRequiresExactPublishedAdmission()
		{
			var current = Episode(sequence: 2);
			Assert.That(M13ResponseClaimCA.Synthesize(CPos.Zero, 1, 50, "Multi0", current,
				current, true).Episode, Is.EqualTo(current));
			foreach (var elected in new[] { default, Episode(sequence: 1), Episode("Multi1", 2) })
				Assert.That(M13ResponseClaimCA.Synthesize(CPos.Zero, 1, 50, "Multi0", current,
					elected, true).Episode.IsKnown, Is.False);
		}

		[TestCase(null)]
		[TestCase("Multi1")]
		public void MissingOrWrongPublisherCannotBecomeKnown(string participant)
		{
			var result = M13ResponseClaimCA.Synthesize(CPos.Zero, 42, 100, participant, Episode());
			Assert.That(result.Episode.IsKnown, Is.False);
			Assert.That(result.Value, Is.EqualTo(42), "UNKNOWN provenance must not change legacy request semantics");
		}

		[TestCase(PrepositionChannelCA.Request)]
		[TestCase(PrepositionChannelCA.DefendAnswer)]
		[TestCase(PrepositionChannelCA.AssistAnswer)]
		public void RallyExpiryAndPulsesNeverRenewKey(PrepositionChannelCA channel)
		{
			var counter = new BotClaimEpisodeCounter("Multi0", "Claim", 0);
			var episode = counter.Admit();
			string first = null;
			for (var pulse = 0; pulse < 100; pulse++)
			{
				var request = M13ResponseClaimCA.Synthesize(new CPos(pulse, 1), pulse, 100 + pulse,
					"Multi0", episode);
				Assert.That(M13ResponseClaimCA.TryKey(channel, request, "Multi0", out var key), Is.True);
				first ??= key;
				Assert.That(key, Is.EqualTo(first));
			}
			var next = M13ResponseClaimCA.Synthesize(CPos.Zero, 1, 500, "Multi0", counter.Admit());
			Assert.That(M13ResponseClaimCA.TryKey(channel, next, "Multi0", out var second), Is.True);
			Assert.That(second, Is.Not.EqualTo(first));
		}

		[Test]
		public void OwnerAndChannelCannotCollide()
		{
			var one = new BotProtectionRequest(CPos.Zero, 1, 100, Episode());
			var other = one with { Episode = Episode("Multi1") };
			M13ResponseClaimCA.TryKey(PrepositionChannelCA.Request, one, "Multi0", out var a);
			M13ResponseClaimCA.TryKey(PrepositionChannelCA.Request, other, "Multi1", out var b);
			M13ResponseClaimCA.TryKey(PrepositionChannelCA.DefendAnswer, one, "Multi0", out var c);
			Assert.That(a, Is.Not.EqualTo(b));
			Assert.That(a, Is.Not.EqualTo(c));
		}

		[TestCase(PrepositionChannelCA.None)]
		[TestCase(PrepositionChannelCA.Threat)]
		[TestCase((PrepositionChannelCA)99)]
		public void NonClaimChannelCannotCreateResponseKey(PrepositionChannelCA channel)
		{
			Assert.That(M13ResponseClaimCA.TryKey(channel,
				new BotProtectionRequest(CPos.Zero, 1, 50, Episode()), "Multi0", out var key), Is.False);
			Assert.That(key, Is.Null);
		}

		[Test]
		public void UnsupportedAssistAndForeignClaimStayUnknown()
		{
			var request = M13ResponseClaimCA.Synthesize(CPos.Zero, 1, 50, "Multi0", default,
				default, electionRequired: true);
			Assert.That(M13ResponseClaimCA.TryKey(PrepositionChannelCA.AssistAnswer,
				request, "Multi0", out _), Is.False);
			Assert.That(M13ResponseClaimCA.TryKey(PrepositionChannelCA.Request,
				new BotProtectionRequest(CPos.Zero, 1, 50, Episode("Multi1")), "Multi0", out _), Is.False);
		}
	}
}
