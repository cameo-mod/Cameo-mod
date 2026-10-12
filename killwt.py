import subprocess, sys
out = subprocess.run(['powershell', '-NoProfile', '-Command',
    "Get-CimInstance Win32_Process -Filter \"Name like 'OpenRA%' or Name like 'dotnet%'\" | Select-Object ProcessId,ExecutablePath | Format-Table -AutoSize"],
    capture_output=True, text=True).stdout
print(out)
for line in out.splitlines():
    parts = line.split()
    if len(parts) >= 2 and 'ember-ars-demo' in line:
        pid = parts[0]
        if pid.isdigit():
            subprocess.run(['taskkill', '/PID', pid, '/F'], capture_output=True)
            print('killed', pid)
