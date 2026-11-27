#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// CONSOL-DUP-03 (CONSOL_DUP_01 step 1, behaviour-neutral). Folded into CONSOLIDATE-TRAIN-01 on top of CONSOL-DUP-02, which already extracted the
	/// two shared helpers this step was written for: NearbyCombatantsCA (flee filter) and FindClosestAirTarget (air target). The train keeps those and
	/// drops the duplicate helpers. These pins fix that single-helper shape for both call sites.
	/// </summary>
	[TestFixture]
	public sealed class ConsolDup03Test
	{
		const string StateBasePath = "OpenRA.Mods.CA/Traits/BotModules/Squads/States/StateBaseCA.cs";
		const string AirStatesPath = "OpenRA.Mods.CA/Traits/BotModules/Squads/States/AirStatesCA.cs";

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + System.AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		[Test]
		public void NearbyCombatantsIsOneHelperForBothFleeChecks()
		{
			var src = RepoFile(StateBasePath);
			Assert.That(src, Does.Contain("var units = NearbyCombatantsCA.Scan(squad, randomSquadUnit.Actor.CenterPosition);"),
				"ShouldFlee reads the circle through the shared scan");
			Assert.That(src, Does.Contain("var enemyAroundUnit = NearbyCombatantsCA.Combatants(squad, units);"),
				"ShouldFlee filters through the shared combatant filter");
			Assert.That(src, Does.Contain("var units = NearbyCombatantsCA.Scan(squad, squadUnit.CenterPosition);"),
				"ShouldFleeSimple reads the circle through the same scan");

			// The observed-attacker filter lives once, in the shared helper; the callers do not repeat it.
			Assert.That(Regex.Matches(src, Regex.Escape("unit.Info.HasTraitInfo<AttackBaseInfo>()")).Count, Is.EqualTo(1),
				"one copy of the observed attacker filter");
		}

		[Test]
		public void FindClosestAirTargetIsOneHelperWithABigThreatFlag()
		{
			var src = RepoFile(AirStatesPath);
			Assert.That(src, Does.Contain("static Actor FindClosestAirTarget(SquadCA owner, Actor leader, WPos pos, bool bigThreatsOnly)"),
				"the helper takes the leader, the position and the big-threat flag");
			Assert.That(src, Does.Contain("(!bigThreatsOnly || owner.SquadManager.Info.BigAirThreats.Contains(a.Info.Name))"),
				"a false flag admits every enemy aircraft, a true flag narrows it to the big threats");
			Assert.That(src, Does.Contain("FindClosestAirTarget(owner, leader, pos, bigThreatsOnly: true);"),
				"the big-threat branch passes true");
			Assert.That(src, Does.Contain("FindClosestAirTarget(owner, leader, pos, bigThreatsOnly: false);"),
				"the general fallback passes false");

			// The threat check stays last in the predicate, so the enumeration and the candidate order do not change.
			var helperStart = src.IndexOf("static Actor FindClosestAirTarget", System.StringComparison.Ordinal);
			var helperEnd = src.IndexOf("protected static Actor FindDefenselessTarget", helperStart, System.StringComparison.Ordinal);
			var helper = src.Substring(helperStart, helperEnd - helperStart);
			Assert.That(helper.IndexOf("IsTargetableBy(leader)", System.StringComparison.Ordinal), Is.LessThan(helper.IndexOf("(!bigThreatsOnly ||", System.StringComparison.Ordinal)));
			Assert.That(helper, Does.Contain("owner.World.Actors.Where(").And.Contain(".ClosestToIgnoringPath(pos);"));
		}
	}
}
