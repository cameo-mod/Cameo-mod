from pathlib import Path
p=Path('tools/audit/multi_turret_targeting.py');s=p.read_text();pos=s.index('\ndef audit(root)');s=s[:pos]+'''
def audit_removals(actor, rules):
    inherited = {t.key for key, parent in rules.inherits_of(actor)
                 for t in rules.resolve(parent).children_named("AttackTurreted")}
    local = {t.key for t in actor.children_named("AttackTurreted")}
    return [f"{actor.key}: removes absent {trait.key[1:]}"
            for trait in actor.children if trait.key.startswith("-AttackTurreted")
            and trait.key[1:] not in inherited | local]

'''+s[pos:];s=s.replace('errors.extend(audit_actor(actor))','errors.extend(audit_actor(actor))\n        if actor.children_named("AttackMultiTurreted"):\n            errors.extend(audit_removals(rules.actors[name], rules))');p.write_text(s)
p=Path('tools/tests/test_multi_turret_targeting.py');s=p.read_text().replace('audit, audit_actor','audit, audit_actor, audit_removals');pos=s.index('    def test_missing_station');s=s[:pos]+'''    def test_local_attack_rename_must_not_remove_nonexistent_inherited_trait(self):
        actor = load_text("unit:\\n\\t-AttackTurreted:\\n\\tAttackMultiTurreted:\\n")[0]
        class Rules:
            def inherits_of(self, actor): return []
        self.assertEqual(audit_removals(actor, Rules()), ["unit: removes absent AttackTurreted"])

'''+s[pos:];p.write_text(s)
