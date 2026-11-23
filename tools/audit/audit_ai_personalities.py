#!/usr/bin/env python3
"""Validate the AI personality wiring.

The five squad-manager instances intentionally duplicate their shared fields.
This audit compares every non-tuning field byte-for-byte and verifies that the
random selector's condition set exactly matches the conditions consumed by the
instances.
"""

from __future__ import annotations

import json
import re
import sys
from fractions import Fraction
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
AI_PATH = ROOT / "mods" / "cameo" / "ai" / "ai.yaml"
CONTROLLER_PATH = ROOT / "OpenRA.Mods.Cameo" / "Traits" / "BotPersonalityController.cs"
PERSONALITIES = ("rush", "turtle", "tech", "expansion", "steamroller", "guerrilla")
CONDITIONS = {f"personality-{name}" for name in PERSONALITIES}
DIFFICULTIES = ("easiest", "veryeasy", "easy", "medium", "hard", "veryhard", "brutal", "challenger", "unbeatable", "god")
# TIER-01 (TABLE_03_R2 079A06F2, Opus ruling 4: proposal values are the target): easiest..god PersonalityReactionDelay.
REACTION_DELAYS = (5500, 5000, 4500, 4000, 3500, 3000, 2500, 2000, 1500, 1000)
TUNING_FIELDS = {
    "MinimumAttackForceDelay",
    "SquadSize",
    "SquadSizeRandomBonus",
    "SquadValue",
    "SquadValueMaxEarlyBonus",
    "SquadValueMinLateBonus",
    "SquadValueMaxLateBonus",
    "MaxIdleUnits",
    "AttackForceInterval",
    "JoinGuerrilla",
    "MaxGuerrillaSize",
    "DangerScanRadius",
    "ProtectionScanRadius",
    "ProtectUnitScanRadius",
    "IdleScanRadius",
    "MaxBaseRadius",
    # Personality-identity fields, allow-listed when guerrilla landed (2026-09-28):
    # its harassment semantics require no staging/rally and off-axis routes,
    # which the prior uniform values could not express.
    "PreferMainTarget",
    "StageBeforeAssault",
    "IndirectRouteChance",
    "HighValueTargetPriority",
    "HarasserTypes",
    # CA-3 (AI_ARCHITECTURE 12.5): the role mix IS the personality flavour -
    # compositions differ by design, and the stage/composition gate patience
    # scales with the personality's tempo.
    "RoleMix",
    "RoleMixRoleFloorPct",
    "StageCompositionTicks",
    "StageRequiredRoles",
    # CA-4 (AI_ARCHITECTURE 12.7): formation movement knobs; whether a personality
    # marches in formation and how far the vanguard may lead is its tempo.
    "FormationMovement",
    "FormationTrailCells",
    "FormationMaxLeadCells",
    "FormationMaxStalledLeadCells",
    # Fast-squad count per personality and game time (maintainer 2026-09-28, DESIGN §19.1a):
    # guerrilla at least twice steamroller's, which spends its units on the main army.
    "MaxGuerrillaSquads",
    "MaxGuerrillaSquadsLate",
    # AggTeam values are per-personality by design (672f2d7dc, PLAYTEST-PORT ARM10 port).
    # AggTeamTest.cs pins them: the reserve share differs per personality (lines 858-861);
    # turtle alone sets the push thresholds, every other personality is 0 (lines 868-873).
    # StalemateSteamrollerProfile is the steamroller identity (default off).
    "AggTeamReserveSharePercent",
    "AggTeamPushArmyValue",
    "AggTeamPushLateTick",
    "StalemateSteamrollerProfile",
}
DEAD_FIELDS = {"RushInterval", "RushAttackScanRadius"}


def lines_for_block(lines: list[str], header: str) -> list[str]:
    for index, line in enumerate(lines):
        if line == f"\t{header}:":
            end = len(lines)
            for candidate in range(index + 1, len(lines)):
                if re.match(r"^\t[A-Za-z0-9_^`-]+(?:@[^:]+)?:", lines[candidate]):
                    end = candidate
                    break
            return lines[index:end]
    return []


def root_block(lines: list[str], header: str) -> list[str]:
    for index, line in enumerate(lines):
        if line == f"{header}:":
            end = len(lines)
            for candidate in range(index + 1, len(lines)):
                if lines[candidate] and not lines[candidate].startswith(("\t", " ")):
                    end = candidate
                    break
            return lines[index:end]
    return []


def field_blocks(block: list[str]) -> dict[str, str]:
    fields: dict[str, str] = {}
    current_name = None
    current: list[str] = []
    for line in block[1:]:
        match = re.match(r"^\t\t([^:\s]+):", line)
        if match:
            if current_name is not None:
                fields[current_name] = "\n".join(current)
            current_name = match.group(1)
            current = [line]
        elif current_name is not None:
            # Comment-only lines must not become part of the previous field's value —
            # a documented field would read as "differs"/"incorrect RequiresCondition"
            # the moment a comment lands under it (2026-10-02 false FAIL on all six
            # personalities).
            if not line.lstrip().startswith("#"):
                current.append(line)
    if current_name is not None:
        fields[current_name] = "\n".join(current)
    return fields


def condition_values(block: list[str]) -> set[str]:
    for line in block:
        if line.startswith("\t\tConditions:"):
            return {value.strip() for value in line.split(":", 1)[1].split(",") if value.strip()}
    return set()


def controller_conditions() -> list[str]:
    source = CONTROLLER_PATH.read_text(encoding="utf-8")
    match = re.search(
        r"public readonly string\[\] Conditions\s*=\s*\{(?P<body>.*?)\};",
        source,
        re.DOTALL,
    )
    if match is None:
        return []
    return re.findall(r'"([^"]+)"', match.group("body"))


def notification_blocks(block: list[str]) -> tuple[dict[str, list[str]], set[str]]:
    blocks: dict[str, list[str]] = {}
    duplicates: set[str] = set()
    for line in block:
        match = re.match(r"^\tObserverConditionNotification@([^:]+):$", line)
        if not match:
            continue

        name = match.group(1)
        if name in blocks:
            duplicates.add(name)
            continue

        blocks[name] = lines_for_block(block, f"ObserverConditionNotification@{name}")

    return blocks, duplicates


BOOLEAN_LIMIT_FIELDS = {"PrioritizeBarracksBeforeRefinery"}
PRODUCTION_TIERS = ("easiest", "veryeasy", "easy", "medium", "hard", "veryhard", "brutal", "challenger", "unbeatable", "cameogod")
FAIR_TIER = "hard"

# DESIGN §19.1 (binding, maintainer 2026-09-28): every module runs on EVERY difficulty and only
# its strength scales. A `RequiresCondition` that names a difficulty condition is a tier gate and
# is forbidden — difficulty-selective behaviour belongs in BotLimits fields, not in which modules
# exist.
TIER_CONDITIONS = {
    "noobbot", "easiestbot", "veryeasybot", "easybot", "mediumbot", "hardbot",
    "veryhardbot", "brutalbot", "challengerbot", "unbeatablebot", "cameogodbot",
}
# Structural nodes that legitimately reference a tier condition: the BotLimits ladder IS the
# strength scale, ProvidesPrerequisite exports each tier's `Xbotplayer` token, and
# GrantConditionOnBotOwner defines the tier conditions themselves.
TIER_GATE_ALLOW_PREFIXES = ("BotLimits@", "ProvidesPrerequisite@", "GrantConditionOnBotOwner@")


def on_line(values: list[int]) -> bool:
    """Equal steps from the first tier to the last; an integer may sit within 0.5 of the line (DESIGN §19.1).
    The half-step tolerance exists only because tier numbers are whole numbers (the engine rounds the line), so a
    non-integer value is never on the line (LADDER-AUDIT-02 LOW, Code Monkey)."""
    if any(Fraction(v).denominator != 1 for v in values):
        return False
    n = len(values) - 1
    return all(2 * abs(n * (v - values[0]) - (values[-1] - values[0]) * i) <= n for i, v in enumerate(values))


def whole_values(path, raw, failures):
    """The tier values as ints, or None after recording why not: a fractional or non-numeric tier value is a
    failure row, never a crash and never a silent int() truncation."""
    out = []
    for value in raw:
        try:
            number = Fraction(str(value).strip())
        except (ValueError, ZeroDivisionError):
            failures.append(f"{path} has a non-numeric tier value {value!r}")
            return None
        if number.denominator != 1:
            failures.append(f"{path} has a non-integer tier value {value!r}: tier numbers are whole numbers")
            return None
        out.append(int(number))
    return out


LADDER_TIERS = ("noob", *PRODUCTION_TIERS)
TIER_NAMES = {tier: tier for tier in LADDER_TIERS} | {"god": "cameogod"}
for tier in LADDER_TIERS:
    TIER_NAMES[f"{tier}bot"] = tier
    TIER_NAMES[f"{tier}botplayer"] = tier

ZERO_ALLOWLIST = Path(__file__).with_name("ai_ladder_legal_zeros.json")
NUMBER = re.compile(r"[+-]?(?:\d+(?:\.\d*)?|\.\d+)")
BOOLEANS = {"true", "false"}


def check_ladder_row(path, values, legal_zeros):
    present = [value.strip() for value in values if value is not None]
    missing = [tier for tier, value in zip(LADDER_TIERS, values) if value is None]
    if missing:
        return [f"{path} missing on {', '.join(missing)}"]
    types = {value.lower() for value in present}
    if types & BOOLEANS or path.rsplit(".", 1)[-1].startswith("Use"):
        return [] if len(types) == 1 and types <= BOOLEANS else [f"{path} inconsistent boolean: {present}"]
    if not any(NUMBER.fullmatch(value) for value in present):
        return []
    if not all(NUMBER.fullmatch(value) for value in present):
        return [f"{path} mixes numeric and nonnumeric values: {present}"]
    numbers = [Fraction(value) for value in present]
    failures = []
    fractional = [value for value, number in zip(present, numbers) if number.denominator != 1]
    if fractional:
        failures.append(f"{path} has non-integer tier values {fractional}: tier numbers are whole numbers")
    elif not on_line(numbers):
        failures.append(f"{path} is not on one equal-step line: {present}")
    if len(set(numbers)) > 1 and (numbers[0] == 0 or numbers[-1] == 0) and not legal_zeros.get(path, "").strip():
        failures.append(f"{path} has a zero endpoint without a cited allowance")
    return failures


def scalar_fields(node, prefix=""):
    for child in node.children:
        path = f"{prefix}.{child.key}" if prefix else child.key
        if child.children:
            yield from scalar_fields(child, path)
        elif child.key not in {"RequiresCondition", "Prerequisites"}:
            yield path, child.value


def ladder_failures(roots, legal_zeros):
    failures = []
    all_tiers = set(LADDER_TIERS)
    for root in roots:
        if root is None:
            continue
        families = {}
        grants = {}
        foreign = {}
        for child in root.children:
            family, _, suffix = child.key.partition("@")
            if family == "GrantConditionOnBotOwner":
                condition = child.get("Condition")
                if condition and condition not in TIER_NAMES:
                    bots = {bot.strip() for bot in (child.get("Bots") or "").split(",")}
                    grants.setdefault(condition, set()).update(bots & all_tiers)
            elif suffix in TIER_NAMES and family not in {"ProvidesPrerequisite", "ModularBot"}:
                variants = families.setdefault(family, {})
                tier = TIER_NAMES[suffix]
                if tier in variants:
                    failures.append(f"{root.key}.{family} has duplicate forms for {tier}")
                variants[tier] = dict(scalar_fields(child))
            elif suffix and family not in {"ProvidesPrerequisite", "ModularBot"}:
                foreign.setdefault(family, []).append(suffix)

        # A family that has tier forms is a ladder family: a sibling whose suffix is not a tier name (a typo such as
        # `@hrad`, or `@brutalbot2`) would otherwise be skipped without a word (LADDER-AUDIT-02 LOW, Code Monkey).
        for family, suffixes in sorted(foreign.items()):
            if family in families:
                failures.append(f"{root.key}.{family} has suffixed forms that are not tier names: {sorted(suffixes)}")

        for condition, enabled in sorted(grants.items()):
            if enabled and enabled != all_tiers:
                missing = ", ".join(t for t in LADDER_TIERS if t not in enabled)
                failures.append(f"{root.key}.{condition} grant missing on {missing}")
        for family, variants in sorted(families.items()):
            fields = sorted({field for variant in variants.values() for field in variant})
            for field in fields:
                values = [variants.get(tier, {}).get(field) for tier in LADDER_TIERS]
                failures.extend(check_ladder_row(f"{root.key}.{family}.{field}", values, legal_zeros))
    return failures


def siege_ladder_failures() -> list[str]:
    """SIEGE-FIX-01 S8 (DESIGN 19.1): the siege evaluator has no on/off switch, and its two graded knobs lie on one equal-step line
    over the 11 tiers (noob = easiest minus one step): StandOffMaxTicks 3000 - 150 r, SiegeReengagePct 40 - 2 r, HysteresisBandPct 25 - r, r = 0 (noob) .. 10
    (cameogod), no zero endpoint. Min* is written for the easiest tier, Max* for cameogod (BotDifficultyLadder.Scale interpolates)."""
    text = (ROOT / "mods" / "cameo" / "ai" / "ai.yaml").read_text(encoding="utf-8")
    match = re.search(r"^\tSiegeEvaluatorBotModule:\n((?:\t\t.*\n|[ \t]*\n)+)", text, re.MULTILINE)
    if not match:
        return ["SiegeEvaluatorBotModule block not found in ai.yaml"]

    block = match.group(1)
    failures = []
    if "BehaviourEnabled" in block:
        failures.append("SiegeEvaluatorBotModule.BehaviourEnabled must not exist: no ladder tier may switch a feature off (SIEGE-FIX-01 S8)")

    knobs = {}
    for key in ("StandOffMaxTicksEasy", "StandOffMaxTicksHard", "SiegeReengagePctEasy", "SiegeReengagePctHard",
                "HysteresisBandPctEasy", "HysteresisBandPctHard"):
        found = re.search(rf"^\t\t{key}:[ \t]*(\d+)[ \t]*$", block, re.MULTILINE)
        if not found:
            failures.append(f"SiegeEvaluatorBotModule.{key} is not written in ai.yaml")
        else:
            knobs[key] = int(found.group(1))

    if failures:
        return failures

    steps = 9  # easiest = rank 0 .. cameogod = rank 9; noob = rank -1
    for name, lo, hi, base, per_rank in (
        ("StandOffMaxTicks", knobs["StandOffMaxTicksEasy"], knobs["StandOffMaxTicksHard"], 3000, -150),
        ("SiegeReengagePct", knobs["SiegeReengagePctEasy"], knobs["SiegeReengagePctHard"], 40, -2),
        ("HysteresisBandPct", knobs["HysteresisBandPctEasy"], knobs["HysteresisBandPctHard"], 25, -1),
    ):
        # C# int arithmetic: min + ((max - min) * rank) / steps, truncated toward zero.
        values = [lo + int(((hi - lo) * rank) / steps) for rank in range(-1, steps + 1)]
        expected = [base + per_rank * r for r in range(0, 11)]
        if values != expected:
            failures.append(f"{name} tiers {values} != the ratified line {expected}")
        if min(values) <= 0:
            failures.append(f"{name} reaches zero or below: {values}")

    return failures


def engdrop_ladder_failures() -> list[str]:
    """ENGDROP (DESIGN 19.1): Trigger B lies on one equal-step line over the 11 tiers (noob = easiest minus one step): chance 50 + 5 r %,
    interval 525 - 30 r ticks, initial delay 675 - 60 r ticks, r = 0 (noob) .. 10 (cameogod). The scheduler is on (UseDropScheduler) and no tier switches
    it off. Easy is written for the easiest tier, Hard for cameogod (BotDifficultyLadder.Scale interpolates)."""
    text = (ROOT / "mods" / "cameo" / "ai" / "ai.yaml").read_text(encoding="utf-8")
    match = re.search(r"^\tEngineerBotModule:\n((?:\t\t.*\n|[ \t]*\n)+)", text, re.MULTILINE)
    if not match:
        return ["EngineerBotModule block not found in ai.yaml"]

    block = match.group(1)
    failures = []
    if not re.search(r"^\t\tUseDropScheduler:[ \t]*true[ \t]*$", block, re.MULTILINE):
        failures.append("EngineerBotModule.UseDropScheduler must be true (ENGDROP: no ladder tier may switch a feature off)")

    knobs = {}
    for key in ("DropChancePctEasy", "DropChancePctHard", "DropIntervalTicksEasy", "DropIntervalTicksHard",
                "DropInitialDelayTicksEasy", "DropInitialDelayTicksHard"):
        found = re.search(rf"^\t\t{key}:[ \t]*(\d+)[ \t]*$", block, re.MULTILINE)
        if not found:
            failures.append(f"EngineerBotModule.{key} is not written in ai.yaml")
        else:
            knobs[key] = int(found.group(1))

    if failures:
        return failures

    steps = 9  # easiest = rank 0 .. cameogod = rank 9; noob = rank -1
    for name, lo, hi, base, per_rank in (
        ("DropChancePct", knobs["DropChancePctEasy"], knobs["DropChancePctHard"], 50, 5),
        ("DropIntervalTicks", knobs["DropIntervalTicksEasy"], knobs["DropIntervalTicksHard"], 525, -30),
        ("DropInitialDelayTicks", knobs["DropInitialDelayTicksEasy"], knobs["DropInitialDelayTicksHard"], 675, -60),
    ):
        # C# int arithmetic: min + ((max - min) * rank) / steps, truncated toward zero.
        values = [lo + int(((hi - lo) * rank) / steps) for rank in range(-1, steps + 1)]
        expected = [base + per_rank * r for r in range(0, 11)]
        if values != expected:
            failures.append(f"{name} tiers {values} != the ratified line {expected}")
        if min(values) <= 0:
            failures.append(f"{name} reaches zero or below: {values}")

    return failures


# WAVE-MIN-01 (USER rule, 2026-11-23): an attack wave launches only with >= N units AND >= V summed unit cost. One line over all 11
# tiers, noob (rank 0, no limit) to cameogod (rank 10): N = 2.4 r rounded half up, V = 1200 r. The generic ten-tier check above
# starts at easiest and would reject the rounded 2.4 line, so these two fields are held to the exact 11-tier table instead.
WAVE_MIN_LINES = {
    "WaveMinUnits": lambda rank: (24 * rank + 5) // 10,
    "WaveMinValue": lambda rank: 1200 * rank,
}
WAVE_MIN_TIER_KEYS = ("noob", *DIFFICULTIES)


def wave_min_line_failures(limits) -> list[str]:
    failures = []
    for field, line in WAVE_MIN_LINES.items():
        raw = [limits[t].get(field) if t in limits else None for t in WAVE_MIN_TIER_KEYS]
        missing = [t for t, v in zip(WAVE_MIN_TIER_KEYS, raw) if v is None]
        if missing:
            failures.append(f"BotLimits.{field} is not written for {missing}: WAVE-MIN-01 writes it in every tier")
            continue
        values = whole_values(f"BotLimits.{field}", raw, failures)
        expected = [line(rank) for rank in range(len(WAVE_MIN_TIER_KEYS))]
        if values is not None and values != expected:
            failures.append(f"BotLimits.{field} {values} != the WAVE-MIN-01 line {expected}")
    return failures


def difficulty_scale_failures() -> list[str]:
    """DESIGN §19.1: every per-tier number lies on one straight line in equal steps, and is written in
    every tier (a missing field falls back to a C# default and breaks the line silently)."""
    sys.path.insert(0, str(ROOT / "tools" / "audit"))
    from miniyaml import Ruleset

    rules = Ruleset(ROOT)
    failures = []
    player = rules.resolve("player")
    limits = {c.key.split("@", 1)[1]: c for c in player.children if c.key.startswith("BotLimits@")}
    if [t for t in DIFFICULTIES if t in limits] != list(DIFFICULTIES):
        return [f"resolved BotLimits tiers {sorted(limits)} != {list(DIFFICULTIES)}"]

    fields = sorted({c.key for block in limits.values() for c in block.children} - {"RequiresCondition"} - BOOLEAN_LIMIT_FIELDS)
    for field in fields:
        if field in WAVE_MIN_LINES:
            continue  # WAVE-MIN-01: its own exact 11-tier line (noob included), checked below
        raw = [limits[t].get(field) for t in DIFFICULTIES]
        missing = [t for t, v in zip(DIFFICULTIES, raw) if v is None]
        if missing:
            failures.append(f"BotLimits.{field} is not written for {missing}: that tier silently uses the C# default")
            continue
        values = whole_values(f"BotLimits.{field}", raw, failures)
        if values is not None and not on_line(values):
            failures.append(f"BotLimits.{field} is not on one equal-step line: {values}")

    failures.extend(wave_min_line_failures(limits))

    behavior = rules.resolve("^BotProductionBehavior")
    for trait in ("ProductionTimeMultiplier", "ProductionCostMultiplier"):
        nodes = [behavior.child(f"{trait}@{t}botplayer") if behavior else None for t in PRODUCTION_TIERS]
        if any(n is None for n in nodes):
            failures.append(f"^BotProductionBehavior.{trait} is missing a tier")
            continue
        values = whole_values(f"^BotProductionBehavior.{trait}", [n.get("Multiplier") for n in nodes], failures)
        if values is None:
            continue
        if not on_line(values):
            failures.append(f"{trait} is not on one equal-step line: {values}")
        fair = values[PRODUCTION_TIERS.index(FAIR_TIER)]
        if fair != 100:
            failures.append(f"{trait}: the fair tier `{FAIR_TIER}` must be 100, is {fair}")
    legal_zeros = json.loads(ZERO_ALLOWLIST.read_text(encoding="utf-8"))
    failures.extend(ladder_failures((player, behavior), legal_zeros))
    return failures


SCALE_FIELDS = ("Min", "Max", "RatioMin", "RatioMax", "Margin", "Growth", "Floor", "TurtleRushLean", "TechRushExpansionLean")
SCALE_CATEGORIES = ("army", "harvester", "refinery", "production", "conyard", "tech", "superweapon", "defence", "aircraft")
# category -> the BotLimits number its minute-0, nothing-seen line must reproduce (DESIGN 19.1 table).
SCALE_VS_LIMITS = {
    "refinery": "RefineryLimit",
    # harvester: removed (USER RULING 2026-11-08). HarvesterLimit is 0 on all tiers: the value cap replaces the count
    # limit, so the ScaleTargets harvester line has no BotLimits count to match (Form B, later item).
    "production": "ProductionTypeLimit",
    "conyard": "ConstructionYardLimit",
}
TECH_LINE = [1, 1, 1, 1, 2, 2, 2, 2, 3, 3]


def scale_line(lo: int, hi: int, steps: int = 10) -> list[int]:
    """The synced math, in Python: thousandths, linear on tier/9, ONE floor at the end (ScaleTargetsEval)."""
    return [(lo + (hi - lo) * t // (steps - 1)) // 1000 for t in range(steps)]


def scale_target_failures() -> list[str]:
    """AI_ARCHITECTURE 12.22 / DESIGN 19.10: every ScaleTargetsBotModule category writes every field, Min <= Max, the tech
    line floors to 1 1 1 1 2 2 2 2 3 3, and at minute 0 with nothing seen each line equals the DESIGN 19.1 table."""
    sys.path.insert(0, str(ROOT / "tools" / "audit"))
    from miniyaml import Ruleset

    rules = Ruleset(ROOT)
    player = rules.resolve("player")
    module = player.child("ScaleTargetsBotModule") if player else None
    if module is None:
        return ["Player has no ScaleTargetsBotModule block (AI_ARCHITECTURE 12.22)"]

    failures = []
    categories = module.child("Categories")
    if categories is None:
        return ["ScaleTargetsBotModule has no Categories block"]

    written = {c.key: c for c in categories.children}
    for name in SCALE_CATEGORIES:
        if name not in written:
            failures.append(f"ScaleTargetsBotModule.Categories.{name} is missing: every category is written (DESIGN 19.1)")
    for name in sorted(set(written) - set(SCALE_CATEGORIES)):
        failures.append(f"ScaleTargetsBotModule.Categories.{name} is not a known category")

    values: dict[str, dict[str, int]] = {}
    for name in SCALE_CATEGORIES:
        node = written.get(name)
        if node is None:
            continue
        missing = [f for f in SCALE_FIELDS if node.get(f) is None]
        if missing:
            failures.append(f"ScaleTargetsBotModule.Categories.{name} does not write {missing}: the field silently uses the C# default")
            continue
        try:
            values[name] = {f: int(node.get(f)) for f in SCALE_FIELDS}
        except ValueError:
            failures.append(f"ScaleTargetsBotModule.Categories.{name}: every field is an integer in thousandths")
            continue
        if values[name]["Min"] > values[name]["Max"]:
            failures.append(f"ScaleTargetsBotModule.Categories.{name}: Min {values[name]['Min']} is above Max {values[name]['Max']}")
        if values[name]["RatioMin"] > values[name]["RatioMax"]:
            failures.append(f"ScaleTargetsBotModule.Categories.{name}: RatioMin is above RatioMax")

    if "tech" in values:
        line = scale_line(values["tech"]["Min"], values["tech"]["Max"])
        if line != TECH_LINE:
            failures.append(f"ScaleTargetsBotModule tech line floors to {line}, not {TECH_LINE}")

    limits = {c.key.split("@", 1)[1]: c for c in player.children if c.key.startswith("BotLimits@")}
    for category, field in SCALE_VS_LIMITS.items():
        if category not in values or [t for t in DIFFICULTIES if t in limits] != list(DIFFICULTIES):
            continue
        expected = [int(limits[t].get(field)) for t in DIFFICULTIES]
        line = scale_line(values[category]["Min"], values[category]["Max"])
        if line != expected:
            failures.append(f"ScaleTargetsBotModule {category}: minute-0 nothing-seen line {line} != BotLimits.{field} {expected} (DESIGN 19.1)")
    return failures


def tier_gate_failures() -> list[str]:
    """DESIGN §19.1: no module is switched on per tier. Any `RequiresCondition` on the resolved
    Player that names a difficulty condition is a violation — the module either runs everywhere
    (strength scaled through BotLimits / per-tier fields) or it is removed."""
    sys.path.insert(0, str(ROOT / "tools" / "audit"))
    from miniyaml import Ruleset

    rules = Ruleset(ROOT)
    failures = []
    for node in rules.resolve("player").children:
        if node.key.startswith(TIER_GATE_ALLOW_PREFIXES):
            continue
        condition = node.get("RequiresCondition")
        if not condition:
            continue
        tiers = sorted(set(re.findall(r"[A-Za-z0-9_-]+", condition)) & TIER_CONDITIONS)
        if tiers:
            failures.append(
                f"{node.key} gates on tier condition(s) {tiers} — DESIGN 19.1: modules run on "
                f"every difficulty; scale strength via BotLimits/per-tier fields, not RequiresCondition"
            )
    return failures


NOOB_TIER = "noob"


def noob_one_step(easiest: int, god: int, floor: int) -> int:
    """NOOB-TIER-01 rule: one linear step below easiest, extrapolated on the easiest..god line (rank -1, C#
    truncating division over 9 steps). Where that is below the floor (1 for *Limit fields, 0 otherwise),
    noob = easiest."""
    value = easiest + int((god - easiest) * -1 / 9)
    return value if value >= floor else easiest


def noob_failures(lines: list[str]) -> list[str]:
    """NOOB-TIER-01 (user ruling, final). Documented noob check, separate from the easiest..god checks:
    - every numeric BotLimits field of noob equals noob_one_step(easiest, god) with the floor rule;
    - noob's RequiresCondition is noobbot and its wiring exists (GrantConditionOnBotOwner@noob, ModularBot@NoobAI);
    - the one-step ScaleTargets line of noob is not below 0 (a negative value would need the easiest fallback)."""
    failures: list[str] = []
    noob = field_blocks(lines_for_block(lines, f"BotLimits@{NOOB_TIER}"))
    easiest = field_blocks(lines_for_block(lines, "BotLimits@easiest"))
    god = field_blocks(lines_for_block(lines, "BotLimits@god"))
    if not noob or not easiest or not god:
        return ["noob, easiest or god BotLimits block missing"]

    def value(fields: dict[str, str], name: str) -> str:
        return fields[name].split(":", 1)[1].strip()

    for name in easiest:
        if name == "RequiresCondition":
            if value(noob, name) != "noobbot":
                failures.append(f"noob RequiresCondition is {value(noob, name)!r}, expected noobbot")
            continue
        e_text = value(easiest, name)
        if not e_text.lstrip("-").isdigit():
            continue  # booleans: checked by the existing order and tier-gate checks
        e, g = int(e_text), int(value(god, name))
        # TIER-01: CapturerInfantryLimitPct is a percent, not a count; its noob value is the proposal's first value 0 (Opus ruling 4).
        floor = 0 if name == "CapturerInfantryLimitPct" else (1 if "Limit" in name else 0)
        expected = noob_one_step(e, g, floor)
        got = value(noob, name) if name in noob else None
        if got != str(expected):
            failures.append(f"noob {name} is {got}, the rule gives {expected} (easiest {e}, god {g})")

    if "\tGrantConditionOnBotOwner@noob:" not in lines or "\tModularBot@NoobAI:" not in lines:
        failures.append("noob wiring missing (GrantConditionOnBotOwner@noob or ModularBot@NoobAI)")

    start = lines.index("\tScaleTargetsBotModule:")
    cats = lines.index("\t\tCategories:", start)
    category = None
    bounds: dict[str, dict[str, int]] = {}
    for line in lines[cats + 1:]:
        if line.strip() and len(line) - len(line.lstrip("\t")) <= 2:
            break
        m = re.match(r"^\t\t\t([a-z]+):$", line)
        if m:
            category = m.group(1)
            bounds[category] = {}
            continue
        m = re.match(r"^\t\t\t\t(Min|Max|RatioMin|RatioMax): (\d+)$", line)
        if m and category:
            bounds[category][m.group(1)] = int(m.group(2))
    for cat, b in bounds.items():
        for lo, hi in (("Min", "Max"), ("RatioMin", "RatioMax")):
            if lo in b and hi in b:
                noob_line = b[lo] + int((b[hi] - b[lo]) * -1 / 9)
                if noob_line < 0:
                    failures.append(f"ScaleTargets {cat} {lo}: noob line {noob_line} < 0 (needs the easiest fallback)")
    return failures


def main() -> int:
    lines = AI_PATH.read_text(encoding="utf-8").splitlines()
    failures: list[str] = []

    player = root_block(lines, "Player")
    if "\tInherits@aidifficulties: ^AIDifficulties" not in player:
        failures.append("Player does not inherit ^AIDifficulties")

    selector = lines_for_block(lines, "GrantRandomCondition@personality")
    controller = lines_for_block(player, "BotPersonalityController")
    source_conditions = controller_conditions()
    granted = condition_values(selector) if selector else set(source_conditions)
    if selector:
        failures.append("legacy GrantRandomCondition@personality remains")
    if not controller:
        failures.append("Player does not wire BotPersonalityController")
    if not source_conditions or len(source_conditions) != len(set(source_conditions)) or set(source_conditions) != CONDITIONS:
        failures.append(f"controller conditions {sorted(set(source_conditions))} != {sorted(CONDITIONS)}")

    # NOOB-TIER-01: the equal-step order is checked over easiest..god exactly as at base; noob is checked
    # by noob_failures() below (its own documented rule).
    bot_limits = [
        match.group(1)
        for line in lines
        if (match := re.match(r"^\tBotLimits@([^:]+):$", line)) and match.group(1) != NOOB_TIER
    ]
    if bot_limits != list(DIFFICULTIES):
        failures.append(f"BotLimits difficulty order {bot_limits} != {list(DIFFICULTIES)}")
    for name, expected_delay in zip(bot_limits, REACTION_DELAYS):
        block = lines_for_block(lines, f"BotLimits@{name}")
        fields = field_blocks(block)
        expected = f"PersonalityReactionDelay: {expected_delay}"
        if fields.get("PersonalityReactionDelay", "").strip() != expected:
            failures.append(f"{name} has incorrect PersonalityReactionDelay")

    blocks = {
        name: lines_for_block(lines, f"SquadManagerBotModuleCA@{name}")
        for name in PERSONALITIES
    }
    missing = [name for name, block in blocks.items() if not block]
    if missing:
        failures.append(f"missing personality blocks: {', '.join(missing)}")

    if "\tSquadManagerBotModuleCA@generic:" in lines:
        failures.append("legacy SquadManagerBotModuleCA@generic block remains")

    notification_blocks_by_name, duplicate_notifications = notification_blocks(player)
    expected_notification_names = {f"personality-{name}" for name in PERSONALITIES}
    notification_names = set(notification_blocks_by_name)
    missing_notifications = expected_notification_names - notification_names
    orphan_notifications = notification_names - expected_notification_names
    if missing_notifications:
        failures.append(f"missing personality notifications: {', '.join(sorted(missing_notifications))}")
    if orphan_notifications:
        failures.append(f"orphan personality notifications: {', '.join(sorted(orphan_notifications))}")
    if duplicate_notifications:
        failures.append(f"duplicate personality notifications: {', '.join(sorted(duplicate_notifications))}")

    for name in PERSONALITIES:
        notification = notification_blocks_by_name.get(f"personality-{name}")
        if notification is None:
            continue

        fields = field_blocks(notification)
        required = f"genericbot && personality-{name}"
        if fields.get("RequiresCondition", "").strip() != f"RequiresCondition: {required}":
            failures.append(f"{name} notification has incorrect RequiresCondition")
        expected_notification = f"Notification: notification-bot-personality-{name}"
        if fields.get("Notification", "").strip() != expected_notification:
            failures.append(f"{name} notification has incorrect Notification")

    consumed = set()
    parsed_fields = {}
    for name, block in blocks.items():
        fields = field_blocks(block)
        parsed_fields[name] = fields
        required = f"genericbot && personality-{name}"
        if fields.get("RequiresCondition", "").strip() != f"RequiresCondition: {required}":
            failures.append(f"{name} has incorrect RequiresCondition")
        consumed.add(f"personality-{name}")
        dead = DEAD_FIELDS.intersection(fields)
        if dead:
            failures.append(f"{name} retains dead fields: {', '.join(sorted(dead))}")

    if consumed != set(source_conditions):
        failures.append(f"consumed conditions {sorted(consumed)} != {sorted(set(source_conditions))}")

    if parsed_fields:
        reference_name = PERSONALITIES[0]
        reference = parsed_fields[reference_name]
        for name in PERSONALITIES[1:]:
            current = parsed_fields[name]
            shared_names = (set(reference) | set(current)) - TUNING_FIELDS - {"RequiresCondition"}
            for field in sorted(shared_names):
                if reference.get(field, "").rstrip("\n") != current.get(field, "").rstrip("\n"):
                    failures.append(f"shared field {field} differs between {reference_name} and {name}")
            if (set(reference) - TUNING_FIELDS - {"RequiresCondition"}) != (
                set(current) - TUNING_FIELDS - {"RequiresCondition"}
            ):
                failures.append(f"shared field set differs between {reference_name} and {name}")

    failures.extend(difficulty_scale_failures())
    failures.extend(engdrop_ladder_failures())
    failures.extend(siege_ladder_failures())
    failures.extend(noob_failures(lines))
    failures.extend(scale_target_failures())
    failures.extend(tier_gate_failures())

    print("# AI personality audit")
    print()
    print(f"- Selector conditions: `{', '.join(sorted(granted))}`")
    print(f"- Consumed conditions: `{', '.join(sorted(consumed))}`")
    print(f"- Personality blocks: {len([block for block in blocks.values() if block])}/{len(PERSONALITIES)}")
    print(f"- Personality notifications: {len(notification_names)}/{len(PERSONALITIES)}")
    print(f"- BotLimits reaction delays: `{', '.join(str(delay) for delay in REACTION_DELAYS)}`")
    print(f"- Explicit tuning allow-list: `{', '.join(sorted(TUNING_FIELDS))}`")
    print()
    if failures:
        print("## FAIL")
        for failure in failures:
            print(f"- {failure}")
        return 1
    print("## PASS")
    print("- Shared non-tuning fields are byte-identical across all personality instances.")
    print("- BotPersonalityController and squad-manager condition sets match exactly.")
    print("- Personality conditions have exactly one matching notification block each.")
    print("- No dead RushInterval/RushAttackScanRadius keys remain.")
    print("- Every per-tier BotLimits number and production multiplier lies on one equal-step line (DESIGN §19.1).")
    print("- ScaleTargetsBotModule writes every category and field, Min <= Max, the tech line is 1 1 1 1 2 2 2 2 3 3 and minute 0 equals the DESIGN 19.1 table (AI_ARCHITECTURE 12.22).")
    print("- No module gates on a difficulty-tier condition (DESIGN §19.1); strength scales via BotLimits.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
