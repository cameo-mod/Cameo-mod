# AI personality audit

- Selector conditions: `personality-expansion, personality-rush, personality-steamroller, personality-tech, personality-turtle`
- Consumed conditions: `personality-expansion, personality-rush, personality-steamroller, personality-tech, personality-turtle`
- Personality blocks: 5/5
- Explicit tuning allow-list: `AttackForceInterval, DangerScanRadius, IdleScanRadius, JoinGuerrilla, MaxBaseRadius, MaxGuerrillaSize, MaxIdleUnits, MinimumAttackForceDelay, ProtectUnitScanRadius, ProtectionScanRadius, SquadSize, SquadSizeRandomBonus, SquadValue, SquadValueMaxEarlyBonus, SquadValueMaxLateBonus, SquadValueMinLateBonus`

## PASS
- Shared non-tuning fields are byte-identical across all five instances.
- GrantRandomCondition and squad-manager condition sets match exactly.
- No dead RushInterval/RushAttackScanRadius keys remain.
