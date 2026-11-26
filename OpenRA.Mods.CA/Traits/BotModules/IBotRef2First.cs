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
	/// <summary>
	/// REF2-FIRST-01: the one switch of the second-refinery rule, readable from the expansion planner (OpenRA.Mods.Cameo). Implemented by BaseBuilderBotModuleCA. While true
	/// the planner lifts every refinery zone cap by one as long as fewer than two own refineries stand, so a reachable field with room gets its second refinery claim.
	/// </summary>
	public interface IBotRef2First
	{
		bool Ref2FloorArmed { get; }
	}
}
