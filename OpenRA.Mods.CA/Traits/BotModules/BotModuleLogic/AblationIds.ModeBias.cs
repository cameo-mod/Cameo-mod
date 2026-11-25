#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	// MODE-BIAS-01.
	public static partial class AblationIds
	{
		// The four mode bias profiles weight the BuildingFractions roll of the staged planner while their trigger holds (Info MbEssentials, MbTechChain, MbAntiRush, MbOpening). Off = the old uniform shuffle.
		public const string MbEssentials = "mb_essentials";
		public const string MbTechChain = "mb_techchain";
		public const string MbAntiRush = "mb_antirush";
		public const string MbOpening = "mb_opening";
	}
}
