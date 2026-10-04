#!/usr/bin/env python3
"""fit_engagement_priors.py - the tier-1 'measured from logs' fitter (DESIGN 19.13 tier 1,
TIER1_FITTER_SPEC; AI_ARCHITECTURE 12.30/12.34).

Reads engagement/1 records (Logs/cameo-ai-engagements.jsonl) plus the balance ledger
(docs/balance/*.json raw stat ledgers) and fits ~1000 stat-normalised coefficients:

1. Delivery x armour strength residuals: the resolved ^Warhead_* Versus table is the pipeline
   prior; the fit measures how much better or worse each delivery family actually trades against
   each armour class than the Lanchester predictor expected. Cells = damage_warheads[].tag
   (e.g. Bullet_Medium) x resolved Armor.Type - never per-unit ids.
2. Static-defence state strengths: the same residual pooled over cells where the attacker was a
   static defence (defence fire effectiveness), plus a global attacking-into-defences correction.
3. One attrition exponent on the predicted ratio (square law = 1000).
4. Timing and response priors: per-enemy-faction attack-timing quantiles (from our defend
   records - a defend for us is their attack), own response-time and army-distance quantiles,
   per-faction-pair suicide-index medians. Faction-keyed tables pool global -> family -> faction;
   'enemy_faction_public: false' (a Random lobby slot) pools to family/global only (ruling 2).

Fitting is closed-form expected-attribution with pseudo-evidence shrinkage (the same pattern as
fit_arsenal_priors.py's shrunk_ratio): residual[d][a] = (obs + K*g) / (g*exp + K*g) where g is the
pooled global obs/exp scale, K = SHRINK_VALUE of pseudo-damage credit, clamped [MIN_MILLI, MAX_MILLI].
obs counts ONLY deaths among the start census (truth.enemy_loss_value / the unit+defence split /
the own_defences census delta - never buildings, harvesters or mid-fight arrivals); exp prices the
real victim force (truth.start values when present). stdlib only, deterministic: same logs in,
same file out.

    python tools/ai/fit_engagement_priors.py <batch-dir> [...] [--write mods/cameo/ai/learned/engagement_priors.yaml]

Without --write it prints a report. The yaml is a committed, reviewed data file read frozen at
match start (DESIGN 19.2); no game code reads it until the phase-B consumer lands behind the
default-OFF `AP_tier1_priors` increment group.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import pathlib
import statistics
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "audit"))
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "balance"))
import ai_log_common as c  # noqa: E402
from firepower import armament_firepower  # noqa: E402

REPO = pathlib.Path(__file__).resolve().parents[2]
PHASE_TICKS = 6000
SHRINK_VALUE = 5000   # pseudo-evidence (damage credit) mixed into every cell's ratio
MIN_MILLI = 500       # EngagemantPriors bounds - never invert a fight
MAX_MILLI = 2000
MIN_PAIR_SAMPLES = 8  # faction-pair table needs this many rows before it stops inheriting

# ── PRIORS-CARRY (SPEC_2026-10-04_claude_priors_carry_over item 2) ───────────────────────────
# A residual is relative, so it survives a rebalance with confidence that decays with the size
# of the move. Three mechanisms, all mirroring the consumer's own carry decay
# (EngagementPriorsBotModule.FactorPermille): decay w = versions^N * exp(-|ln dPrior| / tau).
STALENESS_TAU_MILLI = 350  # shared with the consumer's StalenessTauMilli default; emitted in yaml
VERSION_EVIDENCE_DECAY = 0.7  # carried evidence keeps this fraction per crossed balance boundary
LEGACY_RECORD_WEIGHT = 0.4   # engagement records with no `balance` stats block (pre-Tier4 logs)
# `balance` on a record: {"fingerprint": <opaque provenance string>,
#                         "versus": {"<Tag>|<Armor>": <percent>, ...}}
# — the cells that fight touched, staked to ITS OWN resolved stats (Tier4's field). A record
# that carries it is self-pricing: its residual is valid under any balance version, and its
# evidence decays per-cell as exp(-|ln(rec/now)|*1000/tau). Absent: legacy weight, and its
# cells are staked on the previous fit's PriorPct before today's table — never silently
# re-priced by the current ledger alone.
PREV_DEFAULT_EVIDENCE = SHRINK_VALUE  # carried Evidence@ for a prev row that lacks the field


# ── type profiles from the raw stat ledgers (never hand-parsed yaml; rule 8e) ───────────────

def cycle_ticks(reload_delay: int, burst: int, burst_delays: list[int]) -> int:
    """Mirror of BotWeaponProfile.CycleTicks: burst-1 gaps, then the reload."""
    cycle = max(1, reload_delay)
    for shot in range(burst - 1):
        if not burst_delays:
            cycle += 0
        elif len(burst_delays) == 1:
            cycle += burst_delays[0]
        else:
            cycle += burst_delays[min(shot, len(burst_delays) - 1)]
    return cycle


def _int(value, default: int = 0) -> int:
    try:
        return int(str(value).split(",")[0])
    except (TypeError, ValueError):
        return default


def load_profiles(repo: pathlib.Path) -> tuple[dict, dict, str]:
    """name.lower() -> {cost, hp, armor, weapons: [(tag, dpt)]}; delivery tag -> Versus table;
    the ledger hash. Built from docs/balance/*.json raw ledgers only."""
    profiles: dict[str, dict] = {}
    collisions: list[str] = []
    ledger_files = []
    for path in sorted((repo / "docs" / "balance").glob("*.json")):
        try:
            doc = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            continue
        if not isinstance(doc, dict) or "ledger" not in doc or "sections" not in doc:
            continue
        ledger_files.append(path)
        for entries in doc["sections"].values():
            if not isinstance(entries, dict):
                continue
            for name, a in entries.items():
                if not isinstance(a, dict):
                    continue
                key = name.lower()
                if key in profiles:
                    collisions.append(key)
                    continue
                weapons = []
                for arm in a.get("armaments") or []:
                    whs = [w for w in (arm.get("damage_warheads") or []) if _int(w.get("damage")) > 0]
                    if not whs:
                        continue
                    main = max(whs, key=lambda w: _int(w.get("damage")))
                    delays = [_int(x) for x in str(arm.get("burstdelays") or "").split(",") if str(x).strip()]
                    burst = max(1, _int(arm.get("burst"), 1))
                    cycle = cycle_ticks(_int(arm.get("reloaddelay"), 1), burst, delays)
                    # dpt carries the actor's resolved unconditional firepower product
                    # (firepower.armament_firepower): a buffed unit's real output differs
                    # from its raw damage and must not leak into the delivery x armour cells.
                    dpt = _int(main.get("damage")) * burst / cycle * armament_firepower(a, arm)
                    weapons.append((main.get("tag") or "", dpt))
                profiles[key] = {
                    "cost": _int((a.get("cost") or {}).get("v")),
                    "hp": _int((a.get("hp") or {}).get("v")),
                    "armor": (a.get("armor") or {}).get("v") or "None",
                    "weapons": [w for w in weapons if w[0]],
                }
    digest = hashlib.sha256()
    for path in ledger_files:
        digest.update(path.name.encode())
        digest.update(path.read_bytes())
    return profiles, {"collisions": collisions, "ledger_files": len(ledger_files)}, digest.hexdigest()


def versus_priors(repo: pathlib.Path, tags: list[str]) -> tuple[dict[str, dict[str, int]], list[str]]:
    """delivery tag -> {armor -> percent}, resolved through miniyaml.Ruleset (rule 8e), plus the
    sorted tags whose table came out EMPTY. Empty-prior tags are excluded from the cell fit
    entirely - fitting them against the default-100 prior would fake residuals (lead review).
    Tags already carry the level (Bullet_Medium); unresolved tags get the family template."""
    from miniyaml import Ruleset
    from percentage_damage import versus_table

    rules = Ruleset(repo)
    priors: dict[str, dict[str, int]] = {}
    excluded: list[str] = []
    for tag in tags:
        table: dict[str, int] = {}
        for name in (f"^Warhead_{tag}", f"^Warhead_{tag.rsplit('_', 1)[0]}"):
            node = rules.resolve_weapon(name)
            if node is None:
                continue
            wh = node.child(f"Warhead@{name[len('^Warhead_'):]}")
            table = versus_table(wh) if wh is not None else {}
            if table:
                break
        if table:
            priors[tag] = table
        else:
            excluded.append(tag)
    return priors, sorted(excluded)


# ── the fit ────────────────────────────────────────────────────────────────────────────────

def armour_share(comp: dict[str, int], profiles: dict) -> dict[str, int]:
    """armour class -> committed value, from a type->count composition."""
    out: dict[str, int] = {}
    for t, n in comp.items():
        p = profiles.get(t.lower())
        if not p:
            continue
        out[p["armor"]] = out.get(p["armor"], 0) + p["cost"] * n
    return out


def family_of(faction: str) -> str:
    """Game family of a faction id (td_gdi -> td, ra1_soviets -> ra1, d2k_atreides -> d2k)."""
    return faction.split("_", 1)[0] if faction else ""


def delivery_family(tag: str) -> str:
    """Delivery family of a tag (Bullet_Medium -> Bullet) — the same rsplit the Versus
    template fallback uses (a level tag inherits ^Warhead_<Family>)."""
    return tag.rsplit("_", 1)[0] if "_" in tag else tag


def staleness_decay(now_pct: float, fitted_pct: float) -> float:
    """The carry decay shared with the consumer: exp(-|ln(now/fitted)| * 1000 / tau).
    A moved prior keeps only this fraction of the evidence/residual staked on it."""
    if now_pct == fitted_pct:
        return 1.0
    if now_pct <= 0 or fitted_pct <= 0:
        return 0.0
    return math.exp(-abs(math.log(now_pct / fitted_pct)) * 1000.0 / STALENESS_TAU_MILLI)


def record_balance_stats(r: dict) -> tuple[dict, float]:
    """The record's own stats block -> ({(tag, armor): pct}, base weight). Tier4's log field:
    balance.versus = the Versus percents of the cells this fight touched, resolved under the
    match's rules. Keys arrive as 'Tag|Armor' or 'Tag__x__Armor'; both normalise to tuples."""
    bal = r.get("balance") or {}
    raw = bal.get("versus") or {}
    rec_versus = {}
    for k, v in raw.items():
        key = k.split("__x__", 1) if "__x__" in k else k.split("|", 1)
        if len(key) == 2:
            try:
                rec_versus[(key[0], key[1])] = float(v)
            except (TypeError, ValueError):
                continue
    # self-pricing requires the versus map itself; a bare fingerprint is provenance only
    # (still reported, but not enough to stake cells on) -> legacy weight.
    return rec_versus, (1.0 if rec_versus else LEGACY_RECORD_WEIGHT)


def load_prev(path: pathlib.Path | None) -> dict | None:
    """Parse a previous fitted engagement_priors.yaml -> the posterior anchor for a new fit.
    Flat 'Key: value' rows under BotEngagementPriors (our own generated schema — not game yaml).
    Returns {cells, cell_prior, evidence, defence_state, into_defences_milli,
             global_scale_milli, ledger_hash, fit_version, staleness_tau_milli}."""
    if path is None or not path.exists():
        return None
    cells, cell_prior, evidence, defence_state = {}, {}, {}, {}
    defence_state_evidence = {}
    meta = {"into_defences_milli": None, "into_defences_evidence": None,
            "global_scale_milli": 1000, "ledger_hash": None,
            "fit_version": 0, "staleness_tau_milli": STALENESS_TAU_MILLI}
    in_root = False
    try:
        lines = path.read_text(encoding="utf-8").splitlines()
    except OSError:
        return None
    for line in lines:
        if line.startswith("BotEngagementPriors:"):
            in_root = True
            continue
        if not in_root or line.startswith("#") or not line.startswith("\t") or ":" not in line:
            continue
        key, _, raw = line.strip().partition(":")
        raw = raw.strip()
        for prefix, dest in (("DeliveryArmour@", cells), ("PriorPct@", cell_prior),
                             ("Evidence@", evidence)):
            if key.startswith(prefix):
                ck = key[len(prefix):].split("__x__", 1)
                if len(ck) == 2:
                    try:
                        dest[(ck[0], ck[1])] = float(raw) if prefix == "Evidence@" else int(raw)
                    except ValueError:
                        pass
                break
        else:
            if key.startswith("DefenceStateEvidence@"):
                try:
                    defence_state_evidence[key[len("DefenceStateEvidence@"):]] = float(raw)
                except ValueError:
                    pass
            elif key.startswith("DefenceState@"):
                try:
                    defence_state[key[len("DefenceState@"):]] = int(raw)
                except ValueError:
                    pass
            elif key == "IntoDefencesEvidence":
                try:
                    meta["into_defences_evidence"] = float(raw)
                except ValueError:
                    pass
            elif key == "IntoDefencesMilli":
                meta["into_defences_milli"] = int(raw) if raw.isdigit() else None
            elif key == "GlobalScaleMilli":
                meta["global_scale_milli"] = int(raw) if raw.isdigit() else 1000
            elif key == "LedgerHash":
                meta["ledger_hash"] = raw or None
            elif key == "FitVersion":
                meta["fit_version"] = int(raw) if raw.isdigit() else 0
            elif key == "StalenessTauMilli":
                meta["staleness_tau_milli"] = int(raw) if raw.isdigit() else STALENESS_TAU_MILLI
    return {"cells": cells, "cell_prior": cell_prior, "evidence": evidence,
            "defence_state": defence_state, "defence_state_evidence": defence_state_evidence,
            **meta}


def quantiles(values: list[int], ps=(10, 50, 90)) -> list[int | None]:
    s = sorted(v for v in values if v is not None)
    if not s:
        return [None] * len(ps)
    return [s[min(len(s) - 1, max(0, round(p / 100 * (len(s) - 1))))] for p in ps]


def record_facts(r: dict, profiles: dict, matches_factions: dict) -> dict | None:
    """One engagement/1 record -> the derived facts a single accumulator pass needs, or None
    when the record cannot feed the grid. Purely read-side; the fitter's only write is below.

    The obs side must count ONLY deaths among the start census the exp side predicts for -
    the engagement/1 fields differ on purpose (E1b ruling):
    - outcome.enemy_killed_value / own_lost_value include buildings and harvesters, which have
      no composition row: crediting them to armour cells invented a ~1.5x obs-side inflation.
    - kills of mid-fight reinforcements inflate obs while exp only expects start-force losses.
    - exp must price the REAL victim force (truth.start) when present, not the fogged seen
      value - the bot's prediction is about a fraction lost, and the fraction applies to the
      real census the deaths are drawn from."""
    comp = ((r.get("seen") or {}).get("start") or {}).get("composition")
    if not comp:
        return None
    outcome = r.get("outcome") or {}
    start = (r.get("seen") or {}).get("start") or {}
    truth = r.get("truth") or {}
    truth_start = truth.get("start") or {}
    truth_comp = truth_start.get("composition") or {}

    own_units, own_defs = comp.get("own_units") or {}, comp.get("own_defences") or {}
    enemy_units = truth_comp.get("units") or comp.get("enemy_units") or {}
    enemy_defs = truth_comp.get("defences") or comp.get("enemy_defences") or {}
    own_all, enemy_all = {**own_units, **own_defs}, {**enemy_units, **enemy_defs}
    if not own_all or not enemy_all:
        return None
    enemy_share = armour_share(enemy_all, profiles)
    own_share = armour_share(own_all, profiles)
    if not enemy_share or not own_share:
        return None

    seen_enemy_v = start.get("enemy_unit_value", 0) + start.get("enemy_defence_value", 0)
    has_truth_v = "enemy_unit_value" in truth_start or "enemy_defence_value" in truth_start
    truth_enemy_v = truth_start.get("enemy_unit_value", 0) + truth_start.get("enemy_defence_value", 0)
    enemy_value = max(1, truth_enemy_v if has_truth_v else seen_enemy_v)
    own_value = max(1, start.get("own_committed_value", 0) + start.get("own_defence_value", 0))

    # enemy-side obs: truth.enemy_loss_value tracks real deaths among the start-scan actors
    # exactly; older logs fall back to the unit+defence split (pre-split logs: the total),
    # always bounded by the start census value so reinforcements cannot inflate it.
    loss = truth.get("enemy_loss_value")
    if loss is None:
        if "enemy_killed_unit_value" in outcome or "enemy_killed_defence_value" in outcome:
            loss = outcome.get("enemy_killed_unit_value", 0) + outcome.get("enemy_killed_defence_value", 0)
        else:
            loss = outcome.get("enemy_killed_value", 0)
    enemy_lost = min(loss, enemy_value)

    # own-side obs: defence deaths hide inside own_lost_building_value (own defences are
    # buildings to the writer), so recover them from the fully-seen own_defences census
    # start -> end delta; harvester deaths inside own_lost_unit_value are bounded by the cap.
    own_lost = outcome.get("own_lost_unit_value", outcome.get("own_lost_value", 0))
    end_defs = ((((r.get("seen") or {}).get("end") or {}).get("composition")) or {}).get("own_defences") or {}
    for t, n0 in own_defs.items():
        p = profiles.get(t.lower())
        if p:
            own_lost += max(0, n0 - end_defs.get(t, 0)) * p["cost"]
    own_lost = min(own_lost, own_value)

    # ruling 2: the OFFLINE fit uses the real faction; only in-match consumers honour
    # enemy_faction_public (a Random slot falls back to family/global pools).
    enemy_faction = r.get("enemy_faction") or ""
    if not enemy_faction:  # pre-field logs: the other player's faction in the same match
        for f in matches_factions.get(r.get("game_uid", ""), []):
            if f != r.get("faction"):
                enemy_faction = f

    own_pred = start.get("predicted_own_surviving_permille")
    return {
        # our fire into them (real victims when truth is present), then their fire into us;
        # (mobile units, static defences) stay disjoint so defence fire splits out cleanly
        "dirs": [
            (own_units, own_defs, enemy_share, enemy_value,
             enemy_lost, start.get("predicted_enemy_surviving_permille", 0)),
            (enemy_units, enemy_defs, own_share, own_value,
             own_lost, start.get("predicted_own_surviving_permille", 0)),
        ],
        "into_defences": ((r.get("tactics") or {}).get("into_defences_value") or 0) > 0,
        "ratio": ((own_pred / 1000.0, max(0.0, 1.0 - own_lost / own_value))
                  if own_pred is not None and 0 < own_pred < 1000 else None),
        "kind": r.get("kind"), "start_tick": r.get("start_tick", 0),
        "faction": r.get("faction", ""), "enemy_faction": enemy_faction,
        "response": (r.get("response") or {}), "suicide_milli": (r.get("tactics") or {}).get("suicide_index_milli"),
        "killed_total": enemy_lost, "lost_total": own_lost,
    }


def fit(data: dict, profiles: dict, priors: dict, prev: dict | None = None,
        ledger_hash: str | None = None) -> dict:
    matches_factions: dict[str, list[str]] = {}
    for m in data.get("matches", []):
        pl = m.get("player") or {}
        if pl.get("faction"):
            matches_factions.setdefault(m.get("game_uid", ""), []).append(pl["faction"])

    prev_prior = (prev or {}).get("cell_prior") or {}

    skipped = {"skirmish": 0, "no_composition": 0, "empty_side": 0, "unmapped": 0,
               "legacy_stats": 0, "own_stats": 0}
    facts = []
    for r in data.get("engagements", []):
        if r.get("record") != "engagement":
            continue
        if r.get("skirmish"):
            skipped["skirmish"] += 1
            continue
        f = record_facts(r, profiles, matches_factions)
        if f is None:
            comp = ((r.get("seen") or {}).get("start") or {}).get("composition") or {}
            truth_comp = (((r.get("truth") or {}).get("start") or {}).get("composition")) or {}
            enemy_comp = truth_comp or {}
            if not comp:
                key = "no_composition"
            elif not ({**comp.get("own_units", {}), **comp.get("own_defences", {})}
                      and {**(enemy_comp.get("units") or comp.get("enemy_units") or {}),
                           **(enemy_comp.get("defences") or comp.get("enemy_defences") or {})}):
                key = "empty_side"   # one side had no census actors to attribute into
            else:
                key = "unmapped"     # actor names missing from the ledger profiles
            skipped[key] += 1
            continue
        f["rec_versus"], f["w"] = record_balance_stats(r)
        skipped["own_stats" if f["w"] >= 1.0 else "legacy_stats"] += 1
        facts.append(f)

    # bounded estimator (spec §4): one record's damage credit is capped at 4x the median
    # record so a single 50k-value bloodbath cannot dominate a cell.
    totals = sorted(f["killed_total"] + f["lost_total"] for f in facts)
    cap = 4 * totals[len(totals) // 2] if totals else 0

    obs: dict[tuple[str, str], float] = {}
    exp: dict[tuple[str, str], float] = {}
    fam_obs: dict[tuple[str, str], float] = {}   # (delivery family, armour) partial-pool sums
    fam_exp: dict[tuple[str, str], float] = {}
    ds_obs: dict[str, float] = {}
    ds_exp: dict[str, float] = {}
    into_obs = into_exp = 0.0
    ratios: list[tuple[float, float]] = []
    attack_ticks: dict[str, list[int]] = {}
    response: dict[str, list[int]] = {}
    army_dist: dict[str, list[int]] = {}
    suicide: dict[tuple[str, str], list[int]] = {}

    for f in facts:
        rw = f["w"]
        for attacker_units, attacker_defs, v_share, v_value, killed, surviving_pm in f["dirs"]:
            killed = min(killed, cap)
            total_v = sum(v_share.values()) or 1
            for a, a_value in v_share.items():
                share = a_value / total_v
                killed_a = killed * share
                expected_a = min(v_value, cap) * (1000 - min(1000, max(0, surviving_pm))) / 1000.0 * share
                # delivery -> [mobile power, defence power] against this armour class.
                # The Versus percent the attribution is staked on comes from the RECORD's own
                # stats when it carries them (balance.versus), else the previous fit's PriorPct
                # for that cell, else today's resolved table (PRIORS-CARRY: each log priced by
                # its own stats; legacy logs downweighted, never silently re-priced).
                power: dict[str, list[float]] = {}
                for comp_map, is_def in ((attacker_units, False), (attacker_defs, True)):
                    for t, n in comp_map.items():
                        p = profiles.get(t.lower())
                        if not p:
                            continue
                        for tag, dpt in p["weapons"]:
                            if tag not in priors:  # empty Versus prior: excluded, never fitted
                                continue
                            staked = f["rec_versus"].get((tag, a),
                                                         prev_prior.get((tag, a),
                                                                        priors.get(tag, {}).get(a, 100)))
                            w = n * dpt * staked / 100.0
                            e = power.setdefault(tag, [0.0, 0.0])
                            e[1 if is_def else 0] += w
                total_p = sum(sum(pw) for pw in power.values())
                if total_p <= 0:
                    continue
                for d, (unit_w, def_w) in power.items():
                    weight = (unit_w + def_w) / total_p
                    key = (d, a)
                    # Per-cell staleness: the residual was measured under the staked prior —
                    # carrying it to today's cell decays with how far that prior moved (the
                    # consumer applies the same exp decay to the fitted value).
                    cd = staleness_decay(priors.get(d, {}).get(a, 100),
                                         f["rec_versus"].get((d, a),
                                                             prev_prior.get(key, priors.get(d, {}).get(a, 100))))
                    obs[key] = obs.get(key, 0.0) + killed_a * weight * rw * cd
                    exp[key] = exp.get(key, 0.0) + expected_a * weight * rw * cd
                    fkey = (delivery_family(d), a)
                    fam_obs[fkey] = fam_obs.get(fkey, 0.0) + killed_a * weight * rw * cd
                    fam_exp[fkey] = fam_exp.get(fkey, 0.0) + expected_a * weight * rw * cd
                    if def_w > 0:
                        ds_obs[d] = ds_obs.get(d, 0.0) + killed_a * def_w / total_p * rw
                        ds_exp[d] = ds_exp.get(d, 0.0) + expected_a * def_w / total_p * rw

        if f["into_defences"]:  # the toll our fire paid while their defences were live
            _, _, v_share, v_value, killed, surviving_pm = f["dirs"][0]
            into_obs += min(killed, cap) * rw
            into_exp += min(v_value, cap) * (1000 - min(1000, max(0, surviving_pm))) / 1000.0 * rw

        if f["ratio"]:
            ratios.append(f["ratio"])
        if f["kind"] == "defend":
            if f["enemy_faction"]:
                attack_ticks.setdefault(f["enemy_faction"], []).append(f["start_tick"])
            resp = f["response"]
            if resp.get("response_ticks", -1) >= 0:
                response.setdefault(f["faction"], []).append(resp["response_ticks"])
            if resp.get("army_dist_at_start_cells", -1) >= 0:
                army_dist.setdefault(f["faction"], []).append(resp["army_dist_at_start_cells"])
        elif f["kind"] == "attack" and f["suicide_milli"] is not None and f["enemy_faction"]:
            suicide.setdefault((f["faction"], f["enemy_faction"]), []).append(f["suicide_milli"])

    # E1b global scale: the pooled obs/exp ratio. The first real fit learned one common
    # ~3.3x multiplier (mostly accounting inflation, now fixed) and 149 cells pinned at the
    # cap - the per-cell factors could not express real differences. Cells are now fitted
    # RELATIVE to this scale: pseudo-evidence K of expected damage at the global rate, so
    # thin cells inherit the global level (1000 relative), a uniform scale reads neutral,
    # and only a real per-cell deviation moves a factor (partial pooling global -> family).
    total_obs = sum(obs.values()) + sum(ds_obs.values()) + into_obs
    total_exp = sum(exp.values()) + sum(ds_exp.values()) + into_exp
    global_scale = total_obs / total_exp if total_exp > 0 else 1.0
    g = global_scale if global_scale > 0 else 1.0

    # PRIORS-CARRY: the previous fit's posterior anchors every cell instead of the bare
    # global level. Carried evidence = prev Evidence@ decayed by (a) one balance boundary
    # when the ledger hash moved and (b) the per-cell exp(-|ln(now/fitted)|/tau) decay the
    # consumer itself applies — a cell whose Versus row moved keeps a fraction of its old
    # evidence, one whose row vanished keeps none. The anchor is expressed in the NEW g's
    # relative units so a uniform scale change never shifts it.
    boundary = (prev is not None and ledger_hash is not None
                and prev["ledger_hash"] not in (None, ledger_hash))
    boundary_decay = VERSION_EVIDENCE_DECAY if boundary else 1.0
    g_old = (prev["global_scale_milli"] / 1000.0) if prev else 1.0
    carried: dict[tuple[str, str], tuple[float, float, float]] = {}
    if prev:
        for k, prev_milli in prev["cells"].items():
            d, a = k
            # Same liveness rule as the consumer (EngagementPriorsBotModule.FactorPermille):
            # a missing ARMOUR row reads the Versus-default 100 and only decays against it;
            # a delivery tag gone from today's resolved table is a dead cell (no carry).
            now_pct = priors.get(d, {}).get(a, 100) if d in priors else None
            if now_pct is None:
                w_carry = 0.0
            else:
                w_carry = boundary_decay * staleness_decay(now_pct, prev["cell_prior"].get(k, 100))
            anchor_ev = prev["evidence"].get(k, PREV_DEFAULT_EVIDENCE) * w_carry
            anchor_rel = (prev_milli / 1000.0) * g_old / g   # absolute ratio re-based to new g
            carried[k] = (anchor_ev, anchor_rel, w_carry)

    def shrunk_cell(o, e, k):
        # hierarchical pooling cell -> family -> global: the family level pools SIBLING
        # cells only (the cell's own mass stays in o/e — including it would double-count
        # its evidence, once directly and once through the family anchor).
        fkey = (delivery_family(k[0]), k[1])
        fam_o = fam_obs.get(fkey, 0.0) - o
        fam_e = fam_exp.get(fkey, 0.0) - e
        fam_rel = (fam_o + SHRINK_VALUE * g) / (g * (fam_e + SHRINK_VALUE))
        anchor_ev, anchor_rel, _ = carried.get(k, (0.0, 1.0, 0.0))
        return ((o + SHRINK_VALUE * fam_rel * g + anchor_ev * anchor_rel * g)
                / (g * (e + SHRINK_VALUE + anchor_ev)))

    all_keys = set(obs) | set(exp) | set(carried)
    cells = {k: max(MIN_MILLI, min(MAX_MILLI, round(1000 * shrunk_cell(obs.get(k, 0.0), exp.get(k, 0.0), k))))
             for k in all_keys}
    # Evidence@: the effective N a reviewer and the next refit see — real exp-side evidence
    # plus the carried (already decayed) anchor mass. The constant K shrink is a prior, not
    # evidence, and is deliberately excluded so anchor mass cannot grow unboundedly.
    eff_evidence = {k: round(exp.get(k, 0.0) + carried.get(k, (0.0, 0, 0))[0])
                    for k in all_keys}
    # F1(b): per-cell staleness is the resolved Versus percent the cell was fitted on -
    # exactly what the dpt weight consumed (default 100 when the armour row is absent).
    # The consumer recomputes the current prior per cell and reverts only moved cells.
    cell_prior = {(d, a): priors.get(d, {}).get(a, 100) for (d, a) in cells}

    # The same carry for the pooled defence-state rows and the into-defences scalar: a
    # version boundary decays their stored evidence (no per-cell staleness — the pooled
    # row stakes no single resolved Versus percent).
    def carried_scalar(prev_val, prev_ev):
        if prev_val is None:
            return 0.0, 1.0
        ev = (prev_ev if prev_ev is not None else PREV_DEFAULT_EVIDENCE) * boundary_decay
        return ev, (prev_val / 1000.0) * g_old / g

    def shrunk_scalar(o, e, ev, rel):
        return (o + SHRINK_VALUE * g + ev * rel * g) / (g * (e + SHRINK_VALUE + ev))

    prev_ds = (prev or {}).get("defence_state") or {}
    prev_ds_ev = (prev or {}).get("defence_state_evidence") or {}
    defence_state, ds_evidence = {}, {}
    for d in set(ds_obs) | set(ds_exp) | set(prev_ds):
        ev, rel = carried_scalar(prev_ds.get(d), prev_ds_ev.get(d))
        defence_state[d] = max(MIN_MILLI, min(MAX_MILLI,
                                            round(1000 * shrunk_scalar(ds_obs.get(d, 0.0), ds_exp.get(d, 0.0), ev, rel))))
        ds_evidence[d] = round(ds_exp.get(d, 0.0) + ev)
    ev_i, rel_i = carried_scalar((prev or {}).get("into_defences_milli"),
                                 (prev or {}).get("into_defences_evidence"))
    into_defences_milli = max(MIN_MILLI, min(MAX_MILLI,
                                           round(1000 * shrunk_scalar(into_obs, into_exp, ev_i, rel_i))))
    into_defences_evidence = round(into_exp + ev_i)

    fit_version = ((prev or {}).get("fit_version") or 0) + (1 if boundary or prev is None else 0)

    # attrition exponent: corrected_ratio = ratio ** alpha; grid search the error on surviving
    # fractions (alpha = 1 is square law; only a real improvement moves it off 1000).
    exponent_milli = 1000
    if ratios:
        def cost(alpha: float) -> float:
            total = 0.0
            for pred, realised in ratios:
                frac = pred if alpha == 1 else math.pow(pred, 1 / alpha)
                total += (realised - frac) ** 2
            return total

        base = cost(1.0)
        best_alpha, best = 1.0, base
        for i in range(50, 201):  # 0.50 .. 2.00 in 0.01 steps
            a = i / 100
            c_ = cost(a)
            if c_ < best:
                best, best_alpha = c_, a
        if best < base:
            exponent_milli = round(1000 * best_alpha)

    attack_timing = {f: quantiles(v) for f, v in sorted(attack_ticks.items())}
    response_q = {}
    for f in sorted(set(response) | set(army_dist)):
        p50, p90 = quantiles(response.get(f, []), (50, 90))
        response_q[f] = [p50, p90, (quantiles(army_dist.get(f, []), (50,)))[0]]
    suicide_q = {}
    global_si = [v for vals in suicide.values() for v in vals]
    own_si: dict[str, list[int]] = {}
    for (mine, _), vals in suicide.items():
        own_si.setdefault(mine, []).extend(vals)
    for (mine, theirs), vals in sorted(suicide.items()):
        pool = vals if len(vals) >= MIN_PAIR_SAMPLES else (
            own_si.get(mine, []) if len(own_si.get(mine, [])) >= MIN_PAIR_SAMPLES else global_si)
        if pool:
            suicide_q[(mine, theirs)] = round(statistics.median(pool))

    return {
        "cells": cells, "evidence": {k: (obs.get(k, 0.0), exp.get(k, 0.0)) for k in cells},
        "eff_evidence": eff_evidence, "carried": carried,
        "cell_prior": cell_prior, "global_scale": global_scale,
        "global_scale_milli": max(MIN_MILLI, min(MAX_MILLI, round(1000 * global_scale))),
        "defence_state": defence_state, "defence_state_evidence": ds_evidence,
        "into_defences_milli": into_defences_milli, "into_defences_evidence": into_defences_evidence,
        "exponent_milli": exponent_milli,
        "attack_timing": attack_timing, "response": response_q, "suicide": suicide_q,
        "fitted": len(facts), "record_cap": cap, "skipped": skipped,
        "fit_version": fit_version, "boundary": boundary,
    }


def report(result: dict) -> str:
    s = result["skipped"]
    meta = result.get("meta") or {}
    out = [f"{result['fitted']} fitted engagements "
           f"({s['skirmish']} skirmish, {s['no_composition']} pre-composition, "
           f"{s['empty_side']} empty-side, {s['unmapped']} unmapped skipped; "
           f"{s.get('own_stats', 0)} own-stats, {s.get('legacy_stats', 0)} legacy-stats records)"]
    n_carried = sum(1 for v in result.get("carried", {}).values() if v[0] > 0)
    if result.get("carried") is not None and result.get("fit_version"):
        out.append(f"fit version {result['fit_version']}"
                   f"{' (balance boundary crossed)' if result.get('boundary') else ''}; "
                   f"{n_carried} cells anchored on the previous posterior")
    if result.get("excluded_tags"):
        out.append(f"excluded delivery tags (empty Versus prior, not fitted): {', '.join(result['excluded_tags'])}")
    if meta.get("collisions"):
        out.append(f"profile-name collisions (first ledger wins): {', '.join(meta['collisions'])}")
    out.append(f"attrition exponent: {result['exponent_milli']} milli | into-defences: {result['into_defences_milli']} milli")
    out.append(f"global obs/exp scale: {result['global_scale']:.3f} (cells are relative to it; shrink anchor is the global level)")
    moved = [(k, v, result["evidence"][k]) for k, v in result["cells"].items() if v != 1000]
    moved.sort(key=lambda x: -abs(x[1] - 1000))
    out.append(f"\n## delivery x armour: {len(result['cells'])} cells, {len(moved)} moved")
    for (d, a), milli, (o, e) in moved[:15]:
        out.append(f"- {d} x {a}: {milli} (obs {o:.0f} / exp {e:.0f})")
    out.append(f"\n## defence states: {len(result['defence_state'])}")
    for d, milli in sorted(result["defence_state"].items())[:10]:
        out.append(f"- {d}: {milli}")
    out.append("\n## attack timing (p10/p50/p90 ticks)")
    for f, q in result["attack_timing"].items():
        out.append(f"- {f}: {q}")
    out.append("\n## response (p50/p90 ticks, army dist cells)")
    for f, q in result["response"].items():
        out.append(f"- {f}: {q}")
    out.append("\n## suicide index medians")
    for (m_, t), v in result["suicide"].items():
        out.append(f"- {m_} vs {t}: {v}")
    return "\n".join(out)


def jsonable(result: dict) -> dict:
    """result with tuple-keyed maps flattened to yaml-style string keys for --json output."""
    out = dict(result)
    for k in ("cells", "evidence", "cell_prior", "eff_evidence"):
        out[k] = {"|".join(map(str, key)): v for key, v in result[k].items()}
    out["carried"] = {"|".join(map(str, key)): v for key, v in result["carried"].items()}
    out["suicide"] = {"__vs__".join(map(str, key)): v for key, v in result["suicide"].items()}
    return out


def to_yaml(result: dict, ledger_hash: str) -> str:
    lines = ["# GENERATED by tools/ai/fit_engagement_priors.py - do not edit by hand; regenerate and review.",
             "# Tier-1 offline priors (TIER1_FITTER_SPEC, DESIGN 19.13/19.2): read at match start, frozen.",
             f"# {result['fitted']} fitted engagements.",
             f"# global obs/exp scale the factors are relative to: {result['global_scale']:.4f}"]
    if result.get("excluded_tags"):
        lines.append(f"# excluded delivery tags (empty Versus prior, not fitted): {', '.join(result['excluded_tags'])}")
    lines += ["BotEngagementPriors:", "\tSchema: 1",
             f"\tLedgerHash: {ledger_hash}",
             # The global scale fitted cells are relative to: consumers multiply it back
             # into FITTED lookups (missing/stale cells stay at neutral 1000), so mixed
             # tables need it as a key - a comment would not reach MiniYaml. Clamped to
             # the same [MIN_MILLI, MAX_MILLI] bounds as the factor cells.
             f"\tGlobalScaleMilli: {result['global_scale_milli']}",
             f"\tEngagements: {result['fitted']}",
             f"\tFitVersion: {result['fit_version']}",
             # Shared with the consumer's carry decay (NOVA's parse reads this key; the
             # fitter uses the same tau for its evidence decay).
             f"\tStalenessTauMilli: {STALENESS_TAU_MILLI}",
             f"\tAttritionExponentMilli: {result['exponent_milli']}",
             f"\tIntoDefencesMilli: {result['into_defences_milli']}",
             f"\tIntoDefencesEvidence: {result['into_defences_evidence']}"]
    for (d, a), milli in sorted(result["cells"].items()):
        lines.append(f"\tDeliveryArmour@{d}__x__{a}: {milli}")
        lines.append(f"\tPriorPct@{d}__x__{a}: {result['cell_prior'][(d, a)]}")
        # Effective N behind the fitted value — real + carried evidence (exp-side damage
        # credit). The next refit decays it; reviewers read how sure each cell is.
        lines.append(f"\tEvidence@{d}__x__{a}: {result['eff_evidence'][(d, a)]}")
    for d, milli in sorted(result["defence_state"].items()):
        lines.append(f"\tDefenceState@{d}: {milli}")
        lines.append(f"\tDefenceStateEvidence@{d}: {result['defence_state_evidence'][d]}")
    for f, q in result["attack_timing"].items():
        lines.append(f"\tAttackTiming@{f}: {', '.join(str(x) for x in q)}")
    for f, q in result["response"].items():
        lines.append(f"\tResponse@{f}: {', '.join(str(x) for x in q)}")
    for (m_, t), v in result["suicide"].items():
        lines.append(f"\tSuicideIndex@{m_}__vs__{t}: {v}")
    return "\n".join(lines) + "\n"


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("dirs", nargs="+", type=pathlib.Path)
    ap.add_argument("--write", type=pathlib.Path, help="write the priors yaml here")
    ap.add_argument("--prev", type=pathlib.Path, default=None,
                    help="previous fitted yaml to anchor on (default: the --write target "
                         "when it exists; PRIORS-CARRY posterior carry-over)")
    ap.add_argument("--no-carry", action="store_true",
                    help="fit without the previous posterior (fresh table, FitVersion 1)")
    ap.add_argument("--repo", type=pathlib.Path, default=REPO, help="repo root holding docs/balance")
    ap.add_argument("--json", action="store_true", help="print the result as JSON")
    args = ap.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")

    profiles, meta, ledger_hash = load_profiles(args.repo)
    tags = sorted({tag for p in profiles.values() for tag, _ in p["weapons"]})
    priors, excluded = versus_priors(args.repo, tags)
    prev = None
    if not args.no_carry:
        prev_path = args.prev
        if prev_path is None and args.write is not None and args.write.exists():
            prev_path = args.write
        prev = load_prev(prev_path)
    result = fit(c.load(args.dirs), profiles, priors, prev=prev, ledger_hash=ledger_hash)
    result["meta"] = meta
    result["excluded_tags"] = excluded
    if args.json:
        print(json.dumps(jsonable(result), indent=2, default=str))
    else:
        print(report(result))
    if args.write:
        args.write.parent.mkdir(parents=True, exist_ok=True)
        args.write.write_text(to_yaml(result, ledger_hash), encoding="utf-8", newline="\n")
        print(f"\nwrote {args.write}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
