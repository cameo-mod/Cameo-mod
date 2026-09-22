# audit_family_uniqueness — 160 family templates (159 legacy level templates + 1 active level-less base(s))

Shape checks use authored implicit-range geometry, not heaviness-scaled runtime geometry.

  Heavy     51 distinct shapes   OK
  Light     52 distinct shapes   OK
  Medium    51 distinct shapes   OK
  Super      4 distinct shapes   OK
  Trace      1 distinct shapes   OK

  level-less bases (continuous heaviness):

      CannonAP       radius    120  100,0                              Shield 144

  RAW Shield duplicate groups — 3 (base/Medium compatibility is approved and stays visible):

      Shield    144  ->  CannonAP (^Warhead_CannonAP), CannonAP (^Warhead_CannonAP_Medium)
      Shield    155  ->  MissileThermobaric (^Warhead_MissileThermobaric_Light), Nuclear (^Warhead_Nuclear_Super)
          ⚠ DISTINCT legacy families share a Shield value — fewer than two NEW bases are involved; reported, not failed.
      Shield    205  ->  FlakCryo (^Warhead_FlakCryo_Heavy), IncendiaryYakComposition (^Warhead_IncendiaryYakComposition)
          ⚠ DISTINCT legacy families share a Shield value — fewer than two NEW bases are involved; reported, not failed.

OK — no two families share both a radius and a curve at any level (bases incl.), and no distinct NEW family bases share a Shield value. Raw compatibility and legacy duplicates remain listed above.
