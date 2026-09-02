"""THE Image4x ENGINE CHANGE MUST NOT ALTER ANY SHEET THAT WORKS TODAY.

⛔ WHAT THIS DEFENDS. `docs/patches/ENGINE_image4x_chromeprovider.patch` replaces ChromeProvider's
hardcoded if/else density ladder with a loop that picks the smallest declared variant covering
`dpiScale`. It is an ENGINE change to the soft-fork (CLAUDE.md rule 7), it cannot be compiled or
booted from this container, and it affects EVERY chrome sheet in the mod — not just flags.

So the safety of the whole change rests on one property: **for any collection declaring only
1x/2x/3x, the new loop must choose exactly what the old ladder chose, at every dpiScale.** If that
holds, no working sheet can regress and the blast radius is limited to collections that opt in by
declaring `Image4x`.

These tests are that proof. They mirror both algorithms and compare them across a dense sweep,
including the exact boundaries 1.0, 2.0 and 3.0 where an off-by-one `>` vs `>=` would hide.
⚠ Mirror, not the real thing: this is Python standing in for C#. It proves the ALGORITHM, which is
where the risk is; it does not prove the patch compiles.
"""

from __future__ import annotations

import pathlib
import re

import pytest

ROOT = pathlib.Path(__file__).resolve().parents[2]
PATCH = ROOT / "docs" / "patches" / "ENGINE_image4x_chromeprovider.patch"


def upstream(dpi: float, decl: dict[int, str]) -> tuple[str | None, int]:
    """OpenRA's shipped ladder, verbatim: ChromeProvider.SheetForCollection."""
    image, density = decl.get(1), 1
    if dpi > 2 and decl.get(3):
        image, density = decl[3], 3
    elif dpi > 1 and decl.get(2):
        image, density = decl[2], 2
    return image, density


def cameo(dpi: float, decl: dict[int, str]) -> tuple[str | None, int]:
    """The replacement: smallest declared variant that covers dpiScale, else the largest."""
    image, density = decl.get(1), 1
    for d in (1, 2, 3, 4):
        candidate = decl.get(d)
        if not candidate:
            continue
        image, density = candidate, d
        if d >= dpi:
            break
    return image, density


SCALES = [round(0.5 + 0.05 * i, 2) for i in range(0, 91)] + [1.0, 2.0, 3.0, 4.0, 0.999, 2.001]
# Every shape that actually exists. Measured across Cameo, upstream OpenRA (ra + cnc) and Combined
# Arms: every collection is either `Image` alone or the full Image/Image2x/Image3x triple. Nothing
# anywhere declares Image + Image3x without Image2x — see test_the_divergent_shape_is_unused.
LEGACY = [
    {1: "a"},
    {1: "a", 2: "b"},
    {1: "a", 2: "b", 3: "c"},
]


@pytest.mark.parametrize("decl", LEGACY)
def test_identical_to_upstream_for_every_legacy_collection(decl):
    """⭐ The property the whole change rests on."""
    for dpi in SCALES:
        assert cameo(dpi, decl) == upstream(dpi, decl), (dpi, decl)


def test_the_one_divergence_is_a_shape_nothing_uses():
    """⚠ Honest limit on the compatibility claim, found by the sweep rather than by reading.

    For a collection declaring Image + Image3x but NO Image2x, upstream falls all the way back to
    1x at dpiScale 1.5 (its `else if` tests Image2x and gives up), while the loop picks the 3x
    sheet. The loop's answer is arguably better — a 3x sheet downsampled beats a 1x sheet blown up
    — but it IS a difference, so it is stated rather than papered over. It is unreachable in
    practice: no collection in any of the three projects has this shape.
    """
    decl = {1: "a", 3: "c"}
    assert upstream(1.5, decl) == ("a", 1)
    assert cameo(1.5, decl) == ("c", 3)


def test_the_divergent_shape_is_unused():
    """Fires the day someone creates the one configuration the compatibility proof excludes."""
    import re
    chrome = (ROOT / "mods" / "cameo" / "chrome.yaml").read_text(encoding="utf-8")
    offenders = []
    for block in re.split(r"\n(?=\S)", chrome):
        keys = set(re.findall(r"^\t(Image(?:2x|3x|4x)?):", block, re.M))
        if "Image" in keys and "Image3x" in keys and "Image2x" not in keys:
            offenders.append(block.split(":", 1)[0].strip())
    assert not offenders, (
        f"{offenders} declare Image3x without Image2x — the one shape where the new ladder "
        f"differs from upstream. Add an Image2x, or re-check test_the_one_divergence_is_a_shape_nothing_uses.")


def test_the_exact_boundaries_are_identical():
    """`>` vs `>=` errors hide precisely here."""
    decl = {1: "a", 2: "b", 3: "c"}
    for dpi in (1.0, 2.0, 3.0):
        assert cameo(dpi, decl) == upstream(dpi, decl), dpi


def test_a_4x_sheet_is_used_above_2x_scaling_which_is_the_whole_point():
    """Cameo's flags: 1x, 2x and a 4x-authored sheet — no 3x file exists or is needed."""
    decl = {1: "flags.png", 2: "flags_2x.png", 4: "flags_3x.png"}
    assert cameo(1.0, decl) == ("flags.png", 1)
    assert cameo(1.5, decl) == ("flags_2x.png", 2)
    assert cameo(2.0, decl) == ("flags_2x.png", 2)
    assert cameo(2.5, decl) == ("flags_3x.png", 4)   # ⭐ the bug's DPI band, now correct
    assert cameo(4.0, decl) == ("flags_3x.png", 4)
    assert cameo(9.9, decl) == ("flags_3x.png", 4)   # nothing bigger exists; take the largest


def test_a_3x_collection_still_wins_over_an_absent_4x():
    """^Glyphs declares a correct 3x sheet; it must keep being chosen."""
    decl = {1: "glyphs.png", 2: "glyphs_2x.png", 3: "glyphs_3x.png"}
    assert cameo(2.5, decl) == ("glyphs_3x.png", 3)
    assert cameo(3.5, decl) == ("glyphs_3x.png", 3)


def test_declaring_nothing_but_the_base_never_scales():
    for dpi in SCALES:
        assert cameo(dpi, {1: "only.png"}) == ("only.png", 1)


# ------------------------------------------------------- the patch says what these tests assume

def test_the_patch_adds_the_field_and_the_loop():
    assert PATCH.exists(), "the engine patch is missing"
    src = PATCH.read_text(encoding="utf-8")
    added = "\n".join(l[1:] for l in src.splitlines()
                      if l.startswith("+") and not l.startswith("+++"))
    assert "public readonly string Image4x = null;" in added
    assert "candidateDensity >= dpiScale" in added
    assert "(c.Image, 1), (c.Image2x, 2), (c.Image3x, 3), (c.Image4x, 4)" in added


def test_the_patch_targets_only_chromeprovider():
    """An engine patch that quietly touches a second file is a much bigger review."""
    files = re.findall(r"^\+\+\+ b/(\S+)", PATCH.read_text(encoding="utf-8"), re.M)
    assert files == ["OpenRA.Game/Graphics/ChromeProvider.cs"], files
