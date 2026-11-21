using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
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

		[Test]
		public void FinishedPackageHasNoDevelopmentContent()
			{
				var root = Environment.GetEnvironmentVariable("CAMEO_FINISHED_PACKAGE");
				if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
					Assert.Inconclusive("Set CAMEO_FINISHED_PACKAGE to a finished package directory to run the package scan.");

				foreach (var entry in Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories))
				{
					var name = Path.GetFileName(entry);
					var relative = Path.GetRelativePath(root, entry).Replace('\\', '/');
					Assert.That(name, Does.Not.StartWith("OpenRA.Mods.Cameo.Dev"), relative);
					Assert.That(name, Is.Not.EqualTo("cameo-dev"), relative);
					Assert.That(relative, Does.Not.Contain("rules/dev/"), relative);
					Assert.That(name, Does.Not.Match(@"^cameo-ai-.*\.jsonl$"), relative);
					if (name == "mod.yaml")
					{
						var text = File.ReadAllText(entry);
						Assert.That(text, Does.Not.Contain("OpenRA.Mods.Cameo.Dev.dll"), relative);
						Assert.That(text, Does.Not.Contain("rules/dev/"), relative);
					}
				}
			}

			[Test]
			public void MissingControllerStillUsesResidentPersonalityRecorder()
		{
			var source = Source("mods/Cameo/Traits/BotModules/BotSituation.cs");
			var start = source.IndexOf("string CurrentPersonality()", StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0));
			var end = source.IndexOf("IEnumerable<string> AvailablePersonalities()", start, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start));
			var method = source.Substring(start, end - start);
			Assert.That(method, Does.Contain("TraitOrDefault<BotPersonalityController>()?.CurrentPersonality"));
			Assert.That(method, Does.Contain("?? player.PlayerActor.TraitOrDefault<AiMatchLogRecorder>()?.CurrentPersonality"));
			Assert.That(method, Does.Not.Contain("CameoLogHub"));
		}

		static bool ContainsText(byte[] data, string text) =>
			data.AsSpan().IndexOf(Encoding.UTF8.GetBytes(text)) >= 0
			|| data.AsSpan().IndexOf(Encoding.Unicode.GetBytes(text)) >= 0;

		static string TypeName(MetadataReader metadata, EntityHandle handle) => handle.Kind switch
		{
			HandleKind.TypeDefinition => metadata.GetString(metadata.GetTypeDefinition((TypeDefinitionHandle)handle).Name),
			HandleKind.TypeReference => metadata.GetString(metadata.GetTypeReference((TypeReferenceHandle)handle).Name),
			_ => "",
		};

		// ABLATE-01 (ABLATE_01_PLAN_03 section 2; Opus 2026-11-21 gate before PROD-IDLE G1 lands): the shared half
		// (AblationGate stub, IAblationProvider interface, id constants) ships; nothing else does. Run on the ACTUAL
		// packaged output, like the DEV-only check above: set CAMEO_FINISHED_PACKAGE to the finished package directory.
		[Test]
		public void FinishedPackageHasNoAblationProviderOrOption()
		{
			var root = Environment.GetEnvironmentVariable("CAMEO_FINISHED_PACKAGE");
			if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
				Assert.Inconclusive("Set CAMEO_FINISHED_PACKAGE to a finished package directory to run the package scan.");

			// The three option strings must not exist anywhere in shipped code or yaml (either string encoding).
			var optionStrings = new[] { "Cameo.DevAblate", "CAMEO_DEV_ABLATE", "cameo-ablate" };
			var scanned = new[] { ".dll", ".exe", ".yaml", ".yml", ".json", ".ftl", ".config", ".txt", ".sh", ".ps1" };
			var sawGate = false;

			foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
			{
				var extension = Path.GetExtension(path).ToLowerInvariant();
				if (Array.IndexOf(scanned, extension) < 0)
					continue;

				var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
				var data = File.ReadAllBytes(path);
				foreach (var text in optionStrings)
					Assert.That(ContainsText(data, text), Is.False, relative + " contains \"" + text + "\"");

				if (extension != ".dll" && extension != ".exe")
					continue;

				// Managed assemblies: no type implements IAblationProvider and no provider-like type exists.
				try
				{
					using var stream = new MemoryStream(data);
					using var pe = new PEReader(stream);
					if (!pe.HasMetadata)
						continue;

					var metadata = pe.GetMetadataReader();
					foreach (var typeHandle in metadata.TypeDefinitions)
					{
						var type = metadata.GetTypeDefinition(typeHandle);
						var name = metadata.GetString(type.Name);
						sawGate |= name == "AblationGate";
						if (name != "IAblationProvider")
							Assert.That(name, Does.Not.Contain("AblationProvider"), relative);

						foreach (var implementation in type.GetInterfaceImplementations())
							Assert.That(TypeName(metadata, metadata.GetInterfaceImplementation(implementation).Interface),
								Is.Not.EqualTo("IAblationProvider"), relative + ": " + name + " implements IAblationProvider");
					}
				}
				catch (BadImageFormatException)
				{
					// Native library: only the string scan above applies.
				}
			}

			Assert.That(sawGate, Is.True, "the release package must contain the AblationGate stub that the call sites bind to");
		}
	}
}
