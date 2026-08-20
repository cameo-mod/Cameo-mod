#!/usr/bin/env python3
"""audit_sequences.py — B6 detector (broken art/sequence references).

  S1 resolved actor's render image (RenderSprites.Image or actor name) has no
     sequences definition, for actors that render sprites (BLOCKING-ish)
  S2 explicit sequence names referenced by common traits don't exist in the
     resolved image (WithSpriteBody/WithFacingSpriteBody/WithIdleOverlay/
     support power Icon etc.)
  S3 orphan sequence images: defined in sequences yaml, referenced by nothing
     (informational; feeds B10)
"""

from __future__ import annotations

import sys

from cameo_model import Model
from report import h1, h2, relpath, table

SEQ_TRAITS = {
    "WithSpriteBody": "Sequence",
    "WithFacingSpriteBody": "Sequence",
    "WithIdleOverlay": "Sequence",
    "WithChargeOverlay": "Sequence",
    "WithMoveAnimation": "MoveSequence",
}
ICON_TRAIT_FIELDS = ("Icon", "IconImage")


def main() -> int:
    m = Model()
    rs = m.rs
    s1, s2 = [], []
    referenced_images: set[str] = set()

    for name in sorted(rs.actors):
        if name.startswith("^"):
            continue
        res = rs.resolve(name)
        if res is None:
            continue
        renders = res.children_named("RenderSprites")
        if not renders:
            continue
        image = (renders[0].get("Image") or name).lower()
        referenced_images.add(image)
        img_node = rs.sequence_image(image)
        if img_node is None:
            # only report if the actor would actually draw something
            has_body = any(res.children_named(t) for t in
                           ("WithSpriteBody", "WithFacingSpriteBody",
                            "WithInfantryBody", "WithIdleOverlay"))
            if has_body:
                s1.append([name, image, relpath(res.file, m.root)])
            continue
        seq_names = {c.key.lower() for c in img_node.children}
        if img_node.child("Inherits") is not None:
            seq_names = None  # sequence-level inheritance: skip S2 checks
        for base, fieldname in SEQ_TRAITS.items():
            for tr in res.children_named(base):
                val = tr.get(fieldname)
                img_override = (tr.get("Image") or image).lower()
                if img_override != image:
                    referenced_images.add(img_override)
                    continue
                if val and seq_names is not None and val.lower() not in seq_names:
                    s2.append([name, tr.key, val, image,
                               relpath(res.file, m.root)])
        # icon-ish references on any trait
        for tr in res.children:
            for f in ICON_TRAIT_FIELDS:
                v = tr.get(f)
                if not v:
                    continue
                if f == "IconImage" or tr.key.lower().startswith("buildable"):
                    referenced_images.add(v.lower())

    # sequences also referenced via weapons (projectile images) — approximate
    for wname in rs.weapons:
        w = rs.resolve_weapon(wname)
        if w is None:
            continue
        proj = w.child("Projectile")
        if proj is not None:
            for f in ("Image", "TrailImage", "HitAnim"):
                v = proj.get(f)
                if v:
                    referenced_images.add(v.lower())

    orphans = sorted(img.lower() for img in rs.sequences
                     if img.lower() not in referenced_images)

    print(h1("audit_sequences — art/sequence references (B6)"))
    print(f"S1 missing images: **{len(s1)}**, S2 missing sequences: **{len(s2)}**, "
          f"S3 unreferenced sequence images: **{len(orphans)}** "
          f"(of {len(rs.sequences)} defined)\n")
    print(h2("S1 — actor render image not defined in sequences"))
    print(table(["actor", "image", "rules file"], s1))
    print(h2("S2 — trait sequence missing from image"))
    print(table(["actor", "trait", "sequence", "image", "rules file"], s2))
    print(h2("S3 — sequence images referenced by no live actor/weapon (sample, feeds B10)"))
    print(", ".join(orphans[:200]) + ("…" if len(orphans) > 200 else ""))
    print()
    return 1 if s1 else 0


if __name__ == "__main__":
    sys.exit(main())
