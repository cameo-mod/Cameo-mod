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
	// LATEPUSH-01 P2 (rows lp_army_cap, lp_all_in_publish, lp_reinforce, lp_handover; design 01 D3 to D5, user decision audit A6). Only read behind the one launcher, every switch off in code and in classic.
	public partial class SquadManagerBotModuleCAInfo
	{
		[Desc("LATEPUSH-01 P2 (row lp_army_cap; needs lp_age_min): the age-scaled team minimum is capped at the learned share LatePushArmyCapPermille of the team's eligible army instead of the fixed LatePushCapPercent.",
			"An open all-in and the orders the team leader publishes to the members never apply the cap. Off in code and classic.")]
		public readonly bool LatePushArmyCapEnabled = false;

		[Desc("LATEPUSH-01 P2: the PRIOR of the learned army cap (knob latepush.army_cap), permille of the team's eligible army, clamped to 500 - 1000. Learnable (DEV learning only; a release build reads the accepted snapshot).")]
		public readonly int LatePushArmyCapPermille = 800;

		[Desc("LATEPUSH-01 P2 (row lp_all_in_publish; needs the all-in rows): an open all-in trigger publishes when the team's eligible army (not only its blob) reaches WAVE-MIN, and the guard claims every unit above its floor. No age factor, no cap, no personality number. Off in code and classic.")]
		public readonly bool LatePushAllInPublishEnabled = false;

		[Desc("LATEPUSH-01 P2 (row lp_reinforce; needs the one launcher): a pushed order is reinforced ONCE per seat, as one group, when the seat's pushed squads hold less than LatePushReinforceBelowPercent of what they committed. Off in code and classic.")]
		public readonly bool LatePushReinforceEnabled = false;

		[Desc("LATEPUSH-01 P2: the single reinforcement is due below this percent of the committed units.")]
		public readonly int LatePushReinforceBelowPercent = 60;

		[Desc("LATEPUSH-01 P2: the team is not behind when the observed own/enemy army ratio is at least this percent (an unseen enemy reads 100, so only an observed deficit holds the reinforcement back).")]
		public readonly int LatePushReinforceMinRatioPercent = 100;

		[Desc("LATEPUSH-01 P2 (row lp_handover; needs the one launcher): a personality switch hands the pushed squads of the old module to the open order instead of dissolving them (the new module re-joins them as one group). Off in code and classic.")]
		public readonly bool LatePushHandoverEnabled = false;
	}
}
