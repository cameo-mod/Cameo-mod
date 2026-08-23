# audit_balance_drift — yaml vs committed balance ledger

**1 ledger(s) drifted** — balance numbers were hand-edited in yaml, or a sanctioned apply run was not followed by re-extraction. Fix via the pipeline, never by hand:

## tiberiansun_gdi

```diff
    },
-   "TSDPODE1": {
-    "armor": {
-     "src": "inherited",
-     "v": "Light"
-    },
-    "buildable": false,
-    "cost": {
-     "src": "inherited",
-     "v": "10"
-    },
-    "design": {
```

