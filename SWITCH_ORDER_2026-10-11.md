# SWITCH_ORDER_2026-10-11

**Scope:** inventory of the 60 increment-switch groups in `tools/ai/increment_switches.yaml` at base `700bb16483f6d92664153e98d6f2560c312acaab`. This is a review proposal, not approval to integrate or launch. Fix/safety category means test and same-seed parity, no causal A/B; move into a ratcheted baseline only after its separate review/integration gate. Behavior order is a review-priority hypothesis, not an empirical ranking.

## Fix/safety: test and parity, skip A/B (9)

| Group | Classification note |
|---|---|
| `AG_assault_fanout` | Formation correctness fix candidate; PT7 dead-member crash finding means HOLD until fixed and regression-tested. |
| `AL_emergency_net_loss` | Loss-state restraint and personality preservation. |
| `AN_combat_veto` | Combat safety veto; parity and false-veto review. |
| `BJ_squad_hysteresis` | Corrective order-churn/stutter fix. |
| `BK_squad_order_dedup` | Corrective duplicate-order suppression. |
| `BL_protection_episode_guard` | Corrective protection oscillation guard. |
| `BN_squad_pool_fixes` | Corrective stale/unmatched squad-pool cleanup. |
| `BO_squad_move_dedup` | Corrective repeated-move suppression. |
| `K_cn2_unit_repair` | Repair-path correctness; safety/correctness gate. |

## Behavior switches ? impact-prioritized A/B queue (51)

Each effect is isolated against the current ratchet baseline. Accepted switches join the next control baseline; every test pins that baseline manifest SHA and one added patch. The first three are the amended BU pilot, expansion appetite, and harvester spread. Remaining order is triage only; validate live consumers and nonzero deltas on the reviewed base before freezing a manifest.

| Order | Group | Caveat |
|---:|---|---|
| 1 | `BU_harvester_logistics` |  |
| 2 | `U_ut4_expansion_appetite` |  |
| 3 | `AF_harvester_spread` |  |
| 4 | `ST_scale_targets` |  |
| 5 | `AH_parallel_production` |  |
| 6 | `BT_expansion_prebuild` |  |
| 7 | `AK_build_order_knobs` |  |
| 8 | `T_fb2_demand_capturers` |  |
| 9 | `V_tc2_expansion_claims` |  |
| 10 | `AC_cover_map_expansion` | Prior master pin had this policy already armed; may have zero delta. |
| 11 | `AJ_field_coverage` | Prior master pin had this policy already armed; may have zero delta. |
| 12 | `AD_spaced_base_placement` |  |
| 13 | `F_plug_spawn` |  |
| 14 | `F2_ca_f2p2` |  |
| 15 | `BM_live_combat_model` |  |
| 16 | `BP_effective_unit_value` |  |
| 17 | `I_derived_unit_weights` |  |
| 18 | `M_utility_axes` |  |
| 19 | `G_personality_leads` |  |
| 20 | `J_rolemix_production` |  |
| 21 | `M_cn3_deploy` |  |
| 22 | `AE_army_first` |  |
| 23 | `AM_army_staging` |  |
| 24 | `AO_tier3_bandits` |  |
| 25 | `AQ_inmatch_adapt` |  |
| 26 | `AP_tier1_priors` | Prior inventory found learned-priors input absent/inert; defer until live input exists. |
| 27 | `BB_tc3_coalition_plan` |  |
| 28 | `BC_tc3_rescue_election` |  |
| 29 | `BD_tc3_sectors` |  |
| 30 | `BE_tc3_main_target` |  |
| 31 | `BF_team_capture_claims` |  |
| 32 | `BG_raid_mission_steering` |  |
| 33 | `BH_tc3_assist_election` |  |
| 34 | `E_engt_transport` |  |
| 35 | `H_influence_layers` |  |
| 36 | `D_zone_topology` |  |
| 37 | `N_ca6_target_intel` |  |
| 38 | `O_ca2_defend_reserve` |  |
| 39 | `P_di2_director_pacing` |  |
| 40 | `Q_ut3_defend_share` |  |
| 41 | `R_tc2_sync_attacks` |  |
| 42 | `S_tc2_defend_answers` |  |
| 43 | `W_tc2_role_split` |  |
| 44 | `L_cn2_garrison_defense` |  |
| 45 | `Y_cn3_stealth_squads` |  |
| 46 | `Z_def3_remote_outpost_coverage` |  |
| 47 | `AB_garrison_contest` |  |
| 48 | `X_cn3_bridge_repair` |  |
| 49 | `AA_cn4_region_roles` |  |
| 50 | `BI_front_back_placement` |  |
| 51 | `AI_radar_contacts` |  |

## Sequential acceptance and regression cadence

Predeclare exact manifest thresholds: early ACCEPT at >=75% treatment wins after >=16 rounds when economy/army are not worse; early REJECT at <=25% or on crash/sentinel; otherwise extend through 32, 48, and at most 64 rounds. At the final boundary ACCEPT requires win non-inferiority and improvement in earned/spent and army efficiency; otherwise REJECT/PARK. After each fifth accepted behavior switch, run 16 rounds of the new baseline against original master. Safety/fix groups bypass A/B only after their code review/integration gate.
