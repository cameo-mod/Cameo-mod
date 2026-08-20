#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Single-player only. Gracefully slows game-time when the simulation can't keep up with real",
		"time, instead of dropping render frames (the late-game teleport-stutter). Feeds the existing",
		"server-authoritative TickScale pacing path — it never touches World.Timestep / the simulated dt,",
		"so it is deterministic by construction and a hard no-op in replays, while loading saves, and in",
		"networked (more than one human) games. Enabled by the default-on 'Adaptive Game Speed' lobby",
		"option (shared with the multiplayer host driver AdaptiveGameSpeedHost). Attach to the world actor.")]
	public class AdaptiveGameSpeedInfo : TraitInfo, ILobbyOptions
	{
		[Desc("Slowest the game may run, as a percentage of normal speed. 25 = quarter speed (timestep x4).",
			"Must be deep enough that the stretched timestep can exceed peak late-game tick compute, or the",
			"controller saturates and the late-game stutter returns. Bounded so the timestep stays < 250ms",
			"(the engine's MaxLogicTicksBehind / jank threshold): keep this >= 17.")]
		public readonly int MinimumSpeedPercent = 25;

		[Desc("Only begin slowing once the measured tick period exceeds the current paced budget by this",
			"fraction. Provides a deadband so a game that is just keeping up stays at full speed.")]
		public readonly float OverloadMargin = 0.15f;

		[Desc("EMA smoothing factor for the measured tick period (0-1; higher = more responsive). Needs to",
			"be high enough that the EMA tracks the paced timestep DOWN during recovery — too low and a",
			"recovering scale leaves the EMA lagging above the new pace, which trips a false 'overloaded'",
			"and slams the speed back to the floor (the controller never recovers). 0.30 is tuned for this.")]
		public readonly float Smoothing = 0.30f;

		[Desc("Absolute floor for the per-tick recovery step (scale units), so the game still settles to",
			"exactly 1.0 near full speed. The main recovery is proportional — see RecoveryRate.")]
		public readonly float RecoveryStep = 0.01f;

		[Desc("Proportional recovery: each tick with headroom, shed this fraction of the CURRENT slowdown",
			"(scale - 1). Recovers quickly from deep slow-mo (where ticks are sparse, so fixed steps crawl)",
			"and eases gently as it approaches full speed.")]
		public readonly float RecoveryRate = 0.04f;

		[Desc("After slowing down, hold before probing for speed-up again (ticks). Damps oscillation.")]
		public readonly int OverloadHoldTicks = 25;

		[Desc("Ignore tick-period samples longer than this many ms (load hitches, breakpoints, alt-tab),",
			"so a one-off stall doesn't slam the speed down.")]
		public readonly int MaxSampleMs = 500;

		[Desc("Hold full speed for the first this-many world ticks of a match, ignoring the measured period.",
			"A fresh match pays large ONE-TIME startup costs — world/pathfinding init, JIT of the hot sim",
			"paths, and lazy sprite/sound decode as the opening view renders — which inflate the loop period",
			"without being real sustained load. Without this grace the controller dives into a deep slow-mo",
			"for the opening seconds (the 'slow start before the MCV deploys'). ~100 ticks (~4s) covers a",
			"typical heavy start; raise it if a cold start is slower. Counts match age, so toggling the",
			"feature on mid-match does not re-trigger it.")]
		public readonly int WarmupTicks = 100;

		[Desc("Opt-in diagnostic (default off): write a once-per-second line to Logs/adaptivespeed.log with",
			"measured tick compute, the scale reached, and whether the floor was hit. Only logs while the",
			"feature is active. The log file completes when you quit the game. Useful for re-tuning.")]
		public readonly bool EnableDiagnosticLog = false;

		[Desc("Default state of the Adaptive Game Speed lobby option (governs both this single-player path",
			"and the multiplayer host driver).")]
		public readonly bool CheckboxEnabled = true;

		[Desc("Prevent the Adaptive Game Speed lobby option from being changed in the lobby.")]
		public readonly bool CheckboxLocked = false;

		[Desc("Display order for the Adaptive Game Speed lobby option.")]
		public readonly int CheckboxDisplayOrder = 0;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			yield return new LobbyBooleanOption(map, "adaptivegamespeed",
				"Adaptive Game Speed",
				"Smoothly slow game-time under heavy late-game load instead of dropping frames (teleport-stutter), "
					+ "then speed back up as it clears.",
				true, CheckboxDisplayOrder, CheckboxEnabled, CheckboxLocked);
		}

		public override object Create(ActorInitializer init) => new AdaptiveGameSpeed(this);
	}

	// Pacing-only controller: measures the real wall-clock cost of keeping up and, when the loop can't
	// hold real time, asks for a longer logic interval via the TickScale path. The simulation content is
	// untouched (it reads WorldTick, not wall-clock), so this is determinism-safe and intentionally does
	// nothing in any networked/replay/save context — see Active().
	public class AdaptiveGameSpeed : INotifyCreated, ITick
	{
		readonly AdaptiveGameSpeedInfo info;
		readonly AdaptiveSpeedController controller;

		World world;
		long lastRunTime = -1;
		bool wasActive;

		// Diagnostic accumulators (opt-in — see EnableDiagnosticLog). Per ~1s window.
		readonly bool diag;
		long lastLogTime = -1;
		int winTicks, winSat, winAtCap, winDiscarded;
		float winSumP, winMinP, winMaxP, winSumScale, winMaxScale;

		public AdaptiveGameSpeed(AdaptiveGameSpeedInfo info)
		{
			this.info = info;
			diag = info.EnableDiagnosticLog;
			controller = new AdaptiveSpeedController(info.MinimumSpeedPercent, info.OverloadMargin,
				info.Smoothing, info.RecoveryStep, info.RecoveryRate, info.OverloadHoldTicks);
		}

		void INotifyCreated.Created(Actor self)
		{
			world = self.World;
			if (diag)
				Log.AddChannel("adaptivespeed", "adaptivespeed.log");   // idempotent
		}

		bool Active()
		{
			if (world == null || world.Type != WorldType.Regular)
				return false;

			// Determinism guard: never engage for replays or save-load catch-up — those
			// paths bypass tickScale in SuggestedTimestep, and we must not re-introduce it there.
			if (world.IsReplay || world.IsLoadingGameSave)
				return false;

			// Single-player / skirmish only. Any game with more than one human keeps stock pacing — a
			// locally-derived scale must never leak into networked wall-clock flow control.
			if (world.LobbyInfo.NonBotClients.Count() > 1)
				return false;

			return world.LobbyInfo.GlobalSettings.OptionOrDefault("adaptivegamespeed", info.CheckboxEnabled);
		}

		void ITick.Tick(Actor self)
		{
			var now = Game.RunTime;

			if (!Active())
			{
				// Hard no-op: release any slowdown we applied and reset the controller so the next
				// activation starts clean and an inactive game is byte-for-byte stock pacing.
				if (wasActive)
				{
					controller.Reset();
					LocalPacing.SetTickScale(1f);
					wasActive = false;
					ResetDiagWindow();
					lastLogTime = -1;
				}

				lastRunTime = now;
				return;
			}

			wasActive = true;

			if (lastRunTime < 0)
			{
				lastRunTime = now;
				return;
			}

			var period = now - lastRunTime;
			lastRunTime = now;

			// Discard implausible samples (hitches, debugger, alt-tab) so we don't overreact to a one-off.
			if (period <= 0)
				return;

			// Warm-up grace (see WarmupTicks): skip the opening ticks of a match so one-time startup costs
			// (world/pathfinding init, JIT, lazy sprite/sound decode) aren't read as sustained load and dive
			// the game into slow-mo before anything happens. We don't feed the controller either, so its EMA
			// isn't primed with those transients — the first real sample after warm-up is clean.
			if (Game.LocalTick < info.WarmupTicks)
			{
				if (diag)
					LogDiag(now, period);

				return;
			}

			if (period > info.MaxSampleMs)
			{
				if (diag)
					winDiscarded++;   // surfaced in the log so I can tell if real monster ticks are being ignored
				return;
			}

			var budget = world.Timestep;        // unscaled base timestep (e.g. 40ms @ Normal speed)
			if (budget <= 0)
				return;

			// All the slow/recover signal-processing lives in the shared controller (so a future MP
			// host-driver can reuse it verbatim). Here we just feed it the local period and apply the
			// result to the LOCAL tickScale.
			var scale = controller.Update(period, budget);

			// Invariant we rely on for MP/replay safety — must hold every time we touch the pacing path.
			Debug.Assert(!world.IsReplay && !world.IsLoadingGameSave && world.LobbyInfo.NonBotClients.Count() <= 1,
				"AdaptiveGameSpeed must be a no-op outside local single-human games.");

			LocalPacing.SetTickScale(scale);

			if (diag)
				LogDiag(now, period);
		}

		// Diagnostic. Accumulate this tick into the current ~1s window and flush a summary line once a
		// second. Key signals: actual tick compute (period min/avg/max), the scale reached vs the cap, and
		// atCap% — if atCap is high while still saturated, the floor is too shallow for this machine's load.
		void LogDiag(long now, long period)
		{
			if (lastLogTime < 0)
				lastLogTime = now;

			if (winTicks == 0)
			{
				winMinP = period;
				winMaxP = period;
			}
			else
			{
				winMinP = Math.Min(winMinP, period);
				winMaxP = Math.Max(winMaxP, period);
			}

			winTicks++;
			winSumP += period;
			winSumScale += controller.Scale;
			winMaxScale = Math.Max(winMaxScale, controller.Scale);
			if (controller.Saturated)
				winSat++;
			if (controller.AtCap)
				winAtCap++;

			if (now - lastLogTime < 1000)
				return;

			var avgP = winSumP / winTicks;
			var avgScale = winSumScale / winTicks;
			var speedPct = (int)Math.Round(100f / avgScale);
			var line = FormattableString.Invariant(
					$"t={now / 1000}s ticks={winTicks} period_ms[min={winMinP:F0} avg={avgP:F0} max={winMaxP:F0}] ")
				+ FormattableString.Invariant(
					$"ema={controller.EmaPeriodMs:F0} scale[avg={avgScale:F2} max={winMaxScale:F2} cap={controller.MaxScale:F2}] ")
				+ FormattableString.Invariant(
					$"speed={speedPct}% sat={100 * winSat / winTicks}% atCap={100 * winAtCap / winTicks}% ")
				+ FormattableString.Invariant(
					$"discarded={winDiscarded} budget={world.Timestep}");
			Log.Write("adaptivespeed", line);

			lastLogTime = now;
			ResetDiagWindow();
		}

		void ResetDiagWindow()
		{
			winTicks = 0; winSat = 0; winAtCap = 0; winDiscarded = 0;
			winSumP = 0; winMinP = 0; winMaxP = 0; winSumScale = 0; winMaxScale = 0;
		}
	}
}
