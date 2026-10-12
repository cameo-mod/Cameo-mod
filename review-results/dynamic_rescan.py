from pathlib import Path
r=Path('C:/cameo-wt/codex-multi-turret-engine')
p=r/'OpenRA.Mods.Common/Traits/AutoTarget.cs';s=p.read_text().replace('if (allowMove || targetFrozenActors || ab?.Info.TargetFrozenActors == true)','if (!Info.DynamicWeaponPriority && (allowMove || targetFrozenActors || ab?.Info.TargetFrozenActors == true))')
pos=s.index('\t\tpublic void ScanAndAttack')
s=s[:pos]+'''		/// <summary>Visible, currently reachable weapon targets, without overrides or RNG.</summary>
		public Target ScanForInRangeTarget(Actor self, AttackBase attack)
		{
			if (IsTraitDisabled || Stance < UnitStance.Defend || attack.IsTraitDisabled || attack.IsTraitPaused)
				return Target.Invalid;
			var stances = attack.UnforcedAttackTargetStances();
			return stances == PlayerRelationship.None ? Target.Invalid : ChooseTarget(self, attack, stances,
				Info.ScanRadius > 0 ? WDist.FromCells(Info.ScanRadius) : attack.GetMaximumRange(), false, true);
		}

'''+s[pos:];p.write_text(s)
p=r/'OpenRA.Mods.Common/Traits/Attack/AttackFollow.cs';s=p.read_text();s=s.replace('bool opportunityTargetIsPersistentTarget;','bool opportunityTargetIsPersistentTarget;\n\t\tint dynamicScanTicks;')
s=s.replace('protected virtual Target ScanOpportunityTarget(Actor self, AutoTarget scanner) => scanner.ScanForTarget(self, false, false);','protected virtual Target ScanOpportunityTarget(Actor self, AutoTarget scanner) => scanner.Info.DynamicWeaponPriority\n\t\t\t? scanner.ScanForInRangeTarget(self, this) : scanner.ScanForTarget(self, false, false);')
s=s.replace('if (!IsAiming && Info.OpportunityFire && autoTarget != null &&','if ((!IsAiming || autoTarget?.Info.DynamicWeaponPriority == true && !opportunityTargetIsPersistentTarget && --dynamicScanTicks <= 0)\n\t\t\t\t\t&& Info.OpportunityFire && autoTarget != null &&')
s=s.replace('OpportunityTarget = ScanOpportunityTarget(self, autoTarget);','dynamicScanTicks = 5;\n\t\t\t\t\tOpportunityTarget = ScanOpportunityTarget(self, autoTarget);')
s=s.replace('bool hasTicked;','bool hasTicked;\n\t\t\tint dynamicScanTicks;')
needle='''			public override bool Tick(Actor self)
			{
				if (!IsCanceling && !HasArmamentsFor(target))'''
repl='''			public override bool Tick(Actor self)
			{
				// Automatic demolition pursuit must yield to a reachable ranged engagement.
				// Explicit player orders and force-fire keep their requested target.
				var auto = self.TraitOrDefault<AutoTarget>();
				if (!IsCanceling && !forceAttack && source != AttackSource.Default && auto?.Info.DynamicWeaponPriority == true
					&& --dynamicScanTicks <= 0)
				{
					dynamicScanTicks = 5;
					foreach (var attack in attacks)
					{
						var preferred = auto.ScanForInRangeTarget(self, attack);
						if (preferred.Type == TargetType.Invalid || preferred == target)
							continue;
						ChildActivity?.Cancel(self);
						target = preferred;
						foreach (var a in attacks)
							a.SetRequestedTarget(target, false);
						break;
					}
				}
				if (!IsCanceling && !HasArmamentsFor(target))'''
assert needle in s;s=s.replace(needle,repl);p.write_text(s)
p=r/'OpenRA.Mods.Common/Traits/Attack/AttackGarrisoned.cs';s=p.read_text();s=s.replace('if (!MayRetainOpportunity(station, target) || !Eligible(station, target, force, true).Any())','var rescan = station.AutoTarget?.Info.DynamicWeaponPriority == true && !station.Persistent && !station.Retaliating\n\t\t\t\t\t\t&& --station.ScanTicks <= 0;\n\t\t\t\t\tif (rescan || !MayRetainOpportunity(station, target) || !Eligible(station, target, force, true).Any())');s=s.replace('if (--station.ScanTicks <= 0 && Info.OpportunityFire)','if ((rescan || --station.ScanTicks <= 0) && Info.OpportunityFire)');p.write_text(s)
p=r/'OpenRA.Test/OpenRA.Mods.Common/AttackGarrisonedTest.cs';s=p.read_text();pos=s.index('\t\t[Test]\n\t\tpublic void DynamicVersusOverrides');s=s[:pos]+'''		[Test]
		public void ReadySniperEngagementWinsBeforeStrongerOutOfRangeDemolitionTarget()
		{
			var f = new Fixture(dynamicPriority: true);
			var multi = f.Create("multi", f.Own, WPos.Zero);
			var cannon = multi.TraitsImplementing<Armament>().First();
			Set(cannon.Weapon, "Range", new WDist(1024));
			var chosen = multi.Trait<AutoTarget>().ScanForTarget(multi, true, true, true);
			Assert.That(chosen.Actor, Is.SameAs(f.Infantry));
		}
'''+s[pos:];p.write_text(s)
