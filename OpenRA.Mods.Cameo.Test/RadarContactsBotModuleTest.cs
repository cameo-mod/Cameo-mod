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

using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class RadarContactsBotModuleTest
	{
		// RADAR-A: the contact payload is contractually anonymous — cell, tick,
		// velocity, icon class, owner. Anything richer (type, health, ActorID)
		// is information the human radar never shows, i.e. a fog leak.

		[Test]
		public void ContactPayloadExposesOnlyTheHumanVisibleFields()
		{
			var fields = typeof(BotRadarContact)
				.GetFields(BindingFlags.Public | BindingFlags.Instance)
				.Select(f => f.Name)
				.OrderBy(n => n)
				.ToArray();
			Assert.That(fields, Is.EqualTo(new[] { "Cell", "Class", "Owner", "Tick", "VXPerKilotick", "VYPerKilotick" }));
		}

		[Test]
		public void VelocityIsCellsPerKilotick()
		{
			Assert.That(RadarContactsBotModule.VelocityPerKilotick(5, 25), Is.EqualTo(200));
			Assert.That(RadarContactsBotModule.VelocityPerKilotick(-3, 25), Is.EqualTo(-120));
			Assert.That(RadarContactsBotModule.VelocityPerKilotick(0, 25), Is.EqualTo(0));
		}

		[Test]
		public void VelocityOfZeroElapsedTimeIsZero()
		{
			Assert.That(RadarContactsBotModule.VelocityPerKilotick(5, 0), Is.EqualTo(0));
			Assert.That(RadarContactsBotModule.VelocityPerKilotick(5, -1), Is.EqualTo(0));
		}

		[Test]
		public void FirstSightingHasZeroDrift()
		{
			var track = new RadarContactsBotModule.Track { Cell = new CPos(10, 20), Tick = 100 };
			Assert.That(track.VXPerKilotick, Is.EqualTo(0));
			Assert.That(track.VYPerKilotick, Is.EqualTo(0));
		}

		[Test]
		public void SecondSightingComputesDrift()
		{
			var track = new RadarContactsBotModule.Track { Cell = new CPos(10, 20), Tick = 100 };
			RadarContactsBotModule.UpdateTrack(track, new CPos(15, 10), 150);
			Assert.That(track.VXPerKilotick, Is.EqualTo(100));
			Assert.That(track.VYPerKilotick, Is.EqualTo(-200));
			Assert.That(track.Cell, Is.EqualTo(new CPos(15, 10)));
			Assert.That(track.Tick, Is.EqualTo(150));
		}

		[Test]
		public void SameTickSightingKeepsOlderDrift()
		{
			var track = new RadarContactsBotModule.Track { Cell = new CPos(10, 20), Tick = 100, VXPerKilotick = 42 };
			RadarContactsBotModule.UpdateTrack(track, new CPos(11, 21), 100);
			Assert.That(track.VXPerKilotick, Is.EqualTo(42));
			Assert.That(track.Cell, Is.EqualTo(new CPos(11, 21)));
		}
	}
}
