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
	/// <summary>
	/// CONSOLIDATE-01 c5: the bot's current personality name ("turtle", "rush", ...; empty = none), readable from the CA assembly. Implemented by
	/// BotPersonalityController (OpenRA.Mods.Cameo), which already holds the name. The defence cap of the base builder reads it for the turtle bonus.
	/// </summary>
	public interface IBotCurrentPersonality
	{
		string CurrentPersonality { get; }
	}
}
