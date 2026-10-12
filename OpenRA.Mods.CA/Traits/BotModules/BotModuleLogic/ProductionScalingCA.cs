#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// BZ_production_scaling: the pure decisions behind the income-driven production arms.
	/// Armed = UseProductionScaling on BaseBuilderBotModuleCA (building side, SwitchCondition-gated)
	/// and on ExpansionPlannerBotModule (extra-MCV side). Everything not listed here — the cash
	/// floor, ProductionTypeLimit, pause votes, buildability — is unchanged behaviour.
	/// Integer math only; no RNG; every gate fails closed on a non-positive bar.
	/// </summary>
	public static class ProductionScalingCA
	{
		/// <summary>Banked-cash persistence: the bar must hold uninterrupted for the whole window.
		/// 0/disabled fails closed.</summary>
		public static bool CashHeld(int heldTicks, int persistTicks) =>
			persistTicks > 0 && heldTicks >= persistTicks;

		/// <summary>Every derived unit-production queue holds work (a parallel queue with no free
		/// slot counts busy). No queue at all fails closed — nothing is "starved".</summary>
		public static bool AllBusy(int busyQueues, int totalQueues) =>
			totalQueues > 0 && busyQueues >= totalQueues;

		/// <summary>Net income per producer above the bar: (earned - spent) per tick spread over
		/// the enabled unit queues. Written as a product to stay integer-exact.</summary>
		public static bool SpareIncomeMet(long netIncomePerTick, int producers, int spareBar) =>
			spareBar > 0 && producers > 0 && netIncomePerTick > (long)spareBar * producers;

		/// <summary>Sustained-income admission for the extra-MCV arm. The windowed rate must reach
		/// the bar; a non-positive bar fails closed.</summary>
		public static bool IncomeMet(double incomePerTick, int incomeBar) =>
			incomeBar > 0 && incomePerTick >= incomeBar;

		/// <summary>The extra-MCV allowance: sustained income + a reachable free far field + army
		/// parity admits up to the difficulty-line cap on top of the active target.</summary>
		public static int McvAllowance(bool incomeMet, bool farFieldFree, bool parityAdmits, int maxExtra) =>
			incomeMet && farFieldFree && parityAdmits ? Math.Max(0, maxExtra) : 0;
	}
}
