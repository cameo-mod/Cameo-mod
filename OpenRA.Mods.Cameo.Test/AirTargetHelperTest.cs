#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors. This file is made
 * available under the terms of the GNU General Public License, version 3.
 */
#endregion

using System;
using System.IO;
using NUnit.Framework;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class AirTargetHelperTest
	{
		const string SourcePath = "mods/CA/Traits/BotModules/Squads/States/AirStatesCA.cs";

		static string Source => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", SourcePath)).Replace("\r\n", "\n");

		[Test]
		public void SharedAirTargetQueryKeepsCandidateEnumerationAndNearestSelectionOrder()
		{
			var source = Source;
			var start = source.IndexOf("static Actor FindClosestAirTarget(", StringComparison.Ordinal);
			var end = source.IndexOf("protected static Actor FindDefenselessTarget(", start, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0));
			Assert.That(end, Is.GreaterThan(start));
			var helper = source.Substring(start, end - start);

			Assert.That(helper, Does.Contain("owner.World.Actors.Where("));
			Assert.That(helper, Does.Contain(".ClosestToIgnoringPath(pos)"));
			Assert.That(helper, Does.Not.Contain("OrderBy").And.Not.Contain("Shuffle").And.Not.Contain("Random"));
			Assert.That(helper, Does.Contain("IsPreferredEnemyAircraft(a) &&\n\t\t\t\towner.SquadManager.IsNotHiddenUnit(a) &&\n\t\t\t\ta.IsTargetableBy(leader) &&\n\t\t\t\t(!bigThreatsOnly || owner.SquadManager.Info.BigAirThreats.Contains(a.Info.Name))"));
		}

		[Test]
		public void AirToAirPriorityKeepsTheSameTwoPossibleRngDraws()
		{
			var source = Source;
			var start = source.IndexOf("protected static Actor FindAirTarget(", StringComparison.Ordinal);
			var end = source.IndexOf("static Actor FindClosestAirTarget(", start, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0));
			Assert.That(end, Is.GreaterThan(start));
			var selection = source.Substring(start, end - start);

			Assert.That(Count(selection, ".Next(0, 100)"), Is.EqualTo(2), "the first roll always occurs; the second is only possible for configured big-air tags");
			Assert.That(selection, Does.Contain("var groundTargetPriority = BotRng.For(owner.Bot, nameof(AirStateBaseCA)).Next(0, 100);"));
			Assert.That(selection, Does.Contain("if (owner.SquadManager.Info.BigAirThreats.Count > 0)\n\t\t\t\t{\n\t\t\t\t\tvar regularTargetPriority = BotRng.For(owner.Bot, nameof(AirStateBaseCA)).Next(0, 100);"));
			Assert.That(selection, Does.Contain("FindClosestAirTarget(owner, leader, pos, bigThreatsOnly: true)"));
			Assert.That(selection, Does.Contain("FindClosestAirTarget(owner, leader, pos, bigThreatsOnly: false)"));
		}

		static int Count(string value, string token)
		{
			var count = 0;
			var offset = 0;
			while ((offset = value.IndexOf(token, offset, StringComparison.Ordinal)) >= 0)
			{
				count++;
				offset += token.Length;
			}

			return count;
		}
	}
}
