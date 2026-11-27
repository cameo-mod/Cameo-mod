#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors. This file is made
 * available under the terms of the GNU General Public License, version 3.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class SquadNearbyCombatantsTest
	{
		const string SourcePath = "mods/CA/Traits/BotModules/Squads/States/StateBaseCA.cs";

		static string Source => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", SourcePath)).Replace("\r\n", "\n");

		[Test]
		public void CombatantFilterPreservesWorldCandidateOrderAndVisitsEachCandidateOnce()
		{
			var seen = new List<string>();
			var picked = NearbyCombatantsCA.FilterInCandidateOrder(
				new[] { "first", "second", "third", "fourth" },
				candidate =>
				{
					seen.Add(candidate);
					return candidate is "second" or "fourth";
				});

			Assert.That(seen, Is.EqualTo(new[] { "first", "second", "third", "fourth" }));
			Assert.That(picked, Is.EqualTo(new[] { "second", "fourth" }));
		}

		[Test]
		public void BothRetreatPathsShareOnlyTheNearbyScanAndKeepTheirOriginalOrigins()
		{
			var source = Source;
			Assert.That(source, Does.Contain("var randomSquadUnit = squad.Units.Random(squad.Random);\n\t\t\tvar units = NearbyCombatantsCA.Scan(squad, randomSquadUnit.Actor.CenterPosition);"));
			Assert.That(source, Does.Contain("var squadUnit = squad.Units[0].Actor;\n\t\t\tvar units = NearbyCombatantsCA.Scan(squad, squadUnit.CenterPosition);\n\t\t\tvar enemyAroundUnit = NearbyCombatantsCA.Combatants(squad, units);"));
			Assert.That(source, Does.Contain("return false;\n\n\t\t\tvar enemyAroundUnit = NearbyCombatantsCA.Combatants(squad, units);"), "the building veto stays before enemy classification");
			Assert.That(source, Does.Contain("squad.World.FindActorsInCircle(center, radius).ToList()"));
			Assert.That(source, Does.Not.Contain("OrderBy"), "the world candidate order must not be sorted");
		}
	}
}
