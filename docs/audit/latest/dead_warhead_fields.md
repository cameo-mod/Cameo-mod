scanned 7014 resolved warhead nodes across 2760 weapons

⚠ warhead types with no C# source found (not checked):
     6898  CreateEffect
     5510  LeaveSmudge
     3066  SpreadDamage
     1934  GrantExternalCondition
     1504  DamagesConcrete
      327  SpawnActor
      206  FireShrapnel
       93  TargetDamage
       93  SpawnSmokeParticle
       72  FireCluster
       55  ApplyPhysicalState
       50  DetachDelayedWeapon
       49  DestroyResource
       38  OpenToppedDamage
       32  FireFragment
       30  CreateTintedCells
       25  ShakeScreen
       18  CreateResource
       13  FlashTargetsInRadius
       11  AttachDelayedWeapon
       10  RevealShroud
        6  FlashEffect
        6  TriggerLayerWeapon
        5  ChangeOwner
        3  FireRadius
        3  BackFireShrapnel

DEAD FIELDS — written in yaml, silently discarded by FieldLoader.Load:

   1691 weapons   AreaDamage.ValidTargets
   1682 weapons   AreaDamage.Damage
   1682 weapons   AreaDamage.DamageTypes
   1680 weapons   AreaDamage.ValidRelationships
   1680 weapons   AreaDamage.Versus
    671 weapons   AreaDamagePercentage.Damage
    626 weapons   AreaDamagePercentage.DamageTypes
    625 weapons   AreaDamagePercentage.ValidTargets
    608 weapons   AreaDamagePercentage.Versus
    577 weapons   AreaDamagePercentage.UpdatesUnitStatistics
    257 weapons   AreaDamage.InvalidTargets
    252 weapons   AffectsIntegrity.Damage
    196 weapons   AffectsIntegrity.ValidRelationships
    116 weapons   AffectsIntegrity.ValidTargets
     99 weapons   AffectsIntegrity.InvalidTargets
     84 weapons   AreaDamagePercentage.AffectsParent
     43 weapons   AreaDamagePercentage.InvalidTargets
     39 weapons   AreaDamagePercentage.Delay
     37 weapons   AreaDamagePercentage.ValidRelationships
     22 weapons   AreaDamage.Delay
     20 weapons   AreaDamage.AffectsParent
     16 weapons   AreaDamagePercentage.DebugOverlayColor
     14 weapons   SpawnActorInArea.ImpactActors
      6 weapons   AffectsIntegrity.DamageTypes
      6 weapons   SpawnActorOrWeapon.ValidTargets
      5 weapons   AffectsIntegrity.Falloff
      3 weapons   WarpDamage.Damage
      3 weapons   WarpDamage.Spread
      3 weapons   WarpDamage.DamageTypes
      3 weapons   WarpDamage.ValidTargets
      3 weapons   MindControl.Delay
      3 weapons   SpawnActorOrWeapon.Delay
      2 weapons   AffectsIntegrity.Delay
      2 weapons   AreaDamagePercentage.AirThreshold
      1 weapons   FireReverseRadius.ValidTargets
      1 weapons   FireReverseRadius.ImpactActors
      1 weapons   FireReverseRadius.AirThreshold
      1 weapons   MindControl.ValidTargets
      1 weapons   StealResource.ValidTargets
      1 weapons   StealResource.ValidRelationships
      1 weapons   AreaDamage.Burst
      1 weapons   AffectsIntegrity.AffectsParent

FAIL 42 dead field kind(s) on 1902 weapons (ratchet 15)
**A warhead field was just written that the engine will silently discard.** Fix the field or the type; do not raise DEAD_FIELD_BASELINE.
