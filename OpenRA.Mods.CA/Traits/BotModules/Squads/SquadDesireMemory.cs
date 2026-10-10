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

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	// One runtime-only instance per squad; no player identifiers or learned persistence.
	public sealed class SquadDesireMemory
	{
		readonly int[] desires = new int[SquadDesireEval.StanceCount];
		readonly int[] urgencies = new int[SquadDesireEval.StanceCount];
		int current = -1;
		int lastSwitchTick;
		public int LastSeenTick { get; private set; } = -1;

		public SquadDesireStance Evaluate(in SquadDesireSignals signals, string personality,
			int leakPermille, int hysteresisMilli, int minDwellTicks, int biasCapMilli)
		{
			// Multiple state consumers in the same tick must not accelerate the integrator.
			if (current >= 0 && signals.WorldTick == LastSeenTick)
				return (SquadDesireStance)current;
			SquadDesireEval.Urgencies(in signals, urgencies);
			SquadDesireEval.AddBias(urgencies, personality, Math.Clamp(biasCapMilli, 0, 1000));
			for (var i = 0; i < desires.Length; i++)
				desires[i] = current < 0 ? urgencies[i]
					: SquadDesireEval.Step(desires[i], urgencies[i], Math.Clamp(leakPermille, 0, 1000));
			var next = SquadDesireEval.Pick(desires, current, Math.Max(0, hysteresisMilli),
				(long)signals.WorldTick - lastSwitchTick >= Math.Max(0, minDwellTicks));
			if (next != current)
			{
				current = next;
				lastSwitchTick = signals.WorldTick;
			}
			LastSeenTick = signals.WorldTick;
			return (SquadDesireStance)current;
		}
	}
}
