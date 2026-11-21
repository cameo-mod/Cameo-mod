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

using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// One owner per player. A corrupt restored state is a hold, not absence of a
	// provider; callers must not bypass the budget when Faulted is true.
	public interface IBotRecoverySpendOwner
	{
		RecoverySpendLedgerCA Ledger { get; }
		bool Faulted { get; }
		(ProductionQueue Queue, ActorInfo Item, RecoverySpendPolicyCA.EssentialLane Lane)? SelectEssential(int minimumExcessPower);
	}
}
