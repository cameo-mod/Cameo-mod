#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.FileSystem;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class LearnedFilePathTest
	{
		[TestCase("ai/learned/arsenal_priors.yaml", "cameo|ai/learned/arsenal_priors.yaml")]
		[TestCase("cameo|ai/learned/arsenal_priors.yaml", "cameo|ai/learned/arsenal_priors.yaml")]
		[TestCase("other|nested/file.yaml", "other|nested/file.yaml")]
		public void ResolvePrefixesOnlyBarePaths(string file, string expected)
		{
			Assert.That(LearnedFilePath.Resolve(file), Is.EqualTo(expected));
		}

		[Test]
		public void TheFourConfiguredLearnedFilesResolveInsideTheCameoPackage()
		{
			// M9: the committed defaults stay bare (mod-relative); the resolver is what makes them
			// reachable, so pin every shipped default through it.
			var files = new[]
			{
				new BotLearnedPriorsInfo().PriorsFile,
				new BuildOrderKnobsBotModuleInfo().LearnedFile,
				new EngagementPriorsBotModuleInfo().PriorsFile,
				new PlanBanditBotModuleInfo().LearnedFile
			};

			foreach (var file in files)
				Assert.That(LearnedFilePath.Resolve(file), Does.StartWith("cameo|"), $"{file} must resolve inside the cameo package");
		}

		[Test]
		public void BareNestedPathMissesTheIndexButResolvesThroughThePackage()
		{
			// M9 mechanism: Folder.Contents yields top-level names only, so FileSystem.Exists never
			// indexes "ai/learned/x.yaml"; the cameo| prefix reaches Folder.Contains, which probes the
			// combined on-disk path. A configured file that exists must NOT log "missing".
			var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "cameo_modpkg");
			var rel = Path.Combine("ai", "learned", "priors.yaml");
			Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(root, rel)));
			File.WriteAllText(Path.Combine(root, rel), "x: 1");

			var fs = new OpenRA.FileSystem.FileSystem("cameo", new Dictionary<string, Manifest>(), Array.Empty<IPackageLoader>());
			fs.Mount(new Folder(root), "cameo");
			try
			{
				Assert.That(fs.Exists("ai/learned/priors.yaml"), Is.False,
					"the un-prefixed nested path must miss the top-level-only file index (the M9 bug)");
				Assert.That(fs.Exists(LearnedFilePath.Resolve("ai/learned/priors.yaml")), Is.True,
					"the cameo|-prefixed path must resolve through the package's on-disk Contains");
				using (var stream = fs.Open(LearnedFilePath.Resolve("ai/learned/priors.yaml")))
					Assert.That(stream, Is.Not.Null);
			}
			finally
			{
				fs.UnmountAll();
			}
		}
	}
}
