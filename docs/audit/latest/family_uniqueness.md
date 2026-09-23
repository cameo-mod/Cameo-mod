# audit_family_uniqueness — 160 family templates (159 legacy level templates + 1 active level-less base(s))

Shape checks use authored implicit-range geometry, not heaviness-scaled runtime geometry.

  Heavy     51 distinct shapes   OK
  Light     52 distinct shapes   OK
  Medium    51 distinct shapes   OK
  Super      4 distinct shapes   OK
  Trace      1 distinct shapes   OK

  level-less bases (continuous heaviness):

      CannonAP       radius    120  100,0                              Shield 144

  RAW Shield duplicate groups — 17 (base/Medium compatibility is approved and stays visible):

      Shield    144  ->  CannonAP (^Warhead_CannonAP), CannonAP (^Warhead_CannonAP_Medium)
      Shield    155  ->  MissileThermobaric (^Warhead_MissileThermobaric_Light), Nuclear (^Warhead_Nuclear_Super)
          ⚠ DISTINCT legacy families share a Shield value — fewer than two NEW bases are involved; reported, not failed.
      Shield    174  ->  BlastSonic (^Warhead_BlastSonic_Light), Bullet (^Warhead_Bullet_Heavy)
          ⚠ DISTINCT legacy families share a Shield value — fewer than two NEW bases are involved; reported, not failed.
      Shield    190  ->  MissileSonic (^Warhead_MissileSonic_Light), Toxic (^Warhead_Toxic_Light)
          ⚠ DISTINCT legacy families share a Shield value — fewer than two NEW bases are involved; reported, not failed.
      Shield    192  ->  CannonSonic (^Warhead_CannonSonic_Light), Flak (^Warhead_Flak_Heavy)
          ⚠ DISTINCT legacy families share a Shield value — fewer than two NEW bases are involved; reported, not failed.
      Shield    194  ->  BulletSonic (^Warhead_BulletSonic_Light), MissileThermobaric (^Warhead_MissileThermobaric_Heavy)
          ⚠ DISTINCT legacy families share a Shield value — fewer than two NEW bases are involved; reported, not failed.
      Shield    197  ->  BlastSonic (^Warhead_BlastSonic_Medium), BulletThermobaric (^Warhead_BulletThermobaric_Heavy)
          ⚠ DISTINCT legacy families share a Shield value — fewer than two NEW bases are involved; reported, not failed.
      Shield    205  ->  FlakCryo (^Warhead_FlakCryo_Heavy), IncendiaryYakComposition (^Warhead_IncendiaryYakComposition)
          ⚠ DISTINCT legacy families share a Shield value — fewer than two NEW bases are involved; reported, not failed.
      Shield    213  ->  Chemical (^Warhead_Chemical_Medium), MissileSonic (^Warhead_MissileSonic_Medium)
          ⚠ DISTINCT legacy families share a Shield value — fewer than two NEW bases are involved; reported, not failed.
      Shield    215  ->  CannonSonic (^Warhead_CannonSonic_Medium), Thermobaric (^Warhead_Thermobaric_Heavy)
          ⚠ DISTINCT legacy families share a Shield value — fewer than two NEW bases are involved; reported, not failed.
      Shield    218  ->  BulletSonic (^Warhead_BulletSonic_Medium), MissileChem (^Warhead_MissileChem_Medium)
          ⚠ DISTINCT legacy families share a Shield value — fewer than two NEW bases are involved; reported, not failed.
      Shield    221  ->  BlastSonic (^Warhead_BlastSonic_Heavy), MissileQuantum (^Warhead_MissileQuantum_Light)
          ⚠ DISTINCT legacy families share a Shield value — fewer than two NEW bases are involved; reported, not failed.
      Shield    230  ->  Inferno (^Warhead_Inferno_Medium), MissileSonic (^Warhead_MissileSonic_Heavy)
          ⚠ DISTINCT legacy families share a Shield value — fewer than two NEW bases are involved; reported, not failed.
      Shield    234  ->  CannonSonic (^Warhead_CannonSonic_Heavy), MissileFire (^Warhead_MissileFire_Heavy)
          ⚠ DISTINCT legacy families share a Shield value — fewer than two NEW bases are involved; reported, not failed.
      Shield    235  ->  BulletSonic (^Warhead_BulletSonic_Heavy), CannonTesla (^Warhead_CannonTesla_Light)
          ⚠ DISTINCT legacy families share a Shield value — fewer than two NEW bases are involved; reported, not failed.
      Shield    248  ->  MissileTesla (^Warhead_MissileTesla_Medium), Sonic (^Warhead_Sonic_Medium)
          ⚠ DISTINCT legacy families share a Shield value — fewer than two NEW bases are involved; reported, not failed.
      Shield    259  ->  Sonic (^Warhead_Sonic_Heavy), Waveforce (^Warhead_Waveforce_Medium)
          ⚠ DISTINCT legacy families share a Shield value — fewer than two NEW bases are involved; reported, not failed.

OK — no two families share both a radius and a curve at any level (bases incl.), and no distinct NEW family bases share a Shield value. Raw compatibility and legacy duplicates remain listed above.
