#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	// ALLIN-TRIGGER-01 (USER 2026-11-25): the dynamic all-in. One row for the family and one per trigger, each with its yaml switch (SquadManagerBotModuleCAInfo.AllInAhead.cs).
	// Row off = that trigger never fires; the family row off = none of them and no de-escalation (the old inputs, cap, idle, won, stalemate and the late tick, stay).
	public static partial class AblationIds
	{
		public const string SqAhead = "sq_ahead";
		public const string SqAheadYard = "sq_ahead_yard";
		public const string SqAheadProduction = "sq_ahead_production";
		public const string SqAheadEconomy = "sq_ahead_economy";
		public const string SqAheadDefeat = "sq_ahead_defeat";
		public const string SqAheadTrade = "sq_ahead_trade";
		public const string SqAheadBattle = "sq_ahead_battle";
		public const string SqAheadArmy = "sq_ahead_army";
		public const string SqAheadCounter = "sq_ahead_counter";
		public const string SqAheadSuper = "sq_ahead_super";

		// An all-in order an ahead trigger started is ended again when the observed ratio drops below about 1.0.
		public const string SqAheadDeescalate = "sq_ahead_deescalate";
	}
}
