#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OpenRA.FileSystem;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// M9 (cloud review 2026-10-10, origin/ccr-c7f51935-i53fkj @ 680dd25d6): a bare mod-relative path
	// like "ai/learned/plan_bandits.yaml" never resolves through FileSystem.Exists, because
	// Folder.Contents only indexes top-level names and the mod's root package is never mounted at
	// "ai" or "ai/learned". The explicit "cameo|" mount prefix takes a different branch in Exists
	// that checks the real nested path (Folder.Contains), so it resolves. This locks both halves of
	// that fix: the engine mechanism itself, and that every learned-file default actually uses it.
	[TestFixture]
	public class LearnedFilePathTest
	{
		[Test]
		public void BareModRelativePath_NeverResolvesThroughExists()
		{
			var temp = Directory.CreateTempSubdirectory();
			try
			{
				Directory.CreateDirectory(Path.Combine(temp.FullName, "ai", "learned"));
				File.WriteAllText(Path.Combine(temp.FullName, "ai", "learned", "plan_bandits.yaml"), "BotPlanBandits:\n");

				var fs = new OpenRA.FileSystem.FileSystem("cameo", new Dictionary<string, Manifest>(), Array.Empty<IPackageLoader>());
				fs.Mount(new Folder(temp.FullName), "cameo");

				Assert.That(fs.Exists("ai/learned/plan_bandits.yaml"), Is.False,
					"Folder.Contents only indexes top-level names, so the bare path is never an Exists() index key " +
					"even though the file is physically present — this is the exact asymmetry M9 hit.");
				Assert.That(fs.Exists("cameo|ai/learned/plan_bandits.yaml"), Is.True,
					"the explicit cameo| mount prefix resolves through Folder.Contains (a real nested-path check).");
			}
			finally
			{
				Directory.Delete(temp.FullName, true);
			}
		}

		[TestCaseSource(nameof(LearnedFileDefaults))]
		public void LearnedFileDefault_CarriesExplicitMountPrefix(string label, string path)
		{
			Assert.That(path, Does.StartWith("cameo|"), $"{label}: {path} must resolve through the explicit cameo| mount, not a bare mod-relative path (M9).");
		}

		static object[] LearnedFileDefaults() => new object[]
		{
			new object[] { nameof(BotLearnedPriorsInfo), new BotLearnedPriorsInfo().PriorsFile },
			new object[] { nameof(BuildOrderKnobsBotModuleInfo), new BuildOrderKnobsBotModuleInfo().LearnedFile },
			new object[] { nameof(EngagementPriorsBotModuleInfo), new EngagementPriorsBotModuleInfo().PriorsFile },
			new object[] { nameof(PlanBanditBotModuleInfo), new PlanBanditBotModuleInfo().LearnedFile },
		};
	}
}
