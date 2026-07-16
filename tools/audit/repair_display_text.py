#!/usr/bin/env python3
"""Apply only Git-proven display-text restorations with exact assertions."""

from __future__ import annotations

import argparse
import codecs
import concurrent.futures
import pathlib
from collections import Counter, defaultdict

import classify_display_text_history as history
from audit_display_text import actor_id_pattern, collect_findings


def classify(root: pathlib.Path, jobs: int) -> list[history.Classification]:
    history.ROOT = root
    history.blame_line.cache_clear()
    history.file_lines.cache_clear()
    findings = collect_findings(root)
    actor_ids = {actor_id for finding in findings for actor_id in finding.ids}
    pattern = actor_id_pattern(actor_ids)
    with concurrent.futures.ThreadPoolExecutor(max_workers=max(1, jobs)) as executor:
        return list(executor.map(
            lambda finding: history.classify_one(finding, pattern), findings))


def apply_exact(root: pathlib.Path,
                results: list[history.Classification], write: bool) -> int:
    by_path: dict[str, list[history.Classification]] = defaultdict(list)
    for result in results:
        if result.decision == "FIX":
            by_path[result.finding.path].append(result)

    changed = 0
    for relative_path, items in sorted(by_path.items()):
        path = root / relative_path
        raw = path.read_bytes()
        bom = codecs.BOM_UTF8 if raw.startswith(codecs.BOM_UTF8) else b""
        text = raw[len(bom):].decode("utf-8")
        lines = text.splitlines(keepends=True)

        seen_lines: set[int] = set()
        for result in sorted(items, key=lambda item: item.finding.line):
            finding = result.finding
            if finding.line in seen_lines:
                raise RuntimeError(
                    f"multiple approved repairs target {relative_path}:{finding.line}")
            seen_lines.add(finding.line)
            index = finding.line - 1
            if not 0 <= index < len(lines):
                raise RuntimeError(f"missing target line {relative_path}:{finding.line}")
            if lines[index].count(finding.value) != 1:
                raise RuntimeError(
                    f"expected current value exactly once at "
                    f"{relative_path}:{finding.line}: {finding.value!r}")
            if not result.historical_value:
                raise RuntimeError(
                    f"empty historical value at {relative_path}:{finding.line}")
            lines[index] = lines[index].replace(
                finding.value, result.historical_value, 1)
            changed += 1

        new_raw = bom + "".join(lines).encode("utf-8")

        # Byte-for-byte reversibility proves that no character outside the
        # approved scalar values changed, including BOMs and line endings.
        reverse_lines = new_raw[len(bom):].decode("utf-8").splitlines(keepends=True)
        for result in sorted(items, key=lambda item: item.finding.line):
            index = result.finding.line - 1
            if reverse_lines[index].count(result.historical_value) != 1:
                raise RuntimeError(
                    f"historical value is not uniquely reversible at "
                    f"{relative_path}:{result.finding.line}")
            reverse_lines[index] = reverse_lines[index].replace(
                result.historical_value, result.finding.value, 1)
        reconstructed = bom + "".join(reverse_lines).encode("utf-8")
        if reconstructed != raw:
            raise RuntimeError(
                f"structural equivalence failed for {relative_path}")

        if write:
            path.write_bytes(new_raw)
            if path.read_bytes() != new_raw:
                raise RuntimeError(f"write verification failed for {relative_path}")

    return changed


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=pathlib.Path, required=True)
    parser.add_argument("--apply", action="store_true", help="write approved changes")
    parser.add_argument("--jobs", type=int, default=8)
    parser.add_argument("--expect-fix", type=int, required=True)
    parser.add_argument("--expect-keep", type=int, required=True)
    parser.add_argument("--expect-review", type=int, required=True)
    args = parser.parse_args()

    root = args.root.resolve()
    results = classify(root, args.jobs)
    counts = Counter(result.decision for result in results)
    expected = {
        "FIX": args.expect_fix,
        "KEEP": args.expect_keep,
        "REVIEW": args.expect_review,
    }
    actual = {key: counts[key] for key in expected}
    if actual != expected:
        raise RuntimeError(
            f"classification changed; refusing repair: expected {expected}, got {actual}")

    changed = apply_exact(root, results, args.apply)
    mode = "applied" if args.apply else "verified"
    print(f"{mode} {changed} exact display-text restorations in {root}")
    print(f"classification: {actual}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
