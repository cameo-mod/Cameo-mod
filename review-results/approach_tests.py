from pathlib import Path
p=Path('C:/cameo-wt/codex-multi-turret-engine/OpenRA.Test/OpenRA.Mods.Common/AttackGarrisonedTest.cs');s=p.read_text(encoding='utf-8');pos=s.index('\t\t[Test]\n\t\tpublic void ReadySniper')
s=s[:pos]+'''		[Test]
		public void DynamicPassengerRescansWhenBetterVisibleTargetArrives()
		{
			var f = new Fixture(dynamicPriority: true);
			f.Tank.Trait<Space>().Visible = false;
			f.Enter(f.Rifle);
			f.Tick();
			Assert.That(f.Attack.Stations[0].OpportunityTarget.Actor, Is.SameAs(f.Infantry));
			f.Tank.Trait<Space>().Visible = true;
			for (var i = 0; i < 6; i++) f.Tick();
			Assert.That(f.Attack.Stations[0].OpportunityTarget.Actor, Is.SameAs(f.Tank));
		}
		[TestCase(AttackSource.AutoTarget, true)]
		[TestCase(AttackSource.AttackMove, true)]
		[TestCase(AttackSource.Default, false)]
		public void AutomaticApproachYieldsToReadyTargetButExplicitOrderDoesNot(AttackSource source, bool retarget)
		{
			var f = new Fixture(dynamicPriority: true);
			var attack = f.Rifle.Trait<AttackFollow>();
			var activity = new AttackFollow.AttackActivity(f.Rifle, source, Target.FromActor(f.Tank), false, false);
			activity.Tick(f.Rifle);
			Assert.That(attack.RequestedTarget.Actor, Is.SameAs(retarget ? f.Tank : f.Tank));
		}
'''+s[pos:]
# Give the rifle a ready infantry target while its stronger tank target requires approach.
s=s.replace('var attack = f.Rifle.Trait<AttackFollow>();\n\t\t\tvar activity', 'f.Tank.Trait<Space>().SetCenterPosition(f.Tank, new WPos(6000, 0, 0));\n\t\t\tvar attack = f.Rifle.Trait<AttackFollow>();\n\t\t\tvar activity')
s=s.replace('Is.SameAs(retarget ? f.Tank : f.Tank)','Is.SameAs(retarget ? f.Infantry : f.Tank)');p.write_text(s,encoding='utf-8')
