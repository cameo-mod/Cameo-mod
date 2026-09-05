"""`fit_class` must MERGE its candidate into a class anchor, never replace the entry.

The sign-off workflow is "run `fit_class` for each of the 27 classes, then review the
tables" — so a write that clobbers the entry does not lose one record, it loses all 27.

Measured 2026-08-17 on `mbt`: the write was `anchors[cls] = {...6 keys...}`, and one run
deleted `spec` (cost0/dps0/hp0/range0_wdist/speed0), `armor`, `tech_tier`,
`tech_tier_flag`, `reveals_shroud` and the "★ LOCKED 2026-08-01"
provisional note. Those are the maintainer's DESIGN inputs, not fit outputs:
`formula.class_baseline_price` reads `spec`, and the tier/verifier pair is what enforces
the 2.5x identity. The tool still exited 0 and still wrote a plausible validation table,
so nothing downstream would have noticed until a price came out wrong.

These tests pin the invariant on the COMMITTED ledger (every class keeps its design keys)
and on the merge itself, so the destructive form cannot come back.
"""

from __future__ import annotations

import json
import pathlib
import unittest

import _bootstrap  # noqa: F401 — sys.path side effect

ROOT = pathlib.Path(__file__).resolve().parents[2]
ANCHORS = ROOT / "docs" / "balance" / "class_anchors.json"

# Keys that belong to the MAINTAINER (design inputs), which a fit must never remove.
#
# ⚠ Only the keys present on ALL 27 classes belong here. `tech_tier` is 24/27 (`closecombat`
# and two others record the tier inside `comment` as "Tier factor 0.75=T3" instead), `armor`
# is 13/27, `provisional` 23/27 — asserting those universally fails on legitimate entries,
# which is what a first version of this test did.
DESIGN_KEYS = ("spec", "reveals_shroud", "comment")
# Keys the fit itself owns and is expected to overwrite.
FIT_KEYS = ("anchor_actor", "cost0", "o0", "p0", "q0", "signed_off")


def classes(doc):
    return {k: v for k, v in doc.items() if isinstance(v, dict) and "spec" in v}


class CommittedLedgerTest(unittest.TestCase):
    """The committed anchors must still carry their design inputs."""

    @classmethod
    def setUpClass(cls):
        cls.doc = json.loads(ANCHORS.read_text(encoding="utf-8"))

    def test_every_class_keeps_its_design_keys(self):
        found = classes(self.doc)
        self.assertGreaterEqual(len(found), 20, "class_anchors.json lost whole classes")
        for name, entry in sorted(found.items()):
            for key in DESIGN_KEYS:
                self.assertIn(key, entry,
                              f"class `{name}` lost design key `{key}` — a fit_class run "
                              f"replaced the entry instead of merging into it")

    def test_spec_is_the_full_five_stat_baseline(self):
        for name, entry in sorted(classes(self.doc).items()):
            spec = entry["spec"]
            for field in ("cost0", "dps0", "hp0", "range0_wdist", "speed0"):
                self.assertIn(field, spec, f"class `{name}` spec is missing `{field}`")

    def test_fit_comment_never_squats_on_the_design_comment(self):
        """A fit writes `fit_comment`; `comment` is the maintainer's rationale."""
        for name, entry in sorted(classes(self.doc).items()):
            if entry.get("comment"):
                self.assertNotIn("sign-off pending", entry["comment"],
                                 f"class `{name}`: a fit candidate overwrote the design "
                                 f"comment — it must use `fit_comment`")


class MergeSemanticsTest(unittest.TestCase):
    """The merge itself, exercised without touching the real ledger."""

    def _merge(self, existing, fit):
        """Mirror of fit_class's write. Kept in sync by the source test below."""
        entry = dict(existing or {})
        entry.update(fit)
        return entry

    def test_merge_preserves_unrelated_keys(self):
        existing = {"spec": {"cost0": 800}, "reveals_shroud": "x", "tech_tier": 1.0,
                    "comment": "design rationale", "signed_off": True}
        fit = {"anchor_actor": "a", "cost0": 800.0, "o0": 1.0, "p0": 2.0, "q0": 3.0,
               "signed_off": False, "fit_comment": "candidate"}
        merged = self._merge(existing, fit)
        self.assertEqual(merged["spec"], {"cost0": 800})
        self.assertEqual(merged["reveals_shroud"], "x")
        self.assertEqual(merged["comment"], "design rationale")
        for key in FIT_KEYS:
            self.assertIn(key, merged)

    def test_a_fresh_fit_voids_a_previous_sign_off(self):
        merged = self._merge({"signed_off": True}, {"signed_off": False})
        self.assertFalse(merged["signed_off"],
                         "a new fit moves the numbers, so approval must be reset")

    def test_source_does_not_replace_the_entry(self):
        """Guard the actual source: `anchors[args.cls] = {` with a literal dict is the bug."""
        src = (ROOT / "tools" / "balance" / "fit_class.py").read_text(encoding="utf-8")
        self.assertIn("entry = dict(anchors.get(args.cls) or {})", src,
                      "fit_class no longer merges into the existing anchor entry")
        self.assertNotIn('anchors[args.cls] = {"anchor_actor"', src,
                         "fit_class went back to REPLACING the anchor entry")


if __name__ == "__main__":
    unittest.main()
