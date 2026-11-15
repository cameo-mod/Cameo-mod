using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class DevelopmentLoggingBinaryExclusionTest
	{
		static bool ExcludesDev(string[] assemblyReferences, string[] typeNames) =>
			!assemblyReferences.Any(n => n == "OpenRA.Mods.Cameo.Dev") &&
			!typeNames.Any(n => n.StartsWith("OpenRA.Mods.Cameo.Dev.", StringComparison.Ordinal) ||
				n == "OpenRA.Mods.Cameo.Traits.AiLogFileAppender" ||
				n == "OpenRA.Mods.Cameo.Traits.AiEconomyHealthLogWriter" ||
				n == "OpenRA.Mods.CA.Traits.BotModules.BotPassiveDiagnosticWriter");

		[Test]
		public void ResidentAssembliesContainNoDevWriterOrReference()
		{
			foreach (var assembly in new[] { typeof(CameoLogHub).Assembly, typeof(AiMatchLogWriter).Assembly })
			{
				// GetTypes failures fail the test; a partial or unreadable scan is never accepted.
				var types = assembly.GetTypes().Select(t => t.FullName ?? "").ToArray();
				var references = assembly.GetReferencedAssemblies().Select(a => a.Name ?? "").ToArray();
				Assert.That(ExcludesDev(references, types), Is.True, assembly.FullName);
				Assert.That(ExcludesDev(references.Append("OpenRA.Mods.Cameo.Dev").ToArray(), types), Is.False,
					"a DEV-reference mutant must be rejected");
			}
		}

		[Test]
		public void UnflaggedTestOutputHasNoMountedDevPackage()
		{
			var output = AppContext.BaseDirectory;
			Assert.That(File.Exists(Path.Combine(output, "OpenRA.Mods.Cameo.Dev.dll")), Is.False);
			Assert.That(Directory.Exists(Path.Combine(output, "mods", "cameo-dev")), Is.False);
			Assert.That(Directory.Exists(Path.Combine(output, "mods", "cameo", "rules", "dev")), Is.False);
		}
	}
}
