#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software. It is made
 * available to you under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// Frozen, offline-fitted economy advice. Implementations only adjust existing bounded
	/// decision knobs. They never request production, move a harvester, or select a field.
	/// A missing provider must return the caller's current authored value unchanged.
	/// </summary>
	public interface IBotEconomyLearning
	{
		int ExpansionCashDivisor(int fallback);
		int HarvesterLimit(int fallback);
		int ProductionCashThreshold(int fallback);
		int CreditFloat(int fallback);
	}
}
