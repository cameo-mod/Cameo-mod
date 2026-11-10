#!/usr/bin/env python3
"""Capture provenance: bind produced artifacts to their capture purpose.

Sentinel-diagnostic runs (run_ai_match_batch --sentinel-diagnostic) emit a
non-scoreable diagnostic capture. The record-level capture_kind/scoreable
markers alone do not survive copied raw logs, promoted siblings or derived
campaign receipts, so the producer additionally writes provenance manifests
that bind every produced artifact by relative path + SHA-256 to the game
UID(s) it carries, and every supported scoring ingest MUST resolve a
positive association before any row may become scoreable.

Binding contract (SENTINEL-RUNNER-POLICY Rev7.1 + Sol's binding
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
import os
import pathlib

SCHEMA = 1
SIDECAR_NAME = "capture_provenance.json"
SIBLING_SUFFIX = ".capture.json"
SENTINEL_KIND = "sentinel_diagnostic"
BATCH_KIND = "match_batch"
LEGACY_KIND = "legacy_import"
KNOWN_KINDS = {SENTINEL_KIND, BATCH_KIND, LEGACY_KIND}

def _unique_json_object(pairs):
    obj = {}
    for key, value in pairs:
        if key in obj:
            raise ValueError(f"duplicate JSON key {key!r}")
        obj[key] = value
    return obj

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
        if f.is_symlink():
            raise ValueError(f"refusing to hash a symlink in capture provenance: {f}")
        if f.is_file() and not _is_provenance_name(f.name):
            artifacts[f.relative_to(root).as_posix()] = sha256_file(f)
    return artifacts


def _create_json_once(path: pathlib.Path, doc: dict) -> pathlib.Path:
    """Durably publish a provenance JSON file without replacing prior evidence."""
    raw = (json.dumps(doc, indent=2, sort_keys=True) + "\n").encode("utf-8")
    with path.open("xb") as stream:
        stream.write(raw)
        stream.flush()
        os.fsync(stream.fileno())
    return path


def _snapshot_entry(path: pathlib.Path) -> dict:
    """Stable identity for an input or a provenance/marker candidate.

    Absence is recorded explicitly so a newly appearing association is drift,
    not something silently consulted midway through collection.
    """
    try:
        st = path.lstat()
    except FileNotFoundError:
        return {"present": False, "size": None, "sha256": None}
    if path.is_symlink() or not path.is_file():
        raise ValueError(f"unsupported non-regular capture input: {path}")
    return {"present": True, "size": st.st_size, "sha256": sha256_file(path)}


def snapshot_capture_inputs(paths) -> dict[str, dict]:
    """Snapshot consumed files and every ancestor association/marker candidate.

    The returned mapping is deterministic and includes absent candidate paths
    through the filesystem root. Call verify_capture_inputs after decisions;
    expected_additions is only for a sidecar deliberately published by the
    caller after the pre-publication verification.
    """
    targets = {pathlib.Path(p).absolute() for p in paths}
    candidates: set[pathlib.Path] = set(targets)
    for target in targets:
        candidates.add(target.with_name(target.name + SIBLING_SUFFIX))
        for parent in target.parents:
            candidates.add(parent / SIDECAR_NAME)
            candidates.add(parent / "batch_summary.json")
            candidates.add(parent / "batch_results.jsonl")
    return {str(p): _snapshot_entry(p) for p in sorted(candidates, key=lambda x: str(x).casefold())}


def snapshot_capture_tree(root: pathlib.Path, extra_paths=()) -> dict[str, dict]:
    """Snapshot every member (files and directories), plus external inputs."""
    root = pathlib.Path(root).absolute()
    if root.is_symlink() or not root.is_dir():
        raise ValueError(f"capture root is not a regular directory: {root}")
    members = {}
    for path in sorted(root.rglob("*"), key=lambda p: str(p).casefold()):
        if path.is_symlink() or (getattr(path, "is_junction", lambda: False)()):
            raise ValueError(f"reparse member in capture tree: {path}")
        rel = path.relative_to(root).as_posix()
        if path.is_dir():
            members[rel] = {"kind": "directory"}
        elif path.is_file():
            st = path.stat()
            members[rel] = {"kind": "file", "size": st.st_size,
                            "sha256": sha256_file(path)}
        else:
            raise ValueError(f"unsupported capture tree member: {path}")
    paths = [p for p in root.rglob("*") if p.is_file()]
    paths.extend(pathlib.Path(p) for p in extra_paths)
    return {"__tree_members__": {str(root): members},
            "__inputs__": snapshot_capture_inputs(paths)}


def verify_capture_inputs(snapshot: dict[str, dict], *, expected_additions=()) -> None:
    """Fail if any consumed file, tree member, or association candidate drifted."""
    additions = {str(pathlib.Path(p).absolute()) for p in expected_additions}
    trees = snapshot.get("__tree_members__", {})
    inputs = snapshot.get("__inputs__", snapshot)
    for raw_root, expected_members in trees.items():
        root = pathlib.Path(raw_root)
        actual_members = {}
        if root.is_symlink() or not root.is_dir():
            raise ValueError(f"capture tree disappeared or changed type: {root}")
        for path in sorted(root.rglob("*"), key=lambda p: str(p).casefold()):
            if path.is_symlink() or getattr(path, "is_junction", lambda: False)():
                raise ValueError(f"reparse member in capture tree: {path}")
            rel = path.relative_to(root).as_posix()
            if path.is_dir():
                actual_members[rel] = {"kind": "directory"}
            elif path.is_file():
                st = path.stat()
                actual_members[rel] = {"kind": "file", "size": st.st_size,
                                       "sha256": sha256_file(path)}
            else:
                raise ValueError(f"unsupported capture tree member: {path}")
        for addition in additions:
            try:
                rel = pathlib.Path(addition).relative_to(root).as_posix()
            except ValueError:
                continue
            if rel not in expected_members and rel in actual_members:
                del actual_members[rel]
        if actual_members != expected_members:
            raise ValueError(f"capture tree member/path-presence drift: {root}")
    for raw, expected in inputs.items():
        path = pathlib.Path(raw)
        if raw in additions and expected.get("present") is False:
            continue
        actual = _snapshot_entry(path)
        if actual != expected:
            raise ValueError(f"capture source/association drift: {path}")


def write_provenance(root: pathlib.Path, *, capture_kind: str,
                     scoreable: bool, game_uids=(), extra: dict | None = None) -> pathlib.Path:
    """Write <root>/capture_provenance.json binding this tree's artifacts."""
    if capture_kind not in KNOWN_KINDS:
        raise ValueError(f"unknown capture_kind {capture_kind!r}")
    root = pathlib.Path(root)
    snapshot = snapshot_capture_tree(root)
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
    verify_capture_inputs(snapshot)
    _create_json_once(out, doc)
    if collect_artifacts(root) != artifacts:
        raise ValueError("capture source tree changed during provenance publication")
    verify_capture_inputs(snapshot, expected_additions=[out])
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
    dest = pathlib.Path(dest)
    snapshot = snapshot_capture_inputs([dest])
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
    verify_capture_inputs(snapshot)
    _create_json_once(out, doc)
    verify_capture_inputs(snapshot, expected_additions=[out])
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
    def unique_pairs(pairs):
        obj = {}
        for key, value in pairs:
            if key in obj:
                raise ValueError(f"duplicate JSON key {key!r}")
            obj[key] = value
        return obj
    try:
        data = json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=unique_pairs)
    except (OSError, UnicodeDecodeError, json.JSONDecodeError, ValueError):
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


def file_game_uids(path: pathlib.Path, *, strict: bool = False) -> set[str]:
    """Union UIDs in a JSONL/JSON artifact; strict mode rejects malformed input."""
    uids: set[str] = set()
    try:
        text = path.read_text(encoding="utf-8", errors="replace")
    except OSError as exc:
        if strict:
            raise ValueError(f"cannot read UID-bearing artifact {path}: {exc}") from exc
        return uids
    if path.suffix == ".json":
        data = _load_json(path)
        if data is None:
            if strict:
                raise ValueError(f"malformed JSON UID artifact {path}")
            return uids
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
            row = json.loads(line, object_pairs_hook=_unique_json_object)
        except (json.JSONDecodeError, ValueError) as exc:
            if strict:
                raise ValueError(f"malformed JSONL UID row in {path}: {exc}") from exc
            continue
        if strict and not isinstance(row, dict):
            raise ValueError(f"non-object JSONL UID row in {path}")
        uids |= _row_uids(row)
    return uids


def _association_paths(f: pathlib.Path) -> list[pathlib.Path]:
    """Every resolvable provenance artifact for file f — scanned for conflict,
    not priority-ordered."""
    paths: list[pathlib.Path] = []
    sibling = f.with_name(f.name + SIBLING_SUFFIX)
    if sibling.exists() or sibling.is_symlink():
        paths.append(sibling)
    for d in f.parents:
        sidecar = d / SIDECAR_NAME
        if sidecar.exists() or sidecar.is_symlink():
            paths.append(sidecar)
    return paths


def _marker_summaries(f: pathlib.Path) -> list[pathlib.Path]:
    """Ancestor batch outputs that may carry markers/sentinel_uids."""
    out: list[pathlib.Path] = []
    for d in f.parents:
        for name in ("batch_summary.json", "batch_results.jsonl"):
            p = d / name
            if p.exists() or p.is_symlink():
                out.append(p)
    return out


def _assoc_uids(assoc: dict) -> set[str]:
    raw = assoc.get("game_uids", [])
    if (not isinstance(raw, list)
            or any(not isinstance(u, str) or not u for u in raw)
            or len(raw) != len(set(raw))):
        raise ValueError("malformed game_uids")
    uids = set(raw)
    uid = assoc.get("game_uid")
    if uid is not None:
        if not isinstance(uid, str) or not uid:
            raise ValueError("malformed game_uid")
        uids.add(uid)
    return uids


def _assoc_binding(assoc: dict, association_root: pathlib.Path,
                   f: pathlib.Path, sha_fn, uids: set[str]) -> str | None:
    """Does one association dict bind file f?

    "bound"      — artifact SHA verified for f.
    "uid_only"   — UID membership covered but no artifact entry; enough to
                   mark sentinel scope, never enough to allow scoring.
    "sha_mismatch"/"malformed" — conflicting association (hard reject).
    None         — does not cover f at all.
    """
    artifacts = assoc.get("artifacts")
    if isinstance(artifacts, dict):
        try:
            rel = f.resolve().relative_to(association_root.resolve()).as_posix()
        except (ValueError, OSError):
            rel = None
        if rel in artifacts:
            declared = artifacts[rel]
            if not _is_sha(declared) or declared != declared.lower():
                return "malformed"
            return "bound" if sha_fn() == declared else "sha_mismatch"
    if assoc.get("bound_file") == f.name:
        declared = assoc.get("sha256")
        if not _is_sha(declared) or declared != declared.lower():
            return "malformed"
        return "bound" if sha_fn() == declared else "sha_mismatch"
    try:
        assoc_uids = _assoc_uids(assoc)
    except ValueError:
        return "malformed"
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
    if f.is_symlink() or not f.is_file():
        return REJECT, f"no such artifact {f}"
    try:
        actual_uids = file_game_uids(f, strict=True)
        if game_uids is not None:
            supplied_uids = set(game_uids)
            if (not supplied_uids or any(not isinstance(uid, str) or not uid
                                         for uid in supplied_uids)
                    or actual_uids != supplied_uids):
                claimed = sorted((repr(uid) for uid in supplied_uids))
                return REJECT, (f"caller UID claim {claimed} does not equal "
                                f"artifact UID membership {sorted(actual_uids)}")
        uids = actual_uids
    except (OSError, ValueError, UnicodeError, TypeError) as exc:
        return REJECT, f"malformed UID-bearing artifact {f}: {exc}"
    _sha: list[str] = []

    def sha() -> str:
        if not _sha:
            _sha.append(sha256_file(f))
        return _sha[0]

    allowed = False
    sentinel_seen = False
    reasons: list[str] = []

    for apath in _association_paths(f):
        assoc = _load_json(apath)
        if assoc is None:
            return REJECT, f"malformed provenance file {apath}"
        kind = assoc.get("capture_kind")
        scoreable = assoc.get("scoreable")
        if (kind not in KNOWN_KINDS or type(scoreable) is not bool
                or assoc.get("schema") != SCHEMA):
            return REJECT, f"malformed provenance record {apath}"
        try:
            assoc_uids = _assoc_uids(assoc)
        except ValueError as exc:
            return REJECT, f"malformed provenance record {apath}: {exc}"
        artifacts = assoc.get("artifacts")
        if artifacts is not None and not isinstance(artifacts, dict):
            return REJECT, f"malformed artifact table in {apath}"
        if isinstance(artifacts, dict):
            for rel, digest in artifacts.items():
                parts = _path_parts(rel)
                if (parts is None or pathlib.PurePosixPath(rel.replace("\\", "/")).is_absolute()
                        or ".." in parts or not _is_sha(digest) or digest != digest.lower()):
                    return REJECT, f"malformed artifact entry in {apath}: {rel!r}"
        if not assoc_uids and (kind != BATCH_KIND or not artifacts):
            return REJECT, f"provenance has no UID membership in {apath}"
        binding = _assoc_binding(assoc, apath.parent, f, sha, uids)
        if kind == SENTINEL_KIND or scoreable is False:
            if binding in ("bound", "uid_only", "sha_mismatch"):
                # Quarantine persists through byte drift: a sha_mismatch means
                # this manifest tried to bind the file, so it is in the
                # capture's scope regardless of what the bytes now are. This
                # direction is fail-safe — it can only exclude, never score.
                sentinel_seen = True
                reasons.append(f"{f.name} bound to sentinel capture by {apath}")
                continue
            if binding == "malformed":
                return REJECT, f"conflicting provenance {apath}: {binding}"
            # A sentinel manifest that does not cover this file proves
            # nothing either way; keep scanning for a positive binding.
            reasons.append(f"unrelated sentinel manifest {apath}")
            continue
        # Scoreable manifest (match_batch / legacy_import): positive binding
        # requires artifact SHA match AND complete UID membership.
        if binding == "bound" and uids and uids <= _assoc_uids(assoc):
            allowed = True
        elif binding == "bound":
            if not uids:
                return REJECT, f"hash-bound association lacks verifiable artifact UID membership: {apath}"
            return REJECT, (f"hash-bound association {apath} omits actual UID(s) "
                            f"{sorted(uids - _assoc_uids(assoc))}")
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
                return REJECT, f"malformed marker summary {mpath}"
            if record_marked(data):
                sentinel_seen = True
                reasons.append(f"{f.name} under sentinel-marked batch {mpath}")
            raw_s_uids = data.get("sentinel_uids", [])
            if (not isinstance(raw_s_uids, list)
                    or any(not isinstance(u, str) or not u for u in raw_s_uids)
                    or len(raw_s_uids) != len(set(raw_s_uids))):
                return REJECT, f"malformed sentinel UID index in {mpath}"
            s_uids = set(raw_s_uids)
            if s_uids & uids:
                sentinel_seen = True
                reasons.append(f"{f.name} covered by sentinel index in {mpath}")
        else:
            try:
                lines = mpath.read_text(encoding="utf-8", errors="replace").splitlines()
            except OSError:
                return REJECT, f"unreadable marker summary {mpath}"
            for line in lines:
                if not line.strip():
                    continue
                try:
                    row = json.loads(line, object_pairs_hook=_unique_json_object)
                except (json.JSONDecodeError, ValueError):
                    return REJECT, f"malformed marker row in {mpath}"
                if not isinstance(row, dict):
                    return REJECT, f"malformed marker row in {mpath}"
                if record_marked(row):
                    row_uids = _row_uids(row)
                    if not row_uids:
                        return REJECT, f"sentinel marker row lacks UID in {mpath}"
                    if row_uids & uids:
                        sentinel_seen = True
                        reasons.append(f"{f.name} covered by sentinel batch row in {mpath}")

    if sentinel_seen:
        return SENTINEL, "; ".join(reasons)
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


_HEX = frozenset("0123456789abcdef")


def _is_sha(value) -> bool:
    return (isinstance(value, str) and len(value) == 64
            and all(ch in _HEX for ch in value.lower()))


def _path_parts(p) -> tuple[str, ...] | None:
    """Normalized segment tuple for identity matching — both separators,
    empty and '.' segments dropped. None for non-string/blank input."""
    if not isinstance(p, str) or not p.strip():
        return None
    parts = tuple(seg for seg in p.replace("\\", "/").split("/")
                  if seg not in ("", "."))
    return parts or None


def _resolve_prov_identity(record_path, prov_artifacts: dict):
    """Resolve a consumed artifact's declared path to its entry in an
    embedded binding's {relpath: sha} table.

    A binding key resolves the record only when the key's segments are a
    suffix of the record path's segments — the relpath is anchored at the
    manifest's own directory. Exactly one match resolves the identity; zero
    is a missing entry, more than one is ambiguous — both fail closed, the
    resolver never guesses."""
    parts = _path_parts(record_path)
    if parts is None:
        return None, f"artifact path {record_path!r} missing/malformed"
    hits = []
    for rel in prov_artifacts:
        rel_parts = _path_parts(rel)
        if (rel_parts and len(parts) >= len(rel_parts)
                and parts[len(parts) - len(rel_parts):] == rel_parts):
            hits.append(rel)
    if len(hits) == 1:
        return hits[0], None
    if not hits:
        return None, f"artifact path {record_path!r} absent from embedded binding"
    return None, f"artifact path {record_path!r} resolves ambiguously: {sorted(hits)}"


def reconcile_embedded_provenance(receipt: dict) -> tuple[bool, str]:
    """Metadata-only reconciliation between a receipt's own `artifacts` role
    table and its embedded `capture_provenance` binding.

    The two are independently sourced declarations: the producer writes
    `artifacts` as {role: {path, sha256}} records while the capture layer
    binds the tree as {relpath: sha256}. Every consumed artifact record
    (path + sha256 both present) must resolve to exactly one binding entry
    by unique path-suffix identity and declare the same digest; a record
    whose path and sha256 are both null is a declared-absent artifact
    (e.g. an unproduced replay), not a consumed one. The receipt's game_uid
    must be a bound member. Sentinel-marked bindings fail.

    Pure metadata reconciliation — this attests nothing about file bytes.
    Byte attestation lives at file ingress: assess_file()/gate_ingest()
    verify bytes against the file-level binding, and
    verify_artifact_bytes() verifies a consumed artifact against BOTH the
    receipt declaration and the embedded binding.
    """
    prov = receipt.get("capture_provenance") if isinstance(receipt, dict) else None
    if prov is None:
        return True, "no embedded provenance"
    if not isinstance(prov, dict):
        return False, "embedded capture_provenance is not a mapping"
    if (prov.get("schema") != SCHEMA
            or prov.get("capture_kind") not in KNOWN_KINDS
            or type(prov.get("scoreable")) is not bool
            or prov.get("scoreable") is not True):
        return False, "embedded provenance schema/kind/scoreable fields malformed"
    if record_marked(prov):
        return False, "embedded provenance is sentinel-marked"
    uids = prov.get("game_uids")
    if (not isinstance(uids, list) or not uids
            or any(not isinstance(u, str) or not u for u in uids)
            or len(uids) != len(set(uids))
            or not isinstance(receipt.get("game_uid"), str)
            or receipt.get("game_uid") not in uids):
        return False, "receipt game_uid not bound by embedded provenance"
    prov_artifacts = prov.get("artifacts")
    if not isinstance(prov_artifacts, dict):
        return False, "embedded provenance artifact table missing/malformed"
    for rel, digest in prov_artifacts.items():
        parts = _path_parts(rel)
        if (parts is None or ".." in parts
                or pathlib.PurePosixPath(rel.replace("\\", "/")).is_absolute()
                or not _is_sha(digest) or digest != digest.lower()):
            return False, f"embedded provenance artifact entry {rel!r} malformed"
    artifacts = receipt.get("artifacts")
    if artifacts is None:
        artifacts = {}
    if not isinstance(artifacts, dict):
        return False, "receipt artifacts table malformed"
    for role, record in artifacts.items():
        if not isinstance(record, dict):
            return False, f"artifact record for {role!r} malformed"
        rpath, rsha = record.get("path"), record.get("sha256")
        if rpath is None and rsha is None:
            continue  # declared-absent artifact — nothing consumed to bind
        if not isinstance(rpath, str) or not _is_sha(rsha):
            return False, f"artifact record for {role!r} malformed"
        rel, why = _resolve_prov_identity(rpath, prov_artifacts)
        if rel is None:
            return False, f"{role}: {why}"
        declared = prov_artifacts[rel]
        if declared != rsha:
            return False, f"{role}: sha256 disagrees with embedded binding {rel!r}"
    return True, "receipt artifacts agree with embedded provenance"


def verify_artifact_bytes(receipt: dict, role: str,
                          path: pathlib.Path) -> tuple[bool, str]:
    """File-ingress byte attestation for one consumed artifact.

    The live bytes of `path` must equal BOTH independently sourced
    declarations: the receipt's artifact record for `role` and the embedded
    capture_provenance binding entry for the same resolved identity. A
    receipt without an embedded binding reduces to the role-table check —
    but embedded provenance that exists yet cannot cover the artifact fails
    closed (missing/ambiguous/conflicting entries are never tolerated).
    """
    artifacts = receipt.get("artifacts") if isinstance(receipt, dict) else None
    record = (artifacts or {}).get(role)
    if not isinstance(record, dict):
        return False, f"no artifact record for {role!r}"
    declared_receipt = record.get("sha256")
    if record.get("path") is None or not _is_sha(declared_receipt):
        return False, f"artifact {role!r} not bound in the receipt table"
    f = pathlib.Path(path)
    if not f.is_file():
        return False, f"consumed artifact {f} missing"
    actual = sha256_file(f)
    if actual != declared_receipt:
        return False, f"{role} bytes differ from the receipt artifact record"
    prov = receipt.get("capture_provenance")
    if prov is not None:
        if (not isinstance(prov, dict) or prov.get("schema") != SCHEMA
                or prov.get("capture_kind") not in KNOWN_KINDS
                or type(prov.get("scoreable")) is not bool
                or prov.get("scoreable") is not True
                or not isinstance(prov.get("game_uids"), list)
                or receipt.get("game_uid") not in prov.get("game_uids", [])):
            return False, "embedded provenance schema/kind/UID/scoreable fields malformed"
        prov_artifacts = prov.get("artifacts") if isinstance(prov, dict) else None
        if not isinstance(prov_artifacts, dict):
            return False, "embedded provenance artifact table missing/malformed"
        rel, why = _resolve_prov_identity(record["path"], prov_artifacts)
        if rel is None:
            return False, f"{role}: {why}"
        declared_prov = prov_artifacts[rel]
        if not _is_sha(declared_prov) or declared_prov != declared_prov.lower():
            return False, f"{role}: embedded binding entry {rel!r} malformed"
        if actual != declared_prov:
            return False, f"{role} bytes differ from the embedded provenance binding"
    return True, f"{role} bytes match every declaration"
