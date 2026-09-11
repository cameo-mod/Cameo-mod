# Four-faction role and payload disposition

**Scope:** active RA1 Allies, RA1 Soviets, TD GDI and TD Nod candidate actors.
This records the reviewed changes and remaining questions from the missile,
target-route, trait-route and FireShrapnel receipts. The prior GP-03 completion
claim is withdrawn. Global actors outside the four factions remain follow-on work.

## Applied candidate corrections

- `ra1_allies_rapierjumpjet_missile_AA` targets surface and air, so its heavy
  missile payload now uses `^Warhead_MissileAP_Heavy` and
  `^Effect_MissileAP_Heavy`. Both authored 4000-damage channels, cadence,
  projectile and target mask are preserved. This removes the only strict
  MissileHE-against-Air finding with a direct consumer in the four-faction
  candidate.
- Sol's surface-only change to `td_gdi_havoc_sniper` and `td_gdi_havoc_rifle`
  has been reversed. The rifle already has 8000 Air-capable flat damage, and
  both weapons have Air-capable Chaingun percentage payloads. Their other
  recipient-filtered channels do not justify deleting that existing role.
  Havoc retains its original targeting and damage; any full-payload parity
  change requires a concrete role decision.

The focused target-policy suite passes 15 tests, including restored Havoc
capability. Rapier's one-weapon, one-actor-consumer closure preserves firing
settings and raw damage. Its raw ledger and eleven changed derived fields now
match staged extraction; the global model is unchanged. The missile scan has
378 concrete missile weapons and leaves 15 R1, 8 R2, 4 R3 and 4 R4 findings
globally. None of those strict rows has a direct consumer in these four
factions. The four remaining R4 rows belong to CABAL, StarCraft and Japan.

## Candidate target and secondary closure

The corrected target-route receipt retains 19 candidate review rows:

| rows | disposition | action |
|---:|---|---|
| 3 | Point-defense armaments use utility projectile interception and a minimal local damage channel. | Exclude from ordinary offensive domain parity; no change. |
| 12 | `AffectsIntegrity` channels use recipient tags such as Vehicle, Ship, Air, Cyborg, Defense, Building and Epic. | The domain-only audit cannot represent these mixed recipient tags; preserve them and do not call the rows surface exclusions. |
| 3 | Havoc's rifle/sniper mix broad and recipient-filtered damage channels. | Retain existing capability pending role review; do not nerf the weapon to clear a diagnostic. |
| 1 | The upgraded Nod Stealth Tank's dual-domain missile can emit `CHFlame`, a surface flame cloud. | Keep the existing cloud while the full route is reviewed. The referring mask being inert does not mean the cloud's damage is inert or exempt from payload review. |

The trait allowlist contains 1291 routes on candidate actors: 8 casing, 606
death-empty, 673 death, 3 falls-to-earth and 1 impact route. All resolve to a
weapon identity; this is wiring evidence, not runtime activation or damage
equivalence. The FireShrapnel receipt contains 21 candidate-bound roots and 35
edges: 21 domain matches, 14 custom-tag reviews and zero domain mismatches.
Those counts do not prove complete target/secondary closure. The fourteen
custom-tag cases, spawned actors, script/map roots, activation, geometry and
payload valuation retain their stated limits.

## Remaining V2 Tesla parent decision

`ra1_soviets_v2rocketlauncher_scudtesla` and its two fragment definitions are
surface-only (`Ground, Water`). They use the separate MissileTesla family, so
the HE/AA/AP rule does not decide whether this artillery should gain Air.

1. **Keep surface-only (recommended):** preserves the current artillery role,
   existing fragment masks and targeting behavior. No evidence in the current
   candidate requires an anti-air role.
2. **Add Air to parent and fragments:** creates a dual-domain Tesla artillery
   role. This requires an explicit design decision plus target-selection,
   projectile and fragment runtime review before implementation.

Until that decision is supplied, the current surface-only behavior remains
unchanged and is not a blocker for independent proposal rows.

## Evidence

- `docs/audit/latest/missile_role_triage_v3_20260911.json` and `.md`
- `docs/audit/latest/missile_role_decisions_v3_20260911.json` and `.md`
- `docs/audit/latest/astra_review_20260911/target_routes.json`
- `docs/audit/latest/astra_review_20260911/review.json`
- `docs/audit/latest/secondary_payload_routes_20260911.json`
- `docs/audit/latest/shrapnel_scenario_20260911.json`

No engine, build, launch, commit, push, PR or merge action is included.
