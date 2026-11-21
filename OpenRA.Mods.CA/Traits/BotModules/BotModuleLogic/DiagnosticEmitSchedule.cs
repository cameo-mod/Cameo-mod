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

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// ADP-05 catch-up emit schedule for the DEV-only botpassive streams (unit_position, health).
	/// A stream keeps its next slot tick; at a visited tick t >= slot it emits that slot and advances by
	/// one interval. A bot the round-robin scheduler visits less often than every tick never sees
	/// `t % interval == 0`, but it still emits every slot. When every tick is visited the emitted slots are
	/// exactly the old modulo ticks. Pure arithmetic: no world state, no RNG, read-only telemetry.
	/// </summary>
	public static class DiagnosticEmitSchedule
	{
		/// <summary>
		/// True when the slot `nextSlot` is due at visited tick `t`; `slot` is the scheduled tick to write
		/// and `nextSlot` advances by `interval`. A slot above `maxSlot` is never due.
		/// </summary>
		public static bool TryTake(int t, ref int nextSlot, int interval, int maxSlot, out int slot)
		{
			slot = nextSlot;
			if (interval <= 0 || nextSlot > maxSlot || t < nextSlot)
				return false;

			nextSlot += interval;
			return true;
		}
	}
}
