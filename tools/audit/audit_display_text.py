#!/usr/bin/env python3
"""Detect internal actor ids leaked into player-visible text.

Actor naming migrations must update structural references without rewriting
ordinary prose.  A case-insensitive ``HAND -> td_nod_handofnod`` replacement,
for example, must not turn "Hand of Nod" into an internal actor id.

The audit scans:

* display-oriented MiniYAML scalar fields across the whole mod tree;
* numbered values nested below ``Names`` and ``Descriptions``;
* every value in Fluent files;
* music titles; and
* quoted strings passed to common player-facing Lua APIs.

Comments containing internal ids are reported separately for human inspection.
They are informational because a comment may intentionally document an actor id.

Files loaded by the active manifest are blocking.  Findings in dormant files
are reported separately so disabled content can be repaired without blocking
the live ruleset.
"""

from __future__ import annotations

import argparse
import pathlib
import re
import sys
from dataclasses import dataclass

from cameo_model import Model
from report import h1, h2, table


DISPLAY_KEYS = {
    "briefing", "buttontext", "description", "displayname", "label",
    "message", "menulabel", "name", "objective", "prompt", "text",
    "title",
}
DISPLAY_CONTAINERS = {"names", "descriptions"}

YAML_SCALAR = re.compile(r"^(?P<indent>\s*)(?P<key>[^:#][^:]*):\s*(?P<value>.*)$")
FTL_VALUE = re.compile(r"^\s*(?:[A-Za-z0-9_.-]+|\.[A-Za-z0-9_-]+)\s*=\s*(.*)$")
LUA_STRING = re.compile(r"(?P<quote>['\"])(?P<value>(?:\\.|(?!\1).)*)(?P=quote)")
LUA_DISPLAY_API = re.compile(
    r"(?:DisplayMessage(?:ToPlayer)?|Add(?:Primary|Secondary)?Objective|"
    r"AddObjective|SetMissionText|GetFluentMessage)\s*\("
)
COMMENT = re.compile(r"^\s*#\s?(?P<value>.*)$")
TECHNICAL_DESCRIPTION_REFERENCE = re.compile(
    r",\s*[!~][A-Za-z0-9_.-]+(?:\s*,|\s*$)")


@dataclass(frozen=True)
class Finding:
    active: bool
    path: str
    line: int
    field: str
    ids: tuple[str, ...]
    value: str


def actor_id_pattern(actor_ids: set[str]) -> re.Pattern[str]:
    """Match migration-style actor ids without matching ordinary words."""
    candidates = sorted(
        (a for a in actor_ids
         if len(a) >= 5 and ("_" in a or "." in a)),
        key=len,
        reverse=True,
    )
    if not candidates:
        return re.compile(r"(?!)")
    return re.compile(
        r"(?<![A-Za-z0-9_.])(?:" +
        "|".join(re.escape(a) for a in candidates) +
        r")(?![A-Za-z0-9_.])",
        re.IGNORECASE,
    )


def leaked_ids(value: str, pattern: re.Pattern[str]) -> tuple[str, ...]:
    return tuple(dict.fromkeys(m.group(0).lower() for m in pattern.finditer(value)))


def is_technical_reference(finding: Finding) -> bool:
    return (
        finding.field.lower() == "description" and
        TECHNICAL_DESCRIPTION_REFERENCE.search(finding.value) is not None
    )


def yaml_display_value(path: pathlib.Path, line: str,
                       container: tuple[int, str] | None = None) -> tuple[str, str] | None:
    match = YAML_SCALAR.match(line)
    if not match:
        return None
    key = match.group("key").strip().lstrip("-").split("@", 1)[0].lower()
    value = match.group("value").split(" #", 1)[0].strip()
    if not value:
        return None
    if path.name.lower() == "music.yaml" and not match.group("indent"):
        return "MusicTitle", value
    if key in DISPLAY_KEYS:
        return match.group("key").strip(), value
    if container and len(match.group("indent")) > container[0] and key.isdigit():
        return f"{container[1]}[]", value
    return None


def scan_text_file(path: pathlib.Path, active: bool, root: pathlib.Path,
                   pattern: re.Pattern[str], include_comments: bool = False) -> list[Finding]:
    findings: list[Finding] = []
    rel = path.relative_to(root).as_posix()
    lines = path.read_text(encoding="utf-8-sig", errors="replace").splitlines()
    suffix = path.suffix.lower()
    yaml_container: tuple[int, str] | None = None

    for lineno, line in enumerate(lines, 1):
        candidates: list[tuple[str, str]] = []
        if suffix == ".yaml":
            comment = COMMENT.match(line)
            if comment and include_comments:
                candidates.append(("Comment", comment.group("value")))

            match = YAML_SCALAR.match(line)
            if match:
                indent = len(match.group("indent"))
                if yaml_container and indent <= yaml_container[0]:
                    yaml_container = None
                key = match.group("key").strip().lstrip("-").split("@", 1)[0].lower()
                if not match.group("value").strip() and key in DISPLAY_CONTAINERS:
                    yaml_container = (indent, key.title())

            item = yaml_display_value(path, line, yaml_container)
            if item:
                candidates.append(item)
        elif suffix == ".ftl":
            match = FTL_VALUE.match(line)
            if match:
                candidates.append(("FluentValue", match.group(1).strip()))
            elif line[:1].isspace() and line.strip() and not line.lstrip().startswith(("#", ".")):
                candidates.append(("FluentContinuation", line.strip()))
        elif suffix == ".lua" and LUA_DISPLAY_API.search(line):
            candidates.extend(("LuaString", m.group("value")) for m in LUA_STRING.finditer(line))

        for field, value in candidates:
            ids = leaked_ids(value, pattern)
            if ids:
                findings.append(Finding(active, rel, lineno, field, ids, value))
    return findings


def collect_findings(root: pathlib.Path | None = None,
                     include_comments: bool = False) -> list[Finding]:
    model = Model(root)
    root = model.root
    active_paths = {
        p.resolve() for group in (
            model.rs.manifest.rules,
            model.rs.manifest.weapons,
            model.rs.manifest.sequences,
            model.rs.manifest.fluent,
        ) for p in group
    }
    active_paths.add((root / "mods/cameo/music.yaml").resolve())

    pattern = actor_id_pattern({a.lower() for a in model.rs.actors})
    mod = root / "mods/cameo"
    paths: set[pathlib.Path] = set()
    for glob in ("**/*.yaml", "**/*.ftl", "**/*.lua"):
        paths.update(p for p in mod.glob(glob) if p.is_file())

    findings: list[Finding] = []
    for path in sorted(paths):
        findings.extend(scan_text_file(
            path, path.resolve() in active_paths, root, pattern, include_comments))
    return findings


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--root", type=pathlib.Path,
        help="repository worktree to inspect (defaults to this script's repository)",
    )
    parser.add_argument(
        "--include-comments", action="store_true",
        help="also report comments containing actor ids (informational)",
    )
    args = parser.parse_args()
    findings = collect_findings(args.root, include_comments=args.include_comments)
    comments = [f for f in findings if f.field == "Comment"]
    references = [f for f in findings if is_technical_reference(f)]
    display = [
        f for f in findings
        if f.field != "Comment" and not is_technical_reference(f)
    ]
    active = [f for f in display if f.active]
    dormant = [f for f in display if not f.active]

    def rows(items: list[Finding]) -> list[list[str]]:
        return [[
            f"{f.path}:{f.line}", f.field, ", ".join(f.ids), f.value,
        ] for f in items]

    print(h1("audit_display_text — internal ids leaked into UI prose"))
    print(
        f"Active findings: **{len(active)}**; dormant findings: **{len(dormant)}**; "
        f"technical references: **{len(references)}**; "
        f"comments for inspection: **{len(comments)}**\n"
    )
    print(h2(f"D1 — active display text containing actor ids ({len(active)}) — BLOCKING"))
    print(table(["location", "field", "internal id", "value"], rows(active)))
    print(h2(f"D2 — dormant display text containing actor ids ({len(dormant)})"))
    print(table(["location", "field", "internal id", "value"], rows(dormant)))
    print(h2(f"D0 — technical references in display-named fields ({len(references)}) — INFORMATIONAL"))
    print(table(["location", "field", "internal id", "value"], rows(references)))
    if args.include_comments:
        print(h2(f"D3 — comments containing actor ids ({len(comments)}) — INFORMATIONAL"))
        print(table(["location", "field", "internal id", "value"], rows(comments)))
    return 1 if active else 0


if __name__ == "__main__":
    sys.exit(main())
