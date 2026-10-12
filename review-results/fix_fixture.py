from pathlib import Path
p=Path('C:/cameo-wt/codex-multi-turret-engine/OpenRA.Test/OpenRA.Mods.Common/AttackGarrisonedTest.cs')
s=p.read_text(encoding='utf-8-sig')
s=s.replace('if (dynamicPriority)\n\t\t\t\t{','if (dynamicPriority)\n\t\t\t\t{\n\t\t\t\t\tforeach (var weapon in weapons.Values) Set(weapon.Warheads[0], "ValidTargets", new BitSet<TargetableType>("Infantry", "Vehicle"));',1)
s=s.replace('f.Tick();\n\t\t\tAssert.That(f.Attack.Stations.Select(s => s.OpportunityTarget.Actor), Is.EqualTo(new[] { f.Tank, f.Infantry }));','Assert.That(f.Rifle.Trait<AutoTarget>().Info.DynamicWeaponPriority, Is.True);\n\t\t\tAssert.That(WeaponTargetScore.Against(f.Rifle.Trait<Armament>(), f.Rifle, f.Tank).CompareTo(new WeaponTargetScore(120, 1)), Is.Zero);\n\t\t\tf.Tick();\n\t\t\tAssert.That(f.Attack.Stations.Select(s => s.OpportunityTarget.Actor), Is.EqualTo(new[] { f.Tank, f.Infantry }));',1)
p.write_text(s.rstrip()+'\n',encoding='utf-8')
