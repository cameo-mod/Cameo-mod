#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
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
	/// <summary>
	/// CONSOL-DUP-04 item 2: the learned-YAML open helper (LearnedYaml, next to LearnedFilePath) shares path qualification, the existence check, open and MiniYaml parse, and owns no failure policy.
	/// Each caller keeps its own: BotLearnedPriors (missing = neutral + debug log, errors propagate) and EngagementPriorsBotModule (missing = "none", open/parse errors caught into "error").
	/// </summary>
	[TestFixture]
	public class ConsolDup04LearnedYamlTest
	{
		static string Source(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		static string Between(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text[start..end];
		}

		static OpenRA.FileSystem.FileSystem Mount(string root)
		{
			var fs = new OpenRA.FileSystem.FileSystem("cameo", new Dictionary<string, Manifest>(), Array.Empty<IPackageLoader>());
			fs.Mount(new Folder(root), "cameo");
			return fs;
		}

		[Test]
		public void AMissingFileIsNullAndAnExistingOneIsPackageQualified()
		{
			var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "cameo_modpkg_cd4");
			Directory.CreateDirectory(Path.Combine(root, "ai", "learned"));
			File.WriteAllText(Path.Combine(root, "ai", "learned", "p.yaml"), "a: 1\nb:\n\tc: 2\n");
			var fs = Mount(root);
			try
			{
				Assert.That(LearnedYaml.ExistingPath(fs, "ai/learned/missing.yaml"), Is.Null, "missing is null, nothing is thrown");
				Assert.That(LearnedYaml.ExistingPath(fs, "ai/learned/p.yaml"), Is.EqualTo("cameo|ai/learned/p.yaml"));
				Assert.That(LearnedYaml.ExistingPath(fs, "cameo|ai/learned/p.yaml"), Is.EqualTo("cameo|ai/learned/p.yaml"), "an already qualified path passes through");

				var nodes = LearnedYaml.Parse(fs, "cameo|ai/learned/p.yaml", "ai/learned/p.yaml");
				Assert.That(nodes, Has.Count.EqualTo(2));
				Assert.That(nodes[1].Key, Is.EqualTo("b"));
			}
			finally
			{
				fs.UnmountAll();
			}
		}

		[Test]
		public void OpenErrorsPropagateFromTheHelper()
		{
			var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "cameo_modpkg_cd4b");
			Directory.CreateDirectory(root);
			var fs = Mount(root);
			try
			{
				Assert.That(() => LearnedYaml.Parse(fs, "cameo|ai/learned/never.yaml", "ai/learned/never.yaml"), Throws.Exception, "the helper catches nothing; the caller decides");
			}
			finally
			{
				fs.UnmountAll();
			}
		}

		[Test]
		public void TheHelperOwnsNoFailurePolicy()
		{
			var helper = Source("OpenRA.Mods.Cameo/Traits/BotModules/LearnedFilePath.cs");
			var yaml = helper[helper.IndexOf("internal static class LearnedYaml", StringComparison.Ordinal)..];
			Assert.That(System.Text.RegularExpressions.Regex.IsMatch(yaml, @"(?m)^\s*(try|catch)"), Is.False, "no try or catch statement in the helper");
			Assert.That(yaml, Does.Not.Contain("Log."));
			Assert.That(yaml, Does.Contain("return fs.Exists(path) ? path : null;"));
		}

		[Test]
		public void TheArsenalLoaderKeepsItsPolicyMissingIsNeutralAndErrorsPropagate()
		{
			var arsenal = Source("OpenRA.Mods.Cameo/Traits/BotModules/BotLearnedPriors.cs");
			var load = Between(arsenal, "void IBotTick.BotTick(IBot bot)", "#if CAMEO_DEV_LOGGING");
			Assert.That(load, Does.Contain("var priorsPath = LearnedYaml.ExistingPath(fs, Info.PriorsFile);\n\t\t\tif (priorsPath == null)"));
			Assert.That(load, Does.Contain("LEARNED priors: no priors ({Info.PriorsFile} missing), everything neutral"));
			Assert.That(load, Does.Contain("priors = ArsenalPriors.Parse(LearnedYaml.Parse(fs, priorsPath, Info.PriorsFile));"));
			Assert.That(load, Does.Not.Contain("try"));
			Assert.That(load, Does.Not.Contain("catch"));
			Assert.That(load, Does.Contain("if (loaded)\n\t\t\t\treturn;\n\n\t\t\tloaded = true;"), "the one-time guard is unchanged");
		}

		[Test]
		public void TheEngagementLoaderKeepsItsPolicyNoneAndErrorStates()
		{
			var engagement = Source("OpenRA.Mods.Cameo/Traits/BotModules/EngagementPriorsBotModule.cs");
			var load = Between(engagement, "void IBotTick.BotTick(IBot bot)", "int IBotEngagementPriors.CorrectionMilli");
			Assert.That(load, Does.Contain("var priorsPath = LearnedYaml.ExistingPath(fs, Info.PriorsFile);\n\t\t\tif (priorsPath == null)\n\t\t\t{\n\t\t\t\tpriorsState = \"none\";"));
			Assert.That(load.IndexOf("LearnedYaml.ExistingPath", StringComparison.Ordinal), Is.LessThan(load.IndexOf("try", StringComparison.Ordinal)), "the existence check stays outside the try, as before");
			Assert.That(load, Does.Contain("priors = BotEngagementPriors.Parse(LearnedYaml.Parse(fs, priorsPath, Info.PriorsFile));\n\n\t\t\t\tpriorsState = \"fitted\";"));
			Assert.That(load, Does.Contain("catch (Exception e)\n\t\t\t{\n\t\t\t\tpriors = new BotEngagementPriors();\n\t\t\t\tpriorsState = \"error\";"));
		}
	}
}
