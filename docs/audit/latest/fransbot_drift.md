# Fransbot vendored-source drift check

Upstream: C:\Users\AedisToru\Documents\GitHub\OpenRA-Fransbot\src\Fransbot.OpenRA\Traits  (base ref 9150ded (V1.29.48, 'Native Amphibious Handoffs + Ferry Liveness'))

Vendored files: 29

FAIL — vendored deltas changed since baseline:
  - FransAirCommanderBotModule.cs: diff vs upstream moved [1100, 170] -> [134, 64] (+added/-removed lines)
  - FransBaseBuilderBotModule.cs: diff vs upstream moved [321, 76] -> [325, 79] (+added/-removed lines)
  - FransBotLog.cs: diff vs upstream moved [4, 4] -> [0, 0] (+added/-removed lines)
  - FransCombatIntelBotModule.cs: diff vs upstream moved [18, 2] -> [25, 2] (+added/-removed lines)
  - FransCommandBidBotModule.cs: diff vs upstream moved [112, 7] -> [85, 0] (+added/-removed lines)
  - FransCommanderCoreBotModule.cs: diff vs upstream moved [228, 31] -> [46, 7] (+added/-removed lines)
  - FransDefenseCommanderBotModule.cs: diff vs upstream moved [78, 36] -> [66, 25] (+added/-removed lines)
  - FransGeneralBotModule.cs: diff vs upstream moved [533, 73] -> [239, 28] (+added/-removed lines)
  - FransGroundCommanderBotModule.cs: diff vs upstream moved [207, 37] -> [201, 37] (+added/-removed lines)
  - FransGroundTransferBotModule.cs: diff vs upstream moved [930, 159] -> [80, 22] (+added/-removed lines)
  - FransMcvExpansionManagerBotModule.cs: diff vs upstream moved [1021, 286] -> [100, 60] (+added/-removed lines)
  - FransMinelayerBotModule.cs: diff vs upstream moved [4, 8] -> [7, 10] (+added/-removed lines)
  - FransSpecOpsCommanderBotModule.cs: diff vs upstream moved [209, 78] -> [170, 61] (+added/-removed lines)
  - FransStrategicMapBotModule.cs: diff vs upstream moved [61, 29] -> [30, 24] (+added/-removed lines)
  - FransSupplyTruckBotModule.cs: diff vs upstream moved [4, 3] -> [7, 5] (+added/-removed lines)
  - FransTransportCommanderBotModule.cs: diff vs upstream moved [791, 123] -> [770, 91] (+added/-removed lines)
  - FransUnitBuilderBotModule.cs: diff vs upstream moved [190, 66] -> [198, 70] (+added/-removed lines)

If the change is intentional (new port fix or upstream re-vendor),
re-run with --write-baseline and commit the baseline with the code.
