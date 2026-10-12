from pathlib import Path
p=Path('C:/cameo-wt/codex-multi-turret-engine/OpenRA.Test/OpenRA.Mods.Common/AttackGarrisonedTest.cs');s=p.read_text(encoding='utf-8');pos=s.index('\t\t[Test]\n\t\tpublic void ReadySniper')
s=s[:pos]+'''		sealed class CustomArmorWarhead : TargetDamageWarhead
		{
			public override WeaponTargetScore TargetingVersus(Actor victim, HitShape shape) => new(25, 1);
		}
		[Test]
		public void ScoringUsesCustomWarheadArmorPolicyInsteadOfAuthoredRows()
		{
			var f = new Fixture(dynamicPriority: true);
			var arm = f.Rifle.Trait<Armament>();
			var warhead = Info<CustomArmorWarhead>(("Damage", 10), ("ValidTargets", new BitSet<TargetableType>("Vehicle")));
			Set(arm.Weapon, "Warheads", ImmutableArray.Create<IWarhead>(warhead));
			Assert.That(WeaponTargetScore.Against(arm, f.Rifle, f.Tank).CompareTo(new WeaponTargetScore(25, 1)), Is.Zero);
		}
'''+s[pos:];p.write_text(s,encoding='utf-8')
