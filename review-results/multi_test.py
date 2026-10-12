from pathlib import Path
p=Path('C:/cameo-wt/codex-multi-turret-engine/OpenRA.Test/OpenRA.Mods.Common/AttackGarrisonedTest.cs')
s=p.read_text(encoding='utf-8')
anchor='\t\t\t\tvar weapons = new Dictionary<string, OpenRA.GameRules.WeaponInfo>'
insert='''				actors["multi"] = new ActorInfo("multi", new SpaceInfo(), body,
					Info<AttackMultiTurretedInfo>(("Turrets", ImmutableArray.Create("primary", "secondary"))),
					Info<TurretedInfo>(("Turret", "primary")), Info<TurretedInfo>(("Turret", "secondary")),
					Info<ArmamentInfo>(("Weapon", "rifle"), ("Name", "primary"), ("Turret", "primary")),
					Info<ArmamentInfo>(("Weapon", "rocket"), ("Name", "secondary"), ("Turret", "secondary")),
					new RecorderInfo(), Info<AutoTargetInfo>(("DynamicWeaponPriority", dynamicPriority)),
					Info<AutoTargetPriorityInfo>(("ValidTargets", new BitSet<TargetableType>("Infantry", "Vehicle"))));
'''
assert anchor in s
s=s.replace(anchor,insert+anchor,1)
anchor='\t\t[Test]\n\t\tpublic void DynamicVersusOverridesClassWeightsAndDistanceForRealPassengers()'
insert='''		[Test]
		public void MultiTurretsAimAndFireAtDifferentTargetsInOneTick()
		{
			var f = new Fixture(dynamicPriority: true);
			var multi = f.Create("multi", f.Own, WPos.Zero);
			var attack = multi.Trait<AttackMultiTurreted>();
			var rng = f.World.SharedRandom.Last;
			((ITick)attack).Tick(multi);
			Assert.That(attack.TurretTargets.Select(t => t.Actor), Is.EqualTo(new[] { f.Tank, f.Infantry }));
			Assert.That(multi.Trait<Recorder>().Events.Count, Is.EqualTo(2));
			Assert.That(f.World.SharedRandom.Last, Is.EqualTo(rng));
		}
'''
assert anchor in s
s=s.replace(anchor,insert+anchor,1)
p.write_text(s,encoding='utf-8')
