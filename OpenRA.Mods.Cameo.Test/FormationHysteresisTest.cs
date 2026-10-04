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

using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class FormationHysteresisTest
	{
		const long Lead = 6144; // WDist.FromCells(6).Length
		const long Hyst = 2048; // WDist.FromCells(2).Length

		[Test]
		public void EntersHoldAboveLeadPlusHysteresis()
		{
			Assert.That(SquadMicroEvalCA.ClassifyHolding(Lead + Hyst + 1, Lead, Hyst, false), Is.True);
			Assert.That(SquadMicroEvalCA.ClassifyHolding(Lead + Hyst + 1, Lead, Hyst, true), Is.True);
		}

		[Test]
		public void LeavesHoldAtOrBelowLead()
		{
			Assert.That(SquadMicroEvalCA.ClassifyHolding(Lead, Lead, Hyst, true), Is.False);
			Assert.That(SquadMicroEvalCA.ClassifyHolding(Lead - 1, Lead, Hyst, false), Is.False);
		}

		[Test]
		public void DeadBandKeepsThePreviousClass()
		{
			// The pre-hysteresis hard cut flipped members between Stop and AttackMove every
			// squad tick when their lead hovered at the threshold (user-reported stutter).
			Assert.That(SquadMicroEvalCA.ClassifyHolding(Lead + 1, Lead, Hyst, false), Is.False);
			Assert.That(SquadMicroEvalCA.ClassifyHolding(Lead + Hyst, Lead, Hyst, false), Is.False);
			Assert.That(SquadMicroEvalCA.ClassifyHolding(Lead + 1, Lead, Hyst, true), Is.True);
			Assert.That(SquadMicroEvalCA.ClassifyHolding(Lead + Hyst, Lead, Hyst, true), Is.True);
		}

		[Test]
		public void ZeroHysteresisRestoresTheOldHardCut()
		{
			Assert.That(SquadMicroEvalCA.ClassifyHolding(Lead + 1, Lead, 0, false), Is.True);
			Assert.That(SquadMicroEvalCA.ClassifyHolding(Lead + 1, Lead, 0, true), Is.True);
			Assert.That(SquadMicroEvalCA.ClassifyHolding(Lead, Lead, 0, true), Is.False);
		}
	}
}
