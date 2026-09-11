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

### Newly attached archive

Aedis supplied a newer `INI.zip` after this check. Its archive SHA-256 is
`76765cb3fdb96ab9470abad45e828c8502701b463ba2b8bb2acb403f442a3ec4`.
The contained raw files hash to:

- `Rules.ini`: `786f0ae5babce2e9052c6a484f31c62663bf5150d0e652a3fe2efa75a5a126c9`
- `Enhance.ini`: `d836d5a9f9ac227e6b56262e96e9f665e58af10099b63712ec210ee25176edde`

The same guarded single-source commands produce 862 rows for each label. The
raw output removes `CITY01B` and changes 453 semantic rows for DTA Classic
relative to the committed 863-row corpus. That is expected evidence of the
merged-product dependency: raw `Rules.ini` does not contain every field carried
by the existing merged extraction file. It is therefore safe to read and hash,
but **not safe to replace the corpus with** until the exact merge recipe is
recovered and guarded.

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

The remaining DTA work is the measured nine-field flak delta and recovery of
the merged extraction recipe. Single-source extraction is safe for evidence,
but neither archive revision justifies a corpus replacement, gameplay change
or reference vote by itself.
