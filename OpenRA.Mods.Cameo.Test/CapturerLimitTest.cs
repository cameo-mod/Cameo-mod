#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// S1 (FINDINGS_2026-10-12 §3): the engineer:combat-infantry ratio cap. The maintainer's bar is
	// "at least half" versus the forensics 30:30 — i.e. <= 15 engineers against 30 armed infantry,
	// "preferably lower". BotLimits@hard writes 49 so the forensic bot actually lands at ~14:30.
	[TestFixture]
	public sealed class CapturerLimitTest
	{
		[Test]
		public void TheHardTierBeatsTheHalvedRatio()
		{
			// 49% of 30 armed infantry -> cap 14 -> at most 14 live engineers: 14:30 < 15:30.
			for (var engineers = 0; engineers < 14; engineers++)
				Assert.That(UnitBuilderBotModuleCA.CapturerCapAdmits(49, engineers, 30, 2), Is.True);
			Assert.That(UnitBuilderBotModuleCA.CapturerCapAdmits(49, 14, 30, 2), Is.False);
			Assert.That(UnitBuilderBotModuleCA.CapturerCapAdmits(49, 20, 30, 2), Is.False);
		}

		[Test]
		public void TheRatioScalesWithTheArmy()
		{
			// A 60-infantry army at 49% -> cap 29 -> up to 29 engineers; a 10-infantry raid -> cap 4.
			Assert.That(UnitBuilderBotModuleCA.CapturerCapAdmits(49, 28, 60, 2), Is.True);
			Assert.That(UnitBuilderBotModuleCA.CapturerCapAdmits(49, 29, 60, 2), Is.False);
			Assert.That(UnitBuilderBotModuleCA.CapturerCapAdmits(49, 3, 10, 2), Is.True);
			Assert.That(UnitBuilderBotModuleCA.CapturerCapAdmits(49, 4, 10, 2), Is.False);
		}

		[Test]
		public void TheFloorKeepsCaptureAliveBeforeAnyInfantry()
		{
			// CapturerMinimumUnits: an opener engineer is never rationed away.
			Assert.That(UnitBuilderBotModuleCA.CapturerCapAdmits(49, 0, 0, 2), Is.True);
			Assert.That(UnitBuilderBotModuleCA.CapturerCapAdmits(49, 1, 0, 2), Is.True);
			Assert.That(UnitBuilderBotModuleCA.CapturerCapAdmits(49, 2, 0, 2), Is.False);
		}

		[Test]
		public void ZeroAndNegativePercentDisableTheCap()
		{
			// Classic (switchesActive false) and a hand-set 0 both preserve the pre-S1 mix.
			Assert.That(UnitBuilderBotModuleCA.CapturerCapAdmits(0, 100, 30, 2), Is.True);
			Assert.That(UnitBuilderBotModuleCA.CapturerCapAdmits(-1, 100, 30, 2), Is.True);
		}

		[Test]
		public void TheEasiestTierStaysLoose()
		{
			// 80% of 30 -> cap 24: easy bots keep their engineer-flavoured waste, just bounded.
			Assert.That(UnitBuilderBotModuleCA.CapturerCapAdmits(80, 23, 30, 2), Is.True);
			Assert.That(UnitBuilderBotModuleCA.CapturerCapAdmits(80, 24, 30, 2), Is.False);
		}
	}
}
