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
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// AN inmatch adapt (AI_ARCHITECTURE 12.32): the pure bounded rule — sign, scale, clamp — and the
	// bar arithmetic the squad manager applies on top of it. No World needed.
	[TestFixture]
	public sealed class InMatchAdaptTest
	{
		[Test]
		public void NeutralFormGivesNoBias()
		{
			Assert.That(InMatchAdaptMath.RetreatRatioDelta(0, 15, 20), Is.EqualTo(0));
			Assert.That(InMatchAdaptMath.RetreatRatioDelta(500, 0, 20), Is.EqualTo(0), "gain off = off");
			Assert.That(InMatchAdaptMath.RetreatRatioDelta(-500, 15, 0), Is.EqualTo(0), "zero bound = off");
		}

		[Test]
		public void LosingFormTightensTheBar()
		{
			// -1000 milli running ≈ one clearly lost engagement → +15 pct-points on RetreatRatioPct:
			// squads bail earlier and only commit at higher advantage.
			Assert.That(InMatchAdaptMath.RetreatRatioDelta(-1000, 15, 20), Is.EqualTo(15));
			Assert.That(InMatchAdaptMath.RetreatRatioDelta(-500, 15, 20), Is.EqualTo(7));
		}

		[Test]
		public void WinningFormLoosensTheBar()
		{
			Assert.That(InMatchAdaptMath.RetreatRatioDelta(1000, 15, 20), Is.EqualTo(-15));
			Assert.That(InMatchAdaptMath.RetreatRatioDelta(500, 15, 20), Is.EqualTo(-7));
		}

		[Test]
		public void HardBoundBothDirections()
		{
			Assert.That(InMatchAdaptMath.RetreatRatioDelta(-5000, 15, 20), Is.EqualTo(20));
			Assert.That(InMatchAdaptMath.RetreatRatioDelta(5000, 15, 20), Is.EqualTo(-20));
			Assert.That(InMatchAdaptMath.RetreatRatioDelta(-200000, 15, 20), Is.EqualTo(20), "long match, no overflow");
			Assert.That(InMatchAdaptMath.RetreatRatioDelta(-1000, 15, -20), Is.EqualTo(15), "negative bound config still bounds");
		}

		[Test]
		public void BoundedBiasKeepsBarSane()
		{
			// The consumer clamps at zero; with the default bar (50) and bound (20) the effective bar
			// stays inside [30, 70] — tighten never pushes it past a full-health hold, loosen never to suicide.
			var bar = System.Math.Max(0, 50 + InMatchAdaptMath.RetreatRatioDelta(-5000, 15, 20));
			Assert.That(bar, Is.EqualTo(70));
			bar = System.Math.Max(0, 50 + InMatchAdaptMath.RetreatRatioDelta(5000, 15, 20));
			Assert.That(bar, Is.EqualTo(30));
		}
	}
}
