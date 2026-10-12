Get-ChildItem -Recurse C:\cameo-wt\_ars_ev_before\Replays, C:\cameo-wt\_ars_ev_after\Replays -Filter *.orarep -ErrorAction SilentlyContinue | ForEach-Object { "$($_.Length) $($_.FullName)" }
Get-Content C:\cameo-wt\_ars_ev_after\batch_summary.json -ErrorAction SilentlyContinue | Select-String '"completed"|"matches"|"timed_out"|"stalled"|"died"|"norecord"'
