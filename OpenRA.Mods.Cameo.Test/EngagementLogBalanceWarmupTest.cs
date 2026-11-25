#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

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
			var activeGate = worldLoaded.IndexOf("if (active)", StringComparison.Ordinal);
			var prewarm = worldLoaded.IndexOf("PrewarmBalanceMetadataForDevelopmentLog", StringComparison.Ordinal);
			Assert.That(activeGate, Is.GreaterThanOrEqualTo(0));
			Assert.That(prewarm, Is.GreaterThan(activeGate), "the prewarm call must remain inside if (active)");

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
