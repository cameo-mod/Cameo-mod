#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Server;
using OpenRA.Support;

using S = OpenRA.Server.Server;

namespace OpenRA.Mods.Cameo.ServerTraits
{
	/// <summary>
	/// DEV-ONLY lobby autopilot for the TAKEOVER-SMOKE multi-client harness.
	/// Double-gated: inert unless BOTH `Cameo.DevAutopilot=True` appears on the
	/// OpenRA.Server command line AND `devautopilot.plan` exists in the support dir.
	/// When armed it impersonates each lobby connection through the normal command
	/// dispatch (<see cref="OpenRA.Server.Server.InterpretCommand"/>) —
	/// the same path the UI takes — to assign slots/teams/options, add lobby bots,
	/// ready every client and start the game.
	/// Registered in mod.yaml ServerTraits but changes nothing without the arg + file.
	///
	/// Plan file (one directive per line, `#` comments):
	///   minclients N          wait for N validated connections before acting
	///   delay MS              settle ms between configuration and ready/start
	///   slot INDEX SLOTID     `slot SLOTID` sent as that client's own conn
	///   bot SLOTID TYPE       `slot_bot SLOTID adminIndex TYPE` as the admin conn
	///   botteam SLOTID N      `team botClientIndex N` as the admin conn (after bots)
	///   anything else         sent verbatim as the admin conn
	///                         (map/team/faction/option/spawn/assignteams/...)
	/// </summary>
	public sealed class CameoLobbyAutopilot : ServerTrait, ITick
	{
		const string PlanFile = "devautopilot.plan";
		const int DefaultSettleMs = 2000;

		enum Phase { Idle, Settling, Starting, Done }

		const int StartRetryMs = 500;
		const int MaxStartAttempts = 20;

		readonly List<string[]> directives = [];
		Phase phase = Phase.Idle;

		// Wall-clock orchestration: this trait runs pre-game on the server only,
		// outside the synchronized order stream, and the settle window IS a real-
		// time guarantee for clients to apply the slot/map/option burst (server
		// ITick fires per event-loop iteration, not per timed tick, so it cannot
		// measure this).
		long settleUntilMs;
		long nextAttemptMs;
		int startAttempts;
		int minClients = 1;
		int settleMs;
		bool armed;
		bool checkedGate;

		bool GateOpen(S server)
		{
			if (!CameoDevArgs.IsEnabled("Cameo.DevAutopilot"))
				return false;

			var path = Path.Combine(Platform.SupportDir, PlanFile);
			if (!File.Exists(path))
				return false;

			foreach (var line in File.ReadAllLines(path))
			{
				var text = line.Trim();
				if (text.Length == 0 || text.StartsWith('#'))
					continue;

				var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				if (parts[0].Equals("minclients", StringComparison.OrdinalIgnoreCase))
					minClients = int.Parse(parts[1]);
				else if (parts[0].Equals("delay", StringComparison.OrdinalIgnoreCase))
					settleMs += int.Parse(parts[1]);
				else
					directives.Add(parts);
			}

			Log.Write("server", "CAMEO DEV AUTOPILOT ACTIVE - dev-only lobby drive engaged " +
				$"({directives.Count} directives, minclients {minClients}, settle {settleMs}ms)");
			return true;
		}

		void ITick.Tick(S server)
		{
			if (!checkedGate)
			{
				checkedGate = true;
				armed = GateOpen(server);
			}

			if (!armed || phase == Phase.Done)
				return;

			// A successful start flips State asynchronously; confirm by observing
			// it rather than trusting that startgame was accepted (it silently
			// no-ops on lobby validation failures).
			if (server.State != ServerState.WaitingPlayers)
			{
				if (phase == Phase.Starting)
					Log.Write("server", "CAMEO DEV AUTOPILOT game start confirmed");

				phase = Phase.Done;
				return;
			}

			if (phase == Phase.Idle)
			{
				var conns = server.Conns.Where(c => c.Validated).ToList();
				if (conns.Count < minClients)
					return;

				Execute(server, conns);
				settleUntilMs = Environment.TickCount64 + settleMs + DefaultSettleMs;
				phase = Phase.Settling;
				return;
			}

			if (phase == Phase.Settling)
			{
				if (Environment.TickCount64 < settleUntilMs)
					return;

				phase = Phase.Starting;
			}

			if (Environment.TickCount64 < nextAttemptMs)
				return;

			nextAttemptMs = Environment.TickCount64 + StartRetryMs;

			// Every human readies through their own connection - `state` acts on
			// the sender. Re-issuing is idempotent and recovers clients whose
			// state was reset (e.g. a late map-select callback marks them Invalid).
			foreach (var conn in server.Conns.Where(c => c.Validated))
				server.InterpretCommand("state Ready", conn);

			var admin = server.Conns.FirstOrDefault(c => c.Validated && server.GetClient(c) is { IsAdmin: true });
			if (admin == null)
				return;

			server.InterpretCommand("state Ready", admin);
			server.InterpretCommand("startgame", admin);

			if (++startAttempts >= MaxStartAttempts)
			{
				phase = Phase.Done;
				Log.Write("server", $"CAMEO DEV AUTOPILOT start FAILED - lobby still WaitingPlayers after {startAttempts} attempts");
				return;
			}

			Log.Write("server", $"CAMEO DEV AUTOPILOT start attempt {startAttempts} issued");
		}

		void Execute(S server, List<Connection> conns)
		{
			var adminConn = conns.FirstOrDefault(c => server.GetClient(c) is { IsAdmin: true }) ?? conns[0];

			foreach (var d in directives)
			{
				try
				{
					switch (d[0].ToLowerInvariant())
					{
						case "slot":
						{
							var conn = conns.FirstOrDefault(c => server.GetClient(c)?.Index == int.Parse(d[1]));
							if (conn != null)
								server.InterpretCommand("slot " + d[2], conn);
							else
								Log.Write("server", $"CAMEO DEV AUTOPILOT: no conn for client index {d[1]}");
							break;
						}

						case "bot":
						{
							var adminIdx = server.GetClient(adminConn).Index;
							server.InterpretCommand($"slot_bot {d[1]} {adminIdx} {d[2]}", adminConn);
							break;
						}

						case "botteam":
						{
							var bot = server.LobbyInfo.ClientInSlot(d[1]);
							if (bot != null)
								server.InterpretCommand($"team {bot.Index} {d[2]}", adminConn);
							else
								Log.Write("server", $"CAMEO DEV AUTOPILOT: no bot in slot {d[1]}");
							break;
						}

						default:
							server.InterpretCommand(string.Join(' ', d), adminConn);
							break;
					}
				}
				catch (Exception e)
				{
					Log.Write("server", $"CAMEO DEV AUTOPILOT directive failed '{string.Join(' ', d)}': {e.Message}");
				}
			}
		}
	}
}
