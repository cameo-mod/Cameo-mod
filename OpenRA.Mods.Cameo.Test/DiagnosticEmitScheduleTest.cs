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

using System.Collections.Generic;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// ADP-05: catch-up emit. Written with the code; not run (no build in the PERF WINDOW).
	[TestFixture]
	public class DiagnosticEmitScheduleTest
	{
		static List<(int Slot, int Tick)> Run(int step, int offset, int interval, int maxSlot, int lastTick)
		{
			var next = 0;
			var emitted = new List<(int, int)>();
			for (var t = offset; t <= lastTick; t += step)
				if (DiagnosticEmitSchedule.TryTake(t, ref next, interval, maxSlot, out var slot))
					emitted.Add((slot, t));
			return emitted;
		}

		[Test]
		public void EveryTickVisitedEqualsTheOldModuloTicks()
		{
			var emitted = Run(1, 0, 250, 50000, 60000);

			var expected = new List<int>();
			for (var t = 0; t <= 50000; t++)
				if (t % 250 == 0)
					expected.Add(t);

			Assert.That(emitted.ConvertAll(e => e.Slot), Is.EqualTo(expected));
			Assert.That(emitted.TrueForAll(e => e.Slot == e.Tick), Is.True, "sample tick equals the slot");
		}

		[TestCase(7)]
		[TestCase(12)]
		[TestCase(37)]
		public void RoundRobinVisitsStillEmitEverySlotOnce(int step)
		{
			var emitted = Run(step, 3, 250, 50000, 60000);

			Assert.That(emitted.Count, Is.EqualTo(201), "slots 0, 250 .. 50000");
			for (var i = 0; i < emitted.Count; i++)
			{
				Assert.That(emitted[i].Slot, Is.EqualTo(i * 250), "slot ticks are multiples of the interval");
				Assert.That(emitted[i].Tick, Is.GreaterThanOrEqualTo(emitted[i].Slot), "sampled at or after the slot");
				Assert.That(emitted[i].Tick - emitted[i].Slot, Is.LessThan(step + 250),
					"first visited tick at or after the slot");
			}
		}

		[Test]
		public void VisitGapLongerThanTheIntervalCatchesUpOneSlotPerVisit()
		{
			var emitted = Run(1000, 0, 250, 50000, 5000);

			// A visit every 1000 ticks emits one slot per visit; the slots lag but never skip or duplicate.
			Assert.That(emitted.ConvertAll(e => e.Slot), Is.EqualTo(new List<int> { 0, 250, 500, 750, 1000, 1250 }));
		}

		[Test]
		public void SlotsAboveTheMaximumAreNeverDueAndNonPositiveIntervalIsRejected()
		{
			var next = 50250;
			Assert.That(DiagnosticEmitSchedule.TryTake(99999, ref next, 250, 50000, out _), Is.False);
			Assert.That(next, Is.EqualTo(50250), "nothing advanced");

			next = 0;
			Assert.That(DiagnosticEmitSchedule.TryTake(10, ref next, 0, 50000, out _), Is.False);
			Assert.That(DiagnosticEmitSchedule.TryTake(-1, ref next, 250, 50000, out _), Is.False, "not yet due");
		}
	}
}
