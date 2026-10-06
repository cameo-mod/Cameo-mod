#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software. It is made
 * available under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or (at your
 * option) any later version. For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits.BotModules
{
	/// <summary>
	/// Frozen, offline-fitted engage/retreat thresholds. Implementations must return
	/// neutral values when a scope has no evidence; the combat predictor remains the
	/// only source of combat strength.
	/// </summary>
	public interface IBotFightThresholds
	{
		int RetreatRatioPct(string ownFaction, string enemyFaction, int fallback);
		int EngageMarginPct(string ownFaction, string enemyFaction, int fallback);
	}
}
