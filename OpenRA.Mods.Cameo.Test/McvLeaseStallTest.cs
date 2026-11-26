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
using System.IO;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	// CONSOLIDATE-03 P8 (b), row mcv_lease_stall_release (CONSOLIDATE_03_P8_DESIGN_01.md): a live MCV that has not changed cell for McvLeaseStallTicks gives its field lease up and
	// the field is parked; no order is issued. Default off in code, on in @generic.
	[TestFixture]
	public sealed class McvLeaseStallTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void TheRowIsDefaultOffRegisteredAndOnInGeneric()
		{
			Assert.That(AblationIds.McvLeaseStallRelease, Is.EqualTo("mcv_lease_stall_release"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain(AblationIds.McvLeaseStallRelease));

			var planner = Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.cs");
			Assert.That(planner, Does.Contain("public readonly bool McvLeaseStallRelease = false;"), "default off in code");
			Assert.That(planner, Does.Contain("public readonly int McvLeaseStallTicks = 1500;"));
			Assert.That(planner, Does.Contain("bool McvLeaseStallOn => Info.McvLeaseStallRelease && Info.FieldCoverage && !ablateMcvLeaseStall.Off(player, AblationIds.McvLeaseStallRelease);"));

			var yaml = Source("packaging/ai.yaml");
			Assert.That(System.Text.RegularExpressions.Regex.Matches(yaml, "\t\tMcvLeaseStallRelease: true\n").Count, Is.EqualTo(1), "the genericbot expansion block only");
			Assert.That(yaml, Does.Contain("\t\tMcvLeaseStallTicks: 1500\n"));
		}

		[Test]
		public void TheStallReleasesTheLeaseParksTheFieldAndIssuesNoOrder()
		{
			var planner = Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.cs");
			var sweep = planner.Substring(planner.IndexOf("foreach (var id in mcvLeases.Ids())", StringComparison.Ordinal));
			sweep = sweep.Substring(0, sweep.IndexOf("var diagonalCells = ", StringComparison.Ordinal));

			var gone = sweep.IndexOf("if (SiteHolderGone(", StringComparison.Ordinal);
			var stall = sweep.IndexOf("else if (McvLeaseStallOn)", StringComparison.Ordinal);
			Assert.That(gone, Is.GreaterThanOrEqualTo(0));
			Assert.That(stall, Is.GreaterThan(gone), "a gone holder is released first; the stall test only runs for a live MCV");
			Assert.That(sweep, Does.Contain("mcvLeases.Note(id, heading.Location, leaseNow);"));
			Assert.That(sweep, Does.Contain("mcvLeases.Stalled(id, leaseNow, Info.McvLeaseStallTicks)"));
			Assert.That(sweep, Does.Contain("parkedUntil[stalledField] = leaseNow + Info.ParkTicks;"));
			Assert.That(sweep, Does.Not.Contain("QueueOrder"), "no order: the engine MCV manager stays the only owner");
			Assert.That(sweep, Does.Not.Contain("new Order("));
		}
	}
}
