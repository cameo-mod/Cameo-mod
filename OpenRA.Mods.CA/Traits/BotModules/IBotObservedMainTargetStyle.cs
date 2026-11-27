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
	/// <summary>
	/// LEARN-ALL-02 / B4: the observed-style bucket of the bot's CURRENT MAIN TARGET owner (the fourth axis of the learned decision value). Implemented on the player
	/// actor by the observation classifier (B4). OBSERVATION ONLY: the bucket comes from what the bot saw (its own sight, memory and the match record), never from an enemy's
	/// personality trait, condition or any internal state, so the same classifier works against human players; true labels are used only offline in DEV to score it.
	/// Null = unseen (no qualified observation, or no provider): the learned value is then exactly the three-axis result.
	/// </summary>
	public interface IBotObservedMainTargetStyle
	{
		string ObservedStyleOf(OpenRA.Player target);
	}
}
