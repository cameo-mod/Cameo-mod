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
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// HVCAP-01: BotLimits.HarvesterValueLimit (0 = off). The gate runs after the ScaleTargets harvester target.
	[TestFixture]
	public class HarvesterValueCapTest
	{
		[Test]
		public void OffIsUnchanged()
		{
			Assert.That(HarvesterValueCap.Active(0), Is.False);
			Assert.That(HarvesterValueCap.Active(-5), Is.False);
			Assert.That(HarvesterValueCap.Allows(0, 999999, 500), Is.True);

			// With the value cap off, only the count gate decides: below the limit produces, at the limit refuses.
			Assert.That(HarvesterValueCap.MayProduce(3, 8, 8, 0, 999999, 500), Is.True);
			Assert.That(HarvesterValueCap.MayProduce(8, 8, 8, 0, 0, 0), Is.False);
		}

		[Test]
		public void CapBlocksOvershoot()
		{
			Assert.That(HarvesterValueCap.Allows(1000, 750, 250), Is.True);   // lands exactly on the cap
			Assert.That(HarvesterValueCap.Allows(1000, 751, 250), Is.False); // one credit over
			Assert.That(HarvesterValueCap.MayProduce(2, 8, 8, 1000, 751, 250), Is.False);
			Assert.That(HarvesterValueCap.MayProduce(2, 8, 8, 1000, 750, 250), Is.True);
		}

		[Test]
		public void ScaleTargetsPathIsStillCapped()
		{
			// A provider raises the count limit and lifts MaxHarvesters (as HarvesterBotModuleCA does); the value cap still refuses.
			const int ProviderLimit = 40;
			Assert.That(HarvesterValueCap.MayProduce(5, ProviderLimit, int.MaxValue, 1000, 900, 250), Is.False);
			Assert.That(HarvesterValueCap.MayProduce(5, ProviderLimit, int.MaxValue, 0, 900, 250), Is.True);
		}

		[Test]
		public void SumOfLiveCosts()
		{
			Assert.That(HarvesterValueCap.Sum(new[] { 250, 0, 500 }), Is.EqualTo(750));
			Assert.That(HarvesterValueCap.Sum(Enumerable.Empty<int>()), Is.EqualTo(0));
		}
	}
}
