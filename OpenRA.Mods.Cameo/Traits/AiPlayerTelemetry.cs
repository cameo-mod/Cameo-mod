#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Release-compatible configuration; player telemetry is provided by the DEV mod.")]
	public sealed class AiPlayerTelemetryInfo : TraitInfo
	{
		[Desc("World ticks between per-player economy/army samples.")]
		public readonly int SampleIntervalTicks = 750;

		[Desc("World ticks between combat-unit position samples for the wave/territory detectors.")]
		public readonly int MovementIntervalTicks = 50;

		[Desc("Ticks an MCV removal and a ConYard appearance may be apart and still read as a deploy (or repack).")]
		public readonly int DeployMatchWindowTicks = 10;

		[Desc("Maximum cell distance between a removed MCV and a new ConYard that still reads as a deploy.")]
		public readonly int DeployMatchCells = 3;

		[Desc("Combat units simultaneously marching outward needed to open an attack wave.")]
		public readonly int WaveMinUnits = 5;

		[Desc("Marching units below which an open wave closes again (hysteresis).")]
		public readonly int WaveEndUnits = 2;

		[Desc("Cells a unit must gain away from its own start and toward an enemy inside one movement window to count as marching.")]
		public readonly int MarchMinCells = 4;

		[Desc("Cells around an enemy start that count as that enemy's territory.")]
		public readonly int EnemyTerritoryCells = 20;

		[Desc("Per-player cap on tracked combat units for the wave detector.")]
		public readonly int MaxTrackedUnitsPerPlayer = 512;

		[Desc("Distinct unit-type names kept per sample; the rest aggregate into `other`.")]
		public readonly int MaxUnitTypesPerSample = 48;

		[Desc("ConYard removals held back while a matching MCV appearance can still arrive (Transforms remove the source first).")]
		public readonly int MaxPendingConyardRemovals = 64;

		[Desc("World ticks between per-bot production-queue samples (WBL-P1 conversion diagnostics).")]
		public readonly int QueueIntervalTicks = 250;

		[Desc("Per-seat cap on queue rows inside one bot-queue sample.")]
		public readonly int MaxQueueRowsPerSeat = 32;

		public override object Create(ActorInitializer init) { return new AiPlayerTelemetry(this); }
	}

	/// <summary>Pure helpers of the player telemetry, free of world state so they can be unit-tested.</summary>
	internal static class PlayerTelemetryMath
	{
		/// <summary>Army categories of the sample row, mirroring army_mix_report's class split.</summary>
		public static string MobileCategory(bool aircraft, bool naval, bool infantry)
		{
			if (aircraft)
				return "air";
			if (naval)
				return "naval";
			return infantry ? "infantry" : "vehicle";
		}

		/// <summary>
		/// A combat unit is marching when, inside one movement window, it gained at least
		/// <paramref name="minCells"/> away from its own start AND got closer to an enemy start.
		/// Distances are Euclidean cells; a missing previous enemy distance (-1) cannot prove the
		/// approach leg, so it never marches.
		/// </summary>
		public static bool Marching(double prevHome, double nowHome, double prevEnemy, double nowEnemy, int minCells)
		{
			return prevEnemy >= 0 && nowHome - prevHome >= minCells && nowEnemy < prevEnemy;
		}

		public enum WaveEdge { None, Start, End }

		/// <summary>Hysteresis: a wave opens at <paramref name="startMin"/> marchers, closes below <paramref name="endMin"/>.</summary>
		public static WaveEdge Wave(bool active, int marching, int startMin, int endMin)
		{
			if (!active && marching >= startMin)
				return WaveEdge.Start;
			if (active && marching < endMin)
				return WaveEdge.End;
			return WaveEdge.None;
		}

		/// <summary>Positive (non-negated) prerequisite tokens: `~` hidden markers stripped, `!` inversions dropped.</summary>
		public static List<string> PositivePrereqs(IEnumerable<string> prerequisites)
		{
			var tokens = new List<string>();
			if (prerequisites == null)
				return tokens;

			foreach (var raw in prerequisites)
			{
				var token = (raw ?? "").Trim().TrimStart('~').Trim();
				if (token.Length == 0 || token.StartsWith('!'))
					continue;
				tokens.Add(token.ToLowerInvariant());
			}

			return tokens;
		}

		/// <summary>
		/// Tech depth of every building: 1 + the deepest prerequisite chain, each token resolved
		/// through its shallowest building provider so the depth is the shortest build order that
		/// reaches it. Tokens with no building provider add 0. Deterministic: recursion is
		/// memoized, cycles contribute 0 through the back-edge, roots scan in ordinal order.
		/// </summary>
		public static Dictionary<string, int> TechDepths(
			IReadOnlyDictionary<string, (IReadOnlyList<string> Prereqs, IReadOnlyList<string> Provides)> buildings)
		{
			var providers = new Dictionary<string, List<string>>(StringComparer.Ordinal);
			foreach (var kv in buildings)
				foreach (var token in kv.Value.Provides.Append(kv.Key))
				{
					if (!providers.TryGetValue(token, out var list))
						providers[token] = list = [];
					list.Add(kv.Key);
				}

			var depths = new Dictionary<string, int>(StringComparer.Ordinal);
			var visiting = new HashSet<string>(StringComparer.Ordinal);

			int Depth(string name)
			{
				if (depths.TryGetValue(name, out var d))
					return d;
				if (!buildings.TryGetValue(name, out var spec) || visiting.Contains(name))
					return 0;

				visiting.Add(name);
				var best = 0;
				foreach (var token in spec.Prereqs)
				{
					var shallowest = int.MaxValue;
					if (providers.TryGetValue(token, out var list))
						foreach (var provider in list)
							if (provider != name)
							{
								var candidate = Depth(provider);
								if (candidate > 0 && candidate < shallowest)
									shallowest = candidate;
							}

					if (shallowest != int.MaxValue && shallowest + 1 > best)
						best = shallowest + 1;
				}

				visiting.Remove(name);
				return depths[name] = Math.Max(1, best);
			}

			foreach (var name in buildings.Keys.OrderBy(n => n, StringComparer.Ordinal))
				Depth(name);

			return depths;
		}

		/// <summary>Spend categories of the bot-queue row (WBL-P1 spec: unit, defence, crawl/link, tech, economy).</summary>
		public static readonly string[] QueueCategories = ["unit", "defence", "crawl_link", "tech", "economy"];

		/// <summary>Queue row state: disabled beats busy; a paused head item reads paused, not idle.</summary>
		public static string QueueState(bool enabled, bool hasHead, bool paused)
		{
			if (!enabled)
				return "disabled";
			if (!hasHead)
				return "idle";
			return paused ? "paused" : "busy";
		}

		/// <summary>
		/// Idle-queue hold reason in the unit builder's own gate order: a pause vote (cash
		/// reserved by a named provider) outranks the cash floor, which outranks the
		/// bank-saturation sweep stop, then prerequisites and the unit cap; the remainder
		/// is genuinely no desire. Inferred from the same public signals the module reads.
		/// </summary>
		public static string IdleReason(bool reserved, bool belowCashFloor, bool saturated,
			bool noBuildable, bool atUnitCap)
		{
			if (reserved)
				return "cash_reserved";
			if (belowCashFloor)
				return "no_cash";
			if (saturated)
				return "saturation_stop";
			if (noBuildable)
				return "prerequisite_missing";
			if (atUnitCap)
				return "unit_cap";
			return "no_desire";
		}

		/// <summary>
		/// Spend category of a produced item: defence types and the defence tag map to
		/// defence, refinery types/tag to economy, fragile (crawl/link family) types to
		/// crawl_link, every other building to tech. Unit-queue items are always unit.
		/// </summary>
		public static string BuildingCategory(bool defence, bool refinery, bool crawlLink)
		{
			if (defence)
				return "defence";
			if (refinery)
				return "economy";
			if (crawlLink)
				return "crawl_link";
			return "tech";
		}

		/// <summary>
		/// Union of every FragileTypes list declared on the player actor: the crawl/link
		/// family is a content-level taxonomy, not per-module truth, so a seat without a
		/// live CA base builder (Frans) still classifies fragile-family items instead of
		/// silently falling through to tech.
		/// </summary>
		public static HashSet<string> UnionFragileTypes(IEnumerable<IEnumerable<string>> declared)
		{
			var set = new HashSet<string>(StringComparer.Ordinal);
			if (declared == null)
				return set;

			foreach (var d in declared)
				if (d != null)
					set.UnionWith(d);

			return set;
		}

		/// <summary>
		/// Order-issue marker key shared by the unit-production seam (which writes it
		/// from the owning player's actor id) and the queue poll (which looks it up
		/// with the same player actor id). The poll's per-queue head dictionary keeps
		/// the queue HOST actor id instead — in this mod ProductionQueue traits live
		/// on factory buildings, never the player entity — so the two key families
		/// deliberately differ. One constructor for both call sites keeps them equal.
		/// </summary>
		public static string SeamKey(long actorId, string queueType, string item)
		{
			return actorId + ":" + queueType + "|" + item;
		}

		/// <summary>
		/// Consume one pending order-issue marker: true when a marker existed (the
		/// head-diff must NOT count that acceptance/spend again), false otherwise (the
		/// start came through a path the seam does not observe and is counted here).
		/// </summary>
		public static bool ConsumeSeamStart(Dictionary<string, int> markers, string key)
		{
			if (!markers.TryGetValue(key, out var pending) || pending <= 0)
				return false;
			if (pending == 1)
				markers.Remove(key);
			else
				markers[key] = pending - 1;
			return true;
		}
	}

	internal sealed class BuildingTrack
	{
		public Actor Actor;
		public OpenRA.Player Owner;
	}

	internal sealed class DeployEvent
	{
		public OpenRA.Player Owner;
		public CPos Cell;
		public int Tick;
		public string ActorName;
		public long ActorId;
		public string Cause;
	}

	/// <summary>Pure matching of an MCV removal against a yard added moments earlier (the engine's Transform adds the yard first).</summary>
	internal static class DeployMatchMath
	{
		/// <summary>Index of the newest entry of the same owner within windowTicks and cells (Chebyshev) of the removal; -1 when none.</summary>
		public static int Find<TOwner>(IReadOnlyList<(TOwner Owner, CPos Cell, int Tick)> entries, TOwner owner, CPos cell, int tick, int windowTicks, int cells)
		{
			for (var i = entries.Count - 1; i >= 0; i--)
			{
				var e = entries[i];
				if (!EqualityComparer<TOwner>.Default.Equals(e.Owner, owner)
					|| Math.Abs(e.Tick - tick) > windowTicks
					|| Math.Max(Math.Abs(e.Cell.X - cell.X), Math.Abs(e.Cell.Y - cell.Y)) > cells)
					continue;

				return i;
			}

			return -1;
		}
	}

	internal sealed class PendingDeployAdd
	{
		public Actor Actor;
		public OpenRA.Player Owner;
		public CPos Cell;
		public int Tick;
	}

	internal sealed class PendingConyardRemoval
	{
		public OpenRA.Player Owner;
		public string ActorName;
		public long ActorId;
		public CPos Cell;
		public int Tick;
		public string Cause;
	}

	internal sealed class SeatSample
	{
		public int Harvesters, Refineries, Conyards, Production, Buildings, Units;
		public long ArmyInfantry, ArmyVehicle, ArmyAir, ArmyNaval, ArmyDefence;
		public readonly Dictionary<string, int> UnitTypes = new(StringComparer.Ordinal);
	}

	internal sealed class SeatTelemetry
	{
		public OpenRA.Player Player;
		public string Seat;
		public CPos Home;
		public int Team;
		public bool WaveActive;
		public bool FirstUnitLogged;
		public int ExpansionDeploys;
		public int EventSeq;
		public int IdleProductionTicks;
		public int TechTier;
		public readonly SeatSample Sample = new();
		public readonly Dictionary<uint, CPos> Units = new();
		public readonly Dictionary<uint, (double Home, double Enemy)> Dists = new();
		public readonly Dictionary<uint, (double Home, double Enemy)> PreviousDists = new();
		public readonly Dictionary<string, bool> PowerReady = new(StringComparer.Ordinal);

		// WBL-P1 conversion diagnostics (bot seats only): per-window counters reset after
		// each queue sample; QueueHeads persists so a new head item reads as an acceptance.
		public bool QueueHelpersResolved;
		public int LastSpent = -1;
		public BaseBuilderBotModuleCA BaseBuilder;
		public PlayerResources Resources;
		public readonly Dictionary<string, int> QueueRequested = new(StringComparer.Ordinal);
		public readonly Dictionary<string, int> QueueAccepted = new(StringComparer.Ordinal);
		public readonly Dictionary<string, int> QueueCompleted = new(StringComparer.Ordinal);
		public readonly Dictionary<string, long> QueueSpend = new(StringComparer.Ordinal);
		public readonly Dictionary<string, int> QueueSkipped = new(StringComparer.Ordinal);
		public readonly Dictionary<string, ProductionItem> QueueHeads = new(StringComparer.Ordinal);

		// Order-issue markers left by the unit-production seam so the head-diff does not
		// count the same acceptance a second time. Keys come from PlayerTelemetryMath.SeamKey
		// on the player-actor id at both sites — queue hosts are factory buildings, so the
		// marker pool is shared by a seat's same-type queues and the totals stay exact.
		public readonly Dictionary<string, int> SeamStarts = new(StringComparer.Ordinal);
	}

	// The release rules retain this registration; collection lives in the DEV assembly.
	public sealed class AiPlayerTelemetry
	{
		public AiPlayerTelemetry(AiPlayerTelemetryInfo info) { }
	}
}
