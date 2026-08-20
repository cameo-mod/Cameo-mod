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

using System;
using System.Collections.Generic;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Common.Lint;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cameo.Widgets
{
	public class AutoControlGroupsWidget : Widget
	{
		readonly World world;
		readonly ModData modData;
		readonly int hotkeyCount;

		HotkeyReference[] registerTypesHotkeys;

		public readonly string RegisterTypesForGroupKeyPrefix = null;

		[CustomLintableHotkeyNames]
		public static IEnumerable<string> LinterHotkeyNames(MiniYamlNode widgetNode, Action<string> emitError)
		{
			var registerTypesPrefixNode = widgetNode.Value.NodeWithKeyOrDefault("RegisterTypesForGroupKeyPrefix");
			if (registerTypesPrefixNode == null || string.IsNullOrEmpty(registerTypesPrefixNode.Value.Value))
				yield break;

			var registerTypesPrefix = registerTypesPrefixNode.Value.Value;
			var count = Game.ModData.DefaultRules.Actors[SystemActors.World].TraitInfo<IControlGroupsInfo>().Groups.Length;
			for (var i = 0; i < count; i++)
				yield return registerTypesPrefix + (i + 1).ToStringInvariant("D2");
		}

		[ObjectCreator.UseCtor]
		public AutoControlGroupsWidget(ModData modData, World world)
		{
			this.modData = modData;
			this.world = world;
			hotkeyCount = world.ControlGroups.Groups.Length;
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			if (RegisterTypesForGroupKeyPrefix != null)
				registerTypesHotkeys = Exts.MakeArray(hotkeyCount,
					i => modData.Hotkeys[RegisterTypesForGroupKeyPrefix + (i + 1).ToStringInvariant("D2")]);
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Event != KeyInputEvent.Down || registerTypesHotkeys == null)
				return false;

			for (var i = 0; i < hotkeyCount; i++)
			{
				if (registerTypesHotkeys[i].IsActivatedBy(e))
				{
					world.WorldActor.Trait<AutoControlGroupsManager>().RegisterTypesForControlGroup(i);
					return true;
				}
			}

			return false;
		}
	}
}
