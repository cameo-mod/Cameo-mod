"""Read schema-1 observational insurance streams; absent/incomplete evidence is UNKNOWN."""
import json
import math
import pathlib

MAX_BYTES = 128 * 1024 * 1024
MAX_LINE = 65536
MAX_ROWS = 200000
REASONS = {"dynamic_rescue", "dynamic_purifier_bonus", "legacy_secondaryinsurance"}
BASIS = "engine_earned_delta_excludes_starting_cash_and_refunds"


def _object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("duplicate field")
        result[key] = value
    return result


def read(path):
    """One match per file, complete tick-zero start and covered end per bot."""
    try:
        with pathlib.Path(path).open("rb") as stream:
            data = stream.read(MAX_BYTES + 1)
        if len(data) > MAX_BYTES:
            raise ValueError("file bound")
        lines = data.splitlines(keepends=True)
        if not lines or len(lines) > MAX_ROWS:
            raise ValueError("row bound")
        states = {}
        match = None
        for line in lines:
            if len(line) > MAX_LINE or not line.endswith(b"\n"):
                raise ValueError("line bound/truncated")
            r = json.loads(line, object_pairs_hook=_object)
            if not isinstance(r, dict) or r.get("schema") != "cameo-insurance" or type(r.get("version")) is not int or r["version"] != 1:
                raise ValueError("schema")
            uid = r.get("game_uid")
            if not isinstance(uid, str) or not 0 < len(uid) <= 128 or match not in (None, uid):
                raise ValueError("match")
            match = uid
            for k in ("slot", "seq", "tick", "timestep", "requested", "credited", "cumulative"):
                limit = (1 << 63) - 1 if k in ("credited", "cumulative") else (1 << 31) - 1
                if type(r.get(k)) is not int or not 0 <= r[k] <= limit:
                    raise ValueError("integer")
            if r.get("complete") is not True or r.get("income_basis") != BASIS:
                raise ValueError("incomplete")
            for k in ("difficulty", "faction"):
                if not isinstance(r.get(k), str) or not 0 < len(r[k]) <= 128:
                    raise ValueError("profile")
            slot = r["slot"]
            if slot not in states:
                if len(states) >= 64 or r.get("kind") != "start" or r["seq"] != 0 or r["tick"] != 0 or r["cumulative"]:
                    raise ValueError("start")
                states[slot] = {"seq": -1, "tick": 0, "total": 0, "end": False, "reasons": {},
                                "difficulty": r["difficulty"], "faction": r["faction"], "timestep": r["timestep"]}
            s = states[slot]
            if s["end"] or r["seq"] != s["seq"] + 1 or r["tick"] < s["tick"] or any(r[k] != s[k] for k in ("difficulty", "faction", "timestep")):
                raise ValueError("sequence/identity")
            s["seq"], s["tick"] = r["seq"], r["tick"]
            kind = r.get("kind")
            if kind == "payout":
                if r.get("reason") not in REASONS or r["requested"] <= 0 or r["credited"] > r["requested"]:
                    raise ValueError("payout")
                s["total"] += r["credited"]
                s["reasons"][r["reason"]] = s["reasons"].get(r["reason"], 0) + r["credited"]
            elif kind in ("start", "end"):
                if r.get("reason") != "none" or r["requested"] or r["credited"] or (kind == "start" and r["seq"] != 0):
                    raise ValueError("boundary")
            else:
                raise ValueError("kind")
            if r["cumulative"] != s["total"]:
                raise ValueError("total")
            if kind == "end":
                income = r.get("income")
                share = r.get("income_share")
                if type(income) is not int or not s["total"] <= income < (1 << 31):
                    raise ValueError("income")
                if income == 0:
                    if share is not None:
                        raise ValueError("zero denominator")
                elif type(share) not in (int, float) or not math.isfinite(share) or abs(share - s["total"] / income) > 1e-12:
                    raise ValueError("share")
                s.update(end=True, income=income, share=share)
            elif r.get("income") is not None or r.get("income_share") is not None:
                raise ValueError("premature summary")
        if not states or any(not s["end"] for s in states.values()):
            raise ValueError("missing terminal")
        return [{"game_uid": match, "slot": slot, **s, "status": "RECORDED"} for slot, s in sorted(states.items())]
    except (OSError, ValueError, TypeError, KeyError, UnicodeError, OverflowError):
        return [{"status": "UNKNOWN", "path": str(path)}]


def summaries(paths, timestep=None):
    found = False
    seen = set()
    results = []
    for arg in paths:
        p = pathlib.Path(arg)
        files = [p] if p.is_file() and p.name.startswith("cameo-ai-insurance-") else list(p.rglob("cameo-ai-insurance-*.jsonl")) if p.is_dir() else []
        for f in sorted(files):
            resolved = f.resolve()
            if resolved in seen:
                continue
            seen.add(resolved)
            found = True
            results.extend(read(f))
    counts = {}
    for r in results:
        if r["status"] == "RECORDED":
            key = r["game_uid"], r["slot"]
            counts[key] = counts.get(key, 0) + 1
    for r in results:
        if r["status"] == "RECORDED":
            if counts[r["game_uid"], r["slot"]] > 1:
                yield {"status": "UNKNOWN", "path": "duplicate insurance match/slot evidence"}
                continue
            if timestep is not None and r["timestep"] != timestep:
                continue
        yield r
    if not found:
        yield {"status": "UNKNOWN", "path": "insurance telemetry absent"}


def print_summary(paths, timestep=None):
    print("insurance credits (engine Earned income ledger; starting cash/refunds excluded):")
    for r in summaries(paths, timestep):
        if r["status"] == "UNKNOWN":
            print(f"  UNKNOWN: {r['path']}")
        else:
            share = "UNKNOWN (zero income)" if r["share"] is None else f"{100 * r['share']:.2f}%"
            print(f"  match {r['game_uid']} slot {r['slot']} {r['difficulty']}/{r['faction']}: "
                  f"{r['total']} insurance credits / {r['income']} income ({share}); {r['reasons']}")
