from pathlib import Path
r=Path('C:/cameo-wt/codex-multi-turret-engine')
p=r/'OpenRA.Mods.Common/Traits/AutoTarget.cs'
s=p.read_text(encoding='utf-8')
s=s.replace('bool targetFrozenActors = false, WDist extraRange = default)','bool targetFrozenActors = false, WDist extraRange = default, Func<Armament, Target, bool> armFilter = null)',1)
s=s.replace('stationArmaments, canFire, targetFrozenActors);','stationArmaments, canFire, targetFrozenActors, armFilter);',1)
s=s.replace('Func<Target, bool> canFire = null, bool targetFrozenActors = false)','Func<Target, bool> canFire = null, bool targetFrozenActors = false, Func<Armament, Target, bool> armFilter = null)',1)
s=s.replace('if (arm.IsTraitDisabled || arm.IsTraitPaused ||\n\t\t\t\t\t\t\t!arm.Info.TargetRelationships','if (arm.IsTraitDisabled || arm.IsTraitPaused || armFilter != null && !armFilter(arm, target) ||\n\t\t\t\t\t\t\t!arm.Info.TargetRelationships',1)
p.write_text(s,encoding='utf-8')
p=r/'OpenRA.Mods.Common/Traits/Attack/AttackGarrisoned.cs'
s=p.read_text(encoding='utf-8')
s=s.replace('internal Armament[] Arms = [];','''internal Armament[] Arms = [];
		internal PassengerWeaponStation[] WeaponStations = [];''',1)
anchor='\t/// <summary>Uses AttackBase orders'
insert='''	internal sealed class PassengerWeaponStation
	{
		internal Armament[] Arms;
		internal Turreted Turret;
		internal Target Target = Target.Invalid;
		internal int ScanTicks;
	}

'''
s=s.replace(anchor,insert+anchor,1)
anchor='\t\t\t\t\t\thash = hash * 31 + port.ScanTicks;'
s=s.replace(anchor,anchor+'''
						foreach (var weaponStation in port.WeaponStations)
							hash = (hash * 31 + Sync.HashTarget(weaponStation.Target)) * 31 + weaponStation.ScanTicks;''',1)
anchor='\t\t\tstation.AutoTarget = actor.TraitOrDefault<AutoTarget>();'
s=s.replace(anchor,'''			station.WeaponStations = station.Arms.GroupBy(a => a.Turret)
				.Select(g => new PassengerWeaponStation { Turret = g.Key, Arms = g.ToArray() }).ToArray();
'''+anchor,1)
anchor='\t\t\tstation.ScanTicks = 0;'
s=s.replace(anchor,anchor+'''
			foreach (var weaponStation in station.WeaponStations)
			{
				weaponStation.Target = Target.Invalid;
				weaponStation.ScanTicks = 0;
			}''',1)
s=s.replace('Info.TargetFrozenActors, new WDist(Offset(station).Length))','Info.TargetFrozenActors, new WDist(Offset(station).Length),\n\t\t\t\t(a, t) => Eligible(station, t, false, true).Contains(a))',1)
anchor='\t\t\t\tvar arms = Eligible(station, target, force, true).ToArray();'
insert='''				if (station.WeaponStations.Any(g => g.Turret != null))
				{
					FireWeaponStations(station);
					continue;
				}
'''
assert anchor in s
s=s.replace(anchor,insert+anchor,1)
anchor='\t\tIEnumerable<IRenderable> IRender.Render'
insert='''		void FireWeaponStations(FirePortStation station)
		{
			var occupant = station.Occupant;
			var position = host.CenterPosition + Offset(station);
			station.Position?.SetCenterPosition(occupant, position);
			foreach (var group in station.WeaponStations)
			{
				bool EligibleGroup(Target t, bool force) => Eligible(station, t, force, true).Any(group.Arms.Contains);
				var target = station.RequestedTarget;
				var force = station.RequestedForce;
				if (!EligibleGroup(target, force))
				{
					force = station.Persistent && station.OpportunityForce;
					if ((station.Persistent || station.Retaliating) && EligibleGroup(station.OpportunityTarget, force))
						target = station.OpportunityTarget;
					else
					{
						force = false;
						if (--group.ScanTicks <= 0 || !EligibleGroup(group.Target, false))
						{
							group.ScanTicks = Info.ScanInterval;
							var hostAuto = host.TraitOrDefault<AutoTarget>();
							group.Target = Info.OpportunityFire && hostAuto?.IsTraitDisabled != true && hostAuto?.Stance >= UnitStance.Defend
								? station.AutoTarget?.ScanForTarget(host, group.Arms, t => EligibleGroup(t, false),
									Info.TargetFrozenActors, new WDist(Offset(station).Length),
									(a, t) => Eligible(station, t, false, true).Contains(a)) ?? Target.Invalid
								: Target.Invalid;
						}
						target = group.Target;
					}
				}
				group.Target = target;
				var arms = Eligible(station, target, force, true).Where(group.Arms.Contains).ToArray();
				if (arms.Length == 0)
					continue;
				IsAiming = true;
				if (group.Turret != null)
				{
					if (!group.Turret.FaceTarget(occupant, target))
						continue;
				}
				else if (station.Facing != null)
					station.Facing.Facing = (target.CenterPosition - position).Yaw;
				foreach (var arm in arms)
					arm.CheckFire(occupant, station.Facing, target);
			}
		}

'''
assert anchor in s
s=s.replace(anchor,insert+anchor,1)
p.write_text(s,encoding='utf-8')
print('Passenger turret subgroups wired with per-arm range/cone filtering')
