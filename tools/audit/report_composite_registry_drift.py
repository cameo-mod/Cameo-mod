"""Read-only review queue; fresh diagnostic output never grants approval."""
import argparse
from collections import Counter
import json

import intentional_composites as registry
from audit_three_way_split import main_warhead_nodes
from miniyaml import Ruleset
from survey_weapon_structure import inventory

OUT = registry.ROOT / 'docs/audit/latest/composite_registry_drift.json'
SNAPSHOT_FIELDS = ('mains', 'main_digest', 'weapon_digest', 'referrers', 'referrer_digest')
DECISION_FIELDS = ('category', 'component_purposes', 'mains', 'rationale',
                   'review_reference', 'overlap_justification')


def compare_entry(name, entry, decision, live, reachability):
    categories = []
    if entry is None or decision is None:
        categories.append('manifest-curated-name-disagreement')
    if live is None or len(live['mains']) < 2:
        categories.append('missing-or-no-longer-stacked')
    if decision is not None and (live is None or decision.get('mains') != live['mains']):
        categories.append('curated-main-names-differ-from-live')
    snapshot_changes = [key for key in SNAPSHOT_FIELDS
                        if entry is not None and live is not None and entry.get(key) != live.get(key)]
    if 'mains' in snapshot_changes:
        categories.append('manifest-main-names-differ-from-live')
    if 'main_digest' in snapshot_changes and 'mains' not in snapshot_changes:
        categories.append('main-behavior-drift-with-unchanged-names')
    if 'weapon_digest' in snapshot_changes:
        categories.append('resolved-weapon-behavior-drift')
    if ('referrers' in snapshot_changes or 'referrer_digest' in snapshot_changes
            or entry is not None and entry.get('expected_reachability') != reachability):
        categories.append('reference-or-reachability-drift')
    decision_changes = [key for key in DECISION_FIELDS
                        if entry is not None and decision is not None and entry.get(key) != decision.get(key)]
    if decision_changes:
        categories.append('decision-metadata-disagreement')
    return {'weapon': name, 'categories': categories,
            'manifest_mains': entry.get('mains') if entry is not None else None,
            'curated_mains': decision.get('mains') if decision is not None else None,
            'live_mains': live['mains'] if live is not None else None,
            'live_raw_stack_reachability': reachability,
            'changed_snapshot_fields': snapshot_changes,
            'changed_decision_fields': decision_changes}


def build():
    rules = Ruleset(registry.ROOT)
    findings = registry.validate_manifest(rules, main_warhead_nodes)
    manifest = registry.load_manifest()['entries']
    decisions = registry.curated_decisions()
    # This callback disables approval classification solely to obtain raw counts.
    # Never publish its artificial reviewed/unreviewed partitions.
    raw = inventory(rules, reviewed_predicate=lambda _name, _mains: False)
    reachability = {name: kind for key, kind in (
        ('direct_actor_armament', 'direct'), ('indirect_weapon_graph', 'indirect'), ('unreached', 'unreached'))
        for name in raw['sets'][key]}
    refs = registry.resolved_referrer_index(rules)
    rows = []
    for name in sorted(set(manifest) | set(decisions)):
        live = registry.live_snapshot(rules, name, main_warhead_nodes, refs) if rules.weapon(name) is not None else None
        rows.append(compare_entry(name, manifest.get(name), decisions.get(name), live, reachability.get(name)))
    counts = Counter(category for row in rows for category in row['categories'])
    return {'scope': 'Diagnostic only; categories overlap and never imply safe reapproval. '
                     'Unchanged main names can conceal changed damage, armor, geometry or state delivery. '
                     'Only raw topology counts are published; reviewed status is unavailable while validation fails. '
                     'All validator findings are retained, including schema findings not represented by row categories. '
                     'No manifest, curated decisions, weapons or actor data are written.',
            'registry_status': 'blocked' if findings else 'valid',
            'validation_finding_count': len(findings), 'validation_findings': findings,
            'category_counts_overlapping': dict(sorted(counts.items())),
            'raw_topology_counts': {key: value for key, value in raw['counts'].items()
                                    if not key.startswith(('reviewed_', 'unreviewed_'))},
            'entries': rows}


def main():
    parser = argparse.ArgumentParser(__doc__)
    parser.add_argument('--write', action='store_true', help='write diagnostic JSON only, never approval fingerprints')
    args = parser.parse_args()
    result = build()
    text = json.dumps(result, indent=2, sort_keys=True) + '\n'
    if args.write:
        OUT.write_text(text, encoding='utf-8')
    elif not OUT.exists() or OUT.read_text(encoding='utf-8') != text:
        raise SystemExit('Composite-registry drift diagnostic is stale')
    print(json.dumps({key: value for key, value in result.items() if key not in {'entries', 'validation_findings'}}, indent=2))
    # A fresh report of an invalid registry is still a failed validation.
    return 1 if result['registry_status'] == 'blocked' else 0


if __name__ == '__main__':
    raise SystemExit(main())
