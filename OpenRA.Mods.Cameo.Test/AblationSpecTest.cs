#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	// ABLATE-01 DEV value parser (ABLATE_01_PLAN_02 section 2). The source file is linked from the DEV assembly.
	// Written with the code; not run (no build in the PERF WINDOW).
	[TestFixture]
	public class AblationSpecTest
	{
		static readonly string[] Known = { "pi_lean", "pi_army_ratio", "cn_waves" };

		static AblationSpec Parse(string value)
		{
			Assert.That(AblationSpec.TryParse(value, Known, out var spec, out var error), Is.True, error);
			return spec;
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("   ")]
		public void EmptyOrAbsentIsTheFullBundle(string value)
		{
			var spec = Parse(value);
			Assert.That(spec.Ids, Is.Empty);
			Assert.That(spec.IsOff("pi_lean", 1), Is.False);
		}

		[Test]
		public void ListWithoutTeamIsOffForEveryPlayerIncludingNoTeam()
		{
			var spec = Parse("pi_lean, pi_army_ratio");
			Assert.That(spec.Ids, Is.EquivalentTo(new[] { "pi_lean", "pi_army_ratio" }));
			Assert.That(spec.Team, Is.Null);
			foreach (var team in new[] { 0, 1, 2 })
			{
				Assert.That(spec.IsOff("pi_lean", team), Is.True);
				Assert.That(spec.IsOff("cn_waves", team), Is.False, "not in the list");
			}
		}

		[Test]
		public void TeamSuffixLimitsTheListToThatLobbyTeam()
		{
			var spec = Parse("pi_lean,cn_waves@team2");
			Assert.That(spec.Team, Is.EqualTo(2));
			Assert.That(spec.IsOff("pi_lean", 2), Is.True);
			Assert.That(spec.IsOff("cn_waves", 2), Is.True);
			Assert.That(spec.IsOff("pi_lean", 1), Is.False);
			Assert.That(spec.IsOff("pi_lean", 0), Is.False, "players without a team are never matched by a team value");
		}

		[TestCase("pi_nope", "unknown")]
		[TestCase("pi_lean,pi_nope", "unknown")]
		[TestCase("pi_lean,", "empty id")]
		[TestCase(",pi_lean", "empty id")]
		[TestCase("pi_lean@team0", "bad team suffix")]
		[TestCase("pi_lean@team", "bad team suffix")]
		[TestCase("pi_lean@blue", "bad team suffix")]
		[TestCase("pi_lean@team-1", "bad team suffix")]
		[TestCase("@team1", "empty id")]
		public void InvalidValuesFailClosedWithAReason(string value, string reason)
		{
			Assert.That(AblationSpec.TryParse(value, Known, out var spec, out var error), Is.False);
			Assert.That(error, Does.Contain(reason));
			Assert.That(spec, Is.SameAs(AblationSpec.Empty), "a failed parse never hands back a partial spec");
		}

		[Test]
		public void RegistryIdsAreTheConstantsOfThePartialAblationIds()
		{
			var ids = AblationSpec.RegistryIds();
			Assert.That(ids, Does.Contain("pi_lean"));
			Assert.That(ids.Count(i => i.StartsWith("pi_")), Is.EqualTo(9));
			Assert.That(AblationSpec.TryParse("pi_queue_depth,pi_recovery@team1", ids, out _, out _), Is.True);
		}
	}
}
