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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	public static class BotFactionView
	{
		/// <summary>DESIGN §19.5 + engine Player.cs:65/175-177/196-198: the lobby-visible faction; Random or hidden picks return "".</summary>
		public static string PublicFactionOf(OpenRA.Player player) => PublicName(player?.DisplayFaction);

		/// <summary>
		/// The public view of a faction NAME — a map PlayerReference's faction string, resolved against the
		/// map ruleset the way engine ResolveDisplayFaction resolves it (Player.cs:151): "" for Random/hidden
		/// or unresolvable names, under the same predicate PublicFactionOf applies to players.
		/// </summary>
		public static string PublicFactionName(World world, string factionName)
		{
			var factions = world.Map.Rules.Actors[SystemActors.World].TraitInfos<FactionInfo>();
			return PublicName(factions.FirstOrDefault(f => f.InternalName == factionName));
		}

		/// <summary>The shared predicate: a faction is publicly known only when it has a concrete InternalName
		/// and is not a Random picker (a FactionInfo whose RandomFactionMembers holds the real candidates).</summary>
		internal static string PublicName(FactionInfo faction) =>
			faction == null || faction.InternalName == null || faction.RandomFactionMembers?.Count > 0
				? "" : faction.InternalName;
	}
}
