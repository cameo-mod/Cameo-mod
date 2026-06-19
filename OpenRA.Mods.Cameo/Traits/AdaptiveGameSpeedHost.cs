#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Multiplayer host-side driver for adaptive game-speed (Stage C Prong C). On the game host ONLY,",
		"measures the host's per-tick simulation compute and submits an absolute pacing scale to the server",
		"(an immediate 'AdaptivePacingScale' order). The server validates admin + sanitizes it and folds it",
		"into the authoritative TickScale broadcast (max with the per-player relative scale), so every client",
		"slows UNIFORMLY to the host's sustainable pace. Pacing-only — it never touches World.Timestep / the",
		"simulated dt and the host applies the value the SERVER rebroadcasts (like every client), not its local",
		"one — so it is deterministic by construction. Inert unless this client is the host, more than one human",
		"is connected (single-human games use the local AdaptiveGameSpeed path), and the Cameo",
		"'AdaptiveGameSpeedEnabled' setting is on. Attach to the world actor.")]
	public class AdaptiveGameSpeedHostInfo : TraitInfo
	{
		[Desc("Slowest the game may run, as a percentage of normal speed. 25 = quarter speed (timestep x4).",
			"The server independently clamps the absolute scale, so this is the controller's own floor.")]
		public readonly int MinimumSpeedPercent = 25;

		[Desc("Only begin slowing once measured sim-compute exceeds the current paced budget by this fraction.")]
		public readonly float OverloadMargin = 0.15f;

		[Desc("EMA smoothing for the measured sim-compute (0-1; higher = more responsive). See AdaptiveGameSpeed.")]
		public readonly float Smoothing = 0.30f;

		[Desc("Absolute floor for the per-tick recovery step (scale units).")]
		public readonly float RecoveryStep = 0.01f;

		[Desc("Proportional recovery: each tick with headroom, shed this fraction of the current slowdown.")]
		public readonly float RecoveryRate = 0.04f;

		[Desc("After slowing down, hold before probing for speed-up again (ticks). Damps oscillation.")]
		public readonly int OverloadHoldTicks = 25;

		[Desc("Ignore sim-compute samples longer than this many ms (load hitches, breakpoints).")]
		public readonly int MaxSampleMs = 500;

		[Desc("Only send a new pacing order when the scale moves by at least this much, to throttle network",
			"traffic. The final return to exactly 1.0 (full speed) is always sent regardless.")]
		public readonly float SendThreshold = 0.02f;

		[Desc("While a slowdown is in effect, re-send the current scale at least this often (ms) as a heartbeat",
			"so the server can auto-release the slowdown if the host stops reporting (disconnect/crash). Must be",
			"comfortably below the server's expiry window.")]
		public readonly int HeartbeatMs = 1000;

		public override object Create(ActorInitializer init) => new AdaptiveGameSpeedHost(this);
	}

	// Host-authoritative MP pacing driver. Reuses the shared AdaptiveSpeedController (the same maths as the
	// single-player AdaptiveGameSpeed), but feeds it the host's clean SIM-COMPUTE signal (Game.LastWorldTickTimeMs,
	// which excludes the lockstep network wait) and routes the result through the server instead of the local
	// pacing path. It only SENDS; the applied slowdown arrives back via the server's TickScale rebroadcast.
	public class AdaptiveGameSpeedHost : INotifyCreated, ITick
	{
		readonly AdaptiveGameSpeedHostInfo info;
		readonly AdaptiveSpeedController controller;

		World world;
		float lastSentScale = 1f;
		long lastSendTime;
		bool wasActive;

		public AdaptiveGameSpeedHost(AdaptiveGameSpeedHostInfo info)
		{
			this.info = info;
			controller = new AdaptiveSpeedController(info.MinimumSpeedPercent, info.OverloadMargin,
				info.Smoothing, info.RecoveryStep, info.RecoveryRate, info.OverloadHoldTicks);
		}

		void INotifyCreated.Created(Actor self)
		{
			world = self.World;
		}

		bool Active()
		{
			if (world == null || world.Type != WorldType.Regular)
				return false;

			// Never drive pacing for replays or save-load catch-up (those bypass tickScale entirely).
			if (world.IsReplay || world.IsLoadingGameSave)
				return false;

			// Host-authoritative: only the host (which runs the bots and is the sim bottleneck) drives the
			// global pace, and only in a real multiplayer game. A single human (incl. listen-server + bots)
			// is handled locally by AdaptiveGameSpeed, so the two never both drive.
			if (!OpenRA.Game.IsHost || world.LobbyInfo.NonBotClients.Count() <= 1)
				return false;

			return world.GetSettings<CameoSettings>().AdaptiveGameSpeedEnabled;
		}

		void ITick.Tick(Actor self)
		{
			if (!Active())
			{
				// Release any slowdown we requested so the server's absolute term falls back to 1.0.
				if (wasActive)
				{
					controller.Reset();
					if (lastSentScale != 1f)
						Send(1f, OpenRA.Game.RunTime);

					wasActive = false;
				}

				return;
			}

			wasActive = true;

			var budget = world.Timestep;
			if (budget <= 0)
				return;

			// Clean sim-compute signal: the wall cost of the previous world.Tick() only (the lockstep wait
			// happens outside it), so a host blocked on remote orders is NOT read as compute load — MP4.
			var compute = OpenRA.Game.LastWorldTickTimeMs;
			if (compute > info.MaxSampleMs)
				return;   // one-off hitch / breakpoint — don't overreact

			var scale = controller.Update(compute, budget);
			var now = OpenRA.Game.RunTime;

			// Send on a meaningful change (the server pushes the rebroadcast immediately); always send the
			// final settle to exactly 1.0; and while slowed, re-send periodically as a heartbeat so the server
			// auto-releases if we drop out. We never apply locally — the server rebroadcast does that.
			var changed = Math.Abs(scale - lastSentScale) >= info.SendThreshold;
			var settled = scale == 1f && lastSentScale != 1f;
			var heartbeat = scale > 1f && now - lastSendTime >= info.HeartbeatMs;
			if (changed || settled || heartbeat)
				Send(scale, now);
		}

		void Send(float scale, long now)
		{
			// Server-only send: bypasses the local immediate-order path so the value is never echoed,
			// processed, or recorded into our replay — it only reaches the server, which folds it into the
			// authoritative TickScale broadcast. We never apply it locally; the rebroadcast does that.
			OpenRA.Network.HostPacing.SendScale(scale);
			lastSentScale = scale;
			lastSendTime = now;
		}
	}
}
