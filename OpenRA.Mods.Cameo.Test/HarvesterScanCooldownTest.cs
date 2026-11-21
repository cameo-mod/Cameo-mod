#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is made available under the GNU General Public License
 * version 3 or any later version. See COPYING for details.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class HarvesterScanCooldownTest
	{
		[Test]
		public void InitialDelayReloadAndResourceEmptyFreezeKeepTheirBoundaries()
		{
			var cooldown = HarvesterScanCooldown.StartInitial(100, 50);
			Assert.That(HarvesterScanCooldown.IsDue(cooldown, 149), Is.False);
			Assert.That(HarvesterScanCooldown.IsDue(cooldown, 150), Is.True);
			var throttled = HarvesterScanCooldown.StartInitial(100, 50);
			Assert.That(HarvesterScanCooldown.IsDue(throttled, 148), Is.False);
			Assert.That(HarvesterScanCooldown.IsDue(throttled, 152), Is.True,
				"An N=8 visit fires on the first sampled tick at or after due.");
			HarvesterScanCooldown.Reload(ref throttled, 152, 50);
			Assert.That(HarvesterScanCooldown.IsDue(throttled, 200), Is.False);
			Assert.That(HarvesterScanCooldown.IsDue(throttled, 208), Is.True,
				"A late visit reloads from the visit tick instead of catching up.");

			HarvesterScanCooldown.Reload(ref cooldown, 180, 50);
			Assert.That(HarvesterScanCooldown.IsDue(cooldown, 229), Is.False);
			Assert.That(HarvesterScanCooldown.IsDue(cooldown, 230), Is.True,
				"A late eligible visit reloads from its actual tick, without catch-up.");

			cooldown = HarvesterScanCooldown.StartInitial(0, 100);
			HarvesterScanCooldown.Freeze(ref cooldown, 30);
			Assert.That(HarvesterScanCooldown.IsDue(cooldown, 129), Is.False);
			Assert.That(HarvesterScanCooldown.IsDue(cooldown, 130), Is.True,
				"An empty resource layer pauses the remaining delay.");

			var zeroInitialDelay = HarvesterScanCooldown.StartInitial(0, 0);
			Assert.That(HarvesterScanCooldown.IsDue(zeroInitialDelay, 0), Is.False);
			Assert.That(HarvesterScanCooldown.IsDue(zeroInitialDelay, 1), Is.True);

			cooldown = HarvesterScanCooldown.StartInitial(0, 375);
			Assert.That(HarvesterScanCooldown.IsDue(cooldown, 370), Is.False,
				"The old interval had five ticks remaining at disable.");
			cooldown = HarvesterScanCooldown.StartInitial(500, 375);
			Assert.That(HarvesterScanCooldown.IsDue(cooldown, 874), Is.False);
			Assert.That(HarvesterScanCooldown.IsDue(cooldown, 875), Is.True,
				"TraitEnabled resets all three scan timers from the re-enable tick.");
		}
	}
}
