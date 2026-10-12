from pathlib import Path
root=Path('C:/cameo-wt/codex-multi-turret-engine')
p=root/'OpenRA.Mods.Common/Traits/Attack/AttackGarrisoned.cs'
s=p.read_text()
s=s.replace('station.Arms = [];\n\t\t\tstation.AutoTarget', 'station.Arms = [];\n\t\t\tstation.WeaponStations = [];\n\t\t\tstation.AutoTarget')
start=s.index('\t\t\t\tforeach (var arm in arms)\n\t\t\t\t\tif (arm.CheckFire')
end=s.index('\n\t\t\t}\n\t\t\tbase.Tick',start)
s=s[:start]+'''\t\t\t\tforeach (var arm in arms)
					FireArm(station, arm, target, yaw);'''+s[end:]
pos=s.index('\t\tvoid FireWeaponStations')
s=s[:pos]+'''		void FireArm(FirePortStation station, Armament arm, Target target, WAngle yaw)
		{
			if (!arm.CheckFire(station.Occupant, station.Facing, target) || arm.Info.MuzzleSequence == null || station.Render == null)
				return;
			var animation = new Animation(host.World, station.Render.GetImage(station.Occupant), () => yaw);
			var muzzle = new AnimationWithOffset(animation, () => Offset(station), () => false,
				p => RenderUtils.ZOffsetFromCenter(host, p, 1024));
			station.Muzzles.Add(muzzle);
			animation.PlayThen(arm.Info.MuzzleSequence, () => station.Muzzles.Remove(muzzle));
		}
'''+s[pos:]
s=s.replace('if ((station.Persistent || station.Retaliating) && EligibleGroup(station.OpportunityTarget, force))','if ((station.Persistent || station.Retaliating) && MayRetainOpportunity(station, station.OpportunityTarget)\n\t\t\t\t\t\t&& EligibleGroup(station.OpportunityTarget, force))')
s=s.replace('if (--group.ScanTicks <= 0 || !EligibleGroup(group.Target, false))','var hostAuto = host.TraitOrDefault<AutoTarget>();\n\t\t\t\t\t\tvar mayScan = Info.OpportunityFire && hostAuto?.IsTraitDisabled != true\n\t\t\t\t\t\t\t&& !(hostAuto?.Stance < UnitStance.Defend) && station.AutoTarget != null\n\t\t\t\t\t\t\t&& !station.AutoTarget.IsTraitDisabled && station.AutoTarget.Stance >= UnitStance.Defend;\n\t\t\t\t\t\tif (!mayScan)\n\t\t\t\t\t\t\tgroup.Target = Target.Invalid;\n\t\t\t\t\t\telse if (--group.ScanTicks <= 0 || !EligibleGroup(group.Target, false))')
s=s.replace('\n\t\t\t\t\t\t\tvar hostAuto = host.TraitOrDefault<AutoTarget>();\n\t\t\t\t\t\t\tgroup.Target = Info.OpportunityFire && hostAuto?.IsTraitDisabled != true && hostAuto?.Stance >= UnitStance.Defend\n\t\t\t\t\t\t\t\t? station.AutoTarget?.ScanForTarget', '\n\t\t\t\t\t\t\tgroup.Target = station.AutoTarget.ScanForTarget')
s=s.replace('?? Target.Invalid\n\t\t\t\t\t\t\t\t: Target.Invalid;', ';')
s=s.replace('arm.CheckFire(occupant, station.Facing, target);','FireArm(station, arm, target, group.Turret?.WorldOrientation.Yaw ?? (target.CenterPosition - position).Yaw);')
p.write_text(s)
p=root/'OpenRA.Test/OpenRA.Mods.Common/AttackGarrisonedTest.cs';s=p.read_text()
s=s.replace('new RecorderInfo(), Info<AutoTargetInfo>(("DynamicWeaponPriority", dynamicPriority)),','Info<PassengerInfo>(("Weight", 1)), new OpenRA.Mods.AS.Traits.GarrisonerInfo(),\n\t\t\t\t\tnew RecorderInfo(), Info<AutoTargetInfo>(("DynamicWeaponPriority", dynamicPriority)),')
s=s.replace('                if (dynamicPriority)', '\t\t\t\tif (dynamicPriority)')
pos=s.index('\t\t[Test]\n\t\tpublic void DynamicVersusOverrides')
s=s[:pos]+'''		[TestCase(false)]
		[TestCase(true)]
		public void PassengerTurretsFireIndependentlyAndRespectStanceImmediately(bool cargo)
		{
			var f = new Fixture(dynamicPriority: true, cargo: cargo);
			var multi = f.Create("multi", f.Own, WPos.Zero);
			f.Enter(multi);
			f.Tick();
			Assert.That(f.Attack.Stations[0].WeaponStations.Select(s => s.Target.Actor), Is.EqualTo(new[] { f.Tank, f.Infantry }));
			Assert.That(f.Recorder.Events.Count, Is.EqualTo(2));
			multi.Trait<AutoTarget>().SetStance(multi, UnitStance.HoldFire);
			foreach (var arm in multi.TraitsImplementing<Armament>())
				for (var i = 0; i < 5; i++) ((ITick)arm).Tick(multi);
			f.Tick();
			Assert.That(f.Recorder.Events.Count, Is.EqualTo(2));
			Assert.That(f.Attack.Stations[0].WeaponStations.Select(s => s.Target.Type), Is.All.EqualTo(TargetType.Invalid));
			f.Exit(multi);
			Assert.That(f.Attack.Stations[0].WeaponStations, Is.Empty);
		}
'''+s[pos:];p.write_text(s)
