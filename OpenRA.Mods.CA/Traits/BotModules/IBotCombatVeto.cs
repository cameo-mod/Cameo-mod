#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// Tier-2 combat veto (DESIGN 19.13, AI_ARCHITECTURE 12.31): the verdict the squad manager writes to the
	/// mission card when a provider blocks a commit. Numbers ride along so EL can score vetoed vs non-vetoed
	/// decisions on the same trade scale the engagement log already uses.
	/// </summary>
	public sealed class CombatVetoVerdict
	{
		/// <summary>The BotMissionReasons value recorded on the DENIED record.</summary>
		public string Reason;

		/// <summary>Free-form evidence for the record's Detail field (e.g. "trade=-452;ratio=0.71;ownv=3200;foev=5100").</summary>
		public string Detail;

		/// <summary>The own squad's value at stake (the record's Value field).</summary>
		public int OwnValue;
	}

	/// <summary>
	/// Tier-2 (DESIGN 19.13): a provider consulted where a squad commits an attack (PredictsWin) or a retreat
	/// (PredictsLoss). Plain data in/out — the squad manager owns the squad, the scan and the emission; the
	/// provider owns the verdict. Consultations never issue orders and never touch state the manager did not
	/// hand over, so the seam stays one-owner-per-decision (FRANSBOT review LC1).
	/// </summary>
	public interface IBotCombatVeto
	{
		/// <summary>
		/// Attack-commit consult, called inside PredictsWin. ownUnits = the squad's units; seenEnemies = the
		/// observed enemy list the commit scan already produced (fog-honest by construction); targetPos = where
		/// the squad is committing. Return true to block the attack.
		/// </summary>
		bool TryVetoAttack(IReadOnlyList<Actor> ownUnits, IReadOnlyList<Actor> seenEnemies, WPos targetPos, out CombatVetoVerdict verdict);

		/// <summary>
		/// Retreat-commit consult, called inside PredictsLoss only when the predictor already forecasts a loss.
		/// Return true to block the retreat — the squad stands and fights.
		/// </summary>
		bool TryVetoRetreat(IReadOnlyList<Actor> ownUnits, IReadOnlyList<Actor> seenEnemies, out CombatVetoVerdict verdict);
	}
}
