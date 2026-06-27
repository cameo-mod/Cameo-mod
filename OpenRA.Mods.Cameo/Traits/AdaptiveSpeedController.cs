#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using System;

namespace OpenRA.Mods.Cameo.Traits
{
	// Pure, source- and sink-agnostic core of the adaptive game-speed controller.
	//
	// Feed it the measured wall-clock period between sim ticks — which the game loop makes equal to
	// max(pacedTimestep, actual compute) — plus the unscaled base timestep; it returns the pacing
	// multiplier (>= 1) to apply. It deliberately knows NOTHING about where the period comes from or
	// where the resulting scale goes. That separation is what keeps multiplayer clean:
	//   * Single-player: the AdaptiveGameSpeed trait feeds it the LOCAL loop period and applies the
	//     result to the LOCAL OrderManager tickScale (LocalPacing).
	//   * Multiplayer (future, host-authoritative): a host-side driver feeds it the HOST's loop period
	//     and BROADCASTS the result through the server's existing authoritative TickScale frame, so every
	//     client applies the same value. Same maths; MP-safe because the scale stays server-distributed
	//     and pacing-only (never World.Timestep / the simulated dt).
	//
	// Keep in lockstep with tools/sim_adaptive_gamespeed.py, which is the reference used to tune it.
	public sealed class AdaptiveSpeedController
	{
		public readonly float MaxScale;

		readonly float overloadMargin;
		readonly float smoothing;
		readonly float recoveryStep;
		readonly float recoveryRate;
		readonly int overloadHoldTicks;

		float emaPeriodMs;
		int overloadHold;

		public float Scale { get; private set; } = 1f;
		public bool Saturated { get; private set; }
		public float EmaPeriodMs => emaPeriodMs;
		public bool AtCap => Scale >= MaxScale - 0.001f;

		public AdaptiveSpeedController(int minimumSpeedPercent, float overloadMargin, float smoothing,
			float recoveryStep, float recoveryRate, int overloadHoldTicks)
		{
			// minimumSpeedPercent 25 -> MaxScale 4.0 (timestep x4). Lower-bounded to 17 so the paced
			// timestep stays < 250ms (the loop's MaxLogicTicksBehind / jank threshold): 100/17*40 = 235ms.
			MaxScale = 100f / Math.Clamp(minimumSpeedPercent, 17, 100);
			this.overloadMargin = overloadMargin;
			this.smoothing = smoothing;
			this.recoveryStep = recoveryStep;
			this.recoveryRate = recoveryRate;
			this.overloadHoldTicks = overloadHoldTicks;
		}

		public void Reset()
		{
			emaPeriodMs = 0f;
			overloadHold = 0;
			Scale = 1f;
			Saturated = false;
		}

		// periodMs = measured wall time since the previous tick; budgetMs = unscaled timestep (World.Timestep).
		// Caller is responsible for discarding implausible samples (period <= 0, hitch outliers) and for
		// ensuring budgetMs > 0 before calling.
		public float Update(long periodMs, int budgetMs)
		{
			emaPeriodMs = emaPeriodMs <= 0f ? periodMs : emaPeriodMs + smoothing * (periodMs - emaPeriodMs);

			// The period equals max(paced timestep, actual compute). If it exceeds the pace we are currently
			// asking for, the machine can't sustain even this speed -> slow further. Otherwise there is
			// headroom, so probe back toward full speed (this is what recovers after a battle).
			var pacedTimestep = budgetMs * Scale;
			Saturated = emaPeriodMs > pacedTimestep * (1f + overloadMargin);

			if (Saturated)
			{
				// Slow slightly MORE than exactly-compute (by the overload margin) so the paced timestep ends
				// up a touch above the real tick cost — a little slack to sleep each tick = steady pacing,
				// instead of sitting on the saturation edge where any wobble re-stutters.
				var target = emaPeriodMs / budgetMs * (1f + overloadMargin);
				if (target > Scale)
					Scale = target;

				overloadHold = overloadHoldTicks;
			}
			else if (overloadHold > 0)
				overloadHold--;
			else
			{
				// Proportional recovery (shed a fraction of the current slowdown) with a small absolute floor
				// so it still settles to exactly 1.0. Smoothing must be high enough that the EMA tracks the
				// falling paced timestep here, or recovery trips a false 'Saturated' and never completes.
				Scale -= Math.Max(recoveryStep, (Scale - 1f) * recoveryRate);
			}

			Scale = Math.Clamp(Scale, 1f, MaxScale);

			// Snap tiny residuals to exactly 1.0 so a game that isn't overloaded runs at stock pace.
			if (Scale < 1.01f)
				Scale = 1f;

			return Scale;
		}
	}
}
