"""Launch the B1/B7 weapon-validation map and assert damage receipts.

B1 (playtest-b1-exorcist-orb, a65c04afd): Hakureiring3 InvalidTargets went from
`Ground` to `Epic, Psionic, Monster`. The exorcist's orb ring is
PeriodicExplosion -> ReimuDreamSeal gated on `spellcard` (detonates on the
exorcist; FireRadius launches four ReimuYinYangDrone lasers that shrapnel
Hakureiring3 at enemies in range). The gate grants spellcard via a map-local
ExternalCondition shim; an Epic bait actor inside Armament@SPECIAL range
proves Hakureiring2 still fires (it is expected to die by design). Ordinary
tanks inside the orb envelope but outside both armament ranges must be
damaged; Epic/Monster/Psionic controls inside the envelope must be untouched.

B7 (playtest-b7-japan-sw-air, 890fba61e): MagicOrb/MagicOrb2 Warhead@Tesla_Heavy
and Warhead@Tesla_Heavy_ExtraDamage went from `Ground, Water` to
`Ground, Water, Air`. SupportPowerBotASModule fires MagicOrbHailstorm at a
bait structure cluster. Orbs rain over a ~5-10c annulus and each fires 8
bullets straight down to ground level; HitShape.DistanceFromEdge is 3D, so
airborne victims are only hit by near-direct impacts below them. Assertions:
the ground control and the landed aircraft must be damaged, and at least one
airborne aircraft must be damaged (aimed spawner orbs can reach ~z=2000);
the z=4000 fixed-wing and friendly-fire results are reported, not asserted.

Run `python tools/tests/ai_b1b7_gate.py`. Do not run concurrently with the
other AI runtime gates (shared %APPDATA% logs).
"""

from __future__ import annotations

import json
import os
import pathlib
import re
import subprocess
import sys

TESTS_DIR = pathlib.Path(__file__).resolve().parent
if str(TESTS_DIR) not in sys.path:
    sys.path.insert(0, str(TESTS_DIR))

from _bootstrap import REPO_ROOT


MAP = "ai_b1b7_gate_20261009"
TIMEOUT_SECONDS = 360
CONFIG_KEYS = {"MOD_ID", "ENGINE_DIRECTORY"}
RECEIPT_RE = re.compile(r"^B17_RECEIPT (\S+) hp0=(-?\d+)(?: hpmin=(-?\d+))? hp1=(-?\d+) z=(-?\d+)")


def fail(message: str, detail=None) -> None:
    print(f"ASSERTION FAILED: {message}")
    if detail is not None:
        print(f"Detail: {detail}")
    raise SystemExit(1)


def read_config(path: pathlib.Path, values: dict[str, str]) -> None:
    if not path.is_file():
        return

    assignment = re.compile(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*?)\s*$")
    for line in path.read_text(encoding="utf-8").splitlines():
        match = assignment.match(line)
        if not match or match.group(1) not in CONFIG_KEYS:
            continue

        value = match.group(2)
        if len(value) >= 2 and value[0] == value[-1] and value[0] in "\"'":
            value = value[1:-1]
        values[match.group(1)] = value


def load_config() -> tuple[str, pathlib.Path]:
    values: dict[str, str] = {}
    read_config(REPO_ROOT / "mod.config", values)
    read_config(REPO_ROOT / "user.config", values)

    mod_id = values.get("MOD_ID")
    engine_directory = values.get("ENGINE_DIRECTORY")
    if not mod_id:
        fail("mod.config/user.config did not define MOD_ID")
    if not engine_directory:
        fail("mod.config/user.config did not define ENGINE_DIRECTORY")

    engine = pathlib.Path(engine_directory)
    if not engine.is_absolute():
        engine = REPO_ROOT / engine
    return mod_id, engine.resolve()


def support_directory(engine: pathlib.Path) -> pathlib.Path:
    local = engine / "Support"
    if local.is_dir():
        return local

    appdata = os.environ.get("APPDATA")
    if not appdata or not pathlib.Path(appdata).is_dir():
        fail("neither engine/Support nor a usable %APPDATA% resolves the OpenRA SupportDir")
    return pathlib.Path(appdata) / "OpenRA"


def output_tail(output: str) -> str:
    lines = output.splitlines()
    if len(lines) > 40:
        lines = lines[-40:]
    return "\n".join(lines)


def run_openra(executable: pathlib.Path, args: list[str], engine: pathlib.Path) -> tuple[int, str]:
    try:
        process = subprocess.Popen(
            [str(executable), *args],
            cwd=engine,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
            encoding="utf-8",
            errors="replace",
        )
    except OSError as error:
        fail(f"could not launch OpenRA: {error}")
    try:
        output, _ = process.communicate(timeout=TIMEOUT_SECONDS)
    except subprocess.TimeoutExpired:
        process.terminate()
        try:
            output, _ = process.communicate(timeout=10)
        except subprocess.TimeoutExpired:
            process.kill()
            output, _ = process.communicate()
        fail(f"OpenRA timed out after {TIMEOUT_SECONDS}s\nOpenRA output tail:\n{output_tail(output)}")
    return process.returncode, output


def parse_receipts(output: str) -> dict[str, dict]:
    receipts: dict[str, dict] = {}
    for line in output.splitlines():
        match = RECEIPT_RE.match(line.strip())
        if match:
            name, hp0, hpmin, hp1, z = match.groups()
            low = int(hpmin) if hpmin is not None else int(hp1)
            receipts[name] = {"hp0": int(hp0), "hpmin": low, "hp1": int(hp1), "z": int(z)}
    return receipts


def expect_damaged(name: str, receipts: dict[str, dict], failures: list[str]) -> None:
    entry = receipts.get(name)
    if entry is None:
        failures.append(f"missing receipt for {name}")
        return
    if entry["hp0"] <= 0:
        failures.append(f"{name} had no baseline health (hp0={entry['hp0']})")
        return
    # hpmin is the lowest health observed since the baseline, so damage
    # that self-heals before the final sample still counts.
    if entry["hpmin"] >= entry["hp0"]:
        failures.append(
            f"{name} took no damage (hp0={entry['hp0']} hpmin={entry['hpmin']} hp1={entry['hp1']})"
        )


def expect_untouched(name: str, receipts: dict[str, dict], failures: list[str]) -> None:
    entry = receipts.get(name)
    if entry is None:
        failures.append(f"missing receipt for {name}")
        return
    if entry["hpmin"] < entry["hp0"] or entry["hp1"] != entry["hp0"] or entry["hp1"] <= 0:
        failures.append(
            f"{name} was damaged or destroyed "
            f"(hp0={entry['hp0']} hpmin={entry['hpmin']} hp1={entry['hp1']})"
        )


def main() -> int:
    mod_id, engine = load_config()
    executable = engine / "bin" / "OpenRA.exe"
    if not executable.is_file():
        fail(f"OpenRA executable is missing: {executable}; run make.cmd all first")

    support = support_directory(engine)
    logs_dir = support / "Logs"
    exceptions_before = set()
    if logs_dir.is_dir():
        exceptions_before = {p.name for p in logs_dir.glob("exception-*.log")}

    args = [
        f"Game.Mod={mod_id}",
        "Sound.Device=none",
        "Engine.EngineDir=..",
        f"Engine.ModSearchPaths={REPO_ROOT / 'mods'},{engine / 'mods'}",
        f"Launch.Map={MAP}",
        "Launch.Benchmark=ai-b1b7-gate-",
    ]
    exit_code, output = run_openra(executable, args, engine)
    if exit_code != 0:
        fail(f"OpenRA exited with code {exit_code}\nOpenRA output tail:\n{output_tail(output)}")

    if logs_dir.is_dir():
        new_exceptions = sorted(
            p.name for p in logs_dir.glob("exception-*.log") if p.name not in exceptions_before
        )
        if new_exceptions:
            fail(f"new exception log(s) written: {new_exceptions}")

    if "AI_B1B7_GATE_COMPLETED" not in output:
        fail(f"gate script did not complete\nOpenRA output tail:\n{output_tail(output)}")

    receipts = parse_receipts(output)
    if not receipts:
        fail(f"no B17_RECEIPT lines were printed\nOpenRA output tail:\n{output_tail(output)}")

    failures: list[str] = []

    # B1: the Epic bait inside Armament@SPECIAL range must be dead or
    # damaged — the 240k Hakureiring2 snipe is expected to kill it. If it
    # is untouched, the anti-boss armament never fired.
    bosstarget = receipts.get("b1_bosstarget")
    if bosstarget is None:
        failures.append("missing receipt for b1_bosstarget")
    elif bosstarget["hp1"] > 0 and bosstarget["hpmin"] >= bosstarget["hp0"]:
        failures.append(
            f"b1_bosstarget was never engaged "
            f"(hp0={bosstarget['hp0']} hpmin={bosstarget['hpmin']} hp1={bosstarget['hp1']}): "
            "Hakureiring2 never fired"
        )

    # B1: ordinary tanks inside the orb envelope (outside both armament
    # ranges; armaments pause under spellcard) must take orb-missile damage.
    for name in ("b1_tank_a", "b1_tank_b", "b1_tank_c"):
        expect_damaged(name, receipts, failures)

    # B1: Epic/Monster/Psionic controls in the same envelope must be
    # untouched — Hakureiring3's InvalidTargets keeps them unaimable and
    # undamageable.
    for name in ("b1_epic", "b1_monster", "b1_psionic"):
        expect_untouched(name, receipts, failures)

    # B7: at least one bait structure must be damaged — proof that
    # SupportPowerBotASModule actually fired MagicOrbHailstorm. Without
    # this, every "undamaged aircraft" is inconclusive rather than a pass
    # or a fail.
    bait_damaged = any(
        (r := receipts.get(n)) is not None and r["hp0"] > 0 and r["hpmin"] < r["hp0"]
        for n in ("b7_bait_a", "b7_bait_b")
    )
    for name in ("b7_bait_a", "b7_bait_b"):
        if name not in receipts:
            failures.append(f"missing receipt for {name}")
    if not bait_damaged:
        failures.append(
            "the bait cluster took no damage — MagicOrbHailstorm never fired"
        )

    # B7: the ground control and the landed aircraft sit at z~0 inside the
    # orb annulus and must take blast damage.
    expect_damaged("b7_ground", receipts, failures)
    expect_damaged("b7_landed", receipts, failures)

    landed = receipts.get("b7_landed", {})
    if landed and landed["z"] >= 1500:
        failures.append(
            f"b7_landed never descended (z={landed['z']}): landed case unproven"
        )
    landed_state = f"landed_z={landed.get('z', -1)}"

    # B7 airborne: bullets detonate at ground level and HitShape distance is
    # 3D, so only aimed or near-direct orbs reach ~z=2000 helicopters and
    # z=4000 fixed-wing is at the edge of reach. At least one airborne
    # subject must be damaged for the Air fix to count as effective.
    airborne = ["b7_hover", "b7_moving", "b7_high"]
    damaged_air = [
        name for name in airborne
        if (r := receipts.get(name)) is not None and r["hpmin"] < r["hp0"] and r["hp0"] > 0
    ]
    for name in airborne:
        if name not in receipts:
            failures.append(f"missing receipt for {name}")
    if not damaged_air:
        failures.append(
            "no airborne aircraft was damaged "
            + "; ".join(
                f"{n} hp0={receipts[n]['hp0']} hpmin={receipts[n]['hpmin']} "
                f"hp1={receipts[n]['hp1']} z={receipts[n]['z']}"
                for n in airborne if n in receipts
            )
        )

    if failures:
        print("Receipts:")
        for name in sorted(receipts):
            e = receipts[name]
            print(f"  {name}: hp0={e['hp0']} hpmin={e['hpmin']} hp1={e['hp1']} z={e['z']}")
        fail("; ".join(failures))

    friendly = receipts.get("b7_friendly", {"hp0": -1, "hpmin": -1, "hp1": -1})
    high = receipts.get("b7_high", {"hp0": -1, "hpmin": -1, "hp1": -1, "z": -1})
    print(
        "AI B1/B7 weapon gate PASS: "
        f"receipts={len(receipts)} process_exit={exit_code} {landed_state} "
        f"air_damaged={damaged_air} "
        f"high_altitude={high['hp0']}->{high['hp1']}@z{high['z']} "
        f"friendly_fire={friendly['hp0']}->{friendly['hp1']} (min {friendly['hpmin']})"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
