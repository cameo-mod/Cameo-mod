#!/usr/bin/env python3
"""Classify display-text findings against their exact Git history.

This is deliberately read-only.  A finding is eligible for an exact restore
only when every step back to clean prose is blamed on a rename commit and the
commit changed no characters outside the leaked internal-id span.  Everything
else is left for review.
"""

from __future__ import annotations

import argparse
import concurrent.futures
import difflib
import functools
import pathlib
import re
import subprocess
from collections import Counter, defaultdict
from dataclasses import dataclass

from audit_display_text import Finding, actor_id_pattern, collect_findings, leaked_ids


ROOT = pathlib.Path(__file__).resolve().parents[2]
REFERENCE_IN_DESCRIPTION = re.compile(r",\s*[!~][A-Za-z0-9_.-]+(?:\s*,|\s*$)")


@dataclass(frozen=True)
class Blame:
    commit: str
    original_line: int
    summary: str
    previous_commit: str | None
    previous_path: str | None
    filename: str
    content: str


@dataclass(frozen=True)
class Classification:
    finding: Finding
    decision: str
    historical_value: str
    commits: tuple[str, ...]
    reason: str


def git(*args: str) -> str:
    result = subprocess.run(
        ["git", *args], cwd=ROOT, check=False, text=True,
        encoding="utf-8", errors="replace", stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    if result.returncode:
        raise RuntimeError(
            f"git {' '.join(args)} failed ({result.returncode}): "
            f"{result.stderr.strip()}"
        )
    return result.stdout


@functools.lru_cache(maxsize=None)
def blame_line(revision: str, path: str, line: int) -> Blame:
    output = git(
        "blame", "--line-porcelain", "-C", "-C", "-M",
        revision, "-L", f"{line},{line}", "--", path,
    )
    lines = output.splitlines()
    header = lines[0].split()
    metadata: dict[str, str] = {}
    content = ""
    for item in lines[1:]:
        if item.startswith("\t"):
            content = item[1:]
            break
        key, _, value = item.partition(" ")
        metadata[key] = value

    previous_commit = previous_path = None
    if "previous" in metadata:
        previous_commit, previous_path = metadata["previous"].split(" ", 1)

    return Blame(
        commit=header[0],
        original_line=int(header[1]),
        summary=metadata.get("summary", ""),
        previous_commit=previous_commit,
        previous_path=previous_path,
        filename=metadata.get("filename", path),
        content=content,
    )


@functools.lru_cache(maxsize=None)
def file_lines(revision: str, path: str) -> tuple[str, ...]:
    return tuple(git("show", f"{revision}:{path}").splitlines())


def value_frame(line: str, value: str) -> tuple[str, str] | None:
    start = line.find(value)
    if start < 0:
        return None
    return line[:start], line[start + len(value):]


def framed_value(line: str, prefix: str, suffix: str) -> str | None:
    if not line.startswith(prefix) or not line.endswith(suffix):
        return None
    end = len(line) - len(suffix) if suffix else len(line)
    if end < len(prefix):
        return None
    return line[len(prefix):end]


def rename_summary(summary: str) -> bool:
    lowered = summary.lower()
    return "rename" in lowered or "naming migration" in lowered


def changes_confined_to_ids(old: str, new: str,
                            pattern: re.Pattern[str]) -> bool:
    spans = [(match.start(), match.end()) for match in pattern.finditer(new)]
    if not spans or old == new:
        return False

    def inside(position: int) -> bool:
        return any(start <= position < end for start, end in spans)

    def touches(position: int) -> bool:
        return any(start <= position <= end for start, end in spans)

    matcher = difflib.SequenceMatcher(a=old, b=new, autojunk=False)
    for tag, _i1, _i2, j1, j2 in matcher.get_opcodes():
        if tag == "equal":
            continue
        if j1 == j2:
            if not touches(j1):
                return False
        elif not all(inside(position) for position in range(j1, j2)):
            return False
    return True


def classify_one(finding: Finding, pattern: re.Pattern[str]) -> Classification:
    current_path = ROOT / finding.path
    current_line = current_path.read_text(
        encoding="utf-8-sig", errors="replace").splitlines()[finding.line - 1]
    frame = value_frame(current_line, finding.value)
    if frame is None:
        return Classification(finding, "REVIEW", "", (), "value is not unique on its source line")
    prefix, suffix = frame

    revision = "HEAD"
    path = finding.path
    line = finding.line
    value = finding.value
    commits: list[str] = []

    for _ in range(16):
        remaining = leaked_ids(value, pattern)
        if not remaining:
            if (finding.field.lower() == "description" and
                    REFERENCE_IN_DESCRIPTION.search(finding.value)):
                return Classification(
                    finding, "KEEP", value, tuple(commits),
                    "Description contains an explicit hidden prerequisite reference",
                )
            return Classification(
                finding, "FIX", value, tuple(commits),
                "exact pre-migration display value recovered",
            )

        blame = blame_line(revision, path, line)
        if blame.content != framed_line(prefix, value, suffix):
            return Classification(
                finding, "REVIEW", value, tuple(commits),
                "line framing changed while following history",
            )
        if not rename_summary(blame.summary):
            return Classification(
                finding, "REVIEW", value, tuple(commits),
                f"last change is not a recognized rename commit: {blame.summary}",
            )
        if not blame.previous_commit or not blame.previous_path:
            return Classification(
                finding, "REVIEW", value, tuple(commits),
                "rename commit has no traceable parent line",
            )

        previous_lines = file_lines(blame.previous_commit, blame.previous_path)
        if not 1 <= blame.original_line <= len(previous_lines):
            return Classification(
                finding, "REVIEW", value, tuple(commits),
                "parent line number is outside the historical file",
            )
        previous_line = previous_lines[blame.original_line - 1]
        previous_value = framed_value(previous_line, prefix, suffix)
        if previous_value is None:
            return Classification(
                finding, "REVIEW", value, tuple(commits),
                "non-display characters changed in the rename commit",
            )
        if not changes_confined_to_ids(previous_value, value, pattern):
            return Classification(
                finding, "REVIEW", value, tuple(commits),
                "rename commit changed text outside an internal-id span",
            )

        commits.append(blame.commit)
        revision = blame.previous_commit
        path = blame.previous_path
        line = blame.original_line
        value = previous_value

    return Classification(
        finding, "REVIEW", value, tuple(commits),
        "history traversal exceeded the safety limit",
    )


def framed_line(prefix: str, value: str, suffix: str) -> str:
    return f"{prefix}{value}{suffix}"


def markdown_escape(value: str) -> str:
    return value.replace("|", "\\|").replace("\n", "\\n")


def render(results: list[Classification]) -> str:
    decision_counts = Counter(result.decision for result in results)
    active_counts = Counter(
        result.decision for result in results if result.finding.active)
    by_id: dict[str, Counter[str]] = defaultdict(Counter)
    for result in results:
        for actor_id in result.finding.ids:
            by_id[actor_id][result.decision] += 1

    output = [
        "# Display-text migration review",
        "",
        "This report is read-only. `FIX` means the exact pre-migration value was "
        "recovered through a rename-only blame chain. `KEEP` is a technical "
        "reference inside a display-named field. `REVIEW` is never changed "
        "automatically.",
        "",
        f"Findings: **{len(results)}**; FIX: **{decision_counts['FIX']}**; "
        f"KEEP: **{decision_counts['KEEP']}**; REVIEW: **{decision_counts['REVIEW']}**.",
        "",
        f"Active findings — FIX: **{active_counts['FIX']}**; "
        f"KEEP: **{active_counts['KEEP']}**; REVIEW: **{active_counts['REVIEW']}**.",
        "",
        "## Identifier-family separation",
        "",
        "| internal id | FIX | KEEP | REVIEW |",
        "|---|---:|---:|---:|",
    ]
    for actor_id in sorted(by_id):
        counts = by_id[actor_id]
        output.append(
            f"| `{actor_id}` | {counts['FIX']} | {counts['KEEP']} | {counts['REVIEW']} |"
        )

    for decision in ("KEEP", "REVIEW", "FIX"):
        items = [result for result in results if result.decision == decision]
        output.extend([
            "",
            f"## {decision} ({len(items)})",
            "",
            "| active | location | field | current value | historical value | provenance | reason |",
            "|---|---|---|---|---|---|---|",
        ])
        for result in sorted(
                items,
                key=lambda item: (
                    not item.finding.active, item.finding.path, item.finding.line,
                    item.finding.field,
                )):
            finding = result.finding
            commits = ", ".join(f"`{commit[:10]}`" for commit in result.commits)
            output.append(
                "| {active} | `{path}:{line}` | `{field}` | {current} | {historical} | "
                "{commits} | {reason} |".format(
                    active="yes" if finding.active else "no",
                    path=finding.path,
                    line=finding.line,
                    field=markdown_escape(finding.field),
                    current=markdown_escape(finding.value),
                    historical=markdown_escape(result.historical_value),
                    commits=commits,
                    reason=markdown_escape(result.reason),
                )
            )
    return "\n".join(output) + "\n"


def main() -> int:
    global ROOT
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--root", type=pathlib.Path, default=ROOT,
        help="repository worktree to inspect (defaults to this script's repository)",
    )
    parser.add_argument("--output", type=pathlib.Path, help="write Markdown report to this path")
    parser.add_argument("--jobs", type=int, default=8, help="parallel Git blame workers")
    args = parser.parse_args()
    ROOT = args.root.resolve()

    findings = collect_findings(ROOT)
    all_ids = {actor_id for finding in findings for actor_id in finding.ids}
    pattern = actor_id_pattern(all_ids)
    with concurrent.futures.ThreadPoolExecutor(max_workers=max(1, args.jobs)) as executor:
        results = list(executor.map(lambda finding: classify_one(finding, pattern), findings))

    report = render(results)
    if args.output:
        output = args.output if args.output.is_absolute() else ROOT / args.output
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(report, encoding="utf-8", newline="\n")
    else:
        print(report, end="")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
