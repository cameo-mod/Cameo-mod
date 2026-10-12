import os
d = r'C:\cameo-wt\_ars_demo\Replays\cameo\{DEV_VERSION}'
for f in sorted(os.listdir(d)):
    print(f, os.path.getsize(os.path.join(d, f)))
