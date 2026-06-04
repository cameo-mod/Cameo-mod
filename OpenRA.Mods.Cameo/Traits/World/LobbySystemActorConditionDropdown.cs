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
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Grants a condition on the world or player actors based on a lobby dropdown selection.")]
	public class LobbySystemActorConditionDropdownInfo : TraitInfo, ILobbyOptions
	{
		[FieldLoader.Require]
		[Desc("Internal id for this dropdown.")]
		public readonly string ID = null;

		[FieldLoader.Require]
		[FluentReference]
		[Desc("Display name for this dropdown.")]
		public readonly string Label = null;

		[FluentReference]
		[Desc("Tooltip description for this dropdown.")]
		public readonly string Description = null;

		[FieldLoader.Require]
		[Desc("Default option key in the `Values` list.")]
		public readonly string Default = null;

		[FieldLoader.Require]
		[FluentReference(dictionaryReference: LintDictionaryReference.Values)]
		[Desc("Options to choose from.")]
		public readonly Dictionary<string, string> Values = null;

		[Desc("Condition to grant for each option key. Empty or omitted values grant no condition.")]
		public readonly Dictionary<string, string> Conditions = null;

		[Desc("Condition to grant for each tileset id when the selected option grants no condition.")]
		public readonly Dictionary<string, string> TileSetConditions = null;

		[Desc("Comma-separated tileset ids that allow each option key. Empty or omitted values allow all tilesets.")]
		public readonly Dictionary<string, string> Tilesets = null;

		[Desc("Prevent the dropdown from being changed from its default value.")]
		public readonly bool Locked = false;

		[Desc("Display the dropdown in the lobby.")]
		public readonly bool Visible = true;

		[Desc("Display order for the dropdown in the lobby.")]
		public readonly int DisplayOrder = 0;

		[Desc("System actors to grant condition to. Only supports: World, Player")]
		public readonly SystemActors Actors = SystemActors.World;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			yield return new LobbyOption(map, ID, Label, Description, Visible, DisplayOrder, Values, Default, Locked);
		}

		public override object Create(ActorInitializer init) { return new LobbySystemActorConditionDropdown(this); }
	}

	public class LobbySystemActorConditionDropdown : INotifyCreated, ITick
	{
		readonly LobbySystemActorConditionDropdownInfo info;
		bool grantToPlayer;
		string condition;

		public LobbySystemActorConditionDropdown(LobbySystemActorConditionDropdownInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			var selected = self.World.LobbyInfo.GlobalSettings.OptionOrDefault(info.ID, info.Default);
			info.Conditions?.TryGetValue(selected, out condition);

			if (string.IsNullOrEmpty(condition))
			{
				if (selected == info.Default || info.TileSetConditions == null)
					return;

				info.TileSetConditions.TryGetValue(self.World.Map.Tileset, out condition);
			}

			if (string.IsNullOrEmpty(condition))
				return;

			if (info.Tilesets != null && info.Tilesets.TryGetValue(selected, out var tilesets) && !string.IsNullOrWhiteSpace(tilesets))
			{
				var mapTileset = self.World.Map.Tileset;
				if (!tilesets.Split(',').Select(t => t.Trim()).Contains(mapTileset, System.StringComparer.OrdinalIgnoreCase))
					return;
			}

			if (info.Actors.HasFlag(SystemActors.World))
				self.GrantCondition(condition);

			grantToPlayer = info.Actors.HasFlag(SystemActors.Player);
		}

		void ITick.Tick(Actor self)
		{
			// World actor is created before Player actors, so this doesn't work in Created.
			if (!grantToPlayer)
				return;

			foreach (var player in self.World.Players)
				player.PlayerActor.GrantCondition(condition);

			grantToPlayer = false;
		}
	}
}
