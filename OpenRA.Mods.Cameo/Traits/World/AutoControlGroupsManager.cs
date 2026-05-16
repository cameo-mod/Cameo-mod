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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Automatically assigns newly produced units to control groups based on unit type.")]
	public class AutoControlGroupsManagerInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new AutoControlGroupsManager(init.World); }
	}

	public class AutoControlGroupsManager : IGameSaveTraitData
	{
		readonly World world;
		readonly Dictionary<int, HashSet<string>> autoAssignTypes = [];

		public AutoControlGroupsManager(World world)
		{
			this.world = world;
			world.ActorAdded += OnActorAdded;
		}

		void OnActorAdded(Actor a)
		{
			if (a.Owner != world.LocalPlayer)
				return;

			foreach (var (group, types) in autoAssignTypes)
			{
				if (types.Contains(a.Info.Name))
				{
					world.ControlGroups.AddToControlGroup(a, group);
					break;
				}
			}
		}

		public void RegisterTypesForControlGroup(int group)
		{
			if (world.Selection.Actors.Count == 0)
				return;

			var ownedActors = world.Selection.Actors.Where(a => a.Owner == world.LocalPlayer).ToArray();
			autoAssignTypes[group] = ownedActors.Select(a => a.Info.Name).ToHashSet();

			var registeredTypes = autoAssignTypes[group];
			var selectedSet = world.Selection.Actors.ToHashSet();
			foreach (var a in world.Actors)
			{
				if (a.Owner == world.LocalPlayer && a.IsInWorld && !a.IsDead &&
					registeredTypes.Contains(a.Info.Name) && !selectedSet.Contains(a))
					world.ControlGroups.AddToControlGroup(a, group);
			}

			var displayNames = ownedActors
				.Select(a => a.Info.TraitInfoOrDefault<TooltipInfo>())
				.Where(t => t != null)
				.Select(t => FluentProvider.GetMessage(t.Name))
				.Distinct();
			TextNotificationsManager.AddTransientLine($"Auto group {world.ControlGroups.Groups[group]}: {string.Join(", ", displayNames)}");
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			var autoAssign = autoAssignTypes
				.Select(kv => new MiniYamlNode(kv.Key.ToStringInvariant(), FieldSaver.FormatValue(kv.Value.ToArray())))
				.ToList();

			return [new("AutoAssignTypes", new MiniYaml("", autoAssign))];
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, MiniYaml data)
		{
			var autoAssignNode = data.NodeWithKeyOrDefault("AutoAssignTypes");
			if (autoAssignNode != null)
			{
				foreach (var n in autoAssignNode.Value.Nodes)
				{
					var types = FieldLoader.GetValue<string[]>(n.Key, n.Value.Value);
					autoAssignTypes[Exts.ParseInt32Invariant(n.Key)] = [.. types];
				}
			}
		}
	}
}
