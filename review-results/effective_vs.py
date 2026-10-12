from pathlib import Path
r=Path('C:/cameo-wt/codex-multi-turret-engine')
p=r/'OpenRA.Mods.Common/Warheads/DamageWarhead.cs';s=p.read_text();pos=s.index('\t\tprotected virtual int DamageVersus');s=s[:pos]+'''		/// <summary>Armor effectiveness for automatic targeting; custom armor policies override this.
		/// No damage, impact modifiers or hidden actor state is applied by this read-only query.</summary>
		public virtual WeaponTargetScore TargetingVersus(Actor victim, HitShape shape) =>
			WeaponTargetScore.ArmorMultiplier(victim.TraitsImplementing<Armor>()
				.Where(a => !a.IsTraitDisabled && a.Info.Type != null && Versus.ContainsKey(a.Info.Type) &&
					(shape.Info.ArmorTypes.IsEmpty || shape.Info.ArmorTypes.Contains(a.Info.Type)))
				.Select(a => Versus[a.Info.Type]));

'''+s[pos:];p.write_text(s)
p=r/'OpenRA.Mods.Common/Traits/WeaponTargetScore.cs';s=p.read_text();start=s.index('\t\t\t\tvar multiplier = ArmorMultiplier');end=s.index('\n\t\t\t\ttotal =',start);s=s[:start]+'\t\t\t\tvar multiplier = warhead.TargetingVersus(victim, shape);'+s[end:];p.write_text(s)
p=Path('C:/cameo-wt/codex-multi-turret/OpenRA.Mods.Cameo/Warheads/AreaDamageWarhead.cs');s=p.read_text();pos=s.index('\t\tprotected override int DamageVersus');s=s[:pos]+'''		// Use the same effective Heaviness table, class-armor combination and plating layer
		// as actual damage. Reading the authored Versus rows alone would rank the wrong targets.
		public override WeaponTargetScore TargetingVersus(Actor victim, HitShape shape) =>
			new(VersusFrom(effectiveVersus, victim, shape), 1);

'''+s[pos:];p.write_text(s)
