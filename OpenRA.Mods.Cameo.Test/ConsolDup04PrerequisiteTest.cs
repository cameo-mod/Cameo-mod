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
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// CONSOL-DUP-04 item 1: the upgrade completion check (UnitBuilder.UpgradePrerequisitesHeld) reuses TechDepthCA.Positive instead of its own token rule.
	/// The two rules were NOT the same text, so the substitution is pinned: case (the owned set is case-insensitive), whitespace, multiple and embedded '!', embedded '~' are
	/// compared token by token against the old rule, the differing edge cases are pinned as documented differences, and the guard is that no shipped rule and no loaded
	/// Buildable token can contain them.
	/// </summary>
	[TestFixture]
	public class ConsolDup04PrerequisiteTest
	{
		// The old rule, verbatim: every '~' removed, nothing trimmed, nothing lower-cased; a token that starts with '!' (after the '~' removal) or is empty is skipped.
		static string LegacyNeed(string prerequisite)
		{
			var p = prerequisite.Replace("~", "");
			return p.Length > 0 && !p.StartsWith('!') ? p : null;
		}

		static bool LegacyBlocks(string token, ISet<string> owned)
		{
			var p = token.Replace("~", "");
			return p.Length > 0 && !p.StartsWith('!') && !owned.Contains(p);
		}

		static bool NewBlocks(string token, ISet<string> owned)
		{
			var need = TechDepthCA.Positive(token);
			return need != null && !owned.Contains(need);
		}

		static HashSet<string> Owned(params string[] names) => new(names, StringComparer.OrdinalIgnoreCase);

		static DirectoryInfo RepoRoot()
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return dir;
		}

		static string UnitBuilderSource() =>
			File.ReadAllText(Path.Combine(RepoRoot().FullName, "OpenRA.Mods.CA/Traits/BotModules/UnitBuilderBotModuleCA.cs")).Replace("\r\n", "\n");

		[Test]
		public void CaseDoesNotChangeTheDecisionBecauseTheOwnedSetIsCaseInsensitive()
		{
			var owned = Owned("ra1_powerplant");
			foreach (var token in new[] { "ra1_powerplant", "RA1_PowerPlant", "Ra1_POWERPLANT", "~RA1_PowerPlant", "~~Ra1_PowerPlant", "ra1_barracks", "RA1_Barracks", "~RA1_Barracks" })
				Assert.That(NewBlocks(token, owned), Is.EqualTo(LegacyBlocks(token, owned)), token);

			// The caller's set (UnitBuilder.OwnedPrerequisites) is built with OrdinalIgnoreCase; the pin keeps it that way.
			var unit = UnitBuilderSource();
			var owner = unit.Substring(unit.IndexOf("HashSet<string> OwnedPrerequisites()", StringComparison.Ordinal), 200);
			Assert.That(owner, Does.Contain("new HashSet<string>(StringComparer.OrdinalIgnoreCase)"));
		}

		[Test]
		public void ThePlainAndTheNegatedTokensAgreeWithTheOldRule()
		{
			var owned = Owned("x");
			foreach (var token in new[] { "x", "y", "~x", "~y", "~~x", "~!x", "!x", "!y", "!!x", "!~x", "~!~x", "", "~", "~~" })
				Assert.That(NewBlocks(token, owned), Is.EqualTo(LegacyBlocks(token, owned)), "[" + token + "]");

			// Several '!' at the START are still a negation under both rules (nothing is required).
			foreach (var token in new[] { "!x", "!!x", "!!!x", "~!!x", "!~!x" })
			{
				Assert.That(LegacyNeed(token), Is.Null, token);
				Assert.That(TechDepthCA.Positive(token), Is.Null, token);
			}
		}

		[Test]
		public void TheDifferingEdgeCasesArePinnedAsDocumentedDifferences()
		{
			var owned = Owned("x");

			// Whitespace: the new rule trims; the old one compared " x " literally (never owned, so it blocked the offer).
			Assert.That(LegacyBlocks(" x ", owned), Is.True);
			Assert.That(NewBlocks(" x ", owned), Is.False);
			Assert.That(LegacyBlocks(" ", owned), Is.True, "a whitespace-only token blocked under the old rule");
			Assert.That(NewBlocks(" ", owned), Is.False, "and is skipped under the new one");

			// A '!' that is not at the start: the new rule treats it as a negation, the old one required the literal token.
			Assert.That(LegacyBlocks("x!y", owned), Is.True);
			Assert.That(NewBlocks("x!y", owned), Is.False);
			Assert.That(LegacyBlocks("x!", owned), Is.True);
			Assert.That(NewBlocks("x!", owned), Is.False);

			// A '~' inside the token: the old rule removed it, the new one only strips leading ones.
			Assert.That(LegacyNeed("a~b"), Is.EqualTo("ab"));
			Assert.That(TechDepthCA.Positive("a~b"), Is.EqualTo("a~b"));
		}

		[Test]
		public void TheLoaderTrimsEveryTokenSoWhitespaceCannotReachTheRule()
		{
			// BuildableInfo.Prerequisites is loaded by FieldLoader as a comma list; each token is trimmed.
			var loaded = FieldLoader.GetValue<string[]>("Prerequisites", " a ,\t~b , !c ,  ");
			Assert.That(loaded, Is.Not.Empty);
			foreach (var token in loaded)
				Assert.That(token, Is.EqualTo(token.Trim()), "[" + token + "]");
		}

		[Test]
		public void EveryShippedPrerequisiteTokenGivesTheSameNeedUnderBothRules()
		{
			var root = RepoRoot();
			var line = new Regex(@"^\s*-?Prerequisites(?:@\w+)?:\s*(.*)$", RegexOptions.Multiline | RegexOptions.CultureInvariant);
			var tokens = 0;
			var differing = new List<string>();
			foreach (var mod in Directory.GetDirectories(Path.Combine(root.FullName, "mods")))
			{
				var rules = Path.Combine(mod, "rules");
				if (!Directory.Exists(rules))
					continue;

				foreach (var file in Directory.EnumerateFiles(rules, "*.yaml", SearchOption.AllDirectories))
					foreach (Match m in line.Matches(File.ReadAllText(file).Replace("\r\n", "\n")))
					{
						var value = m.Groups[1].Value;
						var hash = value.IndexOf('#');
						if (hash >= 0)
							value = value[..hash];

						foreach (var token in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
						{
							tokens++;
							var old = LegacyNeed(token)?.ToLowerInvariant();
							if (old != TechDepthCA.Positive(token))
								differing.Add(Path.GetFileName(file) + ": [" + token + "] old=" + old + " new=" + TechDepthCA.Positive(token));
						}
					}
			}

			// One shipped typo (starwars.yaml: "anyhq. ~disabled", a missing comma): under both rules it names a prerequisite no provider can give ("anyhq. disabled" or "anyhq. ~disabled"), so the offer is blocked either way.
			differing.RemoveAll(d => d.EndsWith("[anyhq. ~disabled] old=anyhq. disabled new=anyhq. ~disabled", StringComparison.Ordinal));
			var typo = Owned("anyhq", "disabled");
			Assert.That(LegacyBlocks("anyhq. ~disabled", typo), Is.True);
			Assert.That(NewBlocks("anyhq. ~disabled", typo), Is.True);

			Assert.That(tokens, Is.GreaterThan(1000), "the scan must find the shipped rules");
			Assert.That(differing, Is.Empty, "the shipped tokens may not use whitespace, an embedded '!' or an embedded '~'");
		}

		[Test]
		public void TheUpgradeCheckUsesTheSharedRule()
		{
			var unit = UnitBuilderSource();
			var check = unit.Substring(unit.IndexOf("bool UpgradePrerequisitesHeld(string name, ISet<string> owned)", StringComparison.Ordinal), 900);
			Assert.That(check, Does.Contain("var need = TechDepthCA.Positive(prerequisite);\n\t\t\t\t\tif (need != null && !owned.Contains(need))\n\t\t\t\t\t\treturn false;"));
			Assert.That(check, Does.Not.Contain("Replace(\"~\""), "the second token rule is gone");
		}
	}
}
