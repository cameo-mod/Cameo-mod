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
		/// Per-(delivery, armour) fitted correction on one weapon's damage (the tier-1 delivery grid,
		/// `DeliveryArmour@<tag>__x__<armour>` cells). Default neutral; staleness is the provider's own check
		/// (the `PriorPct@` twin vs the weapon's resolved Versus row).
		/// </summary>
		int CorrectionMilli(BotWeaponProfile weapon, string targetArmor) => 1000;

		/// <summary>Correction on this weapon's damage when the TARGET is a static defence — the fitter's
		/// `IntoDefencesMilli` scalar (the "suicide into defences" term). Default neutral.</summary>
		int IntoDefencesMilli(BotWeaponProfile weapon) => 1000;

		/// <summary>Correction on this weapon's damage when the ATTACKER is a static defence — the fitter's
		/// `DefenceState@<delivery>` residuals. Default neutral.</summary>
		int DefenceFireMilli(BotWeaponProfile weapon) => 1000;

		/// <summary>Fitted attrition exponent in milli (1000 = square law); the eval applies it as ratio^alpha.</summary>
		int AttritionExponentMilli => 1000;

		/// <summary>Load state for the match record — `none` / `error` / `stale:N` / `fitted:N` — prefixed with a
		/// source label (`"delivery:"`). Null when the provider carries no priors file.</summary>
		string PriorsState => null;
	}
}
