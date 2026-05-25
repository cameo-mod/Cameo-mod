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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("When only one team remains, its members turn on each other as FFA.")]
	public class BackstabGameModeInfo : TraitInfo, ILobbyOptions
	{
		[Desc("Default state of the Backstab Mode checkbox in the lobby.")]
		public readonly bool BackstabCheckboxEnabled = false;

		[Desc("Prevent the Backstab Mode state from being changed in the lobby.")]
		public readonly bool BackstabCheckboxLocked = false;

		[Desc("Display order for the Backstab Mode checkbox in the lobby.")]
		public readonly int BackstabCheckboxDisplayOrder = 0;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			yield return new LobbyBooleanOption(map, "backstab",
				"Backstab Mode",
				"When only one team remains, its members fight among themselves.",
				true, BackstabCheckboxDisplayOrder, BackstabCheckboxEnabled, BackstabCheckboxLocked);
		}

		public override object Create(ActorInitializer init) => new BackstabGameMode(init.World, this);
	}

	public class BackstabGameMode : ITick, IWorldLoaded
	{
		readonly World world;
		readonly BackstabGameModeInfo info;

		bool armed;
		bool triggered;
		Dictionary<OpenRA.Player, int> originalTeams;

		public BackstabGameMode(World world, BackstabGameModeInfo info)
		{
			this.world = world;
			this.info = info;
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			if (!w.LobbyInfo.GlobalSettings.OptionOrDefault("backstab", info.BackstabCheckboxEnabled))
				return;

			originalTeams = w.Players
				.Where(p => !p.NonCombatant)
				.ToDictionary(p => p, p => (w.LobbyInfo.ClientWithIndex(p.ClientIndex) ?? new Session.Client()).Team);

			var combatants = originalTeams.Keys.ToArray();

			if (combatants.Length <= 2)
				return;

			// Need at least one team with 2+ players (the future backstabbers)
			var hasMultiPlayerTeam = combatants
				.Where(p => originalTeams[p] != 0)
				.GroupBy(p => originalTeams[p])
				.Any(g => g.Count() >= 2);

			if (!hasMultiPlayerTeam)
				return;

			// Need at least 2 distinct groups so there are opponents to defeat first
			var distinctGroups = combatants.Select(p => originalTeams[p]).Distinct().Count();
			if (distinctGroups < 2)
				return;

			armed = true;
		}

		void ITick.Tick(Actor self)
		{
			if (!armed || triggered)
				return;

			var survivors = world.Players
				.Where(p => !p.NonCombatant && p.WinState == WinState.Undefined)
				.ToArray();

			if (survivors.Length < 2)
				return;

			// All survivors must share the same original non-zero team
			var survivorTeams = survivors
				.Select(p => originalTeams.TryGetValue(p, out var t) ? t : -1)
				.Distinct()
				.ToArray();

			if (survivorTeams.Length != 1 || survivorTeams[0] <= 0)
				return;

			triggered = true;
			TriggerBackstab(survivors);
		}

		void TriggerBackstab(OpenRA.Player[] survivors)
		{
			foreach (var p1 in survivors)
			{
				foreach (var p2 in survivors)
				{
					if (p1 == p2)
						continue;

					p1.AlliedPlayersMask = p1.AlliedPlayersMask.Except(p2.PlayerMask);
					p1.EnemyPlayersMask = p1.EnemyPlayersMask.Union(p2.PlayerMask);
				}

				p1.PlayerActor.TraitOrDefault<ConquestVictoryConditions>()?.ResetOtherPlayersCache();
			}

			TextNotificationsManager.AddTransientLine("Backstab! Every player for themselves!");
		}
	}
}
