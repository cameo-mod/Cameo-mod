from pathlib import Path
p=Path('C:/cameo-wt/codex-multi-turret-engine/OpenRA.Mods.Common/Traits/AutoTarget.cs')
s=p.read_text(encoding='utf-8')
s=s.replace('public readonly bool AllowMovement = true;', '''public readonly bool AllowMovement = true;

		[Desc("Rank visible weapon targets by Versus, then distance and ActorID, ignoring class priority weights.")]
		public readonly bool DynamicWeaponPriority = false;''',1)
s=s.replace('var chosenTargetRange = 0;', '''var chosenTargetRange = 0;
			var chosenWeaponScore = WeaponTargetScore.Neutral;
			var chosenDistanceSquared = long.MaxValue;
			var chosenActorId = uint.MaxValue;
			var chosenInRange = false;''',1)
s=s.replace('if (ati.Priority < chosenTargetPriority)', 'if (!Info.DynamicWeaponPriority && ati.Priority < chosenTargetPriority)',1)
s=s.replace('else if (target.Type == TargetType.FrozenActor)\n\t\t\t\t{', '''else if (target.Type == TargetType.FrozenActor)
				{
					// Dynamic effectiveness reads only currently visible armor, never frozen live backing state.
					if (Info.DynamicWeaponPriority)
						continue;''',1)
anchor='\t\t\t\t// Evaluate whether we want to target this actor'
insert='''				if (Info.DynamicWeaponPriority)
				{
					var bestScore = new WeaponTargetScore(0, 1);
					var found = false;
					var inRange = false;
					foreach (var arm in armaments)
					{
						if (arm.IsTraitDisabled || arm.IsTraitPaused ||
							!arm.Info.TargetRelationships.HasRelationship(self.Owner.RelationshipWith(owner)) ||
							!arm.Weapon.IsValidAgainst(target, self.World, arm.Actor))
							continue;
						var readyRange = target.IsInRange(self.CenterPosition, arm.MaxRange()) &&
							(arm.Weapon.MinRange == WDist.Zero || !target.IsInRange(self.CenterPosition, arm.Weapon.MinRange));
						if (!allowMove && stationArmaments == null && !readyRange)
							continue;
						// Shoot a currently reachable ranged target before approaching a demolition target.
						if (inRange && !readyRange)
							continue;
						var score = WeaponTargetScore.Against(arm, arm.Actor, target.Actor);
						if (!found || readyRange && !inRange || score.CompareTo(bestScore) > 0)
							bestScore = score;
						found = true;
						inRange |= readyRange;
					}
					if (!found)
						continue;
					var delta = target.CenterPosition - self.CenterPosition;
					var distanceSquared = (long)delta.X * delta.X + (long)delta.Y * delta.Y + (long)delta.Z * delta.Z;
					var id = target.Actor.ActorID;
					if (chosenTarget.Type == TargetType.Invalid || inRange && !chosenInRange ||
						inRange == chosenInRange && WeaponTargetScore.Prefer(bestScore, distanceSquared, id,
							chosenWeaponScore, chosenDistanceSquared, chosenActorId))
					{
						chosenTarget = target;
						chosenWeaponScore = bestScore;
						chosenDistanceSquared = distanceSquared;
						chosenActorId = id;
						chosenInRange = inRange;
					}
					continue;
				}

'''
assert anchor in s
s=s.replace(anchor,insert+anchor,1)
p.write_text(s,encoding='utf-8')
print('Dynamic scanner wired, legacy default preserved')
