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
	// TECHCHAIN-PAYGO-01.
	public static partial class AblationIds
	{
		// Stages S3 and S5 admit a tech-chain pick with the bank above 0 instead of at its full cost (Info BbTechChainPaygo). Off = test A6 unchanged.
		public const string BbTechChainPaygo = "bb_techchain_paygo";
	}
}
