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

using System.Collections.Generic;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// CV combat veto (AI_ARCHITECTURE §12.31, DESIGN §19.13 tier 2, fleet ORDERS_2026-10-03): the provider consulted
	/// where a squad commits an attack or a retreat. It blocks an attack whose predicted trade on the SEEN forces
	/// (including remembered static defences) falls below its threshold, and a retreat that cannot outrun.
	///
	/// The squad state machine and the squad manager stay the single order owners and only consult this; the veto
	/// itself issues no orders and learns nothing ("variety proposes, the veto disposes"). Implementations answer
	/// through the same <c>BotCombatPredictor</c> every predictor consumer already uses — one authority, never two.
	/// An absent provider means "no veto" — identical to today's behaviour.
	/// </summary>
	public interface IBotCombatVeto
	{
		/// <summary>
		/// True = block this engage: the predicted trade loses beyond the engage threshold (hysteresis inside:
		/// <paramref name="alreadyCommitted"/> squads use the lower abort line). Consulted at the idle-state engage
		/// check and again beside the siege consult while approaching.
		/// <paramref name="enemies"/> is the seen threat set the caller already gathered (fog-marked by the caller).
		/// <paramref name="reason"/> gets the card reason when vetoed (<c>outmatched</c>).
		/// </summary>
		bool VetoEngage(SquadCA squad, IReadOnlyList<Actor> enemies, bool alreadyCommitted, out string reason);

		/// <summary>
		/// True = block this wave launch: the candidate force loses against the remembered defences at the target
		/// plus the remembered enemy army, parity-floored at the force's own value (a loss must be proven —
		/// AI_ARCHITECTURE §12.31 / CP §2.3). Consulted once per <c>CreateAttackForce</c> before the squad registers.
		/// </summary>
		bool VetoLaunch(IReadOnlyList<Actor> force, CPos targetCell, out string reason);

		/// <summary>
		/// True = block this flee: the seen pursuers outrun the squad (mean speed), so standing trades more than
		/// dying while walking. Consulted at the flee sites after the existing flee tests already chose flight.
		/// <paramref name="reason"/> gets <c>x_no_outrun</c> when vetoed.
		/// </summary>
		bool VetoFlee(SquadCA squad, IReadOnlyList<Actor> pursuers, out string reason);
	}
}
