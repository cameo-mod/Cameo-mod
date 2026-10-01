# audit_balance_drift — yaml vs committed balance ledger

**1 ledger(s) drifted** — balance numbers were hand-edited in yaml, or a sanctioned apply run was not followed by re-extraction. Fix via the pipeline, never by hand:

## tiberiansun_cabal

```diff
       "reloaddelay": "25",
+      "requires": "!cydamaged",
       "slot": "Armament@PRIMARY",
@@ -3999,2 +4000,3 @@
       "range": "1536",
+      "requires": "cydamaged",
       "slot": "Armament@Suicide",
```

