#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits.BotModuleLogic
{
	/// <summary>
	/// Tier-1 learned priors hook (AI_ARCHITECTURE §12.31, DESIGN §19.13): bounded corrections fitted offline by the
	/// tier-1 fitter from closed engagement records, frozen at match start, applied inside the veto's damage
	/// assembly. The file that feeds an implementation owns the unit→delivery-key mapping — this interface stays
	/// stat-normalised per the fleet rule (no per-unit ids in code).
	/// An absent provider means 1000 — the pure <c>BotCombatPredictor</c> numbers, unchanged.
	/// </summary>
	public interface IBotEngagementPriors
	{
		/// <summary>
		/// Damage multiplier in thousandths (1000 = neutral) for one attacker profile firing on one target profile.
		/// Implementations must be bounded — a fitted value clamps to a sane range and never inverts a fight.
		/// </summary>
		int CorrectionMilli(BotUnitProfile attacker, BotUnitProfile target);

		/// <summary>
		/// Fitted attrition exponent in thousandths (1000 = the pure square law). The eval applies it to the
		/// predicted ratio (<c>ratio^alpha</c>) and re-derives surviving fractions, preserving the Lanchester
		/// invariant. Default neutral — a provider that carries no exponent changes nothing.
		/// </summary>
		int AttritionExponentMilli => 1000;

		/// <summary>
		/// Load state for the match record — e.g. <c>none</c> / <c>error</c> / <c>fitted:N/carried:M/decay:D</c> —
		/// prefixed with a source label. Null when the provider carries no priors file (the field is then omitted).
		/// </summary>
		string PriorsState => null;
	}
}
