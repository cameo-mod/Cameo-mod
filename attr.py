# Hotspot attribution: classify stutter-flagged units + alternations by emitter tag.
# Tags: h2 = protection rally/lure + engage (hotspot #2),
#       h8 = defence staging/flee/idle grouped (hotspot #8),
#       None = attack-force/march-lattice/non-squad orders (~#3 + other).
import sys, collections
sys.path.insert(0, r"C:\cameo-wt\ar2-desync")
from order_trace import trace

files = sys.argv[1:]
grand_pairs = collections.Counter()
grand_units = collections.Counter()
grand_flag_pairs = collections.Counter()

for path in files:
    units, raw = trace(path)
    label = path.split("\\")[-1]
    pairs = collections.Counter()          # (tagA -> tagB) alternation counts
    unit_tags = collections.Counter()      # flagged unit -> dominant tag class
    flag_pairs = collections.Counter()     # pair counts restricted to flagged units
    n_flag = 0
    for uid, seq in units.items():
        seq.sort(key=lambda e: e[0])
        alts = [(seq[i][0], seq[i-1][1], seq[i][1], seq[i-1][3] or "x", seq[i][3] or "x")
                for i in range(1, len(seq)) if seq[i][1] != seq[i-1][1]]
        for _, oa, ob, ta, tb in alts:
            pairs[f"{ta}:{oa}->{tb}:{ob}"] += 1
        # stutter flag: >=2 alts in any 25-tick window
        fr = [a[0] for a in alts]
        best = j = 0
        for i, f in enumerate(fr):
            while fr[j] < f - 25:
                j += 1
            best = max(best, i - j + 1)
        if best >= 2:
            n_flag += 1
            tags = collections.Counter()
            for _, oa, ob, ta, tb in alts:
                tags[ta] += 1
                tags[tb] += 1
                flag_pairs[f"{ta}:{oa}->{tb}:{ob}"] += 1
            dom = tags.most_common(1)[0][0] if tags else "none"
            unit_tags[dom] += 1
    print(f"[{label}] flagged={n_flag}")
    for k, v in pairs.most_common(10):
        print(f"    all : {k}: {v}")
    for k, v in flag_pairs.most_common(10):
        print(f"    flag: {k}: {v}")
    print(f"    unit dominant tag: {dict(unit_tags)}")
    grand_pairs.update(pairs)
    grand_units.update(unit_tags)
    grand_flag_pairs.update(flag_pairs)

print("=== AGGREGATE ===")
for k, v in grand_pairs.most_common(12):
    print(f"  all : {k}: {v}")
for k, v in grand_flag_pairs.most_common(12):
    print(f"  flag: {k}: {v}")
print(f"  flagged-unit dominant tag: {dict(grand_units)}")
