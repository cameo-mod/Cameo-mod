# audit_balance_drift — yaml vs committed balance ledger

**2 ledger(s) drifted** — balance numbers were hand-edited in yaml, or a sanctioned apply run was not followed by re-extraction. Fix via the pipeline, never by hand:

## d2k_atreides

```diff
       "versus_templates": [
-       "^MG"
+       "^MG",
+       "^d2k_fremen_s"
       ],
```

## d2k_corrino

```diff
        "^Projectile_Laser_Heavy",
-       "^Effect_Laser_Heavy"
+       "^d2k_laser_heavy"
       ],
@@ -1703,3 +1703,3 @@
        "^Projectile_Laser_Heavy",
-       "^Effect_Laser_Heavy"
+       "^d2k_laser_heavy"
       ],
```

