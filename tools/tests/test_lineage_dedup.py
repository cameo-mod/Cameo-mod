"""One roster must cast one vote — and the three drifted copies of that rule.

PRIOR ART: no test covered reference-source de-duplication at all. `test_class_membership.py`
consolidates the template->class map, which is the same failure mode one layer down (three private
copies of a map, disagreeing, one of them carrying a live bug); this covers the reference corpus.

⛔ MAINTAINER ORDER 2026-09-03: *"All data needs to be unique and then used as a geometric mean for
the design."* Dedup is step 1 of the synthesis, ahead of the mean — the geometric mean has no
defence against a roster that votes five times.
"""

from __future__ import annotations

import pathlib
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))
import lineage_dedup as ld          # noqa: E402
import reference_lineages as rl     # noqa: E402
import reference_distribution as rd  # noqa: E402
import synthesize_reference as syn  # noqa: E402


class TheRulingsAreOneListTest(unittest.TestCase):
    """Three private copies is what produced a member label that matched no source."""

    def test_every_ruled_label_exists_in_the_corpus(self):
        """⛔ THE LIVE BUG. `reference_distribution.LINEAGE_MEMBERS` listed `"RA2/YR"` while the
        parser labels that source `"RA2/YR (raw INI)"`, so the member never matched and voted all
        along — in the one list whose own comment warned that this exact thing would happen."""
        corpus = ld.corpus()
        missing = sorted(l for l in rl.all_labels() if l not in corpus)
        self.assertEqual(missing, [], f"ruled labels absent from the corpus: {missing}")

    def test_the_stale_label_is_gone(self):
        self.assertNotIn("RA2/YR", rl.all_labels())
        self.assertIn("RA2/YR (raw INI)", rl.all_labels())

    def test_all_three_consumers_read_the_shared_list(self):
        for name in ("synthesize_reference", "reference_distribution", "lineage_dedup"):
            src = (ROOT / "tools" / "balance" / f"{name}.py").read_text(encoding="utf-8")
            self.assertIn("import reference_lineages", src, name)

    def test_no_consumer_re_forks_a_private_copy(self):
        """A literal set of source names assigned to a LINEAGE_* or SUPERSEDED name is a new
        fourth copy. The rulings may only be assigned FROM `reference_lineages`."""
        for name in ("synthesize_reference", "reference_distribution"):
            src = (ROOT / "tools" / "balance" / f"{name}.py").read_text(encoding="utf-8")
            for line in src.splitlines():
                stripped = line.strip()
                if stripped.startswith(("LINEAGE_MEMBERS", "LINEAGE_MAP", "SUPERSEDED")) \
                        and "=" in stripped and "reference_lineages" not in stripped:
                    self.fail(f"{name} re-forks the rulings: {stripped}")

    def test_synthesize_actually_applies_the_lineage_collapse(self):
        """`pool()` still carries a 2026-08-30 'no collapsing' note. That override was about not
        curating away sources that DISAGREE; it never licensed one roster to vote five times, and
        the 2026-09-03 order settles it. If SUPERSEDED stops covering the lineages, the rifle layer
        silently goes back to five RA2 votes."""
        self.assertEqual(syn.SUPERSEDED.get("RA2 vanilla"), "Romanov's Vengeance")
        self.assertEqual(syn.SUPERSEDED.get("Tiberian Sun"), "OpenRA Tiberian Sun")

    def test_a_member_is_not_dropped_when_its_representative_is_absent(self):
        """Dropping a member whose representative is not in the corpus deletes the lineage's only
        vote — the collapse would then REMOVE evidence rather than de-duplicate it."""
        self.assertEqual(rl.superseded_map(present={"Romanov's Vengeance"}).get("Tiberian Sun"),
                         None)
        self.assertEqual(rl.superseded_map(present={"OpenRA Tiberian Sun"}).get("RA2 vanilla"),
                         None)


class TheTestIsScaleFreeTest(unittest.TestCase):
    """'Identical and just scaled' is the case the maintainer named, so it must read DUPLICATE."""

    def test_a_uniformly_scaled_copy_is_a_duplicate(self):
        base = {f"u{i}": 1.0 + i for i in range(20)}
        scaled = {k: v * 7.5 for k, v in base.items()}
        stats = ld.compare(base, scaled)
        self.assertEqual(ld.verdict(stats), "DUPLICATE")
        self.assertAlmostEqual(stats[1], 1 / 7.5, places=6)   # the offset is reported, not hidden

    def test_a_rebalance_at_the_same_scale_is_not_a_duplicate(self):
        base = {f"u{i}": 1.0 + i for i in range(20)}
        tweaked = dict(base)
        for i in range(0, 20, 2):                              # half the roster re-tuned
            tweaked[f"u{i}"] = base[f"u{i}"] * 2.0
        self.assertNotEqual(ld.verdict(ld.compare(base, tweaked)), "DUPLICATE")

    def test_the_tail_guard_rejects_bulk_agreement_with_violent_outliers(self):
        """`w10` alone passes a pair agreeing on the bulk and disagreeing 10x on a few units.
        That is a rebalance, not a copy, which is why `w25` is required too."""
        base = {f"u{i}": 1.0 + i for i in range(40)}
        mostly = dict(base)
        for i in range(6):
            mostly[f"u{i}"] = base[f"u{i}"] * 10
        stats = ld.compare(base, mostly)
        self.assertGreaterEqual(stats[2], ld.DUP_W10)          # w10 passes ...
        self.assertLess(stats[3], ld.DUP_W25)                  # ... and w25 is what catches it
        self.assertNotEqual(ld.verdict(stats), "DUPLICATE")

    def test_too_few_shared_units_is_reported_not_judged(self):
        """`Red Alert 1` and `Tiberian Dawn` — different games — read 70% agreement over 10 shared
        names. Agreement measured on a handful of units is noise."""
        few = {f"u{i}": 1.0 + i for i in range(ld.MIN_SHARED - 1)}
        self.assertIsNone(ld.compare(few, dict(few)))
        self.assertEqual(ld.verdict(None), "too-few-shared")


class TheMeasurementReproducesTheRuledLineageTest(unittest.TestCase):
    def test_the_ra2_family_is_recovered_from_the_data_alone(self):
        """The strongest evidence the thresholds are not fitted to a wish: the five members the
        maintainer ruled together in 2026-08-30 fall out of the measurement unprompted."""
        _, _, dup_pairs, groups = ld.measure()
        found = [set(g) for g in groups]
        self.assertIn({"RA2 vanilla", "Yuri's Revenge", "RA2/YR (raw INI)",
                       "OpenRA RA2 official", "Yuri's Revenge on OpenRA"}, found)

    def test_tiberian_sun_and_its_openra_port_are_one_roster(self):
        """26 of 27 shared units agree within 10%, 25 of them to three decimals."""
        data = ld.corpus()
        stats = ld.compare(data["Tiberian Sun"], data["OpenRA Tiberian Sun"])
        self.assertEqual(ld.verdict(stats), "DUPLICATE")

    def test_the_score_does_not_depend_on_argument_order(self):
        """⛔ IT DID. Written as `0.9 <= dev <= 1/0.9` the band is mathematically symmetric and
        numerically is not: the TS Stealth Tank sits exactly on it (1.60/1.44) and passed one way,
        failed the other — 96% vs 93% on the same pair. Scored in log space now."""
        data = ld.corpus()
        for a, b in (("Tiberian Sun", "OpenRA Tiberian Sun"),
                     ("Red Alert 1", "OpenRA Red Alert"),
                     ("RA2 vanilla", "Yuri's Revenge")):
            fwd, rev = ld.compare(data[a], data[b]), ld.compare(data[b], data[a])
            self.assertAlmostEqual(fwd[2], rev[2], places=12, msg=f"w10 {a} ~ {b}")
            self.assertAlmostEqual(fwd[3], rev[3], places=12, msg=f"w25 {a} ~ {b}")
            self.assertEqual(ld.verdict(fwd), ld.verdict(rev), f"{a} ~ {b}")

    def test_tiberian_dawn_and_red_alert_are_NOT_duplicates_of_their_openra_ports(self):
        """⛔ THE MAINTAINER'S OWN EXAMPLE, AND THE CORPUS DISAGREES WITH IT. OpenRA rebalances TD
        and RA1 as it ports them — Mammoth 12.0 vs 17.4x rifle, Tesla Tank 2.2 vs 8.0x — so both
        keep a vote. Scale is not the issue: both pairs sit at a median offset of exactly 1.00x.
        If this ever flips, the reference corpus changed and the ruling needs re-reading."""
        data = ld.corpus()
        for a, b in (("Tiberian Dawn", "OpenRA Tiberian Dawn"),
                     ("Red Alert 1", "OpenRA Red Alert")):
            stats = ld.compare(data[a], data[b])
            self.assertNotEqual(ld.verdict(stats), "DUPLICATE", f"{a} ~ {b}")
            self.assertAlmostEqual(stats[1], 1.0, delta=0.15, msg=f"{a} ~ {b} offset")

    def test_a_ruled_lineage_that_stops_measuring_as_one_is_reported_not_hidden(self):
        """Romanov's Vengeance is elected over the lineage while measuring as a REBALANCE of it.
        That is the maintainer's call and it stands — but the code must never quietly re-derive a
        different answer, so the disagreement has to keep surfacing."""
        data, _, dup_pairs, _ = ld.measure()
        problems = ld.audit_rulings(data, dup_pairs)
        self.assertTrue(problems["not_measured"],
                        "the ruling/measurement disagreement stopped being reported")


class TheChassisLayerIsUnaffectedTest(unittest.TestCase):
    def test_collapsing_ts_changes_no_chassis_source(self):
        """`Tiberian Sun` is a Document-4 label and the chassis layer reads Document 5 only, so
        the new lineage must be a no-op there. Asserted rather than assumed: a silent change to
        the published distributions would be the worst outcome of this commit."""
        self.assertNotIn("Tiberian Sun", {r["source"] for r in rd.peer_rows()})
        self.assertIn("OpenRA Tiberian Sun", {r["source"] for r in rd.peer_rows()})


if __name__ == "__main__":
    unittest.main()
