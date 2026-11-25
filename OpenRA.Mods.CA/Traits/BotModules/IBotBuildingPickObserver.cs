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
	/// BUILD-ALL-B, DEV only: record-only observer of the staged base-build planner. Implemented by the DEV player telemetry (OpenRA.Mods.Cameo.Dev);
	/// this assembly does not reference it by name. Every call site is compiled out of a release build (Conditional CAMEO_DEV_LOGGING), and the
	/// planner never reads anything back, so an absent observer leaves behaviour bit-identical.
	/// </summary>
	public interface IBotBuildingPickObserver
	{
		/// <summary>The planner admitted <paramref name="actor"/> (the start: the candidate, or the power plant A5 swapped in) from <paramref name="stage"/>.</summary>
		void BuildingPicked(Player owner, int tick, string actor, string stage, string candidate, int bank);

		/// <summary>Test A6 refused a start the waiver did not cover (the caller rate-limits per name).</summary>
		void BuildingA6Held(Player owner, int tick, string actor, string stage, int bank, int cost);

		/// <summary>The wanted-refinery helper returned no refinery; <paramref name="gate"/> names the first gate that held it (the observer rate-limits).</summary>
		void RefineryHeld(Player owner, int tick, string gate, int refineries);
	}
}
