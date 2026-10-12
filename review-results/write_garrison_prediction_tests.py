from pathlib import Path
p=Path('OpenRA.Mods.Cameo.Test/GarrisonPredictionTest.cs')
license=Path('OpenRA.Mods.CA/Traits/BotModules/BotModuleLogic/BotCombatPredictor.cs').read_text(encoding='utf-8-sig').split('using System;')[0]
p.write_text(license+'''using System.Linq;
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
			var unknown = BotUnitProfiles.Get(f.World.Map.Rules, f.Infantry.Info);
			Assert.That(own.DamagePerTickAgainst(unknown), Is.Zero);
		}
		[Test]
		public void ConeAndCaptureInvalidateThePredictorWithoutMutatingStationState()
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
			var f = new AttackGarrisonedTest.Fixture(schema: new AttackGarrisonedSPInfo());
			Assert.That(f.Host.Trait<AttackGarrisoned>().GetType(), Is.EqualTo(typeof(AttackGarrisoned)));
		}
	}
}
''',encoding='utf-8',newline='\n')
