#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OpenRA.Mods.Cameo.Test
{
	// MULTI-BUILDER-FIX-01: V06 crashed at world creation ("Actor player has multiple traits of type UnitBuilderBotModuleCA") because
	// CLASSIC-FREEZE-01 added @classic copies of the unit and base builders and AiMatchLogRecorder used a single-instance lookup.
	// Guard: no mod code may use a single-instance lookup for a trait type that ai.yaml declares more than once.
	[TestFixture]
	public sealed class MultiBuilderFix01Test
	{
		static string Root => Path.Combine(AppContext.BaseDirectory, "source");

		static string[] MultiInstanceTypes()
		{
			var ai = File.ReadAllText(Path.Combine(Root, "packaging", "ai.yaml")).Replace("\r\n", "\n");
			return Regex.Matches(ai, @"^\t([A-Za-z]+)@[^:\n]*:", RegexOptions.Multiline)
				.Select(m => m.Groups[1].Value)
				.GroupBy(t => t)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.ToArray();
		}

		[Test]
		public void TheUnitAndBaseBuildersAreDeclaredMoreThanOnce()
		{
			var types = MultiInstanceTypes();
			Assert.That(types, Does.Contain("UnitBuilderBotModuleCA"));
			Assert.That(types, Does.Contain("BaseBuilderBotModuleCA"));
		}

		[Test]
		public void NoModCodeUsesASingleInstanceLookupForAMultiInstanceTrait()
		{
			var types = MultiInstanceTypes();
			var lookup = new Regex(@"\b(Trait|TraitOrDefault|TraitInfo|TraitInfoOrDefault)<(?:[A-Za-z_][A-Za-z0-9_]*\.)*(" + string.Join("|", types) + @")(Info)?>\s*\(");
			var hits = Directory.GetFiles(Path.Combine(Root, "mods"), "*.cs", SearchOption.AllDirectories)
				.SelectMany(f => File.ReadAllLines(f).Select((l, i) => (f, i, l)))
				.Where(x => lookup.IsMatch(x.l))
				.Select(x => $"{Path.GetFileName(x.f)}:{x.i + 1}: {x.l.Trim()}")
				.ToArray();
			Assert.That(hits, Is.Empty, "single-instance lookup of a trait that ai.yaml declares more than once throws at world creation");
		}

		[Test]
		public void TheMatchLogRecorderSubscribesToEveryUnitBuilder()
		{
			var src = File.ReadAllText(Path.Combine(Root, "mods", "Cameo", "Traits", "AiMatchLogRecorder.cs"));
			Assert.That(src, Does.Contain("foreach (var unitBuilder in self.TraitsImplementing<UnitBuilderBotModuleCA>())"));
		}
	}
}
