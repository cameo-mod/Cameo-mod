"""tune_plan_bandits — the offline half of the tier-3 pooled bandits (fleet orders 2026-10-03).

Reads match directories (each a `run_ai_match_batch.py --support-dir`, holding `Logs/`) and folds
every engagement record that carries a `bandit` block (written by PlanBanditBotModule-armed bots:
`scope`, `personality_arm`, `plan_arm`) into per-arm posteriors over the EL score `total_milli`:

1. **Updates** stats = (n, mean, m2) per (bandit, arm) at every level of the scope chain:
   `any` <- `family_<own family>` <- `<own faction>` <- `<own faction>__vs__<enemy faction>`.
   A record whose `bandit.personality_arm` or `bandit.plan_arm` is empty contributes nothing for
   that bandit (a missing arm was never drawn — attributing it would fabricate evidence).
   `bandit.armed` (the "+"-joined set of granted decision-side module conditions, e.g.
   combatveto+inmatchadapt) is carried for survivorship conditioning: `--armed-only SET` folds only
   records produced under that exact armed set and leaves the rest unprocessed for a different fit.
2. **Decays** retained stats by --decay-factor before folding new records (the sliding-window
   discount of the order): n, mean and m2 are all scaled so a stale posterior widens instead of
   just fading.
3. **Writes** (`--write`) `mods/cameo/ai/learned/plan_bandits.yaml` — `<bandit>@<scope>` nodes with
   `<arm>: n mean m2` children, the exact file PlanBanditLearned.Parse reads. Each game is used
   once (the file keeps the processed game ids), so a re-run on the same directories changes
   nothing. Without --write it only reports.

Pure python (stdlib only). The yaml is a committed, reviewed data file: frozen in release,
trained on dev — never written into rules.
"""
from __future__ import annotations

import argparse
import hashlib
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import ai_log_common as c  # noqa: E402

REPO = pathlib.Path(__file__).resolve().parents[2]
LEARNED_PATH = REPO / "mods" / "cameo" / "ai" / "learned" / "plan_bandits.yaml"

BANDITS = ("Personality", "Plan")
DECAY_FACTOR = 0.95  # retained-evidence discount per --write pass (sliding window)
ROOT_KEY = "BotPlanBandits"


# ---------------------------------------------------------------- the learned file

def empty_learned() -> dict:
    return {"stats": {}, "processed": []}


def parse_learned(text: str) -> dict:
    """Read the file this script writes (tab-indented MiniYaml, fixed shape); unknown nodes are ignored."""
    learned = empty_learned()
    scope: tuple | None = None
    in_root = False
    for raw in text.splitlines():
        line = raw.rstrip("\r")
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        depth = len(line) - len(line.lstrip("\t"))
        key, _, value = line.strip().partition(":")
        value = value.strip()
        if depth == 0:
            in_root = key == ROOT_KEY
            scope = None
        elif in_root and depth == 1:
            scope = None
            for bandit in BANDITS:
                if key.startswith(bandit + "@"):
                    scope = (bandit, key[len(bandit) + 1:])
                    learned["stats"].setdefault(scope, {})
            if key == "Processed":
                learned["processed"] = [v for v in value.split(",") if v]
        elif in_root and depth == 2 and scope is not None:
            parts = value.split()
            if len(parts) == 3:
                n, mean, m2 = int(parts[0]), float(parts[1]), float(parts[2])
                learned["stats"][scope][key] = [n, mean, max(0.0, m2)]

    return learned


def render_learned(learned: dict) -> str:
    """The exact MiniYaml shape PlanBanditLearned.Parse reads."""
    lines = [
        "# Tier-3 pooled bandit posteriors (fleet orders 2026-10-03): written ONLY by",
        "# tools/ai/tune_plan_bandits.py --write from dev/harness training runs, reviewed and committed;",
        "# read at match start by PlanBanditBotModule, never written into rules.",
        "#   <bandit>@<scope>: <arm>: n mean m2",
        "#   bandit = Personality | Plan; scope = any | family_<f> | <faction> | <faction>__vs__<enemy>",
        "#   n observations, sample mean and sum of squared deviations of the EL `total_milli` reward.",
        ROOT_KEY + ":",
    ]
    for (bandit, scope), arms in sorted(learned["stats"].items()):
        lines.append(f"\t{bandit}@{scope}:")
        for arm, (n, mean, m2) in sorted(arms.items()):
            lines.append(f"\t\t{arm}: {n} {mean:.2f} {m2:.2f}")
    if learned["processed"]:
        lines.append("\tProcessed: " + ",".join(sorted(set(learned["processed"]))))
    return "\n".join(lines) + "\n"


# ---------------------------------------------------------------- update

def game_key(row: dict) -> str:
    """A short stable id of one bot's game (the file would otherwise list full game uids)."""
    return hashlib.sha1(f"{row['game_uid']}|{row['player']}".encode()).hexdigest()[:10]


def family_of(faction: str) -> str:
    """Mirror of BuildOrderKnobsEval.FamilyOf: the part before the first underscore."""
    return faction.partition("_")[0]


def scope_chain(scope: str) -> list[str]:
    """`own__vs__enemy` (or a bare own scope) -> [matchup, own, family_own, any]; a bare scope stays itself."""
    if "__vs__" in scope:
        own, _, enemy = scope.partition("__vs__")
        return [scope, own, "family_" + family_of(own), "any"]
    return [scope]


def welford_add(stats: list, x: float) -> None:
    n, mean, m2 = stats
    n += 1
    delta = x - mean
    mean += delta / n
    m2 += delta * (x - mean)
    stats[0], stats[1], stats[2] = n, mean, m2


def decay(stats: dict, factor: float) -> None:
    """The sliding-window discount: shrink n, mean and m2 so a stale posterior widens toward the prior."""
    if factor >= 1:
        return
    for arms in stats.values():
        for key in list(arms):
            n, mean, m2 = arms[key]
            n = int(n * factor + 0.5)
            if n <= 0:
                del arms[key]
            else:
                arms[key] = [n, mean * factor, m2 * factor * factor]


def update(rows: list[dict], learned: dict, decay_factor: float = DECAY_FACTOR,
           armed_only: str | None = None) -> dict:
    """Fold unprocessed bandit-attributed engagements into the posteriors.

    armed_only (nova review 2026-10-03): when set, only records whose `bandit.armed` set matches exactly
    are folded; mismatched records are left unprocessed so a differently-filtered pass on another learned
    file still sees them. Armed decision-side modules (combatveto, inmatchadapt, ...) filter which fights
    ever exist — conditioning the fit on the set is how survivorship bias is kept honest.
    Returns {"folded", "skipped", "filtered"}."""
    done = set(learned["processed"])
    def eligible(r: dict) -> bool:
        return r.get("record") == "engagement" and game_key(r) not in done
    fresh = [r for r in rows if r.get("bandit") and eligible(r)
             and (armed_only is None or (r["bandit"].get("armed") or "none") == armed_only)]
    filtered = [r for r in rows if r.get("bandit") and eligible(r)
                and armed_only is not None and (r["bandit"].get("armed") or "none") != armed_only]
    skipped = [r for r in rows if eligible(r) and not r.get("bandit")]
    if not fresh:
        return {"folded": 0, "skipped": len(skipped), "filtered": len(filtered)}

    decay(learned["stats"], decay_factor)
    for r in fresh:
        total = r.get("score", {}).get("total_milli", 0)
        bandit = r["bandit"]
        for name in BANDITS:
            arm = bandit.get("personality_arm" if name == "Personality" else "plan_arm") or ""
            if not arm:
                continue
            for scope in scope_chain(bandit.get("scope") or "any"):
                stats = learned["stats"].setdefault((name, scope), {})
                welford_add(stats.setdefault(arm, [0, 0.0, 0.0]), float(total))
        learned["processed"].append(game_key(r))
        done.add(game_key(r))
    for r in skipped:
        learned["processed"].append(game_key(r))
        done.add(game_key(r))
    # `filtered` rows are deliberately NOT marked processed: a mismatched armed-set belongs to a different fit.
    return {"folded": len(fresh), "skipped": len(skipped), "filtered": len(filtered)}


# ---------------------------------------------------------------- report

def report(learned: dict) -> str:
    lines = ["bandit scope arm n mean m2".replace(" ", "\t")]
    for (bandit, scope), arms in sorted(learned["stats"].items()):
        for arm, (n, mean, m2) in sorted(arms.items()):
            lines.append(f"{bandit}\t{scope}\t{arm}\t{n}\t{mean:.1f}\t{m2:.0f}")
    return "\n".join(lines)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("batch_dirs", nargs="*", type=pathlib.Path)
    ap.add_argument("--learned", type=pathlib.Path, default=LEARNED_PATH,
                    help="the learned file to read (and write with --write)")
    ap.add_argument("--decay", type=float, default=DECAY_FACTOR,
                    help="retained-evidence discount per pass (sliding window); 1 keeps everything")
    ap.add_argument("--write", action="store_true", help="apply the decayed update to the learned file")
    ap.add_argument("--armed-only", default=None, metavar="SET",
                    help="fold only records whose bandit.armed equals SET exactly (e.g. 'combatveto+plan_bandits'; "
                         "'none' for unarmed-set records). Mismatched records stay unprocessed for other fits.")
    args = ap.parse_args()

    rows = c.load(args.batch_dirs)["engagements"] if args.batch_dirs else []
    learned = parse_learned(args.learned.read_text(encoding="utf-8")) if args.learned.exists() else empty_learned()

    result = update(rows, learned, args.decay, armed_only=args.armed_only)
    print(f"folded {result['folded']} bandit-attributed engagements"
          + (f", skipped {result['skipped']} unattributed" if result["skipped"] else "")
          + (f", held {result['filtered']} for a different armed-set" if result["filtered"] else ""))
    print(report(learned))

    if args.write:
        args.learned.parent.mkdir(parents=True, exist_ok=True)
        args.learned.write_text(render_learned(learned), encoding="utf-8")
        print(f"wrote {args.learned}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
