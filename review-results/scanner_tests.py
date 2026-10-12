from pathlib import Path
p=Path('C:/cameo-wt/codex-multi-turret-engine/OpenRA.Test/OpenRA.Mods.Common/AttackGarrisonedTest.cs')
s=p.read_text(encoding='utf-8').replace('using System.Collections.Generic;','using System.Collections.Generic;\nusing System.Collections.Frozen;',1).replace('using OpenRA.Mods.Common.Traits;','using OpenRA.Mods.Common.Traits;\nusing OpenRA.Mods.Common.HitShapes;',1)
s=s.replace('int rifleMinRange = 0)','int rifleMinRange = 0, bool dynamicPriority = false)',1)
s=s.replace('new AutoTargetInfo(), Info<AutoTargetPriorityInfo>', 'Info<AutoTargetInfo>(("DynamicWeaponPriority", dynamicPriority)), Info<AutoTargetPriorityInfo>',1)
anchor='\t\t\t\tSet(weapons["rifle"], "MinRange", new WDist(rifleMinRange));'
insert='''				if (dynamicPriority)
				{
					foreach (var (name, armor) in new[] { ("infantry", "Infantry"), ("tank", "Heavy") })
						actors[name] = new ActorInfo(name, new SpaceInfo(), body,
							Info<TargetableInfo>(("TargetTypes", new BitSet<TargetableType>(name == "tank" ? "Vehicle" : "Infantry"))),
							Info<HealthInfo>(("HP", 1000)), Info<ArmorInfo>(("Type", armor)),
							Info<HitShapeInfo>(("Type", new CircleShape(new WDist(100)))));
					Set(weapons["rifle"].Warheads[0], "Versus", new Dictionary<string, int> { ["Infantry"] = 80, ["Heavy"] = 120 }.ToFrozenDictionary());
					Set(weapons["rocket"].Warheads[0], "Versus", new Dictionary<string, int> { ["Infantry"] = 120, ["Heavy"] = 80 }.ToFrozenDictionary());
				}
'''
assert anchor in s
s=s.replace(anchor,insert+anchor,1)
anchor='\t\t[Test]\n\t\tpublic void SelectedArmamentsAndEachPortRangeFilterBeforePriority()'
insert='''		[Test]
		public void DynamicVersusOverridesClassWeightsAndDistanceForRealPassengers()
		{
			var f = new Fixture(dynamicPriority: true); f.Enter(f.Rifle); f.Enter(f.Rocket);
			f.Tick();
			Assert.That(f.Attack.Stations.Select(s => s.OpportunityTarget.Actor), Is.EqualTo(new[] { f.Tank, f.Infantry }));
			Assert.That(f.Recorder.Events.Count, Is.EqualTo(2));
		}
		[Test]
		public void DynamicVersusNeverSelectsHiddenBetterTarget()
		{
			var f = new Fixture(dynamicPriority: true); f.Enter(f.Rifle);
			f.Tank.Trait<Space>().Visible = false;
			f.Tick();
			Assert.That(f.Attack.Stations[0].OpportunityTarget.Actor, Is.SameAs(f.Infantry));
		}
'''
assert anchor in s
s=s.replace(anchor,insert+anchor,1)
p.write_text(s,encoding='utf-8')
print('Added real passenger scanner/firing tests')
