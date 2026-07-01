"""Migrate a Cameo-generated .oramap so it loads in Combined Arms (CA).

Cameo and CA share the JUNGLE/BARREN tile ART but renumber template Ids
differently. A generated map stores raw (templateId, index) cells in map.bin, so
to play a Cameo map in CA we: (1) remap each cell's templateId to CA's equivalent
(matched by shared art stem), and (2) flip RequiresMod to ca. frameIndex is kept
(matched templates have identical cell layout).

Usage:
    python tools/migrate_map_to_ca.py <map.oramap> [out.oramap]
    # options: --ca <path-to-combined-arms> --cameo <path-to-Cameo-mod>

The Cameo->CA Id remap is DERIVED at runtime from both mods' tileset YAML (by
shared image stem), so it is never a stale hand table. Tiles with no CA match are
left unchanged and reported (a coverage gap to inspect).
"""
import sys, os, re, io, zipfile, struct, argparse
from collections import defaultdict

def parse_templates(path):
    """Id -> image stem (lowercase, no ext)."""
    txt = open(path, encoding="utf-8", errors="replace").read()
    out = {}
    for b in re.split(r"\n(?=\tTemplate@)", txt):
        mid = re.search(r"^\s*Id:\s*(\d+)", b, re.M)
        if not mid:
            continue
        mim = re.search(r"^\s*Images?:\s*([^\n,]+)", b, re.M)
        img = os.path.splitext(mim.group(1).strip().lower())[0] if mim else None
        out[int(mid.group(1))] = img
    return out

def norm_stem(stem):
    """Cameo's upscaled SHPs map back to the shared original stems."""
    m = re.match(r"jungle_sh(\d+)$", stem or "")
    if m:
        return ["sh" + m.group(1), "sh%02d" % int(m.group(1))]
    m = re.match(r"jungle_bridge(\w+)$", stem or "")
    if m:
        return ["bridge" + m.group(1)]
    return [stem]

def derive_remap(cameo_yaml, ca_yaml):
    cam = parse_templates(cameo_yaml)
    ca = parse_templates(ca_yaml)
    ca_by_img = defaultdict(list)
    for i, img in ca.items():
        if img:
            ca_by_img[img].append(i)
    remap = {}
    for cid, img in cam.items():
        for c in norm_stem(img):
            if c in ca_by_img:
                remap[cid] = ca_by_img[c][0]
                break
    return remap

def tileset_of(map_yaml_text):
    m = re.search(r"^Tileset:\s*(\S+)", map_yaml_text, re.M)
    return m.group(1).upper() if m else None

def set_requires_mod(map_yaml_text, mod):
    if re.search(r"^RequiresMod:", map_yaml_text, re.M):
        return re.sub(r"^RequiresMod:.*$", f"RequiresMod: {mod}", map_yaml_text, count=1, flags=re.M)
    # insert after MapFormat line (always present near top)
    return re.sub(r"(^MapFormat:.*$)", r"\1\nRequiresMod: " + mod, map_yaml_text, count=1, flags=re.M)

def remap_bin(data, remap):
    """Rewrite tile-layer template Ids in a map.bin byte array. Returns (new_bytes, stats)."""
    fmt = data[0]
    width, height = struct.unpack_from("<HH", data, 1)
    tiles_off, heights_off, res_off = struct.unpack_from("<III", data, 5)
    buf = bytearray(data)
    changed = 0
    unmatched = defaultdict(int)
    pos = tiles_off
    for _ in range(width * height):
        t = struct.unpack_from("<H", buf, pos)[0]
        if t in remap:
            if remap[t] != t:
                struct.pack_into("<H", buf, pos, remap[t])
                changed += 1
        elif t not in (0,):
            unmatched[t] += 1
        pos += 3  # u16 type + u8 index
    return bytes(buf), {"format": fmt, "size": (width, height), "changed": changed,
                        "unmatched": dict(unmatched)}

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("map")
    ap.add_argument("out", nargs="?")
    ap.add_argument("--cameo", default=r"C:\Users\Blackrobe\repo\Cameo-mod")
    ap.add_argument("--ca", default=r"C:\Users\Blackrobe\repo\combined-arms")
    a = ap.parse_args()

    with zipfile.ZipFile(a.map) as z:
        names = z.namelist()
        map_yaml = z.read("map.yaml").decode("utf-8")
        map_bin = z.read("map.bin")
        others = {n: z.read(n) for n in names if n not in ("map.yaml", "map.bin")}

    ts = tileset_of(map_yaml)
    if ts not in ("JUNGLE", "BARREN"):
        print(f"Tileset is {ts}; this tool only supports JUNGLE/BARREN. Aborting.")
        sys.exit(2)

    cam_yaml = os.path.join(a.cameo, "mods", "cameo", "tilesets", ts.lower() + ".yaml")
    ca_yaml = os.path.join(a.ca, "mods", "ca", "tilesets", ts.lower() + ".yaml")
    remap = derive_remap(cam_yaml, ca_yaml)

    new_bin, stats = remap_bin(map_bin, remap)
    new_yaml = set_requires_mod(map_yaml, "ca")

    out = a.out or re.sub(r"\.oramap$", "", a.map) + ".ca.oramap"
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as z:
        z.writestr("map.yaml", new_yaml)
        z.writestr("map.bin", new_bin)
        for n, b in others.items():
            z.writestr(n, b)

    print(f"tileset={ts}  map.bin {stats['size'][0]}x{stats['size'][1]} (format {stats['format']})")
    print(f"remapped {stats['changed']} tile cells via {len(remap)}-entry stem table; RequiresMod -> ca")
    if stats["unmatched"]:
        tot = sum(stats["unmatched"].values())
        print(f"WARNING: {tot} cells use {len(stats['unmatched'])} tile Id(s) with NO CA match "
              f"(left unchanged): {dict(list(stats['unmatched'].items())[:20])}")
    else:
        print("all tile cells covered (no unmatched Ids).")
    print(f"wrote -> {out}")
    print("Next: drop this into CA's maps folder and load it in Combined Arms.")

if __name__ == "__main__":
    main()
