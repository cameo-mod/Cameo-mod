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
using NUnit.Framework;

namespace OpenRA.Mods.Cameo.Test
{
	// The seat-event channel died at tick 35 of every match: MovementSample iterates
	// world.Actors, and non-physical actors (PlayerActor, effect actors) pass the
	// IsInWorld/!IsDead/Owner guards but have OccupiesSpace == null, so actor.Location
	// threw NullReferenceException (site=tick, frame Actor.get_Location). The engine
	// contract Actor.Location => OccupiesSpace.TopLeft means any unguarded .Location
	// read over an arbitrary world.Actors loop is the same crash. These are source-text
	// contract pins (same convention as SlaveMinerUnboundIdleTest): they cannot run a
	// world, so they pin the guard and the engine contract that makes it necessary.
	[TestFixture]
	public class AiPlayerTelemetryOccupiesSpaceGuardTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file));

		static string Section(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text.Substring(start, end - start);
		}

		[Test]
		public void MovementSampleSkipsActorsWithoutOccupiesSpace()
		{
			var body = Section(Source("mods/Cameo/Traits/AiPlayerTelemetry.cs"),
				"void MovementSample(", "static (double Distance, string Seat) NearestEnemy(");
			var guard = body.IndexOf("OccupiesSpace == null", StringComparison.Ordinal);
			Assert.That(guard, Is.GreaterThanOrEqualTo(0),
				"MovementSample's world.Actors loop must skip actors with no IOccupySpace " +
				"(PlayerActor/effect actors crash actor.Location via OccupiesSpace.TopLeft)");
			var firstLocation = body.IndexOf(".Location", StringComparison.Ordinal);
			Assert.That(firstLocation, Is.GreaterThan(guard),
				"the OccupiesSpace guard must precede every .Location read in the loop");
		}

		[Test]
		public void EngineLocationContractStillDereferencesOccupiesSpace()
		{
			// If an engine bump makes Location null-safe the guard stays harmless, but this
			// pin forces a revisit: a .Location read anywhere over world.Actors is only safe
			// while the guard exists.
			var actor = Source("engine_Actor.cs");
			Assert.That(actor, Does.Contain("public CPos Location => OccupiesSpace.TopLeft"));
			Assert.That(actor, Does.Contain("public WPos CenterPosition => OccupiesSpace.CenterPosition"));
		}

		[Test]
		public void FailStillCarriesTheTickSiteTag()
		{
			// The site tags are what localized this NRE; a regression that drops the tag makes
			// the next channel death unfindable again.
			var telemetry = Source("mods/Cameo/Traits/AiPlayerTelemetry.cs");
			var tick = Section(telemetry, "void ITick.Tick(Actor self)", "void CheckBuildingOwners()");
			Assert.That(tick, Does.Contain("Fail(e, \"tick\")"));
		}
	}
}
