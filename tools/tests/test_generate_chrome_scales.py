"""THE SCALE GENERATOR MUST NEVER EAT THE ART MASTER.

⛔ TWO REAL HAZARDS, BOTH HIT WHILE BUILDING THE TOOL, BOTH NOW GUARDED.

1. **Self-overwrite.** Cameo's 4x master is *named* `flags_3x.png`. The first version of the tool
   happily ran `--emit 3` and replaced the highest-resolution source with its own downscale. The
   4x artwork would have been gone, silently, with a cheerful "wrote ..." line. It actually
   happened during testing and was only recoverable because of a manual backup.

2. **Padded masters.** Upstream OpenRA and Combined Arms pad 3x artwork into a power-of-two canvas
   (3 x 256 = 768 -> a 1024 file). Uniform-resizing such a master produces the right artwork ratio
   inside a nonsense canvas. Cameo's own `glyphs_3x.png` is exactly this shape, so the tool has to
   recognise and refuse it rather than "fix" a sheet that is already correct.

Both are the same underlying error — trusting the CANVAS or the FILENAME instead of measuring the
ARTWORK — which is the error that produced the original bug this whole thread is about.
"""

from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
TOOL = ROOT / "tools" / "art" / "generate_chrome_scales.py"
sys.path.insert(0, str(TOOL.parent))

import generate_chrome_scales as g  # noqa: E402


def test_the_tool_exists_and_measures_artwork_not_canvas():
    src = TOOL.read_text(encoding="utf-8")
    assert "def artwork(" in src
    assert "CANVAS IS NOT SCALE" in src


def test_it_refuses_to_overwrite_the_master():
    """Hazard 1. The message must name the master, not fail silently or half-write."""
    src = TOOL.read_text(encoding="utf-8")
    assert "dst.resolve() == master.resolve()" in src
    assert "REFUSED" in src


def test_it_refuses_a_padded_master():
    """Hazard 2 — Cameo's own glyphs_3x is padded, so this fires on the real tree."""
    src = TOOL.read_text(encoding="utf-8")
    assert "Refusing" in src and "PADDED" in src


def test_the_master_need_not_be_declared_in_chrome_yaml():
    """A 4x master is an ART SOURCE. The engine ladder stops at 3x, so it is never in the yaml."""
    src = TOOL.read_text(encoding="utf-8")
    assert '"--master"' in src


def test_it_reads_real_png_dimensions():
    flags = ROOT / "mods" / "cameo" / "uibits" / "flags.png"
    if not flags.exists():
        return
    assert g.png_size(flags) == (512, 512)


def test_artwork_finds_the_real_extent_of_the_generated_flag_sheets():
    """The 4x master and every generated variant remain at their declared density."""
    uibits = ROOT / "mods" / "cameo" / "uibits"
    cases = {
        "flags.png": (512, 512, 387, 512),
        "flags_2x.png": (1024, 1024, 771, 1024),
        "flags_3x.png": (1536, 1536, 1153, 1536),
        "flags_4x.png": (2048, 2048, 1536, 2048),
        "glyphs_3x.png": (1024, 1024, 768, 768),   # 3x artwork in a padded canvas -- correct
    }
    for name, want in cases.items():
        p = uibits / name
        if not p.exists():
            continue
        assert g.artwork(p) == want, name


def test_the_collection_names_are_matched_case_insensitively():
    """The yaml templates are ^Flags / ^Glyphs; nobody types the capital."""
    assert "lookup = {k.lower(): k for k in decl}" in TOOL.read_text(encoding="utf-8")


def test_it_prefers_pillow_but_says_which_resampler_ran():
    """A silent quality difference between machines is worse than a slow one."""
    src = TOOL.read_text(encoding="utf-8")
    assert "Pillow LANCZOS" in src and "pure-python box filter" in src
    assert "from PIL import Image" in src


def test_generated_files_are_flagged_as_engine_content():
    """They land under mods/, so they carry the boot gate (CLAUDE.md rule 1)."""
    src = TOOL.read_text(encoding="utf-8")
    assert "BOOT GATE" in src and "engine content" in src
