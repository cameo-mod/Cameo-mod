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
	/// Optional per-bot retention counters sampled by the cameo-ai-memory telemetry channel.
	/// Implemented by bot modules whose persistent structures grow with game volume
	/// (remembered contacts, fog tables, mission history). Read from the game thread on a
	/// bounded cadence; keys are stable compile-time constants, values are counts,
	/// capacities or byte sizes — never actor references or hidden-information content.
	/// </summary>
	public interface IAiMemoryTelemetryProbe
	{
		IEnumerable<KeyValuePair<string, long>> MemoryProbeCounts { get; }
	}
}
