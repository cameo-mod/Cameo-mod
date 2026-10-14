#!/usr/bin/env python3
"""Capture provenance: bind produced artifacts to their capture purpose.

Sentinel-diagnostic runs (run_ai_match_batch --sentinel-diagnostic) emit a
non-scoreable diagnostic capture. The record-level capture_kind/scoreable
markers alone do not survive copied raw logs, promoted siblings or derived
campaign receipts, so the producer additionally writes provenance manifests
that bind every produced artifact by relative path + SHA-256 to the game
UID(s) it carries, and every supported scoring ingest MUST resolve a
positive association before any row may become scoreable.

Binding contract (SENTINEL-RUNNER-POLICY rev6.1 + Sol's binding
interpretation):
- Every resolution route must prove actual artifact SHA and complete UID
  membership. Mere directory ancestry, a marker-free batch summary, or a
  bare scoreable:true assertion is not positive binding.
- Scan ALL resolvable associations for conflict; any contradiction or a
  sentinel-bound association rejects. Missing proof rejects — a detached
  bare file is never auto-historical.
- Canonical telemetry rows are never edited; provenance lives in sidecar
  files only.
"""
from __future__ import annotations

import hashlib
import json
import pathlib

SCHEMA = 1
SIDECAR_NAME = "capture_provenance.json"
SIBLING_SUFFIX = ".capture.json"
SENTINEL_KIND = "sentinel_diagnostic"
BATCH_KIND = "match_batch"
LEGACY_KIND = "legacy_import"
KNOWN_KINDS = {SENTINEL_KIND, BATCH_KIND, LEGACY_KIND}

ALLOW = "allow"
SENTINEL = "sentinel"   # bound to a sentinel capture — skip, never scoreable
REJECT = "reject"       # missing/malformed/conflicting provenance — hard error


def sha256_file(path: pathlib.Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def record_marked(record: dict) -> bool:
    """Record-level quarantine predicate: either field alone rejects."""
    return (isinstance(record, dict)
            and (record.get("capture_kind") == SENTINEL_KIND
                 or record.get("scoreable") is False))


def _is_provenance_name(name: str) -> bool:
    return name == SIDECAR_NAME or name.endswith(SIBLING_SUFFIX)


def collect_artifacts(root: pathlib.Path) -> dict[str, str]:
    """SHA-256 every regular file under root, keyed by posix relpath.

    Provenance files themselves are excluded (self-reference); everything else
    is bound, including canonical match logs at their producer paths.
    """
    artifacts: dict[str, str] = {}
    for f in sorted(root.rglob("*")):
        if f.is_file() and not _is_provenance_name(f.name):
            artifacts[f.relative_to(root).as_posix()] = sha256_file(f)
    return artifacts


def write_provenance(root: pathlib.Path, *, capture_kind: str,
                     scoreable: bool, game_uids=(), extra: dict | None = None) -> pathlib.Path:
    """Write <root>/capture_provenance.json binding this tree's artifacts."""
    if capture_kind not in KNOWN_KINDS:
        raise ValueError(f"unknown capture_kind {capture_kind!r}")
    artifacts = collect_artifacts(root)
    doc = {
        "schema": SCHEMA,
        "capture_kind": capture_kind,
        "scoreable": bool(scoreable),
        "game_uids": sorted(set(str(u) for u in game_uids if u)),
        "artifacts": artifacts,
    }
    if extra:
        doc.update(extra)
    out = root / SIDECAR_NAME
    out.write_text(json.dumps(doc, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    return out


def write_file_sibling(dest: pathlib.Path, *, game_uids, capture_kind: str,
                       scoreable: bool, extra: dict | None = None) -> pathlib.Path:
    """File-adjacent provenance for a derived/copy artifact.

    Written next to the destination file as <name>.capture.json. The
    scoreable form is reserved for transforms that rebind genuinely scoreable
    evidence to new bytes (e.g. anonymize_legacy_logs); quarantine copies
    always write scoreable:False.
    """
    if capture_kind not in KNOWN_KINDS:
        raise ValueError(f"unknown capture_kind {capture_kind!r}")
    doc = {
        "schema": SCHEMA,
        "capture_kind": capture_kind,
        "scoreable": bool(scoreable),
        "game_uids": sorted(set(str(u) for u in game_uids if u)),
        "sha256": sha256_file(dest),
        "bound_file": dest.name,
    }
    if extra:
        doc.update(extra)
    out = dest.with_name(dest.name + SIBLING_SUFFIX)
    out.write_text(json.dumps(doc, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    return out


def write_promoted_sibling(dest: pathlib.Path, *, game_uid: str,
                           capture_kind: str = SENTINEL_KIND) -> pathlib.Path:
    """File-adjacent provenance for a promoted/copied artifact.

    Written next to the destination file as <name>.capture.json so a copied
    artifact still carries its quarantine binding.
    """
    return write_file_sibling(dest, game_uids=[game_uid],
                              capture_kind=capture_kind, scoreable=False)


def _load_json(path: pathlib.Path):
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeDecodeError, json.JSONDecodeError):
        return None
    return data if isinstance(data, dict) else None


def _row_uids(row) -> set[str]:
    """game_uid plus record_id prefix forms (`<uid>|<seat>`), including the
    per-seat carriers nested in a batch_results row's `bot_outcomes`."""
    uids: set[str] = set()
    if not isinstance(row, dict):
        return uids
    uid = row.get("game_uid")
    if isinstance(uid, str) and uid:
        uids.add(uid)
    rid = row.get("record_id")
    if isinstance(rid, str) and "|" in rid:
        uids.add(rid.rsplit("|", 1)[0])
    for sub in row.get("bot_outcomes") or []:
        if isinstance(sub, dict):
            uids |= _row_uids(sub)
    return uids


def file_game_uids(path: pathlib.Path) -> set[str]:
    """Tolerant scan: union of game_uids referenced by a JSONL/JSON artifact."""
    uids: set[str] = set()
    try:
        text = path.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return uids
    if path.suffix == ".json":
        data = _load_json(path)
        if data is not None:
            uids |= _row_uids(data)
            for key in ("results", "bot_outcomes"):
                for row in data.get(key) or []:
                    uids |= _row_uids(row)
            for u in data.get("sentinel_uids") or []:
                if isinstance(u, str):
                    uids.add(u)
        return uids
    for line in text.splitlines():
        line = line.strip()
        if not line:
            continue
        try:
            row = json.loads(line)
        except json.JSONDecodeError:
            continue
        uids |= _row_uids(row)
    return uids


def _association_paths(f: pathlib.Path) -> list[pathlib.Path]:
    """Every resolvable provenance artifact for file f — scanned for conflict,
    not priority-ordered."""
    paths: list[pathlib.Path] = []
    sibling = f.with_name(f.name + SIBLING_SUFFIX)
    if sibling.is_file():
        paths.append(sibling)
    for d in f.parents:
        sidecar = d / SIDECAR_NAME
        if sidecar.is_file():
            paths.append(sidecar)
    return paths


def _marker_summaries(f: pathlib.Path) -> list[pathlib.Path]:
    """Ancestor batch outputs that may carry markers/sentinel_uids."""
    out: list[pathlib.Path] = []
    for d in f.parents:
        for name in ("batch_summary.json", "batch_results.jsonl"):
            p = d / name
            if p.is_file():
                out.append(p)
    return out


def _assoc_uids(assoc: dict) -> set[str]:
    uids = {u for u in (assoc.get("game_uids") or []) if isinstance(u, str)}
    if isinstance(assoc.get("game_uid"), str):
        uids.add(assoc["game_uid"])
    return uids


def _assoc_binding(assoc: dict, f: pathlib.Path, sha_fn, uids: set[str]) -> str | None:
    """Does one association dict bind file f?

    "bound"      — artifact SHA verified for f.
    "uid_only"   — UID membership covered but no artifact entry; enough to
                   mark sentinel scope, never enough to allow scoring.
    "sha_mismatch"/"malformed" — conflicting association (hard reject).
    None         — does not cover f at all.
    """
    artifacts = assoc.get("artifacts")
    if isinstance(artifacts, dict):
        for base in f.parents:
            try:
                rel = f.relative_to(base).as_posix()
            except ValueError:
                continue
            if rel in artifacts:
                declared = artifacts[rel]
                if not isinstance(declared, str) or len(declared) != 64:
                    return "malformed"
                return "bound" if sha_fn() == declared else "sha_mismatch"
    if assoc.get("bound_file") == f.name:
        declared = assoc.get("sha256")
        if not isinstance(declared, str) or len(declared) != 64:
            return "malformed"
        return "bound" if sha_fn() == declared else "sha_mismatch"
    assoc_uids = _assoc_uids(assoc)
    if uids and assoc_uids and uids <= assoc_uids:
        return "uid_only"
    return None


def assess_file(path: pathlib.Path, game_uids: set[str] | None = None) -> tuple[str, str]:
    """Provenance verdict for ingesting `path` at a scoring sink.

    "allow"    — ≥1 association positively binds the file (artifact SHA +
                 complete UID membership) and none mark it sentinel/conflict.
    "sentinel" — bound to a sentinel capture: skip its rows, never scoreable.
    "reject"   — missing/malformed/conflicting provenance: hard error; a
                 detached bare file is never ingested as historical.
    """
    f = pathlib.Path(path)
    if not f.is_file():
        return REJECT, f"no such artifact {f}"
    uids = set(game_uids) if game_uids is not None else file_game_uids(f)
    _sha: list[str] = []

    def sha() -> str:
        if not _sha:
            _sha.append(sha256_file(f))
        return _sha[0]

    allowed = False
    reasons: list[str] = []

    for apath in _association_paths(f):
        assoc = _load_json(apath)
        if assoc is None:
            return REJECT, f"malformed provenance file {apath}"
        kind = assoc.get("capture_kind")
        scoreable = assoc.get("scoreable")
        if kind not in KNOWN_KINDS or type(scoreable) is not bool:
            return REJECT, f"malformed provenance record {apath}"
        binding = _assoc_binding(assoc, f, sha, uids)
        if kind == SENTINEL_KIND or scoreable is False:
            if binding in ("bound", "uid_only", "sha_mismatch"):
                # Quarantine persists through byte drift: a sha_mismatch means
                # this manifest tried to bind the file, so it is in the
                # capture's scope regardless of what the bytes now are. This
                # direction is fail-safe — it can only exclude, never score.
                return SENTINEL, f"{f.name} bound to sentinel capture by {apath}"
            if binding == "malformed":
                return REJECT, f"conflicting provenance {apath}: {binding}"
            # A sentinel manifest that does not cover this file proves
            # nothing either way; keep scanning for a positive binding.
            reasons.append(f"unrelated sentinel manifest {apath}")
            continue
        # Scoreable manifest (match_batch / legacy_import): positive binding
        # requires artifact SHA match AND complete UID membership.
        if binding == "bound" and (not uids or uids <= _assoc_uids(assoc)):
            allowed = True
        elif binding == "bound":
            reasons.append(f"{apath}: UID(s) {sorted(uids - _assoc_uids(assoc))} not bound")
        elif binding in ("sha_mismatch", "malformed"):
            return REJECT, f"conflicting provenance {apath}: {binding}"
        else:
            reasons.append(f"{apath} does not bind {f.name}")

    # Ancestor batch outputs: explicit sentinel markers/sentinel_uids reject
    # immediately; a marker-free summary is not positive binding (Sol).
    for mpath in _marker_summaries(f):
        if mpath == f:
            # A file's own marked rows never quarantine the file itself —
            # row-level skipping is each consuming sink's job. Markers scope
            # OTHER artifacts under the same directory.
            continue
        if mpath.name == "batch_summary.json":
            data = _load_json(mpath)
            if data is None:
                continue
            if record_marked(data):
                return SENTINEL, f"{f.name} under sentinel-marked batch {mpath}"
            s_uids = {u for u in (data.get("sentinel_uids") or []) if isinstance(u, str)}
            if s_uids & uids:
                return SENTINEL, f"{f.name} covered by sentinel index in {mpath}"
        else:
            try:
                lines = mpath.read_text(encoding="utf-8", errors="replace").splitlines()
            except OSError:
                continue
            for line in lines:
                if not line.strip():
                    continue
                try:
                    row = json.loads(line)
                except json.JSONDecodeError:
                    continue
                if record_marked(row) and (_row_uids(row) & uids):
                    return SENTINEL, f"{f.name} covered by sentinel batch row in {mpath}"

    if allowed:
        return ALLOW, f"{f.name} bound"
    detail = "; ".join(reasons) if reasons else "no resolvable association"
    return REJECT, f"unbound provenance for {f.name}: {detail}"


def gate_ingest(path: pathlib.Path) -> bool:
    """Score/aggregate ingest gate for sinks that should treat the file level.

    True when the file is positively bound for scoring; False when it is
    sentinel-bound (skip — quarantined evidence); raises ValueError when
    provenance is missing, malformed, or conflicting (a detached bare file is
    never scoreable)."""
    verdict, why = assess_file(path)
    if verdict == SENTINEL:
        return False
    if verdict == REJECT:
        raise ValueError(f"unbound provenance for {path}: {why}")
    return True


def rebind_artifact(path: pathlib.Path) -> list[pathlib.Path]:
    """Rewrite every resolvable association doc that binds `path` so it binds
    the file's CURRENT bytes — the derived-provenance write for byte-changing
    transforms (CAT-3 refuse-or-rebind). Quarantine docs are never rebound:
    a sentinel binding cannot be laundered through a byte rewrite.
    The binding is re-derived completely: artifact SHA plus UID membership —
    a transform that rewrites embedded game_uids (e.g. anonymization) leaves
    the docs claiming the file's new uids, never the old ones.
    Returns the provenance docs updated."""
    f = pathlib.Path(path)
    new_sha = sha256_file(f)
    new_uids = file_game_uids(f)
    updated: list[pathlib.Path] = []
    for apath in _association_paths(f):
        assoc = _load_json(apath)
        if not isinstance(assoc, dict):
            continue
        if assoc.get("capture_kind") == SENTINEL_KIND or assoc.get("scoreable") is False:
            continue
        changed = False
        bound = False
        artifacts = assoc.get("artifacts")
        if isinstance(artifacts, dict):
            try:
                rel = f.relative_to(apath.parent).as_posix()
            except ValueError:
                rel = None
            if rel is not None and rel in artifacts:
                bound = True
                if artifacts[rel] != new_sha:
                    artifacts[rel] = new_sha
                    changed = True
        if assoc.get("bound_file") == f.name:
            bound = True
            if isinstance(assoc.get("sha256"), str) and assoc["sha256"] != new_sha:
                assoc["sha256"] = new_sha
                changed = True
        if bound and new_uids:
            have = _assoc_uids(assoc)
            if not new_uids <= have:
                assoc["game_uids"] = sorted(have | new_uids)
                changed = True
        if changed:
            apath.write_text(json.dumps(assoc, indent=2, sort_keys=True) + "\n",
                             encoding="utf-8")
            updated.append(apath)
    return updated


def provenance_state(path: pathlib.Path) -> str:
    """Four-state classification for migration/transform surfaces.

    "bound"    — positively bound scoreable evidence; a transform that changes
                 the bytes must emit derived provenance or lose the association.
    "sentinel" — bound to a sentinel capture; transforms must refuse.
    "corrupt"  — an association exists but is malformed or conflicting;
                 transforms must refuse (never launder corrupt provenance).
    "absent"   — no resolvable association at all (the legacy state); a
                 transform leaves the output unbound, so downstream CAT-1
                 ingest still fails closed until an operator binds it.
    """
    verdict, why = assess_file(path)
    if verdict == SENTINEL:
        return "sentinel"
    if verdict == ALLOW:
        return "bound"
    if why.startswith("malformed") or "conflicting provenance" in why:
        return "corrupt"
    return "absent"
