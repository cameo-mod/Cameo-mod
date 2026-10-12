import os, shutil
d = r'C:\cameo-wt\_ars_demo\Replays\cameo\{DEV_VERSION}'
os.makedirs(os.path.join(d, 'superseded'), exist_ok=True)
for f in os.listdir(d):
    if f.endswith('.orarep'):
        shutil.move(os.path.join(d, f), os.path.join(d, 'superseded', f))
print(sorted(os.listdir(d)))
