# AI personality audit

- Selector conditions: `personality-expansion, personality-rush, personality-steamroller, personality-tech, personality-turtle`
- Consumed conditions: `personality-expansion, personality-rush, personality-steamroller, personality-tech, personality-turtle`
- Personality blocks: 5/5
- Personality notifications: 5/5
- BotLimits reaction delays: `7500, 6750, 6000, 5250, 4500, 3750, 3000, 2250, 1500, 750`
- Explicit tuning allow-list: `AttackForceInterval, DangerScanRadius, IdleScanRadius, JoinGuerrilla, MaxBaseRadius, MaxGuerrillaSize, MaxIdleUnits, MinimumAttackForceDelay, ProtectUnitScanRadius, ProtectionScanRadius, SquadSize, SquadSizeRandomBonus, SquadValue, SquadValueMaxEarlyBonus, SquadValueMaxLateBonus, SquadValueMinLateBonus`

## PASS
- Shared non-tuning fields are byte-identical across all five instances.
- BotPersonalityController and squad-manager condition sets match exactly.
- Personality conditions have exactly one matching notification block each.
- No dead RushInterval/RushAttackScanRadius keys remain.
