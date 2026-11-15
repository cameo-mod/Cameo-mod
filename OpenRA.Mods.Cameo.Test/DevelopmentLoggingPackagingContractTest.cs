using System;
using System.IO;
using NUnit.Framework;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class DevelopmentLoggingPackagingContractTest
	{
		static string Source(string path) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", path));

		[Test]
		public void ReleaseManifestDoesNotLoadDevelopmentAssemblyOrRules()
		{
			var manifest = Source("packaging/cameo-mod.yaml");
			Assert.That(manifest, Does.Not.Contain("OpenRA.Mods.Cameo.Dev.dll"));
			Assert.That(manifest, Does.Not.Contain("rules/dev/"));
		}

		[Test]
		public void PackageBuildForcesOffAndChecksForDevelopmentContent()
		{
			var functions = Source("packaging/functions.sh");
			Assert.That(functions, Does.Contain("-p:CameoDevLogging=false"));
			Assert.That(functions, Does.Contain("-p:CameoOfficialPackaging=true"));
			Assert.That(functions, Does.Contain("if ! DEV_CONTENT=$(find"));
			Assert.That(functions, Does.Contain("assert_no_dev_content \"${DEST_PATH}\""));
			foreach (var platform in new[] { "linux", "macos", "windows" })
				Assert.That(Source("packaging/" + platform + "-buildpackage.sh"), Does.Contain("assert_no_dev_content"), platform);
		}

		[Test]
		public void DevelopmentEntryPointHasAnExplicitOptIn()
		{
			var targets = Source("packaging/Directory.Build.targets");
			Assert.That(targets, Does.Contain("<CameoDevLogging Condition=\"'$(CameoDevLogging)' == ''\">false</CameoDevLogging>"));
			Assert.That(targets, Does.Contain("Official packages must exclude development logging."));
			var entry = Source("packaging/CameoDev.proj");
			Assert.That(entry, Does.Contain("The DEV entry point requires CameoDevLogging=true."));
		}

		[Test]
		public void SeparateDevelopmentRulesMountTheRecordingTraits()
		{
			var release = Source("packaging/world.yaml");
			var dev = Source("packaging/dev-logging.yaml");
			foreach (var trait in new[] { "AiMemoryTelemetryDev", "AiPlayerTelemetryDev", "AiEconomyHealthCaptureDev",
				"AiMatchLogWriterDev", "AiMissionLogWriterDev", "AiSituationLogWriterDev", "InsuranceTelemetryDev",
				"AiEngagementLogWriterDev", "AiPlacementLogWriterDev" })
			{
				Assert.That(dev, Does.Contain("\t" + trait + ":"), trait);
				Assert.That(release, Does.Not.Contain("\t" + trait + ":"), trait);
			}
		}
	}
}
