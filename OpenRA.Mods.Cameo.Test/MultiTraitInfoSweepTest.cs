#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// Regression pins for the multi-TraitInfo sweep: single-instance lookups
	/// (TraitInfo&lt;T&gt; / Trait&lt;T&gt; / *OrDefault) throw "multiple instances"
	/// whenever an actor carries two assignable infos — @-variant twins or a
	/// concrete type plus its subclass. Converted sites must tolerate the
	/// multiset and aggregate deterministically.
	/// </summary>
	[TestFixture]
	public class MultiTraitInfoSweepTest
	{
		static PowerInfo Power(int amount)
		{
			var info = new PowerInfo();
			typeof(PowerInfo).GetField("Amount").SetValue(info, amount);
			return info;
		}

		static RangedGpsProviderInfo GpsProvider(int cells)
		{
			var info = new RangedGpsProviderInfo();
			typeof(RangedGpsProviderInfo).GetField("Range").SetValue(info, WDist.FromCells(cells));
			return info;
		}

		[Test]
		public void FransClassifierToleratesMultiPowerActors()
		{
			// The EMBER crash class: a bio-reactor family ships Power@main +
			// Power@overcharge — TraitInfoOrDefault<PowerInfo> threw on it.
			var plant = new ActorInfo("plant", new BuildingInfo(), Power(90), Power(40));
			Assert.DoesNotThrow(() => FransActorClass.IsPowerPlant(plant));
			Assert.That(FransActorClass.IsPowerPlant(plant), Is.True);

			// Every instance negative = a sink, not a plant.
			var sink = new ActorInfo("sink", new BuildingInfo(), Power(-30), Power(-10));
			Assert.That(FransActorClass.IsPowerPlant(sink), Is.False);
		}

		[Test]
		public void RadarRangeTakesTheStrongestProvider()
		{
			// ra1_allies_radardome / yuri_psychicsensor carry two providers —
			// the effective dot radius is the max, and single-instance lookups
			// must not throw while asking for it.
			var dual = new ActorInfo("dome", new BuildingInfo(), GpsProvider(12), GpsProvider(8));
			Assert.DoesNotThrow(() => BaseFrontBackPlannerBotModule.RadarRangeCells(dual));
			Assert.That(BaseFrontBackPlannerBotModule.RadarRangeCells(dual), Is.EqualTo(12));
			Assert.That(BaseFrontBackPlannerBotModule.IsRadarProvider(dual), Is.True);

			var single = new ActorInfo("solo", new BuildingInfo(), GpsProvider(6));
			Assert.That(BaseFrontBackPlannerBotModule.RadarRangeCells(single), Is.EqualTo(6));

			var none = new ActorInfo("none", new BuildingInfo());
			Assert.That(BaseFrontBackPlannerBotModule.RadarRangeCells(none), Is.EqualTo(0));
			Assert.That(BaseFrontBackPlannerBotModule.IsRadarProvider(none), Is.False);
		}
	}
}
