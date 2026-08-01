=== Effect Warhead Naming Audit ===
Template CreateEffect warhead names found: ['@1', '@2Eff', '@3Eff', '@3EffWater', '@4EffAir', '@4EffWater', '@4Eff_2', '@4Eff_2Water', '@4Others', '@4Vehicles', '@Effect', '@Effect1', '@Effect2', '@EffectAir', '@EffectWater', '@EffectWeld', '@ShieldHitEffect', '@ShieldHitEffectNuclear']
Total concrete CreateEffect warheads: 1577
Total violations: 2
Files with violations: 2

--- ContentPacks\TiberianSun\GDI\yaml\weapons.yaml (1 violations) ---
  L1246: DropPodExplode -> Warhead@1Eff: CreateEffect

--- maps\survival_extracted\Weapons.yaml (1 violations) ---
  L28: PortableIoncannon -> Warhead@0Eff: CreateEffect

