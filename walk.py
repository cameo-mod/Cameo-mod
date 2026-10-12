import os, sys
for r, d, fs in os.walk(sys.argv[1]):
    for f in fs:
        print(os.path.join(r, f), os.path.getsize(os.path.join(r, f)))
