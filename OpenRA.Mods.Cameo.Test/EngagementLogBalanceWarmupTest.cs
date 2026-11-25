using System;
using System.IO;
using NUnit.Framework;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class EngagementLogBalanceWarmupTest
	{
		static string RepoFile(string relative, [System.Runtime.CompilerServices.CallerFilePath] string testPath = "") =>
			File.ReadAllText(Path.Combine(Path.GetDirectoryName(testPath), "..", relative)).Replace("\r\n", "\n");

		[Test]
		public void DevelopmentWriterPrewarmsBalanceMetadataAtWorldLoadBehindItsSinkGate()
		{
			var dev = RepoFile("OpenRA.Mods.Cameo.Dev/AiEngagementLogWriterDev.cs");
			var worldLoaded = dev.Substring(dev.IndexOf("void IWorldLoaded.WorldLoaded", StringComparison.Ordinal),
				dev.IndexOf("public void Append", StringComparison.Ordinal) - dev.IndexOf("void IWorldLoaded.WorldLoaded", StringComparison.Ordinal));
			Assert.That(worldLoaded, Does.Contain("world.WorldActor.Trait<CameoLogHub>().Sink.Enabled"));
			Assert.That(worldLoaded, Does.Contain("if (active)"));
			Assert.That(worldLoaded, Does.Contain("PrewarmBalanceMetadataForDevelopmentLog"));

			var module = RepoFile("OpenRA.Mods.Cameo/Traits/BotModules/EngagementLogBotModule.cs");
			var warmupStart = module.IndexOf("public void PrewarmBalanceMetadataForDevelopmentLog()", StringComparison.Ordinal);
			Assert.That(warmupStart, Is.GreaterThanOrEqualTo(0));
			var warmupEnd = module.IndexOf("bool Ready()", warmupStart, StringComparison.Ordinal);
			Assert.That(warmupEnd, Is.GreaterThan(warmupStart));
			var warmup = module.Substring(warmupStart, warmupEnd - warmupStart);
			Assert.That(warmup, Does.Contain("Sink.Enabled == true"));
			Assert.That(warmup, Does.Contain("_ = Balance();"));
		}
	}
}
