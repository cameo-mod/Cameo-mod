#!/usr/bin/env python
"""Build compact review sheets and audits for preview-only volcanic shores."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


TEMPLATES = tuple(f"sh{index:02d}" for index in range(1, 55))
BACKGROUND = (73, 86, 99)
DEFAULT_WORKBENCH = Path.home() / "Documents/agents/volcanic-theater/shorelines/workbench"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--workbench",
        type=Path,
        default=DEFAULT_WORKBENCH,
    )
    args = parser.parse_args()
    workbench = args.workbench.resolve()

    audits = [audit_template(workbench, template) for template in TEMPLATES]
    failures = [audit for audit in audits if audit["status"] != "pass"]

    summary = {
        "preview_only": True,
        "scope": "RA Temperate Beach templates sh01-sh54",
        "excluded_historical_sh_prefix": {
            "templates": [f"sh{index:02d}" for index in range(55, 65)],
            "reason": "tileset metadata categorizes these as Debris/Rock, not Beach",
        },
        "template_count": len(audits),
        "pass_count": len(audits) - len(failures),
        "failure_count": len(failures),
        "failures": [audit["template"] for audit in failures],
        "templates_with_cleanup_notes": [
            audit["template"] for audit in audits if audit.get("warnings")
        ],
        "templates": audits,
    }
    (workbench / "mass_conversion_audit_sh01_sh54.json").write_text(
        json.dumps(summary, indent=2),
        encoding="utf-8",
    )

    status_by_template = {audit["template"]: audit["status"] for audit in audits}
    build_sheet(
        workbench / "lava_seepage_mass_review_sh01_sh54.png",
        workbench,
        "lava_seepage_composite_{template}.png",
        status_by_template,
    )
    build_sheet(
        workbench / "temperate_donor_mass_review_sh01_sh54.png",
        workbench,
        "temperate_donor_{template}.png",
        status_by_template,
    )
    build_sheet(
        workbench / "phase_connectivity_mass_review_sh01_sh54.png",
        workbench,
        "phase_connectivity_{template}.png",
        status_by_template,
        fallbacks={"sh04": "edge_role_map_sh04.png"},
    )

    print((workbench / "lava_seepage_mass_review_sh01_sh54.png").resolve())
    print((workbench / "mass_conversion_audit_sh01_sh54.json").resolve())
    print(json.dumps({key: summary[key] for key in ("template_count", "pass_count", "failure_count", "failures")}, indent=2))
    return 1 if failures else 0


def audit_template(workbench: Path, template: str) -> dict[str, object]:
    required = (
        f"temperate_donor_{template}.png",
        f"lava_seepage_composite_{template}.png",
        f"lava_seepage_promoted_24px_{template}.png",
        f"alpha_beach_review_{template}.png",
        f"metrics_{template}.json",
        f"lava_seepage_metrics_{template}.json",
    )
    missing = [name for name in required if not (workbench / name).is_file()]
    if missing:
        return {
            "template": template,
            "status": "fail",
            "reasons": [f"missing output: {name}" for name in missing],
        }

    metrics = json.loads((workbench / f"metrics_{template}.json").read_text(encoding="utf-8"))
    seepage = json.loads(
        (workbench / f"lava_seepage_metrics_{template}.json").read_text(encoding="utf-8")
    )
    promoted = seepage["promoted_24px_dim_red"]
    reasons: list[str] = []
    warnings: list[str] = []

    expect(metrics.get("preview_only") is True, "not marked preview-only", reasons)
    expect(seepage.get("primary") == "promoted_24px_dim_red", "wrong promoted primary", reasons)
    expect(promoted.get("glow_fade_pixels") == 24.0, "wrong seepage depth", reasons)
    expect(promoted.get("seepage_color_mode") == "red cooling ramp", "wrong seepage color mode", reasons)
    expect(promoted.get("seepage_red_strength") == 0.78, "wrong red seepage strength", reasons)

    if template == "sh04":
        expect(
            metrics.get("edge_roles")
            == {"top": "ground", "bottom": "lava", "left": "both", "right": "both"},
            "approved sh04 edge roles changed",
            reasons,
        )
    else:
        expect(metrics.get("blank_subtile_alpha_pixels") == 0, "alpha leaked into blank subtiles", reasons)
        expect(metrics.get("mask_pixels_outside_occupancy") == 0, "mask escaped occupied subtiles", reasons)
        expect(metrics.get("direct_ground_lava_seam_conflicts") == 0, "direct ground/lava seam conflict", reasons)
        exposed = metrics.get("exposed_beach_seed_pixels", 0)
        covered = metrics.get("covered_exposed_beach_seed_pixels", 0)
        missed = exposed - covered
        expect(missed <= max(4, round(exposed * 0.05)), "too many exposed beach seeds missed", reasons)
        if missed:
            warnings.append(f"discarded {missed} disconnected exposed donor-color specks")

    primary = workbench / f"lava_seepage_composite_{template}.png"
    alias = workbench / f"lava_seepage_promoted_24px_{template}.png"
    expect(primary.read_bytes() == alias.read_bytes(), "promoted alias differs from primary", reasons)

    unassigned_pixels = sum(
        component["pixels"]
        for component in metrics.get("free_space_components", [])
        if component.get("role") == "unassigned"
    )
    discarded_glow_candidates = promoted.get("orphan_glow_pixels", 0)
    if discarded_glow_candidates:
        warnings.append(
            f"suppressed {discarded_glow_candidates} crack candidates disconnected from lava"
        )
    if unassigned_pixels:
        warnings.append(
            f"resolved {unassigned_pixels} phase pixels by nearest coherent component"
        )
    return {
        "template": template,
        "status": "fail" if reasons else "pass",
        "reasons": reasons,
        "warnings": warnings,
        "template_size_subtiles": metrics.get("template_size_subtiles", [3, 3]),
        "occupied_subtile_count": len(metrics.get("occupied_subtiles", list(range(9)))),
        "internal_subtile_joins": metrics.get("internal_subtile_joins"),
        "internal_edge_label_mismatched_pixels": metrics.get("internal_edge_label_mismatched_pixels"),
        "unassigned_phase_pixels": unassigned_pixels,
        "heated_crack_pixels_in_beach": promoted.get("heated_crack_pixels_in_beach"),
        "discarded_orphan_glow_candidate_pixels": discarded_glow_candidates,
    }


def expect(condition: bool, reason: str, reasons: list[str]) -> None:
    if not condition:
        reasons.append(reason)


def build_sheet(
    path: Path,
    workbench: Path,
    filename_pattern: str,
    statuses: dict[str, str],
    fallbacks: dict[str, str] | None = None,
) -> None:
    fallbacks = fallbacks or {}
    panels: list[tuple[str, str, Image.Image]] = []
    for template in TEMPLATES:
        image_path = workbench / filename_pattern.format(template=template)
        if not image_path.is_file():
            image_path = workbench / fallbacks.get(template, "")
            if not image_path.is_file():
                raise FileNotFoundError(image_path)
        with Image.open(image_path) as source:
            panels.append((template, statuses[template], source.convert("RGB")))

    columns = 6
    rows = (len(panels) + columns - 1) // columns
    header = 22
    panel_width = max(image.width for _, _, image in panels)
    panel_height = max(image.height for _, _, image in panels)
    sheet = Image.new(
        "RGB",
        (columns * panel_width, rows * (panel_height + header)),
        BACKGROUND,
    )
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for index, (template, status, panel) in enumerate(panels):
        x = (index % columns) * panel_width
        y = (index // columns) * (panel_height + header)
        color = (118, 238, 144) if status == "pass" else (255, 108, 96)
        draw.text((x + 5, y + 6), f"{template} {status.upper()}", fill=color, font=font)
        offset_x = x + (panel_width - panel.width) // 2
        offset_y = y + header + (panel_height - panel.height) // 2
        sheet.paste(panel, (offset_x, offset_y))
    sheet.save(path)


if __name__ == "__main__":
    raise SystemExit(main())
