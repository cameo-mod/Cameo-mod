using System.Text;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// PERF-SPK-01: the admission rule shared by AiEconomyHealthLogWriter and AiQueuedJsonlWriter (parity by construction).
	[TestFixture]
	public class AiJsonlRowRulesTest
	{
		const long Big = long.MaxValue;

		[Test]
		public void AcceptedRowCountsItsUtf8BytesPlusNewline()
		{
			Assert.That(AiJsonlRowRules.TryCount("{\"a\":1}", 0, 0, Big, int.MaxValue, out var count), Is.True);
			Assert.That(count, Is.EqualTo(8));
			Assert.That(AiJsonlRowRules.TryCount("é", 0, 0, Big, int.MaxValue, out count), Is.True);
			Assert.That(count, Is.EqualTo(3));
		}

		[Test]
		public void NullAndEmbeddedLineBreaksAreRefused()
		{
			Assert.That(AiJsonlRowRules.TryCount(null, 0, 0, Big, int.MaxValue, out _), Is.False);
			Assert.That(AiJsonlRowRules.TryCount("a\nb", 0, 0, Big, int.MaxValue, out _), Is.False);
			Assert.That(AiJsonlRowRules.TryCount("a\rb", 0, 0, Big, int.MaxValue, out _), Is.False);
		}

		[Test]
		public void OverlongRowIsRefused()
		{
			var line = new string('x', AiEconomyHealthLogWriter.MaximumLineBytes);
			Assert.That(AiJsonlRowRules.TryCount(line, 0, 0, Big, int.MaxValue, out _), Is.False, "the newline makes it one byte too long");
			Assert.That(AiJsonlRowRules.TryCount(line[1..], 0, 0, Big, int.MaxValue, out _), Is.True);
		}

		[Test]
		public void ByteLimitIsExactlyMet()
		{
			// One row of 8 counted bytes fits exactly in a 16-byte file that already holds 8 bytes.
			Assert.That(AiJsonlRowRules.TryCount("{\"a\":1}", 8, 0, 16, int.MaxValue, out _), Is.True);
			Assert.That(AiJsonlRowRules.TryCount("{\"a\":1}", 9, 0, 16, int.MaxValue, out _), Is.False);
		}

		[Test]
		public void RecordLimitIsExactlyMet()
		{
			Assert.That(AiJsonlRowRules.TryCount("{}", 0, 9, Big, 10, out _), Is.True);
			Assert.That(AiJsonlRowRules.TryCount("{}", 0, 10, Big, 10, out _), Is.False);
		}

		[Test]
		public void LoneSurrogateThrowsSoCallersRefuse()
		{
			var lone = new string(new[] { (char)0xD800 });
			Assert.Throws<EncoderFallbackException>(() => AiJsonlRowRules.TryCount(lone, 0, 0, Big, int.MaxValue, out _));
		}
	}
}
