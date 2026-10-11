"""Regression test for write_variant_from_oramap (SPEC_2026-10-08_batch_variant_rewrite_fix).

Pins the map-rewrite grammar over every shipped .oramap:
  * synthetic duel actors land at the END of the existing Actors block
    (original actor order / ActorIDs / RNG-visible creation order preserved),
  * the harness rules ship as reserved duel_rules.yaml — an archive's own
    rules.yaml is never overwritten and harness rules append LAST to any
    existing Rules list (nonempty, empty, and absent keys all handled),
  * malformed or colliding archives fail closed (duplicate Actors sections,
    reserved-file collision, duplicate reserved include, synthetic actor-id
    collision).

Run:  python tools/ai/test_variant_rewrite_sweep.py   (or: python -m unittest)
"""

import contextlib
import io
import pathlib
import re
import shutil
import sys
import tempfile
import unittest
import zipfile

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import run_ai_match_batch as m

MAPS_DIR = m.REPO_ROOT / "mods" / "cameo" / "maps"
MATCHUP = {
    "side_a": {"faction": "td_gdi", "bot": "hard"},
    "side_b": {"faction": "td_nod", "bot": "hard"},
}
TIME_LIMIT = 9000
RESERVED = m.HARNESS_RULES_NAME  # duel_rules.yaml


def map_class(map_text, names):
    cls = []
    rules_lines = re.findall(r"(?m)^Rules:[ \t]*([^\n]*)$", map_text)
    if not rules_lines:
        cls.append("no_rules_key")
    else:
        v = rules_lines[0].split("#")[0].strip()
        if not v:
            cls.append("empty_rules")
        elif "rules.yaml" in [e.strip() for e in v.split(",")]:
            cls.append("bundled_rules" if "rules.yaml" in names else "rules_ref_no_file")
        else:
            cls.append("custom_rules")
    if not re.search(r"(?m)^Actors:[ \t]*$", map_text):
        cls.append("no_actors")
    else:
        roots = [mm.group(0) for mm in re.finditer(r"(?m)^\S[^:\n]*:", map_text)]
        if roots and roots[-1].strip() != "Actors:":
            cls.append("non_actors_last")
    return cls


def actors_block(text):
    heads = list(re.finditer(r"(?m)^Actors:[ \t]*$", text))
    if len(heads) != 1:
        return None
    h = heads[0]
    nxt = re.search(r"\n\S", text[h.end():])
    return text[h.end(): h.end() + nxt.start() if nxt else len(text)]


def verify_variant(dest, orig_text, orig_names, orig_rules_bytes):
    """Return a list of invariant violations for a rewritten variant dir."""
    errs = []
    text = (dest / "map.yaml").read_text(encoding="utf-8")
    blk = actors_block(text)
    if blk is None:
        return ["actors_section_count!=1"]
    synth = re.findall(r"(?m)^\t(Multi\d+_(?:base|u\d+)):", text)
    for sname in synth:
        if not re.search(r"(?m)^\t" + sname + ":", blk):
            errs.append("synthetic %s outside Actors" % sname)
    ids = re.findall(r"(?m)^\t(\w+):", blk)
    synthset = set(synth)
    seen_synth = False
    for i in ids:
        if i in synthset:
            seen_synth = True
        elif seen_synth:
            errs.append("original actor %s after synthetic" % i)
            break
    rl = re.findall(r"(?m)^Rules:[ \t]*([^\n]*)$", text)
    if len(rl) != 1:
        errs.append("rules_key_count=%d" % len(rl))
    else:
        entries = [e.strip() for e in rl[0].split("#")[0].split(",") if e.strip()]
        if not entries or entries[-1] != RESERVED:
            errs.append("%s not last: %r" % (RESERVED, entries))
        om = re.search(r"(?m)^Rules:[ \t]*([^\n]*)$", orig_text)
        orig_entries = (
            [e.strip() for e in om.group(1).split("#")[0].split(",") if e.strip()]
            if om else []
        )
        for e in orig_entries:
            if e not in entries:
                errs.append("lost include " + e)
    if "rules.yaml" in orig_names:
        if not (dest / "rules.yaml").exists() or \
                (dest / "rules.yaml").read_bytes() != orig_rules_bytes:
            errs.append("bundled rules.yaml clobbered")
    dr = dest / RESERVED
    if not dr.exists():
        errs.append(RESERVED + " missing")
    elif "TimeLimitDefault: %d" % TIME_LIMIT not in dr.read_text(encoding="utf-8"):
        errs.append(RESERVED + " TimeLimitDefault not patched")
    return errs


def spawn_count(map_text):
    """Lenient mpspawn count (does not fail on <2 like mp_spawn_cells)."""
    return len(re.findall(r"(?m)^\t\w+: mpspawn\n", map_text))


def write_quiet(oramap, dest):
    with contextlib.redirect_stdout(io.StringIO()):
        m.write_variant_from_oramap(oramap, dest, MATCHUP, TIME_LIMIT)


def make_oramap(path, map_text, extra=None):
    with zipfile.ZipFile(path, "w") as z:
        z.writestr("map.yaml", map_text)
        for name, data in (extra or {}).items():
            z.writestr(name, data)


def base_map_text():
    """map.yaml of the first shipped archive — guarantees all non-target
    rewrite checks (mpspawns, PlayerReference blocks, Categories) pass."""
    first = sorted(MAPS_DIR.glob("*.oramap"))[0]
    with zipfile.ZipFile(first) as z:
        return z.read("map.yaml").decode("utf-8")


class TestFullPoolSweep(unittest.TestCase):
    """Every shipped .oramap rewrites cleanly or fails only on <2 mpspawns,
    with all invariants holding on success. Prints the affected-class
    histogram."""

    def test_full_pool(self):
        archives = sorted(MAPS_DIR.glob("*.oramap"))
        self.assertTrue(archives, "no .oramap archives under %s" % MAPS_DIR)
        out_root = pathlib.Path(tempfile.mkdtemp(prefix="variant_sweep_test_"))
        self.addCleanup(shutil.rmtree, out_root, True)
        hist, bad, legit_reject = {}, [], 0
        for oramap in archives:
            with zipfile.ZipFile(oramap) as z:
                names = set(z.namelist())
                orig_text = z.read("map.yaml").decode("utf-8")
                orig_rules = z.read("rules.yaml") if "rules.yaml" in names else None
            cls = map_class(orig_text, names)
            for c in cls:
                hist[c] = hist.get(c, 0) + 1
            hist["total"] = hist.get("total", 0) + 1
            dest = out_root / oramap.stem
            try:
                write_quiet(oramap, dest)
            except SystemExit:
                if spawn_count(orig_text) < 2:
                    legit_reject += 1
                else:
                    bad.append((oramap.stem, ["fail() with >=2 mpspawns"], cls))
                continue
            errs = verify_variant(dest, orig_text, names, orig_rules)
            if errs:
                bad.append((oramap.stem, errs, cls))
        for k in sorted(hist):
            print("  %-20s %d" % (k, hist[k]))
        print("  legit_reject (<2 mpspawn) %d" % legit_reject)
        self.assertEqual(
            [], bad,
            "variant rewrite violations: " + "; ".join(
                "%s %s %s" % (n, c, e[:3]) for n, e, c in bad[:10]))


class TestFailClosed(unittest.TestCase):
    """Malformed / colliding archives must fail closed, not corrupt output."""

    def setUp(self):
        self.tmp = pathlib.Path(tempfile.mkdtemp(prefix="variant_failclosed_"))
        self.addCleanup(shutil.rmtree, self.tmp, True)
        self.orig = base_map_text()

    def _expect_fail(self, map_text, extra=None, tag="map"):
        oramap = self.tmp / (tag + ".oramap")
        make_oramap(oramap, map_text, extra)
        with self.assertRaises(SystemExit):
            write_quiet(oramap, self.tmp / (tag + "_out"))

    def test_reserved_file_in_archive(self):
        self._expect_fail(self.orig, {RESERVED: "x: y\n"}, "reserved_file")

    def test_rules_references_reserved(self):
        text = self.orig
        if not re.search(r"(?m)^Rules:", text):
            text = re.sub(r"(?m)^(Categories: .+)$", r"\1\n\nRules: base.yaml", text)
        text = re.sub(r"(?m)^Rules:[ \t]*([^\n]*)$",
                      lambda mo: "Rules: " + (mo.group(1).strip() + "," if mo.group(1).strip() else "")
                      + RESERVED, text, count=1)
        self._expect_fail(text, tag="reserved_ref")

    def test_duplicate_actors_section(self):
        self._expect_fail(self.orig + "\nActors:\n\tX: y\n\t\tOwner: N\n",
                          tag="dup_actors")

    def test_synthetic_id_collision(self):
        text = re.sub(r"(?m)^(Actors:[ \t]*\n)",
                      r"\1\tMulti0_base: mcv\n\t\tOwner: Neutral\n\t\tLocation: 1,1\n",
                      self.orig, count=1)
        self._expect_fail(text, tag="synth_collision")


if __name__ == "__main__":
    unittest.main(verbosity=2)
