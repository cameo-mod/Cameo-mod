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

using System.Reflection;
using NUnit.Framework;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Test.TestFixtures;
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

		// A live trait instance without a world: constructor never ran, so Info and
		// IsTraitDisabled are pinned directly — exactly what the enabled-filter reads.
		static RangedGpsProvider LiveProvider(int cells, bool disabled)
		{
			var trait = Uninitialized.Of<RangedGpsProvider>();
			var cond = typeof(ConditionalTrait<RangedGpsProviderInfo>);
			cond.GetField("Info").SetValue(trait, GpsProvider(cells));
			cond.GetField("<IsTraitDisabled>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
				.SetValue(trait, disabled);
			return trait;
		}

		static Carryable CarryableWith(Actor carrier)
		{
			var carryable = Uninitialized.Of<Carryable>();
			typeof(Carryable).GetField("<Carrier>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
				.SetValue(carryable, carrier);
			return carryable;
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
			// Prospective placement is deliberately OPTIMISTIC: info-level Max counts
			// every declared provider variant, including the condition-gated twin that
			// spawns disabled (the dome/sensor carry mutually exclusive 20000/30000
			// ranges). Live actors go through EnabledRadarRangeCells — pinned below.
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

		[Test]
		public void LiveRadarCountsOnlyEnabledProviders()
		{
			// The review-flagged bug: an un-upgraded dome/sensor spawns with the strong
			// variant DISABLED — the live radius is the enabled provider's range, not
			// the info-level Max (~19 cells, not ~29).
			var unUpgraded = new[] { LiveProvider(19, disabled: false), LiveProvider(29, disabled: true) };
			Assert.That(BaseFrontBackPlannerBotModule.EnabledRadarRangeCells(unUpgraded), Is.EqualTo(19));

			var upgraded = new[] { LiveProvider(19, disabled: true), LiveProvider(29, disabled: false) };
			Assert.That(BaseFrontBackPlannerBotModule.EnabledRadarRangeCells(upgraded), Is.EqualTo(29));

			var poweredDown = new[] { LiveProvider(19, disabled: true), LiveProvider(29, disabled: true) };
			Assert.That(BaseFrontBackPlannerBotModule.EnabledRadarRangeCells(poweredDown), Is.EqualTo(0));
		}

		[Test]
		public void CarryableAnyChecksEveryInstance()
		{
			// ordos_pythontank carries AutoCarryable plus Carryable: whichever instance
			// holds the carrier, the existential check must see it — FirstOrDefault can
			// pick the inactive twin.
			var carrier = Uninitialized.Actor();
			var free = CarryableWith(null);
			var carrying = CarryableWith(carrier);
			Assert.That(AttachableTo.AnyCarrierAttached(new[] { free, carrying }), Is.True);
			Assert.That(AttachableTo.AnyCarrierAttached(new[] { carrying, free }), Is.True);
			Assert.That(AttachableTo.AnyCarrierAttached(new[] { free, CarryableWith(null) }), Is.False);
			Assert.That(AttachableTo.AnyCarrierAttached(System.Array.Empty<Carryable>()), Is.False);
		}
	}
}
