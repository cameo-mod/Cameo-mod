using System.IO;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// SPK-01: the batched insurance path flushes once per batch; the default path keeps its per-record watermark.
	[TestFixture]
	public class AiEconomyHealthDeferredFlushTest
	{
		sealed class CountingStream : MemoryStream
		{
			public int Flushes;
			public override void Flush()
			{
				Flushes++;
				base.Flush();
			}
		}

		[Test]
		public void DeferredWriterFlushesOncePerBatch()
		{
			using var stream = new CountingStream();
			using var writer = new AiEconomyHealthLogWriter(stream, flushEachRecord: false);
			Assert.That(writer.TryWrite("{\"a\":1}"), Is.True);
			Assert.That(writer.TryWrite("{\"a\":2}"), Is.True);
			Assert.That(writer.TryWrite("{\"a\":3}"), Is.True);
			Assert.That(stream.Flushes, Is.Zero);
			Assert.That(writer.FlushPending(), Is.True);
			Assert.That(stream.Flushes, Is.EqualTo(1));
			Assert.That(writer.Records, Is.EqualTo(3));
		}

		[Test]
		public void DefaultWriterStillFlushesEveryRecord()
		{
			using var stream = new CountingStream();
			using var writer = new AiEconomyHealthLogWriter(stream);
			Assert.That(writer.TryWrite("{\"a\":1}"), Is.True);
			Assert.That(writer.TryWrite("{\"a\":2}"), Is.True);
			Assert.That(stream.Flushes, Is.EqualTo(2));
		}

		[Test]
		public void FlushFailureIsSticky()
		{
			using var stream = new CountingStream();
			using var writer = new AiEconomyHealthLogWriter(stream, flushEachRecord: false);
			Assert.That(writer.TryWrite("{\"a\":1}"), Is.True);
			writer.Dispose();
			Assert.That(writer.FlushPending(), Is.False);
			Assert.That(writer.Complete, Is.False);
		}
	}
}
