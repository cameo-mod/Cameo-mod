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

using System;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Test;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class GarrisonPredictionTest
	{
		[Test]
		public void PredictorSumsOnlyPassengersEligibleAtTheQueriedPosition()
		{
			var f = new AttackGarrisonedTest.Fixture(); f.Enter(f.Rifle); f.Enter(f.Rocket);
			f.Infantry.Trait<IPositionable>().SetCenterPosition(f.Infantry, new WPos(6000, 0, 0));
			var own = BotUnitProfiles.Get(f.Host, f.Own);
			var infantry = BotUnitProfiles.Get(f.Infantry, f.Own);
			Assert.That(own.DamagePerTickAgainst(infantry), Is.EqualTo(20.0 / 3).Within(0.00001));
			Assert.That(own.MaximumRangeAgainst(infantry), Is.EqualTo(new WDist(8192)));
			f.Exit(f.Rocket);
			Assert.That(own.DamagePerTickAgainst(infantry), Is.Zero, "Snapshot must not retain an exited weapon as eligible");
			Assert.That(own.MaximumRangeAgainst(infantry), Is.EqualTo(WDist.Zero));
		}
		[Test]
		public void EnemyCargoCompositionIsPrivateAndHiddenTargetsHaveNoPortPrediction()
		{
			var f = new AttackGarrisonedTest.Fixture(); f.Enter(f.Rifle); f.Enter(f.Rocket);
			var enemyView = BotUnitProfiles.Get(f.Host, f.Enemy);
			Assert.That(enemyView.Weapons.Any(w => w.PortArmament != null), Is.False);
			var own = BotUnitProfiles.Get(f.Host, f.Own);
			var observed = BotUnitProfiles.Get(f.Infantry, f.Own);
			f.Infantry.Trait<IDefaultVisibility>().GetType().GetField("Visible").SetValue(f.Infantry.Trait<IDefaultVisibility>(), false);
			Assert.That(own.DamagePerTickAgainst(observed), Is.Zero);
			Assert.That(BotUnitProfiles.Get(f.Infantry, f.Own).ObservedActor, Is.Null);
			var unknown = BotUnitProfiles.Get(f.World.Map.Rules, f.Infantry.Info);
			Assert.That(own.DamagePerTickAgainst(unknown), Is.Zero);
		}
		[Test]
		public void ConeAndDisableInvalidateThePredictorWithoutMutatingStationState()
		{
			var f = new AttackGarrisonedTest.Fixture([WVec.Zero, WVec.Zero], [new WAngle(64), new WAngle(64)],
				[new WVec(-1, 0, 0).Yaw, new WVec(1, 0, 0).Yaw]);
			f.Enter(f.Rifle); f.Enter(f.Rocket);
			var own = BotUnitProfiles.Get(f.Host, f.Own); var target = BotUnitProfiles.Get(f.Infantry, f.Own);
			var hash = f.Attack.StationHash; var rng = f.World.SharedRandom.Last;
			Assert.That(own.DamagePerTickAgainst(target), Is.EqualTo(20.0 / 3).Within(0.00001));
			Assert.That(f.Attack.StationHash, Is.EqualTo(hash)); Assert.That(f.World.SharedRandom.Last, Is.EqualTo(rng));
			// Capture via the production notification path is covered by the engine fixture;
			// disabled host capability must also invalidate a previously built profile.
			f.Host.GrantCondition("disabled");
			Assert.That(own.DamagePerTickAgainst(target), Is.Zero);
		}
		[Test]
		public void LegacyModAliasCreatesTheCanonicalCommonRuntime()
		{
			#pragma warning disable CS0618 // Verify the one-release compatibility alias.
			var f = new AttackGarrisonedTest.Fixture(schema: new AttackGarrisonedSPInfo());
			#pragma warning restore CS0618
			Assert.That(f.Host.Trait<AttackGarrisoned>().GetType(), Is.EqualTo(typeof(AttackGarrisoned)));
		}
		[Test]
		public void LegacyModAliasCarriesObsoleteGuidance()
		{
			#pragma warning disable CS0618 // Verify the one-release compatibility alias.
			var alias = typeof(AttackGarrisonedSPInfo);
			#pragma warning restore CS0618
			var obsolete = (ObsoleteAttribute)Attribute.GetCustomAttribute(alias, typeof(ObsoleteAttribute));
			Assert.That(obsolete.Message, Is.EqualTo("Use AttackGarrisoned instead."));
		}
	}
}
