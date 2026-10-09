using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class AiEconomyHealthLogWriterTest
	{
		[Test]
		public void Utf8BoundsIncludeNewlineAndFailureIsSticky()
		{
			using var stream = new MemoryStream();
			using var writer = new AiEconomyHealthLogWriter(stream, byteLimit: 4);
			Assert.That(writer.TryWrite("\"é\""), Is.False);
			Assert.That(writer.TryWrite("{}"), Is.False);
			Assert.That(stream.Length, Is.Zero);
			Assert.That(writer.Complete, Is.False);
		}

		[Test]
		public void RecordBudgetNeverSilentlyDropsOrResumes()
		{
			using var stream = new MemoryStream();
			using var writer = new AiEconomyHealthLogWriter(stream, recordLimit: 1);
			Assert.That(writer.TryWrite("{}"), Is.True);
			Assert.That(writer.TryWrite("{}"), Is.False);
			Assert.That(writer.Records, Is.EqualTo(1));
			Assert.That(writer.Bytes, Is.EqualTo(3));
			Assert.That(writer.MaximumRecordBytes, Is.EqualTo(3));
			Assert.That(Encoding.UTF8.GetString(stream.ToArray()), Is.EqualTo("{}\n"));
			Assert.That(writer.Complete, Is.False);
		}

		[TestCase("{}\n{}")]
		[TestCase("{}\r{}")]
		public void InvalidLineCannotBeWritten(string value)
		{
			using var stream = new MemoryStream();
			using var writer = new AiEconomyHealthLogWriter(stream);
			Assert.That(writer.TryWrite(value), Is.False);
			Assert.That(stream.Length, Is.Zero);
		}

		[Test]
		public void InvalidSurrogateIsRejectedBeforeWrite()
		{
			using var stream = new MemoryStream();
			using var writer = new AiEconomyHealthLogWriter(stream);
			Assert.That(writer.TryWrite(new string((char)0xd800, 1)), Is.False);
			Assert.That(stream.Length, Is.Zero);
		}

		[Test]
		public void ExistingEvidenceIsNeverOverwritten()
		{
			var path = Path.GetTempFileName();
			try
			{
				File.WriteAllText(path, "KEEP");
				Assert.Throws<IOException>(() => new AiEconomyHealthLogWriter(path));
				Assert.That(File.ReadAllText(path), Is.EqualTo("KEEP"));
			}
			finally { File.Delete(path); }
		}

		sealed class FailingStream : MemoryStream
		{
			public override void Flush() => throw new IOException("injected flush failure");
		}

		[Test]
		public void PartialWriteOrFlushFailureCannotCertifyARecord()
		{
			using var stream = new FailingStream();
			using var writer = new AiEconomyHealthLogWriter(stream);
			Assert.That(writer.TryWrite("{}"), Is.False);
			Assert.That(writer.Records, Is.Zero);
			Assert.That(writer.Complete, Is.False);
			Assert.That(writer.TryWrite("{}"), Is.False);
		}
	}
}
