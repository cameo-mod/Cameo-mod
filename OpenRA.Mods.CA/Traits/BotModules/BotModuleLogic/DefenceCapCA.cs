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

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// CONSOLIDATE-01 c5: the ARMY-FIRST-02 threat-scaled defence cap as a pure function of the CA assembly (ArmyFirstEval lives in OpenRA.Mods.Cameo, which the base
	/// builder cannot reference; ArmyFirstBotModule stays untouched). Same arithmetic, the same numbers: base + floor(perThreat x intensity), the intensity clamped to
	/// 0..1, then scaled by the turtle percent while the turtle bonus applies. A negative base = no cap (-1).
	/// </summary>
	public static class DefenceCapCA
	{
		public static int Cap(int baseCap, int perThreat, double intensity, bool turtleBonus, int turtlePercent)
		{
			if (baseCap < 0)
				return -1;

			var cap = (long)baseCap + (long)Math.Floor(Math.Max(0, perThreat) * Math.Clamp(intensity, 0.0, 1.0));
			if (turtleBonus)
				cap = cap * Math.Max(0, turtlePercent) / 100;

			return (int)Math.Min(int.MaxValue, cap);
		}

		/// <summary>An extra defence is held while the count is at or over the cap; no cap (negative) never holds.</summary>
		public static bool Holds(int cap, int defences) => cap >= 0 && defences >= cap;

		/// <summary>The threat intensity the cap reads: the highest of the anti-air, anti-armour and anti-infantry intensities.</summary>
		public static double Intensity(float antiAir, float antiArmour, float antiInfantry) => Math.Max(antiAir, Math.Max(antiArmour, antiInfantry));

		/// <summary>The turtle bonus: the turtle personality before the end tick of the bonus (early to mid game only).</summary>
		public static bool TurtleBonus(int now, int untilTick, string personality) => now < untilTick && personality == "turtle";

		/// <summary>An armed static defence: a building with an attack trait (walls and unarmed support are not defences).</summary>
		public static bool IsArmedDefence(bool isBuilding, bool hasAttack) => isBuilding && hasAttack;
	}
}
