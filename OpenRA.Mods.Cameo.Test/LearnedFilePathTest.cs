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
using OpenRA.Mods.CA.Traits.BotModules;
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
		public void TheConfiguredLearnedFilesResolveInsideTheCameoPackage()
		{
			// M9: the committed defaults stay bare (mod-relative); the resolver is what makes them
			// reachable, so pin every shipped default through it.
			var files = new[]
			{
				new BotLearnedPriorsInfo().PriorsFile,
				new BuildOrderKnobsBotModuleInfo().LearnedFile,
				new EngagementPriorsBotModuleInfo().PriorsFile,
				new PlanBanditBotModuleInfo().LearnedFile,
				new FightThresholdsBotModuleInfo().LearnedFile
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

		static OpenRA.FileSystem.FileSystem MountPackage(string root)
		{
			var fs = new OpenRA.FileSystem.FileSystem("cameo", new Dictionary<string, Manifest>(), Array.Empty<IPackageLoader>());
			fs.Mount(new Folder(root), "cameo");
			return fs;
		}

		[Test]
		public void FightThresholdsConsumerReadsFittedRowThroughMountedPackage()
		{
			// M9 regression: FightThresholdsBotModule passed the bare nested path to Exists/Open,
			// which can never hit the file index — the learned artifact was invisible at runtime.
			// Drive the consumer's own Load seam through a real mounted package with a synthetic
			// artifact carrying >= 150 evidence, and prove the fitted row is consumed.
			var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "cameo_fightpkg");
			var rel = Path.Combine("ai", "learned", "fight_learning.yaml");
			Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(root, rel)));
			File.WriteAllText(Path.Combine(root, rel),
				"BotFightLearning:\n\tSchema: 1\n\tEvidence@any: 200\n\tEngageMarginPct@any: 137\n\tRetreatRatioPct@any: 42\n");

			var fs = MountPackage(root);
			try
			{
				var info = new FightThresholdsBotModuleInfo();
				var fitted = FightThresholdsBotModule.Load(fs, info.LearnedFile);

				Assert.That(fitted.TryEngageMarginPct("td_gdi", "td_nod", out var margin), Is.True,
					"a fitted engage margin must reach the consumer, not just a resolved string");
				Assert.That(margin, Is.EqualTo(137));
				Assert.That(fitted.TryRetreatRatioPct("td_gdi", "td_nod", out var ratio), Is.True);
				Assert.That(ratio, Is.EqualTo(42));

				// Package-qualified override passes the resolver through unchanged.
				Assert.That(FightThresholdsBotModule.Load(fs, "cameo|" + info.LearnedFile)
					.TryEngageMarginPct("td_gdi", "td_nod", out var overridden), Is.True);
				Assert.That(overridden, Is.EqualTo(137));

				// Missing file stays neutral — no fabricated thresholds.
				Assert.That(FightThresholdsBotModule.Load(fs, "ai/learned/absent.yaml")
					.TryEngageMarginPct("td_gdi", "td_nod", out _), Is.False);
			}
			finally
			{
				fs.UnmountAll();
			}
		}

		[Test]
		public void FightThresholdsConsumerKeepsSparseAndMalformedNeutral()
		{
			var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "cameo_fightpkg_ctrl");
			var dir = Path.Combine(root, "ai", "learned");
			Directory.CreateDirectory(dir);

			// Sparse: below the 150-sample evidence floor — the row must not be consumed.
			File.WriteAllText(Path.Combine(dir, "sparse.yaml"),
				"BotFightLearning:\n\tSchema: 1\n\tEvidence@any: 5\n\tEngageMarginPct@any: 137\n");

			// Malformed: wrong root — parses but is not a valid artifact, so it stays neutral.
			File.WriteAllText(Path.Combine(dir, "malformed.yaml"),
				"NotFightLearning:\n\tSchema: 1\n\tEvidence@any: 500\n\tEngageMarginPct@any: 137\n");

			var fs = MountPackage(root);
			try
			{
				var sparse = FightThresholdsBotModule.Load(fs, "ai/learned/sparse.yaml");
				Assert.That(sparse.TryEngageMarginPct("td_gdi", "td_nod", out _), Is.False,
					"sub-threshold evidence must remain honestly neutral");

				var malformed = FightThresholdsBotModule.Load(fs, "ai/learned/malformed.yaml");
				Assert.That(malformed.TryEngageMarginPct("td_gdi", "td_nod", out _), Is.False,
					"a non-artifact document must not produce thresholds");

				// Off: a constructed module has loaded nothing (and in match the RequiresCondition
				// gate leaves the trait disabled) — every lookup returns the caller's fallback.
				var module = new FightThresholdsBotModule(null, new FightThresholdsBotModuleInfo());
				Assert.That(((IBotFightThresholds)module).EngageMarginPct("td_gdi", "td_nod", 999), Is.EqualTo(999));
				Assert.That(((IBotFightThresholds)module).RetreatRatioPct("td_gdi", "td_nod", 999), Is.EqualTo(999));
			}
			finally
			{
				fs.UnmountAll();
			}
		}
	}
}
