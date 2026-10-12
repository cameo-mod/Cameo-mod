import os, glob
logs = glob.glob(r'C:\Users\AedisToru\AppData\Roaming\OpenRA\Logs\exception-2026-10-04T2*.log')
new = [f for f in logs if os.path.basename(f) > 'exception-2026-10-04T204333Z']
print('new after 204333:', new)
