# DTA refresh readiness — 12 September 2026

## Safe extraction result

The destructive refresh came from replacing the merged extraction product
`rules_DTA_Classic.ini` with the raw install `INI/Rules.ini`. Both files have
2,322 section headers, but the merged product carries fields that are absent
from the raw file; the section count was therefore not an integrity check.
The failed replacement changed 2,991 corpus fields to `None` and was reverted.

The safe single-source path was run against the preserved DTA packet:

```text
extract_ini_units.py --rules extracted/INI/Rules.ini --engine ts
  --label "DTA Classic" --json <external output>
extract_ini_units.py --rules extracted/INI/Rules.ini
  --overlay extracted/INI/Enhance.ini --engine ts
  --label "DTA Enhanced" --json <external output>
```

Results:

- DTA Classic: **863 rows**.
- DTA Enhanced: **863 rows**.
- Rules SHA-256: `6329487eecaa12fb314c31eef3f21b17812ef15b1baa39f223c8205e2d4745d9`.
- Enhance SHA-256: `40a8a48a14239d43cf8f87e96ec9540960c44cfc5a4285f5ae986f1361256cef`.
- Both outputs match the committed corpus on all semantic row fields; only
  the guarded single-source provenance fields are new.
- No repository or source file was overwritten, and no corpus row changed.

## Map gate

The corrected peer artifact from PR #349 was tested in the current checkout,
but importing it wholesale fails the existing base-corpus contract: it removes
unreviewed rows (`GACNST`, `GASAND`, `ORCATRAN`) and changes the expected weapon-
evidence status for 122 rows. That artifact is based on the master-era
extractor, while this branch contains later reviewed corpus/provenance rules.

The current valid map therefore remains the last published map. Regenerate it
only after a current-base-compatible peer artifact with source provenance is
available. Do not mix the corrected CA/OpenRA document with the old peer inputs
or treat the resulting candidate as a reviewed map.

The remaining DTA work is the measured nine-field flak delta and a safe,
reviewed corpus integration; the single-source extraction itself is now proven
safe and does not justify a gameplay or reference vote by itself.
