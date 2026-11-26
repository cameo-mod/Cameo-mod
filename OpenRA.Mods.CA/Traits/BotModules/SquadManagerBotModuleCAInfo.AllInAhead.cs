#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	// ALLIN-TRIGGER-01 (USER 2026-11-25): the dynamic all-in. Needs the CONSOLIDATE-02 launcher (SquadPipeline02Enabled, one launcher), so @classic and every block without it run the old code.
	// Observation only. Every threshold is a yaml number so the learning (LEARN-MULT-01) can move it later.
	public partial class SquadManagerBotModuleCAInfo
	{
		[Desc("ALLIN-TRIGGER-01 family switch (code default off; the six personality blocks set it on) (release; the DEV row is sq_ahead). On (needs SquadPipeline02Enabled): the team leader marks its order ALL-IN when the team is ahead (the triggers below) and ends it again when the observed ratio drops.")]
		public readonly bool ArmyAheadEnabled = false;

		[Desc("ALLIN-TRIGGER-01: no ahead trigger fires before this tick (the first skirmishes decide nothing).")]
		public readonly int ArmyAheadStartTick = 6000;

		[Desc("ALLIN-TRIGGER-01: the observed state (kills, losses, enemy facts, own superweapons) is read at most once per this many ticks. Range 50-1000.")]
		public readonly int ArmyAheadScanTicks = 250;

		[Desc("ALLIN-TRIGGER-01 trigger 4 (row sq_ahead_yard): the enemy's last known construction yard is dead and no MCV is remembered.")]
		public readonly bool ArmyAheadYardEnabled = true;

		[Desc("ALLIN-TRIGGER-01 trigger 5 (row sq_ahead_production): the production buildings seen of an enemy are all dead.")]
		public readonly bool ArmyAheadProductionEnabled = true;

		[Desc("ALLIN-TRIGGER-01 trigger 6 (row sq_ahead_economy): the refineries seen of an enemy are dead and no harvester is known.")]
		public readonly bool ArmyAheadEconomyEnabled = true;

		[Desc("ALLIN-TRIGGER-01 triggers 4-6: at least this percent of the living enemy seats must be crippled (50 = one of two, one of one). Range 1-100.")]
		public readonly int ArmyAheadMajorityPercent = 50;

		[Desc("ALLIN-TRIGGER-01 trigger 2 (row sq_ahead_defeat): an enemy player is defeated and the team has the numbers; fires again for every further defeat.")]
		public readonly bool ArmyAheadDefeatEnabled = true;

		[Desc("ALLIN-TRIGGER-01 trigger 2: the numbers are with the team when it has more living seats than the enemy side, or an army of at least this percent of the enemy army it knows.")]
		public readonly int ArmyAheadDefeatArmyPercent = 120;

		[Desc("ALLIN-TRIGGER-01 trigger 1 (row sq_ahead_trade): the team destroyed at least ArmyAheadTradePercent percent of the value it lost (the observable proxy of twice the points).")]
		public readonly bool ArmyAheadTradeEnabled = true;

		[Desc("ALLIN-TRIGGER-01 trigger 1: kills / losses in percent. Range 100-1000.")]
		public readonly int ArmyAheadTradePercent = 200;

		[Desc("ALLIN-TRIGGER-01 trigger 1: the team must have destroyed at least this much value (early noise is no lead).")]
		public readonly int ArmyAheadTradeMinKills = 4000;

		[Desc("ALLIN-TRIGGER-01 trigger 7 (row sq_ahead_battle): a big battle was won inside ArmyAheadBattleWindowTicks while the team army stands.")]
		public readonly bool ArmyAheadBattleEnabled = true;

		[Desc("ALLIN-TRIGGER-01 trigger 7: the length of the battle window in ticks. Range 300-6000.")]
		public readonly int ArmyAheadBattleWindowTicks = 1500;

		[Desc("ALLIN-TRIGGER-01 trigger 7: kills / losses of the window in percent.")]
		public readonly int ArmyAheadBattlePercent = 200;

		[Desc("ALLIN-TRIGGER-01 trigger 7: the enemy value destroyed inside the window.")]
		public readonly int ArmyAheadBattleMinKills = 6000;

		[Desc("ALLIN-TRIGGER-01 trigger 3 (row sq_ahead_army): the team army is at least ArmyAheadArmyPercent percent of the enemy army the bot sees or remembers plus its defences.")]
		public readonly bool ArmyAheadArmyEnabled = true;

		[Desc("ALLIN-TRIGGER-01 trigger 3: own army / (enemy army + enemy defences) in percent (150 = 1.5 times). Range 100-1000.")]
		public readonly int ArmyAheadArmyPercent = 150;

		[Desc("ALLIN-TRIGGER-01 trigger 8 (row sq_ahead_counter): the enemy is busy at an ally (it asks for defence, the own home is quiet), so the team counters at its base.")]
		public readonly bool ArmyAheadCounterEnabled = true;

		[Desc("ALLIN-TRIGGER-01 trigger 8: the team army must be at least this percent of the enemy defences the bot knows.")]
		public readonly int ArmyAheadCounterPercent = 150;

		[Desc("ALLIN-TRIGGER-01 trigger 10 (row sq_ahead_super): an own superweapon is ready, the all-in goes in with the strike.")]
		public readonly bool ArmyAheadSuperEnabled = true;

		[Desc("ALLIN-TRIGGER-01 trigger 10: a support power counts as a superweapon when its charge interval is at least this many ticks.")]
		public readonly int ArmyAheadSuperMinChargeTicks = 4500;

		[Desc("ALLIN-TRIGGER-01 (row sq_ahead_deescalate): an all-in order an ahead trigger started is ended (its members stop sending every unit) when the team is behind.")]
		public readonly bool ArmyAheadDeescalateEnabled = true;

		[Desc("ALLIN-TRIGGER-01 de-escalation: the team is behind when its army is below this percent of the enemy army it knows, or the exchange of the last ArmyAheadDeescalateWindowTicks is below it (100 = ratio 1.0).")]
		public readonly int ArmyAheadDeescalatePercent = 100;

		[Desc("ALLIN-TRIGGER-01 de-escalation: the window of the exchange check, in ticks. Range 600-9000.")]
		public readonly int ArmyAheadDeescalateWindowTicks = 3000;

		[Desc("ALLIN-TRIGGER-01 de-escalation: the exchange counts only after the team lost at least this much value inside the window.")]
		public readonly int ArmyAheadDeescalateMinLoss = 2000;

		[Desc("ALLIN-TRIGGER-01: an ahead trigger fires only while the team is not behind this percent (a little above the de-escalation percent, so an order is not ended and started again at once).")]
		public readonly int ArmyAheadResumePercent = 110;
	}
}
