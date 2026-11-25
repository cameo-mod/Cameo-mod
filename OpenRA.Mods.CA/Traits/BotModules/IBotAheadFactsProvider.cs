#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits.BotModules
{
	/// <summary>One living enemy seat as the bot OBSERVES it (what it sees now or remembers; nothing the enemy does not show): the values of the master module's enemy profile.</summary>
	public readonly struct AheadEnemyFacts
	{
		/// <summary>The enemy's player actor id: the stable key of the seat.</summary>
		public readonly int Seat;
		public readonly int ArmyValue, DefenceValue, Buildings, Production, Refineries, Harvesters, Yards, Mcvs;

		public AheadEnemyFacts(int seat, int armyValue, int defenceValue, int buildings, int production, int refineries, int harvesters, int yards, int mcvs)
		{
			Seat = seat;
			ArmyValue = armyValue;
			DefenceValue = defenceValue;
			Buildings = buildings;
			Production = production;
			Refineries = refineries;
			Harvesters = harvesters;
			Yards = yards;
			Mcvs = mcvs;
		}
	}

	/// <summary>
	/// ALLIN-TRIGGER-01: the observed enemy facts the squad manager's ahead triggers read. Implemented by the master module (OpenRA.Mods.Cameo, which must not be named from this assembly).
	/// No provider, or one that does not observe through the fog (its facts would be omniscient), means <see cref="AheadFactsEnabled"/> is false and the army, defence, yard, production
	/// and economy triggers stay silent.
	/// </summary>
	public interface IBotAheadFactsProvider
	{
		/// <summary>The facts are fog-honest and fresh enough to read.</summary>
		bool AheadFactsEnabled { get; }

		/// <summary>The living enemy seats (the scoreboard says undefeated), by seat id; the list of the latest snapshot, null when none.</summary>
		IReadOnlyList<AheadEnemyFacts> AheadEnemies { get; }
	}
}
