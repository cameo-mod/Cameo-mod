import sys, pathlib, argparse

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))
sys.path.insert(0, str(ROOT / "tools" / "audit"))

from apply_balance import YamlEditor
from extract_stats import weapon_class_from_types


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--confirm", action="store_true", help="actually write WeaponClass lines")
    args = ap.parse_args()

    file = ROOT / "mods" / "cameo" / "weapons" / "weapons.yaml"
    ed = YamlEditor(file)

    # strip any previously-written WeaponClass lines so they get reinserted at the top
    ed.lines = [l for l in ed.lines if not l.strip().startswith("WeaponClass:")]
    ed.dirty = True

    changes = []
    for line in ed.lines:
        if not line.startswith("^"):
            continue
        name = line.split(":", 1)[0].strip()
        if not name:
            continue
        h = weapon_class_from_types([name])
        if h is None:
            continue
        res = ed.set_weapon_field(name, "WeaponClass", str(h))
        if "not found" in res or res == "unchanged":
            continue
        changes.append(f"{name}: {res}")

    print(f"Would add/update WeaponClass for {len(changes)} templates in {file}")
    for c in changes:
        print(f"  {c}")

    if args.confirm:
        ed.save()
        print("Saved.")
    else:
        print("Dry-run: use --confirm to apply.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
