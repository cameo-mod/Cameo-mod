# Runtime changes now land as source

PR #325 originally carried its gameplay and chrome changes as an unapplied patch series because
it was authored without an engine.  The branch now contains the reviewed runtime files directly:

- `OpenRA.Mods.Cameo/Traits/DynamicBotInsurance.cs`
- `mods/cameo/rules/player.yaml`, `mods/cameo/rules/defaults.yaml`, and `mods/cameo/ai/ai.yaml`
- `mods/cameo/uibits/flags_4x.png` and generated 1x/2x/3x flag sheets
- `mods/cameo/ContentPacks/StarCraft/Protoss/yaml/weapons.yaml`

There is intentionally no patch applicator.  Verify this branch with:

```powershell
.\tools\preflight-build.ps1
.\make all
python tools\audit\audit_bot_insurance.py
python tools\audit\audit_chrome_scale_variants.py
```

Then launch Cameo to the main menu.  The generated flag variants come from the 4x master via
`python tools/art/generate_chrome_scales.py flags --master flags_4x.png --emit 1,2,3 --write`.
