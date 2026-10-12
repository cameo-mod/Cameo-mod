from pathlib import Path
p=Path('C:/cameo-wt/codex-multi-turret/OpenRA.Mods.Cameo/Warheads/AreaDamageWarhead.cs');s=p.read_text(encoding='utf-8-sig');pos=s.index('\t\tprotected override int DamageVersus');s=s[:pos]+'''		// Match the effective Heaviness table, class-armor combination and plating layer.
		public override WeaponTargetScore TargetingVersus(Actor victim, HitShape shape) =>
			new(VersusFrom(effectiveVersus, victim, shape), 1);

'''+s[pos:];p.write_text(s,encoding='utf-8')
