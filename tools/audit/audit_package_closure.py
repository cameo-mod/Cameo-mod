"""depsjson-v1 runtime-package dependency-closure walker.

Draft implementation for task 01a13c09 (Integrator lane). Implements the
closure-check procedure in PACKAGE_SPEC_2026-10-14_runtime_closure_devin.md
(SHA 05f93d3b...): a runtime package is accepted only if its member set provably
equals the transitive dependency closure of the entry points, and the closure
plus the classified exclusions provably equals the built source tree.

Three-way equality (all must hold):
    packaged_set == closure_set
    closure_set + exclusion_set == source_set

Modes (MUTATES-is-the-bar discipline):
    check          default; reads only, audits an existing package dir against
                   a built tree (or just reports the closure of a built tree
                   when --pkg-root is omitted). Zero filesystem writes.
    emit-manifest  writes runtime_package_manifest.json to --manifest-out
                   (must not already exist unless --replace). Everything else
                   identical to check.

Nothing here executes game binaries, builds, or tests. Determinism: all emitted
sets are sorted; JSON is sort_keys+LF; --created-at is an explicit input (never
captured from the clock inside check mode).

Usage:
    py -3 tools/audit/audit_package_closure.py check ^
        --engine-root <built engine dir> --mod-root <source checkout> ^
        [--pkg-root <package dir>]
    py -3 tools/audit/audit_package_closure.py emit-manifest ^
        --engine-root <built engine dir> --mod-root <source checkout> ^
        --pkg-root <package dir> --manifest-out <file.json> ^
        --source-commit <40hex> [--created-at <ISO8601>] [--replace]
"""

import argparse
import hashlib
import json
import os
import re
import sys
from pathlib import Path

ALGORITHM = "depsjson-v1"
HASH_ALGORITHM = "sha256-raw-bytes-v1"

# Entry points whose deps.json documents seed the managed closure.
SEEDS = ["OpenRA.exe", "OpenRA.Server.exe", "OpenRA.Utility.exe"]

# Engine mod assembly always loaded regardless of mod.yaml declarations.
ALWAYS_LOADED_MOD_ASSEMBLIES = ["OpenRA.Mods.Common.dll"]

# Natives that ship beside bin/ and are reached via P/Invoke rather than the
# managed deps graph; kept explicit so a deps.json that misses one still fails
# the check rather than silently shrinking the package.
KNOWN_NATIVES = [
    "SDL2.dll",
    "libEGL.dll",
    "libGLESv2.dll",
    "freetype6.dll",
    "lua51.dll",
    "soft_oal.dll",
    "OpenAL-CS.dll",
    "Eluant.dll",
    "NVorbis.dll",
    "MP3Sharp.dll",
    "TagLibSharp.dll",
    "DiscordRPC.dll",
    "BeaconLib.dll",
    "Mono.Nat.dll",
    "ICSharpCode.SharpZipLib.dll",
    "Newtonsoft.Json.dll",
    "Microsoft.Extensions.DependencyModel.dll",
    "Pfim.dll",
]

# Culture-code satellite dirs under bin/ (managed resources live in
# bin/<culture>/<assembly>.resources.dll).
CULTURE_RE = re.compile(
    r"^(cs|de|es|fr|it|ja|ko|pl|pt-BR|ru|tr|zh-Hans|zh-Hant|zh-HantTW)$"
)

# Source-root mod trees always shipped (mod-source origin).
MOD_SOURCE_DIRS = ["mods/cameo", "mods/cameo-content"]

# Engine-root loose files packaged when present (engine-root origin).
ENGINE_ROOT_FILES = ["VERSION", "AUTHORS", "COPYING", "README.md"]

TEST_INFRA_PATTERNS = [
    re.compile(r"^nunit(\..*)?$", re.I),
    re.compile(r"^testhost(\..*)?$", re.I),
    re.compile(r"^NUnit3\.TestAdapter(\..*)?$", re.I),
    re.compile(r"^Microsoft\.TestPlatform\..*$", re.I),
    re.compile(r"^Microsoft\.VisualStudio\.CodeCoverage.*$", re.I),
    re.compile(r"^\.msCoverage.*$", re.I),
    re.compile(r"^testcentric\..*$", re.I),
    re.compile(r"^testcentric$", re.I),
    re.compile(r".*\.Test\.dll$", re.I),
    re.compile(r"^tools$", re.I),  # engine test/tooling subtree under bin/
]

MOD_ASSEMBLY_RE = re.compile(r"^OpenRA\.Mods\.(?P<name>.+)\.dll$", re.I)


def rel(p: Path, root: Path) -> str:
    return p.relative_to(root).as_posix()


def sha256_file(p: Path) -> str:
    h = hashlib.sha256()
    with open(p, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def iter_files(root: Path):
    for p in sorted(root.rglob("*")):
        if p.is_file() and not p.is_symlink():
            yield p


class ClosureError(Exception):
    pass


def load_deps_json(bin_dir: Path, name: str):
    """Yield asset basenames declared by <name>.deps.json for every library."""
    deps = bin_dir / f"{name}.deps.json"
    if not deps.exists():
        return
    data = json.loads(deps.read_text(encoding="utf-8-sig"))
    for target in data.get("targets", {}).values():
        for lib in target.values():
            for group in ("runtime", "native", "resources", "compile"):
                for asset in lib.get(group, {}) or {}:
                    yield os.path.basename(asset)
            # rid-qualified assets: {path -> {rid, assetType}} — the path is
            # the shipped file, the rid is just a moniker.
            for key in (lib.get("runtimeTargets") or {}):
                yield os.path.basename(key)


def stem(exe_or_dll: str) -> str:
    for ext in (".dll", ".exe"):
        if exe_or_dll.lower().endswith(ext):
            return exe_or_dll[: -len(ext)]
    return exe_or_dll


def managed_closure(bin_dir: Path, loaded_mod_assemblies):
    """BFS over deps.json files from the seed set; returns member basenames."""
    queue = list(SEEDS) + list(loaded_mod_assemblies)
    seen, members = set(), set()
    for s in queue:
        seen.add(s)
    while queue:
        name = queue.pop(0)
        members.add(name)
        base = stem(name)
        for asset in load_deps_json(bin_dir, base):
            if not asset:
                continue
            if not (bin_dir / asset).exists():
                continue
            members.add(asset)
            if asset.lower().endswith(".dll") and asset not in seen:
                seen.add(asset)
                queue.append(asset)
        for sidecar in (f"{base}.deps.json", f"{base}.runtimeconfig.json", f"{base}.dll.config"):
            if (bin_dir / sidecar).exists():
                members.add(sidecar)
        # Executables carry their implementation dll of the same name.
        twin = f"{base}.dll"
        if (bin_dir / twin).exists():
            members.add(twin)
            if twin not in seen:
                seen.add(twin)
                queue.append(twin)
    return members


def parse_mod_assemblies(mod_yaml: Path):
    """Declared assemblies for a mod: `Assemblies:` block or `*.dll` mentions."""
    if not mod_yaml.exists():
        return []
    names = set()
    in_block = False
    for line in mod_yaml.read_text(encoding="utf-8-sig").splitlines():
        stripped = line.strip()
        if stripped.startswith("Assemblies:"):
            in_block = True
            continue
        if in_block and stripped and not line.startswith(("\t", " ")):
            in_block = False
        if in_block:
            m = re.search(r"[\w.\-]+\.dll", stripped)
            if m:
                names.add(m.group(0))
    return sorted(names)


def engine_mod_mounts(mod_yaml: Path):
    """`^EngineDir|mods/<dir>` mounts declared by the mod's FileSystem block."""
    mounts = []
    if not mod_yaml.exists():
        return mounts
    for line in mod_yaml.read_text(encoding="utf-8-sig").splitlines():
        s = line.strip().lstrip("~")
        m = re.match(r"\^EngineDir\|mods[/\\]([\w\-]+)", s)
        if m:
            mounts.append(m.group(1))
    return sorted(set(mounts))


def culture_dirs(bin_dir: Path):
    return sorted(
        d.name for d in bin_dir.iterdir() if d.is_dir() and CULTURE_RE.match(d.name)
    )


def classify_exclusion(rel_path: str, loaded_mod_assemblies):
    """Return exclusion class string or None (=> must be packaged)."""
    name = os.path.basename(rel_path)
    # engine build-tools subtree under bin/ is test/utility infra, not runtime.
    if rel_path.startswith("bin/tools/"):
        return "test-infra"
    for pat in TEST_INFRA_PATTERNS:
        if pat.match(name):
            return "test-infra"
    if name.lower().endswith(".pdb"):
        return "dev-symbols"
    m = MOD_ASSEMBLY_RE.match(name)
    if m and name not in loaded_mod_assemblies:
        return "non-loaded-mod-assembly"
    if re.search(r"^mods[/\\][\w\-]+-content[/\\]", rel_path):
        return "installer-only"
    return None


def build_source_and_closure(engine_root: Path, mod_root: Path):
    """Enumerate the source set and the required closure set.

    Returns (members, exclusions, notes) where each is a dict/set of
    package-relative paths.
    """
    bin_dir = engine_root / "bin"
    if not bin_dir.is_dir():
        raise ClosureError(f"engine bin dir missing: {bin_dir}")
    mod_yaml = mod_root / "mods" / "cameo" / "mod.yaml"
    if not mod_yaml.exists():
        raise ClosureError(f"cameo mod.yaml missing: {mod_yaml}")

    loaded_mod_assemblies = set(ALWAYS_LOADED_MOD_ASSEMBLIES)
    declared = parse_mod_assemblies(mod_yaml)
    if declared:
        loaded_mod_assemblies.update(declared)
    else:
        # Convention: the mod's own assembly for the launched mod id.
        loaded_mod_assemblies.add("OpenRA.Mods.Cameo.dll")
    loaded_mod_assemblies = {
        a for a in loaded_mod_assemblies if (bin_dir / a).exists()
    }

    members = {}  # relpath -> dict

    # --- managed + native bin closure ---
    bin_members = managed_closure(bin_dir, loaded_mod_assemblies)
    bin_members.update(n for n in KNOWN_NATIVES if (bin_dir / n).exists())
    for cd in culture_dirs(bin_dir):
        for f in iter_files(bin_dir / cd):
            bin_members.add(rel(f, bin_dir))
    for name in sorted(bin_members):
        members[f"bin/{name}"] = {
            "kind": "native" if name.lower() in {n.lower() for n in KNOWN_NATIVES} else "managed",
            "origin": "engine-bin",
            "src": bin_dir / name,
        }

    # --- engine mod content mounts ---
    mounted_mods = engine_mod_mounts(mod_yaml) or ["common"]
    for mod_dir in mounted_mods:
        d = engine_root / "mods" / mod_dir
        if not d.is_dir():
            raise ClosureError(f"mounted engine mods dir missing: {d}")
        for f in iter_files(d):
            members[f"mods/{mod_dir}/{rel(f, d)}"] = {
                "kind": "content",
                "origin": "engine-mods",
                "src": f,
            }

    # --- mod source trees ---
    for rel_dir in MOD_SOURCE_DIRS:
        d = mod_root / rel_dir
        if not d.is_dir():
            raise ClosureError(f"mod source dir missing: {d}")
        for f in iter_files(d):
            members[f"{rel_dir}/{rel(f, d)}"] = {
                "kind": "content",
                "origin": "mod-source",
                "src": f,
            }

    # --- engine-root metadata ---
    for name in ENGINE_ROOT_FILES:
        p = engine_root / name
        if p.exists():
            members[name] = {"kind": "metadata", "origin": "engine-root", "src": p}

    # --- source set = everything we enumerated as candidates ---
    source_set = set()
    exclusions = {}
    for f in iter_files(bin_dir):
        source_set.add(f"bin/{rel(f, bin_dir)}")
    for mod_dir in mounted_mods:
        for f in iter_files(engine_root / "mods" / mod_dir):
            source_set.add(f"mods/{mod_dir}/{rel(f, engine_root / 'mods' / mod_dir)}")
    for rel_dir in MOD_SOURCE_DIRS:
        for f in iter_files(mod_root / rel_dir):
            source_set.add(f"{rel_dir}/{rel(f, mod_root / rel_dir)}")
    for name in ENGINE_ROOT_FILES:
        if (engine_root / name).exists():
            source_set.add(name)

    for path in source_set - set(members):
        cls = classify_exclusion(path, loaded_mod_assemblies)
        if cls is None:
            raise ClosureError(
                f"source file not in closure and matches no exclusion class: {path}"
            )
        exclusions[path] = cls

    return members, exclusions, loaded_mod_assemblies


def verify_package(members, pkg_root: Path):
    """Assert packaged set == closure set, hashing every member."""
    pkg_files = {rel(f, pkg_root) for f in iter_files(pkg_root)}
    closure = set(members)
    missing = sorted(closure - pkg_files)
    extra = sorted(pkg_files - closure)
    hash_mismatches = []
    for path in sorted(closure & pkg_files):
        got = sha256_file(pkg_root / path)
        src_hash = sha256_file(members[path]["src"])
        if got != src_hash:
            hash_mismatches.append(path)
    return {
        "complete": not missing and not extra and not hash_mismatches,
        "missing_from_package": missing,
        "extra_in_package": extra,
        "hash_mismatches": hash_mismatches,
    }


def emit_report(members, exclusions, verify):
    return {
        "algorithm": ALGORITHM,
        "member_count": len(members),
        "exclusion_count": len(exclusions),
        "exclusions_by_class": sorted(
            {c: sorted(p for p, cc in exclusions.items() if cc == c)
             for c in set(exclusions.values())}.items()
        ),
        "three_way": verify,
        "members": [
            {"path": p, "sha256": sha256_file(m["src"]),
             "size_bytes": m["src"].stat().st_size,
             "kind": m["kind"], "origin": m["origin"], "required": True}
            for p, m in sorted(members.items())
        ],
    }


def main(argv=None):
    ap = argparse.ArgumentParser(prog="audit_package_closure.py")
    ap.add_argument("mode", choices=["check", "emit-manifest"], nargs="?", default="check")
    ap.add_argument("--engine-root", required=True, type=Path)
    ap.add_argument("--mod-root", required=True, type=Path)
    ap.add_argument("--pkg-root", type=Path)
    ap.add_argument("--manifest-out", type=Path)
    ap.add_argument("--source-commit")
    ap.add_argument("--created-at")
    ap.add_argument("--replace", action="store_true")
    args = ap.parse_args(argv)

    try:
        members, exclusions, loaded = build_source_and_closure(
            args.engine_root, args.mod_root
        )
    except ClosureError as e:
        print(f"CLOSURE-ERROR: {e}", file=sys.stderr)
        return 2

    verify = {"complete": None, "note": "no --pkg-root given; closure only"}
    if args.pkg_root:
        verify = verify_package(members, args.pkg_root)

    report = emit_report(members, exclusions, verify)
    report["loaded_mod_assemblies"] = sorted(loaded)

    if args.mode == "emit-manifest":
        if not args.manifest_out or not args.source_commit or not args.pkg_root:
            print("emit-manifest requires --pkg-root, --manifest-out and "
                  "--source-commit", file=sys.stderr)
            return 2
        if args.manifest_out.exists() and not args.replace:
            print(f"refusing overwrite: {args.manifest_out} (use --replace)",
                  file=sys.stderr)
            return 2
        manifest = {
            "schema": 1,
            "package_id": f"sentinel-runtime-pkg-{args.created_at or 'undated'}",
            "created_at": args.created_at,
            "host_os": "win-x64",
            "pins": {
                "source_commit": args.source_commit,
                "build_configuration": "Release",
            },
            "entry_points": {
                k: {"path": f"bin/{v}", "sha256": sha256_file(args.pkg_root / "bin" / v) if args.pkg_root else None}
                for k, v in {"openra_exe": "OpenRA.exe",
                             "server_exe": "OpenRA.Server.exe",
                             "utility_exe": "OpenRA.Utility.exe"}.items()
            },
            "members": report["members"],
            "closure": {
                "algorithm": ALGORITHM,
                "seed_set": [f"bin/{s}" for s in SEEDS],
                "enumerator": "tools/audit/audit_package_closure.py",
                "member_count": report["member_count"],
                "complete": bool(verify.get("complete")),
            },
            "exclusions": [
                {"path": p, "class": c} for p, c in sorted(exclusions.items())
            ],
            "hash_algorithm": HASH_ALGORITHM,
            "acceptance": {"accepted_by": None, "accepted_at": None,
                           "acceptance_receipt_sha256": None},
        }
        tmp = args.manifest_out.with_suffix(".tmp")
        tmp.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n",
                       encoding="utf-8", newline="\n")
        os.replace(tmp, args.manifest_out)
        print(f"manifest_sha256 {sha256_file(args.manifest_out)}")

    print(json.dumps({k: v for k, v in report.items() if k != "members"},
                     indent=2, sort_keys=True))
    return 0 if verify.get("complete") in (True, None) else 1


if __name__ == "__main__":
    sys.dont_write_bytecode = True
    sys.exit(main())
