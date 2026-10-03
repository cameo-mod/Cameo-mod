#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	public static class BotFactionView
	{
		/// <summary>DESIGN §19.5 + engine Player.cs:65/175-177/196-198: the lobby-visible faction; Random or hidden picks return "".</summary>
		public static string PublicFactionOf(OpenRA.Player player)
		{
			var faction = player?.DisplayFaction;
			return faction == null || faction.InternalName == null || faction.RandomFactionMembers?.Count > 0 ? "" : faction.InternalName;
		}
	}
}
