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

using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// PROD-IDLE G1: the overflow line reader (tier line over carrier value). Written with the code; not run
	// (no build in the PERF WINDOW).
	[TestFixture]
	public class OverflowLineTest
	{
		[TestCase(12000, 8000, 12000)]
		[TestCase(8000, 8000, 8000)]    // hard: tier line 12000 - 800 x 5 equals the old carrier value
		[TestCase(4000, 8000, 4000)]
		[TestCase(0, 8000, 0)]          // a written 0 is "never armed", not "use the carrier"
		[TestCase(-1, 8000, 8000)]      // no line written: the carrier value
		[TestCase(-1, 0, 0)]
		public void TierLineWinsWhenWrittenOtherwiseTheCarrierValue(int tierLine, int carrier, int expected)
		{
			Assert.That(BaseBuilderQueueEvalCA.OverflowLine(tierLine, carrier), Is.EqualTo(expected));
		}

		[Test]
		public void TheElevenTierLinesAreOneStraightLineAndArmStrictlyAboveIt()
		{
			for (var r = 0; r <= 10; r++)
			{
				var line = 12000 - 800 * r;
				Assert.That(BaseBuilderQueueEvalCA.OverflowLine(line, 8000), Is.EqualTo(line));
				Assert.That(BaseBuilderQueueEvalCA.OverflowArmed(true, line + 1, line), Is.True, "r=" + r);
				Assert.That(BaseBuilderQueueEvalCA.OverflowArmed(true, line, line), Is.False, "r=" + r);

				// The carrier is the on/off scope: a bot without it (classic) never arms, whatever its profile line.
				Assert.That(BaseBuilderQueueEvalCA.OverflowArmed(false, line + 1, line), Is.False, "r=" + r);
			}
		}
	}
}
