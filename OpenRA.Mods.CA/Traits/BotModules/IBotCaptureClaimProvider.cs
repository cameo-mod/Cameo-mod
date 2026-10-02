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
	/// <summary>
	/// Cameo (docs/design/AI_ARCHITECTURE.md §12.17, TC-2e): the capture target cell the bot's
	/// engineer owner is currently driving. <c>BotSituation</c> publishes it on the team
	/// broadcast so allied bots can yield a contested capture to the outranking claimant
	/// (lowest ClientIndex wins, the same arbitration TC-2c uses for expansion claims).
	/// Providers live in OpenRA.Mods.Cameo and must not be referenced by name here; null
	/// means no claim in flight.
	/// </summary>
	public interface IBotCaptureClaimProvider
	{
		CPos? CaptureClaimTarget { get; }
	}
}
