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

using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test.TestFixtures
{
	/// <summary>
	/// Utility-axis provider with ctor-set poles; unset axes report Neutral.
	/// Covers the former per-file StubAxes/StubUtilityAxes duplicates.
	/// </summary>
	public sealed class StubUtilityAxes : IBotUtilityAxes
	{
		public StubUtilityAxes(int turtleRush = IBotUtilityAxes.Neutral,
			int steamrollerGuerrilla = IBotUtilityAxes.Neutral,
			int techRushExpansion = IBotUtilityAxes.Neutral)
		{
			UtilityTurtleRush = turtleRush;
			UtilitySteamrollerGuerrilla = steamrollerGuerrilla;
			UtilityTechRushExpansion = techRushExpansion;
		}

		public int UtilityTurtleRush { get; }
		public int UtilityTechRushExpansion { get; }
		public int UtilitySteamrollerGuerrilla { get; }
	}
}
