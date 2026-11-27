#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>How one game ended for one bot seat: the length in ticks and the result (+1 this player won, -1 lost, 0 undecided).</summary>
	public readonly struct LearnGameEnd
	{
		public readonly int Ticks;
		public readonly int Outcome;

		public LearnGameEnd(int ticks, int outcome)
		{
			Ticks = ticks;
			Outcome = outcome;
		}
	}

	/// <summary>One knob's evidence of one game: <see cref="Value"/> is the signed, bounded outcome in [-1, 1] (positive = the knob's value should rise), null = no evidence (the estimate is kept).</summary>
	public readonly struct LearnSignal
	{
		public readonly string Knob;
		public readonly double? Value;

		public LearnSignal(string knob, double? value)
		{
			Knob = knob;
			Value = value;
		}
	}

	/// <summary>
	/// LEARN-ALL-02 P1c-2: the one seam a feature uses to hand its per-game learning signal to the DEV exposure writer (OpenRA.Mods.Cameo.Dev LearnAllExposureWriterDev).
	/// Implemented by a trait of the PLAYER actor that owns armed knobs. The DEV writer calls it once per bot seat per game, from the game-over hook, never on the tick path; the
	/// release build compiles no caller, so a release game never runs it. The implementation adds one <see cref="LearnSignal"/> per knob it armed for this seat, from its own
	/// accumulated counters only (no world scan, no I/O). The writer adds the seat context (own faction, enemies, main target, observed style, personality) itself.
	/// </summary>
	public interface IBotLearnSignals
	{
		void CollectLearnSignals(in LearnGameEnd end, ICollection<LearnSignal> into);
	}
}
