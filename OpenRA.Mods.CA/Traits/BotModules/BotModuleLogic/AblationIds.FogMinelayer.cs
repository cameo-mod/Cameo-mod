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
	// FOG-MINELAYER-01 row (Opus 11-26 BINDING no-omniscience; Haiku FOG_SCAN_03 3ec93acd, Terra RM-04 aabe98b8). Owner: Frontrunner.
	public static partial class AblationIds
	{
		// fog_minelayer off = the genericbot lays no mines at all. It never falls back to the engine MinelayerBotModule, whose four reads see hidden enemies (that module runs for classicbot only).
		public const string FogMinelayer = "fog_minelayer";
	}
}
