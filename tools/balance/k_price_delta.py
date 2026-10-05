#!/usr/bin/env python3
"""k_price_delta.py — roster-wide before/after price table for PRICING-DEFAULT.

Maintainer ruling 2026-10-04: the balance pipeline prices on K-adjusted
`effective_dps` (accuracy, splash, falloff, range, dead zone, reachable
targets) BY DEFAULT instead of raw damage/reload. This tool prices every
class-anchored actor BOTH ways against its class spec and writes the top
movers per faction to `docs/balance/derived/pricing_default_delta.md`, with
the dominant sidecar factor as the reason each unit moved.

It reports only — no yaml or ledger writes. The two columns are internally
consistent because each unit is priced by the same spec anchor in both modes;
`--raw`-style runs elsewhere reproduce the "before" column exactly.

Usage: python tools/balance/k_price_delta.py [--top N] [--out PATH]
"""
from __future__ import annotations

import argparse
import json
import math
import pathlib
import statistics
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/balance"))
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

import fit_class  # noqa: E402
import formula  # noqa: E402
import tier_chain  # noqa: E402
import class_membership  # noqa: E402
import check_band  # noqa: E402

LEDGER = ROOT / "docs" / "balance"
OUT = LEDGER / "derived" / "pricing_default_delta.md"
TOP_N = 10

# Sidecar factor -> (column label, why it moves a price). The dominant factor is
# the one with the largest |ln(factor)| across the unit's priced armaments —
# the single coefficient the K adjustment is mostly paying or charging for.
FACTORS = (
    ("reliability", "accuracy",
     "scatter/travel miss chance at point targets"),
    ("footprint", "splash",
     "damage-weighted blast area catching secondary targets"),
    ("factor_range", "range",
     "outranging or being outranged vs the median weapon"),
    ("factor_targets", "targets",
     "share of the roster the weapon can actually engage"),
    ("factor_deadzone", "deadzone",
     "MinRange annulus the weapon cannot cover"),
    ("overkill", "overkill",
     "per-shot damage wasted past the last needed shot"),
    ("avg_versus", "armor-match",
     "armor-census-weighted Versus profile"),
)


def dominant_factor(darms):
    """[(label, value, detail)] for the priced armament factors furthest from 1.

    `darms` = the unit's derived-sidecar armament rows restricted to the slots
    that price. Weighted by each armament's share of K-adjusted DPS, so the
    reason belongs to the weapon doing the work, not a decorative secondary.
    Returns the top factor plus a runner-up when it is at least 40% as strong —
    a two-coefficient move deserves both names, not a false single cause.
    """
    scored = []
    total_eff = sum(a.get("effective_dps") or 0.0 for a in darms) or 1.0
    for arm in darms:
        share = (arm.get("effective_dps") or 0.0) / total_eff
        if share < 0.25 and len(darms) > 1:
            continue  # secondary armament: never the reason the price moved
        for key, label, detail in FACTORS:
            v = arm.get(key)
            if not isinstance(v, (int, float)):
                continue
            score = abs(math.log(v if key != "footprint" else (1.0 + v / 4.0)))
            if score > 0.02:
                scored.append((score, label, v, detail))
    scored.sort(key=lambda t: -t[0])
    if not scored:
        return []
    top = [scored[0]]
    for cand in scored[1:]:
        if cand[1] != top[0][1] and cand[0] >= 0.4 * top[0][0]:
            top.append(cand)
            break
    return top


def spec_price(inp, spec, anchor_tier):
    """`check_band.price_for`'s spec form: class-baseline price at the anchor's
    relative tier. Returns None for ability-priced classes (dps0 = 0)."""
    if not spec.get("range0_wdist") or not spec.get("dps0"):
        return None
    hp, speed, rng, dps_v, special, _uclass, tier = inp
    rel = tier / anchor_tier if anchor_tier else tier
    return formula.class_baseline_price(
        hp, speed, rng, dps_v,
        spec["hp0"], spec["speed0"], spec["range0_wdist"], spec["dps0"], spec["cost0"],
        special=special, tech_tier=rel)


def delta_rows():
    """Every spec-anchored, combat-eligible actor priced both ways."""
    anchors = {k: v for k, v in
               json.loads((LEDGER / "class_anchors.json").read_text(encoding="utf-8")).items()
               if isinstance(v, dict) and v.get("spec", {}).get("dps0")}
    tier_map = tier_chain.load_derived_map(LEDGER)
    anchor_tiers = {}
    for cls, a in anchors.items():
        aa = a.get("anchor_actor")
        anchor_tiers[cls] = (tier_map.get(aa, {}).get("tier_multiplier", 1.0)
                             if aa and aa in tier_map else (a.get("tech_tier") or 1.0))
    out = []
    for fname, actor, u, du in check_band.collect(tier_map):
        if not check_band.eligible_virtual_member(u):
            continue
        d = u.get("design") or {}
        cls, _reason = class_membership.classify(d)
        if not cls or cls not in anchors:
            continue
        spec = anchors[cls]["spec"]
        try:
            inp_raw, fb_raw = fit_class.unit_inputs(check_band.fitting_unit(u), du, use_k=False)
            inp_k, fb_k = fit_class.unit_inputs(check_band.fitting_unit(u), du, use_k=True)
        except fit_class.PricingScopeError:
            continue
        if inp_raw is None or inp_k is None:
            continue
        pr_raw = spec_price(inp_raw, spec, anchor_tiers[cls])
        pr_k = spec_price(inp_k, spec, anchor_tiers[cls])
        if pr_raw is None or pr_k is None:
            continue
        slots = {(a.get("slot"), a.get("weapon"))
                 for a in (du or {}).get("armaments", [])}
        priced = [a for a in fit_class.pricing_armaments(check_band.fitting_unit(u))
                  if (a.get("slot"), a.get("weapon")) in slots]
        priced_darms = [d for d in (du or {}).get("armaments", [])
                        if (d.get("slot"), d.get("weapon"))
                        in {(a.get("slot"), a.get("weapon")) for a in priced}]
        reasons_from_unpriced = bool(priced) and not priced_darms
        darms = priced_darms or list((du or {}).get("armaments", []))
        prov = any(d.get("model_status") == "provisional"
                   for d in (du or {}).get("armaments", []))
        cost = (u.get("cost") or {}).get("v")
        try:
            cost = float(cost)
        except (TypeError, ValueError):
            continue
        out.append({
            "actor": actor, "faction": pathlib.Path(fname).stem, "cls": cls,
            "cost": cost,
            "raw": pr_raw, "k": pr_k,
            "delta": pr_k - pr_raw,
            "pct": (pr_k / pr_raw - 1.0) if pr_raw else None,
            "reasons": dominant_factor(darms),
            "unpriced_reasons": reasons_from_unpriced,
            "low_rel": [(d.get("weapon"), d.get("reliability"))
                        for d in priced_darms
                        if isinstance(d.get("reliability"), (int, float))
                        and d["reliability"] <= 0.05],
            "fb": fb_k, "prov": prov,
        })
    return out


def reason_text(row):
    if not row["reasons"]:
        return "raw fallback" if row["fb"] else "mixed small factors"
    parts = []
    for _score, label, v, detail in row["reasons"]:
        if label == "splash":
            parts.append(f"splash credit (footprint {v:.2f} cell²)")
            continue
        shown = "<0.01" if v < 0.01 else f"{v:.2f}"
        arrow = "↓" if isinstance(v, (int, float)) and v < 1 else "×"
        parts.append(f"{label} {arrow}{shown} — {detail}")
    return "; ".join(parts)


def suspect_text(row):
    """Data-bug heuristics for the ±25% table — a huge move that no sidecar
    factor explains, or one built on fallback/provisional data, is more likely
    an input defect than a real accuracy/splash/range effect."""
    flags = []
    if row["fb"]:
        flags.append(f"{row['fb']} arm(s) on raw fallback")
    if row["prov"]:
        flags.append("provisional armament model")
    if row.get("unpriced_reasons"):
        flags.append("no priced armament has a sidecar row (term column is indicative only)")
    if row.get("low_rel"):
        arms = ", ".join(f"{w}@{rel:g}" for w, rel in row["low_rel"])
        flags.append(f"near-zero accuracy on {arms} — suspect scatter/sigma input")
    if not row["reasons"]:
        flags.append("UNEXPLAINED — no factor deviates enough to cover the move")
    return "; ".join(flags)


def render_movers_25(rows):
    big = sorted((r for r in rows if abs(r["pct"] or 0.0) > 0.25),
                 key=lambda r: -abs(r["pct"]))
    lines = [
        f"## Every move beyond ±25% ({len(big)} actors)",
        "",
        "Complete list per the maintainer ruling — these rows carry the "
        "maintainer-review `⚠` column: a flag means the move likely reflects a "
        "sidecar INPUT gap rather than a real accuracy/splash/range effect.",
        "",
        "| actor | faction | class | cost | before | after | Δ% | dominant term | ⚠ data-flag |",
        "|---|---|---|---:|---:|---:|---:|---|---|",
    ]
    for r in big:
        reasons = ", ".join(lbl for _s, lbl, _v, _d in r["reasons"]) or "—"
        lines.append(
            f"| `{r['actor']}` | {r['faction']} | {r['cls']} | {r['cost']:.0f} | "
            f"{r['raw']:.0f} | {r['k']:.0f} | {r['pct']:+.1%} | {reasons} | "
            f"{suspect_text(r) or '—'} |")
    lines.append("")
    return lines


def render(rows, top_n=TOP_N):
    lines = [
        "# PRICING-DEFAULT — before/after price delta",
        "",
        "Maintainer ruling 2026-10-04: the pipeline now prices on **K-adjusted "
        "`effective_dps`** (accuracy, splash, falloff, range, dead zone, "
        "reachable targets) instead of raw damage/reload. Every unit priced "
        "below is re-costed by the SAME class spec anchor in both modes — the "
        "delta isolates the K basis change, nothing else.",
        "",
        "Generated by `tools/balance/k_price_delta.py` — read-only; no yaml or "
        "ledger edits. `--raw` on `fit_class`/`check_band`/`propose_class_rebalance` "
        "reproduces the *before* column.",
        "",
    ]
    priced = [r for r in rows if r["pct"] is not None and r["raw"] and r["cost"]]
    med = statistics.median(r["pct"] for r in priced) if priced else 0.0
    toward = sum(1 for r in priced
                 if abs(r["k"] - r["cost"]) < abs(r["raw"] - r["cost"]))
    n_fb = sum(r["fb"] for r in rows)
    n_prov = sum(1 for r in rows if r["prov"])
    lines += [
        f"- **{len(rows)}** actors priced both ways; median price shift "
        f"**{med:+.1%}**; range **{min(r['pct'] for r in priced):+.1%} … "
        f"{max(r['pct'] for r in priced):+.1%}**" if priced else "",
        f"- The new price lands **closer to current cost** for {toward}/{len(priced)} "
        "actors (evidence the coefficient prices a real property, not noise).",
        f"- {n_fb} armament(s) had no sidecar entry and priced on raw fallback; "
        f"{n_prov} actor(s) carry a provisional model_status armament.",
        "",
    ]
    lines += render_movers_25(rows)
    lines.append("## Top movers by faction\n")
    for faction in sorted({r["faction"] for r in rows}):
        fr = [r for r in rows if r["faction"] == faction]
        fr.sort(key=lambda r: -abs(r["pct"] or 0.0))
        top = [r for r in fr if abs(r["pct"] or 0.0) > 0.005][:top_n]
        lines.append(f"### {faction} ({len(fr)} priced)\n")
        if not top:
            lines.append("_No moves above ±0.5%._\n")
            continue
        lines += [
            "| actor | class | cost | before (raw) | after (K) | Δ% | why it moved |",
            "|---|---|---:|---:|---:|---:|---|",
        ]
        for r in top:
            flags = (" †" if r["prov"] else "") + (" ‡" if r["fb"] else "")
            lines.append(
                f"| `{r['actor']}`{flags} | {r['cls']} | {r['cost']:.0f} | "
                f"{r['raw']:.0f} | {r['k']:.0f} | {r['pct']:+.1%} | {reason_text(r)} |")
        lines.append("")
    lines += [
        "† provisional armament model (e.g. unmodeled projectile trajectory).",
        "‡ armament without a derived sidecar entry — priced on raw fallback.",
        "",
    ]
    return "\n".join(lines)


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--top", type=int, default=TOP_N, help="movers per faction")
    ap.add_argument("--out", type=pathlib.Path, default=OUT)
    args = ap.parse_args()
    rows = delta_rows()
    text = render(rows, top_n=args.top)
    args.out.write_text(text, encoding="utf-8", newline="\n")
    print(f"{len(rows)} actors priced both ways -> {args.out}")


if __name__ == "__main__":
    main()
