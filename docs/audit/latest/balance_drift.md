# audit_balance_drift — yaml vs committed balance ledger

**1 ledger(s) drifted** — balance numbers were hand-edited in yaml, or a sanctioned apply run was not followed by re-extraction. Fix via the pipeline, never by hand:

## d2k_ordos

```diff
         "damage": "10000",
-        "falloff": null,
-        "spread": "150",
-        "tag": "LaserWeapon",
-        "type": "SpreadDamage"
-       },
-       {
-        "damage": "1",
-        "falloff": null,
-        "spread": "75",
-        "tag": "LaserWeaponPercentage",
-        "type": "AreaDamagePercentage"
```

