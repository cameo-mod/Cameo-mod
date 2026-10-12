import sys
path, needles = sys.argv[1], [n.lower() for n in sys.argv[2:]]
lines = open(path, encoding='utf-8', errors='replace').read().splitlines()
for l in lines[-60:]:
    if any(n in l.lower() for n in needles):
        print(l)
