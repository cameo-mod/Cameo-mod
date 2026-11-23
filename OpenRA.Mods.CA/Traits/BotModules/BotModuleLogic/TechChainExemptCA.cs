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
	/// BINDING tech chain (11-23 ruling, TECHCHAIN-EXEMPT-01): the pure exemption of a faction's T2 (radar-equivalent) and T3 (tech centre)
	/// gate from the army-first pause and the mobile reserve. Exempt only the first copy: none owned and none queued. The caller supplies the
	/// evidence (list membership, owned count, queued flag) and passes only buildable candidates, so prerequisites are met by construction.
	/// </summary>
	public static class TechChainExemptCA
	{
		public static bool Exempt(bool inTechChainList, int ownedCount, bool queued) => inTechChainList && ownedCount == 0 && !queued;
	}
}
