# audit_balance_drift — yaml vs committed balance ledger

**1 ledger(s) drifted** — balance numbers were hand-edited in yaml, or a sanctioned apply run was not followed by re-extraction. Fix via the pipeline, never by hand:

## redalert2mod_naxis

```diff
         "damage": "4000",
-        "falloff": null,
-        "spread": null,
+        "falloff": "100, 0",
+        "spread": "100",
         "tag": "Bullet_Medium_Flat",
```

