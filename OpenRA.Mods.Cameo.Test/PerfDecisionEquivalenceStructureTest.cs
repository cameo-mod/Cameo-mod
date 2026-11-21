#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using NUnit.Framework;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class PerfDecisionEquivalenceStructureTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file));

		static string Section(string source, string start, string end)
		{
			var startIndex = source.IndexOf(start, StringComparison.Ordinal);
			var endIndex = source.IndexOf(end, startIndex + start.Length, StringComparison.Ordinal);
			Assert.That(startIndex, Is.GreaterThanOrEqualTo(0), $"Missing section start: {start}");
			Assert.That(endIndex, Is.GreaterThan(startIndex), $"Missing section end: {end}");
			return source.Substring(startIndex, endIndex - startIndex);
		}

		[Test]
		public void SquadLeaseCommonPathReusesOrderedSnapshotsAndPreservesReconcileCallSites()
		{
			var source = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.cs");
			var wrapper = Section(source, "void ReconcileSquadLeases()", "void ReconcileSquadLeasesCore(");
			var core = Section(source, "void ReconcileSquadLeasesCore(", "void MissionTaken(");

			Assert.That(source, Does.Contain("readonly HashSet<Actor> heldBySquadsScratch = new();"));
			Assert.That(source, Does.Contain("readonly List<UnitWposWrapper> squadUnitsScratch = new();"));
			Assert.That(wrapper, Does.Contain("ReconcileSquadLeasesCore(heldBySquadsScratch, squadUnitsScratch)"));
			Assert.That(wrapper, Does.Contain("if (reconcilingSquadLeases)"));
			Assert.That(wrapper, Does.Contain("ReconcileSquadLeasesCore(new HashSet<Actor>(), new List<UnitWposWrapper>())"));
			Assert.That(wrapper, Does.Contain("finally"));
			Assert.That(wrapper.IndexOf("finally", StringComparison.Ordinal), Is.GreaterThan(wrapper.IndexOf("ReconcileSquadLeasesCore(heldBySquadsScratch, squadUnitsScratch)", StringComparison.Ordinal)));
			Assert.That(core, Does.Contain("squadUnitSnapshot.AddRange(squad.Units)"));
			Assert.That(core, Does.Contain("foreach (var u in squadUnitSnapshot)"));
			Assert.That(core, Does.Not.Contain("squad.Units.ToList()"));
			Assert.That(core.IndexOf("squadUnitSnapshot.AddRange(squad.Units)", StringComparison.Ordinal),
				Is.LessThan(core.IndexOf("squad.Units.Remove(u)", StringComparison.Ordinal)),
				"snapshot the ordered members before handoff mutates the source list");
			Assert.That(source.Split(new[] { "ReconcileSquadLeases();" }, StringSplitOptions.None).Length - 1, Is.EqualTo(3),
				"retain both normal same-tick passes and the damage-notification pass");
		}
	}
}
