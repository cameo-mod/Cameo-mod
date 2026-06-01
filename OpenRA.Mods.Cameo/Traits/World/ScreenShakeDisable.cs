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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Adds a lobby option to disable all screen shake effects.")]
	public class ScreenShakeDisableInfo : TraitInfo, ILobbyOptions
	{
		[Desc("Default state of the Disable Screen Shake checkbox in the lobby.")]
		public readonly bool CheckboxEnabled = false;

		[Desc("Prevent the checkbox state from being changed in the lobby.")]
		public readonly bool CheckboxLocked = false;

		[Desc("Display order for the Disable Screen Shake checkbox in the lobby.")]
		public readonly int CheckboxDisplayOrder = 0;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			yield return new LobbyBooleanOption(map, "disable-screen-shake",
				"Disable Screen Shake",
				"Prevents all screen shake effects during the match.",
				true, CheckboxDisplayOrder, CheckboxEnabled, CheckboxLocked);
		}

		public override object Create(ActorInitializer init) => new ScreenShakeDisable(init.World, this);
	}

	public class ScreenShakeDisable : IWorldLoaded
	{
		readonly World world;
		readonly ScreenShakeDisableInfo info;

		public ScreenShakeDisable(World world, ScreenShakeDisableInfo info)
		{
			this.world = world;
			this.info = info;
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			if (!w.LobbyInfo.GlobalSettings.OptionOrDefault("disable-screen-shake", info.CheckboxEnabled))
				return;

			var shaker = w.WorldActor.TraitOrDefault<ScreenShaker>();
			if (shaker != null)
				shaker.Disabled = true;
		}
	}
}
