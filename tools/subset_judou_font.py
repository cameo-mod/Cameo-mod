#!/usr/bin/env python
"""Subset the bundled Judou Sans (Hans) UI font to keep the mod's footprint small.

The upstream Judou Sans Hans release ships ~23 MB per weight because it covers
almost every script (full CJK incl. Korean Hangul + rare ideographs, Thai,
Arabic, Hebrew, Devanagari, ...). The game only renders simple left-to-right
scripts, and the realistic playerbase needs Latin, Vietnamese, Cyrillic, Greek
and Chinese. This script subsets each weight down to:

  - Latin (incl. Vietnamese), Cyrillic, Greek, common punctuation/symbols
  - Japanese kana + CJK punctuation / fullwidth forms
  - Chinese limited to the GB2312 common set (~6,763 hanzi, ~99.7% of usage)

It drops Korean Hangul, rare CJK Extension-A, and the complex scripts the engine
cannot shape anyway (Arabic/Hebrew/Thai/Devanagari). Result: ~4.6 MB per weight.

Usage:
    pip install fonttools
    python tools/subset_judou_font.py <src-dir> <out-dir>

where <src-dir> holds the upstream JudouSansHans-Regular.ttf / -Bold.ttf
(from https://github.com/JudouEco/JudouSans releases, "JudouSans-TTF-Hans").
Hinting is kept for crisp small UI text.
"""

import sys
import os
import subprocess

# Scripts kept besides Chinese. Ranges chosen to match what the engine can
# actually render (simple LTR) plus the UI symbol/punctuation blocks.
KEEP_RANGES = ",".join([
    "U+0000-00FF", "U+0100-024F", "U+0250-02AF",  # Latin + IPA
    "U+0300-036F",                                  # combining diacritics
    "U+0370-03FF", "U+0400-04FF", "U+0500-052F",   # Greek, Cyrillic
    "U+1E00-1EFF",                                  # Latin Ext. Additional (Vietnamese)
    "U+2000-206F", "U+2070-209F", "U+20A0-20CF",   # punctuation, super/sub, currency
    "U+2100-214F", "U+2150-218F", "U+2190-21FF",   # letterlike, number forms, arrows
    "U+2200-22FF", "U+2460-24FF", "U+25A0-25FF",   # math, enclosed alnum, shapes
    "U+2600-26FF",                                  # misc symbols (UI bullets etc.)
    "U+3000-303F", "U+3040-309F", "U+30A0-30FF",   # CJK punct, hiragana, katakana
    "U+31C0-31EF", "U+3200-33FF",                   # CJK strokes, enclosed/compat
    "U+FE30-FE4F", "U+FF00-FFEF",                   # CJK compat, halfwidth/fullwidth
])

WEIGHTS = ["Regular", "Bold"]


def gb2312_hanzi_chars():
    """Return every Unicode char in the CJK block encodable as GB2312."""
    out = []
    for cp in range(0x4E00, 0xA000):
        try:
            chr(cp).encode("gb2312")
            out.append(chr(cp))
        except UnicodeEncodeError:
            pass
    return "".join(out)


def main():
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    src_dir, out_dir = sys.argv[1], sys.argv[2]
    os.makedirs(out_dir, exist_ok=True)

    text_file = os.path.join(out_dir, "_gb2312_hanzi.txt")
    hanzi = gb2312_hanzi_chars()
    with open(text_file, "w", encoding="utf-8") as fo:
        fo.write(hanzi)
    print(f"GB2312 hanzi kept: {len(hanzi)}")

    for w in WEIGHTS:
        src = os.path.join(src_dir, f"JudouSansHans-{w}.ttf")
        out = os.path.join(out_dir, f"JudouSansHans-{w}.ttf")
        subprocess.run([
            sys.executable, "-m", "fontTools.subset", src,
            f"--unicodes={KEEP_RANGES}",
            f"--text-file={text_file}",
            f"--output-file={out}",
            "--layout-features=*",
            "--name-IDs=*",
        ], check=True)
        print(f"{w}: {os.path.getsize(src) / 1048576:.1f} MB -> "
              f"{os.path.getsize(out) / 1048576:.1f} MB")

    os.remove(text_file)


if __name__ == "__main__":
    main()
