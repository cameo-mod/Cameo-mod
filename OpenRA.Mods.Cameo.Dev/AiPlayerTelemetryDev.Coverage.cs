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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	// BUILD-ALL-B (Opus ruling 2, 2026-11-25), DEV only: the rows the COVERAGE check needs, which V08 seed 100775 did not log.
	// - bb_listed_set / ub_listed_set: once per bot seat, the effective BuildingFractions / UnitsToBuild of its enabled generic modules (after inheritance
	//   and the content packs), as "name:weight" sorted by name.
	// - bb_type_available / ub_type_available: once per bot seat per listed type, the first poll (every CoveragePollTicks) at which the type is in the
	//   BuildableItems of one of the seat's enabled queues (prerequisites met and a producer standing).
	// - harvester_lost: a killed harvester (a carryall lift or any other removal is not a loss).
	// - bb_pick / bb_a6_hold / bb_refinery_held: the staged planner's admitted starts, its A6 holds and the wanted-refinery gate that held a refinery
	//   (IBotBuildingPickObserver; the refinery row is written on a gate change or after RefineryHeldRepeatTicks).
	// - REF-DEMAND-01 (IBotRefineryDemandObserver): rd_zones (the zone model, once per seat), rd_zone (one zone's verdict, with its id, centre x,y and
	//   radius), rd_demand (the seat's sum), rd_anchor_parked / rd_anchor_pending, rd_refinery_placed (the site x,y, the zone and whether the fallback
	//   spacing was used). rd_zone / rd_demand / park / pending rows are written per (zone, row) on a change or after RefineryDemandRepeatTicks.
	// Record-only: nothing here writes synced state, orders or random draws. A fault ends the channel through Fail(), never the game.
	public sealed partial class AiPlayerTelemetryDev : IBotBuildingPickObserver, IBotRefineryDemandObserver
	{
		const int CoveragePollTicks = 50;
		const int RefineryHeldRepeatTicks = 750;
		const int RefineryDemandRepeatTicks = 750;

		// REF-DEMAND-01: the last (signature, tick) written per (seat, zone, row kind).
		readonly Dictionary<(OpenRA.Player Owner, int Zone, string Kind), (string Signature, int Tick)> demandRows = new();

		sealed class CoverageSeat
		{
			public HashSet<string> Buildings;
			public HashSet<string> Units;
			public readonly HashSet<string> Available = new(StringComparer.Ordinal);
			public UpgradeCompletions[] UpgradeRegistries; // the once-done registries of the seat's UnitBuilder modules (the module decides completion; this file only reads it)
			public HashSet<string> UpgradeQueues; // the seat's UnitBuilder UpgradeServiceQueues (Promotions, Upgrades, Research, Addons by default)
			public readonly Dictionary<string, string> UpgradeAvailable = new(StringComparer.Ordinal); // type -> queue it was first buildable in
			public readonly HashSet<string> UpgradeDone = new(StringComparer.Ordinal);
			public string LastRefineryGate;
			public int LastRefineryGateTick = -1;
			public string LastRef2Key;
			public int LastRef2Tick = -1;
			public string LastRefTierKey;
			public readonly Dictionary<string, int> LastSkipTick = new(StringComparer.Ordinal);
		}

		readonly Dictionary<OpenRA.Player, CoverageSeat> coverage = new();

		// Null until a generic base or unit builder of the seat is enabled (conditions arm after world load); a null result is not cached.
		CoverageSeat CoverageFor(SeatTelemetry seat)
		{
			if (coverage.TryGetValue(seat.Player, out var cov))
				return cov;

			var actor = seat.Player.PlayerActor;
			if (actor == null || actor.IsDead || actor.Disposed)
				return null;

			var bases = actor.TraitsImplementing<BaseBuilderBotModuleCA>().Where(t => t.IsTraitEnabled()).ToList();
			var units = actor.TraitsImplementing<UnitBuilderBotModuleCA>().Where(t => t.IsTraitEnabled()).ToList();
			if (bases.Count == 0 && units.Count == 0)
				return null;

			var buildingWeights = MergeWeights(bases.Select(b => (IReadOnlyDictionary<string, int>)b.Info.BuildingFractions));
			var unitWeights = MergeWeights(units.Select(u => (IReadOnlyDictionary<string, int>)u.Info.UnitsToBuild));
			cov = new CoverageSeat
			{
				Buildings = new HashSet<string>(buildingWeights.Keys, StringComparer.Ordinal),
				Units = new HashSet<string>(unitWeights.Keys, StringComparer.Ordinal),
				UpgradeQueues = new HashSet<string>(units.SelectMany(u => u.Info.UpgradeServiceQueues), StringComparer.Ordinal),
				UpgradeRegistries = units.Select(u => u.UpgradeRegistry).ToArray(),
			};
			coverage[seat.Player] = cov;

			EmitListedSet(seat, "bb_listed_set", buildingWeights);
			EmitListedSet(seat, "ub_listed_set", unitWeights);
			return cov;
		}

		// The largest weight per name over the enabled modules (one generic module per bot in practice).
		static SortedDictionary<string, int> MergeWeights(IEnumerable<IReadOnlyDictionary<string, int>> tables)
		{
			var merged = new SortedDictionary<string, int>(StringComparer.Ordinal);
			foreach (var table in tables)
			{
				if (table == null)
					continue;

				foreach (var kv in table)
					merged[kv.Key] = merged.TryGetValue(kv.Key, out var w) ? Math.Max(w, kv.Value) : kv.Value;
			}

			return merged;
		}

		void EmitListedSet(SeatTelemetry seat, string kind, SortedDictionary<string, int> weights)
		{
			var b = BeginEvent(seat, kind, 1, out _);
			AiMatchLogWriter.AppendNumber(b, "count", weights.Count);
			AiMatchLogWriter.AppendString(b, "types", string.Join(",", weights.Select(kv => kv.Key + ":" + kv.Value.ToString(CultureInfo.InvariantCulture))));
			b.Append('}');
			writer.TryWrite(b.ToString());
		}

		void CoveragePoll(World world)
		{
			foreach (var pair in world.ActorsWithTrait<ProductionQueue>())
			{
				var queue = pair.Trait;
				if (!queue.Enabled || pair.Actor.Owner == null)
					continue;

				var seat = BotSeat(pair.Actor.Owner);
				if (seat == null)
					continue;

				var cov = CoverageFor(seat);
				if (cov == null)
					continue;

				UpgradeDoneRows(seat, cov);
				var upgradeQueue = cov.UpgradeQueues.Contains(queue.Info.Type);
				foreach (var item in queue.BuildableItems())
				{
					var name = item.Name;

					// UPGRADE-COVERAGE: a promotion / upgrade / research / add-on of a served queue that the seat could research (once per seat and type)
					if (upgradeQueue && cov.UpgradeAvailable.TryAdd(name, queue.Info.Type))
						EmitUpgradeRow(seat, "ub_upgrade_available", name, queue.Info.Type);

					var building = cov.Buildings.Contains(name);
					if (!building && !cov.Units.Contains(name))
						continue;

					if (!cov.Available.Add(name))
						continue;

					var b = BeginEvent(seat, building ? "bb_type_available" : "ub_type_available", 1, out _);
					AiMatchLogWriter.AppendString(b, "actor", name);
					AiMatchLogWriter.AppendString(b, "queue", queue.Info.Type);
					b.Append('}');
					writer.TryWrite(b.ToString());
				}
			}
		}

		void EmitUpgradeRow(SeatTelemetry seat, string kind, string actor, string queue, string source = null)
		{
			var b = BeginEvent(seat, kind, 1, out _);
			AiMatchLogWriter.AppendString(b, "actor", actor);
			AiMatchLogWriter.AppendString(b, "queue", queue);
			if (source != null)
				AiMatchLogWriter.AppendString(b, "upg_done_source", source);
			b.Append('}');
			writer.TryWrite(b.ToString());
		}

		// ub_upgrade_done: once per seat and type, the first poll at which the UnitBuilder's once-done registry holds the type as completed (the module's completion rule: the item left its
		// queue and the player holds its prerequisite, or it was already held at the first visit). A type is reported only after it was first seen available.
		void UpgradeDoneRows(SeatTelemetry seat, CoverageSeat cov)
		{
			foreach (var pair in cov.UpgradeAvailable)
			{
				if (cov.UpgradeDone.Contains(pair.Key) || !cov.UpgradeRegistries.Any(r => r.Completed(pair.Key)))
					continue;

				cov.UpgradeDone.Add(pair.Key);
				EmitUpgradeRow(seat, "ub_upgrade_done", pair.Key, pair.Value, cov.UpgradeRegistries.Select(r => r.Source(pair.Key)).FirstOrDefault(x => x != null));
			}
		}

		void HarvesterLost(Actor actor)
		{
			if (!actor.Info.HasTraitInfo<HarvesterInfo>() || LossCause(actor) != "killed")
				return;

			var seat = Seat(actor.Owner);
			if (seat != null)
				EmitEvent(seat, "harvester_lost", actor.Info.Name, actor.Location, cause: "killed",
					startDist: Cells(actor.Location, seat.Home), actorId: actor.ActorID);
		}

		void IBotBuildingPickObserver.BuildingPicked(OpenRA.Player owner, int tick, string actor, string stage, string candidate, int bank)
		{
			ObserverRow(owner, "bb_pick", b =>
			{
				AiMatchLogWriter.AppendString(b, "actor", actor);
				AiMatchLogWriter.AppendString(b, "stage", stage);
				AiMatchLogWriter.AppendString(b, "candidate", candidate);
				AiMatchLogWriter.AppendNumber(b, "bank", bank);
			});
		}

		// CONSOLIDATE-03 P1 bb_decide: the building-pick arbiter's decision (deduped per queue by the caller).
		void IBotBuildingPickObserver.BuildingDecided(OpenRA.Player owner, int tick, string queue, bool sustained, int bank, int tierLine, string actor, string stage, string decider,
			int s8Offers, int refusalCount, string refusals)
		{
			ObserverRow(owner, "bb_decide", b =>
			{
				AiMatchLogWriter.AppendString(b, "queue", queue);
				AiMatchLogWriter.AppendNumber(b, "sustained", sustained ? 1 : 0);
				AiMatchLogWriter.AppendNumber(b, "bank", bank);
				AiMatchLogWriter.AppendNumber(b, "line", tierLine);
				AiMatchLogWriter.AppendString(b, "actor", actor);
				AiMatchLogWriter.AppendString(b, "stage", stage);
				AiMatchLogWriter.AppendString(b, "decider", decider);
				AiMatchLogWriter.AppendNumber(b, "s8_offers", s8Offers);
				AiMatchLogWriter.AppendNumber(b, "refusals", refusalCount);
				AiMatchLogWriter.AppendString(b, "refused", refusals);
			});
		}

		void IBotUnitProductionObserver.CashLedgerObserved(OpenRA.Player owner, int tick, long bank, long unpaidUnit, long unpaidBase, long unpaidUpgrade, int unitReserve,
			int buildingReserveS8, long over, int refusedAfford, int refusedReserve, int refusedDepth, long unpaidCacheDiff)
		{
			ObserverRow(owner, "cash_ledger", b =>
			{
				AiMatchLogWriter.AppendNumber(b, "bank", bank);
				AiMatchLogWriter.AppendNumber(b, "unpaid_unit", unpaidUnit);
				AiMatchLogWriter.AppendNumber(b, "unpaid_base", unpaidBase);
				AiMatchLogWriter.AppendNumber(b, "unpaid_upgrade", unpaidUpgrade);
				AiMatchLogWriter.AppendNumber(b, "reserve_unit", unitReserve);
				AiMatchLogWriter.AppendNumber(b, "reserve_building_s8", buildingReserveS8);
				AiMatchLogWriter.AppendNumber(b, "over", over);
				AiMatchLogWriter.AppendNumber(b, "refused_afford", refusedAfford);
				AiMatchLogWriter.AppendNumber(b, "refused_reserve", refusedReserve);
				AiMatchLogWriter.AppendNumber(b, "refused_depth", refusedDepth);
				AiMatchLogWriter.AppendNumber(b, "unpaid_cache_diff", unpaidCacheDiff);
			});
		}

		void IBotBuildingPickObserver.BuildingA6Held(OpenRA.Player owner, int tick, string actor, string stage, int bank, int cost)
		{
			ObserverRow(owner, "bb_a6_hold", b =>
			{
				AiMatchLogWriter.AppendString(b, "actor", actor);
				AiMatchLogWriter.AppendString(b, "stage", stage);
				AiMatchLogWriter.AppendNumber(b, "bank", bank);
				AiMatchLogWriter.AppendNumber(b, "cost", cost);
			});
		}

		void IBotBuildingPickObserver.BuildingSkipped(OpenRA.Player owner, int tick, string actor, string queue, string stage, string reason, string detail)
		{
			if (writer == null || ended)
				return;

			CoverageSeat cov;
			try
			{
				var seat = BotSeat(owner);
				cov = seat == null ? null : CoverageFor(seat);
			}
			catch (Exception e)
			{
				Fail(e, "bb_skip");
				return;
			}

			// Per walk, not per actor across walks: one row per (player, queue, type) per 750 ticks (cov is per player).
			var key = queue + "|" + actor;
			if (cov == null || (cov.LastSkipTick.TryGetValue(key, out var last) && tick - last < RefineryHeldRepeatTicks))
				return;

			cov.LastSkipTick[key] = tick;
			ObserverRow(owner, "bb_skip", b =>
			{
				AiMatchLogWriter.AppendString(b, "actor", actor);
				AiMatchLogWriter.AppendString(b, "queue", queue);
				AiMatchLogWriter.AppendString(b, "stage", stage);
				AiMatchLogWriter.AppendString(b, "reason", reason);
				AiMatchLogWriter.AppendString(b, "detail", detail);
			});
		}

		void IBotBuildingPickObserver.RefineryHeld(OpenRA.Player owner, int tick, string gate, int refineries, string queue)
		{
			if (writer == null || ended)
				return;

			CoverageSeat cov;
			try
			{
				var seat = BotSeat(owner);
				cov = seat == null ? null : CoverageFor(seat);
			}
			catch (Exception e)
			{
				Fail(e, "bb_refinery_held");
				return;
			}

			var key = gate + "@" + queue;
			if (cov == null || (cov.LastRefineryGate == key && cov.LastRefineryGateTick >= 0 && tick - cov.LastRefineryGateTick < RefineryHeldRepeatTicks))
				return;

			cov.LastRefineryGate = key;
			cov.LastRefineryGateTick = tick;
			ObserverRow(owner, "bb_refinery_held", b =>
			{
				AiMatchLogWriter.AppendString(b, "gate", gate);
				AiMatchLogWriter.AppendNumber(b, "refineries", refineries);
				AiMatchLogWriter.AppendString(b, "queue", queue);
			});
		}

		void IBotBuildingPickObserver.Ref2Verdict(OpenRA.Player owner, int tick, string verdict, string detail, int refineries, string queue)
		{
			if (writer == null || ended)
				return;

			CoverageSeat cov;
			try
			{
				var seat = BotSeat(owner);
				cov = seat == null ? null : CoverageFor(seat);
			}
			catch (Exception e)
			{
				Fail(e, "ref2_verdict");
				return;
			}

			// One row per changed (verdict, detail, queue), or again after RefineryHeldRepeatTicks.
			var key = verdict + "|" + detail + "@" + queue;
			if (cov == null || (cov.LastRef2Key == key && cov.LastRef2Tick >= 0 && tick - cov.LastRef2Tick < RefineryHeldRepeatTicks))
				return;

			cov.LastRef2Key = key;
			cov.LastRef2Tick = tick;
			ObserverRow(owner, "ref2_verdict", b =>
			{
				AiMatchLogWriter.AppendString(b, "verdict", verdict);
				AiMatchLogWriter.AppendString(b, "detail", detail);
				AiMatchLogWriter.AppendNumber(b, "refineries", refineries);
				AiMatchLogWriter.AppendString(b, "queue", queue);
			});
		}

		void IBotBuildingPickObserver.RefTierVerdict(OpenRA.Player owner, int tick, string verdict, string detail, int target, int refineries, string queue)
		{
			if (writer == null || ended)
				return;

			CoverageSeat cov;
			try
			{
				var seat = BotSeat(owner);
				cov = seat == null ? null : CoverageFor(seat);
			}
			catch (Exception e)
			{
				Fail(e, "ref_tier");
				return;
			}

			// One row per CHANGED (verdict, detail, queue), never repeated: a map with no claimable field writes its below_no_anchor row once.
			var key = verdict + "|" + detail + "@" + queue;
			if (cov == null || cov.LastRefTierKey == key)
				return;

			cov.LastRefTierKey = key;
			ObserverRow(owner, "ref_tier", b =>
			{
				AiMatchLogWriter.AppendString(b, "verdict", verdict);
				AiMatchLogWriter.AppendString(b, "detail", detail);
				AiMatchLogWriter.AppendNumber(b, "target", target);
				AiMatchLogWriter.AppendNumber(b, "refineries", refineries);
				AiMatchLogWriter.AppendString(b, "queue", queue);
			});
		}

		static string DemandCell(CPos c) => c.X.ToString(CultureInfo.InvariantCulture) + "," + c.Y.ToString(CultureInfo.InvariantCulture);

		// On a changed signature or after RefineryDemandRepeatTicks only.
		bool DemandRowDue(OpenRA.Player owner, int zone, string kind, string signature, int tick)
		{
			var key = (owner, zone, kind);
			if (demandRows.TryGetValue(key, out var last) && last.Signature == signature && tick - last.Tick < RefineryDemandRepeatTicks)
				return false;

			demandRows[key] = (signature, tick);
			return true;
		}

		void IBotRefineryDemandObserver.RefineryZonesBuilt(OpenRA.Player owner, int tick, int zones, int fields, int points, int radius)
		{
			ObserverRow(owner, "rd_zones", b =>
			{
				AiMatchLogWriter.AppendNumber(b, "zones", zones);
				AiMatchLogWriter.AppendNumber(b, "fields", fields);
				AiMatchLogWriter.AppendNumber(b, "points", points);
				AiMatchLogWriter.AppendNumber(b, "radius", radius);
			});
		}

		void IBotRefineryDemandObserver.RefineryZoneVerdict(OpenRA.Player owner, int tick, int zone, CPos center, int radius, string reason, int refineries,
			int demand, int capRemaining, int yardDistance)
		{
			if (!DemandRowDue(owner, zone, "rd_zone", reason + "|" + refineries + "|" + demand + "|" + capRemaining, tick))
				return;

			ObserverRow(owner, "rd_zone", b =>
			{
				AiMatchLogWriter.AppendNumber(b, "zone", zone);
				AiMatchLogWriter.AppendString(b, "center", DemandCell(center));
				AiMatchLogWriter.AppendNumber(b, "radius", radius);
				AiMatchLogWriter.AppendString(b, "reason", reason);
				AiMatchLogWriter.AppendNumber(b, "refineries", refineries);
				AiMatchLogWriter.AppendNumber(b, "demand", demand);
				AiMatchLogWriter.AppendNumber(b, "cap_remaining", capRemaining);
				AiMatchLogWriter.AppendNumber(b, "yard_dist", yardDistance);
			});
		}

		void IBotRefineryDemandObserver.RefineryDemandSummary(OpenRA.Player owner, int tick, int zones, int zonesInDemand, int demand, int capRemaining, int ownRefineries,
			int friendlyRefineries, int outOfRadius, int unreachable, int capped, int alliedServed)
		{
			var signature = string.Join("|", zonesInDemand, demand, capRemaining, ownRefineries, friendlyRefineries, outOfRadius, unreachable, capped, alliedServed);
			if (!DemandRowDue(owner, -1, "rd_demand", signature, tick))
				return;

			ObserverRow(owner, "rd_demand", b =>
			{
				AiMatchLogWriter.AppendNumber(b, "zones", zones);
				AiMatchLogWriter.AppendNumber(b, "zones_in_demand", zonesInDemand);
				AiMatchLogWriter.AppendNumber(b, "demand", demand);
				AiMatchLogWriter.AppendNumber(b, "cap_remaining", capRemaining);
				AiMatchLogWriter.AppendNumber(b, "own_refineries", ownRefineries);
				AiMatchLogWriter.AppendNumber(b, "friendly_refineries", friendlyRefineries);
				AiMatchLogWriter.AppendNumber(b, "out_of_radius", outOfRadius);
				AiMatchLogWriter.AppendNumber(b, "unreachable", unreachable);
				AiMatchLogWriter.AppendNumber(b, "zone_cap", capped);
				AiMatchLogWriter.AppendNumber(b, "allied_served", alliedServed);
			});
		}

		void IBotRefineryDemandObserver.RefineryAnchorParked(OpenRA.Player owner, int tick, int zone, CPos center, string reason, int untilTick)
		{
			if (!DemandRowDue(owner, zone, "rd_anchor_parked", reason, tick))
				return;

			ObserverRow(owner, "rd_anchor_parked", b =>
			{
				AiMatchLogWriter.AppendNumber(b, "zone", zone);
				AiMatchLogWriter.AppendString(b, "center", DemandCell(center));
				AiMatchLogWriter.AppendString(b, "reason", reason);
				AiMatchLogWriter.AppendNumber(b, "until", untilTick);
			});
		}

		void IBotRefineryDemandObserver.RefineryAnchorPending(OpenRA.Player owner, int tick, int zone, CPos center, int untilTick)
		{
			if (!DemandRowDue(owner, zone, "rd_anchor_pending", "pending", tick))
				return;

			ObserverRow(owner, "rd_anchor_pending", b =>
			{
				AiMatchLogWriter.AppendNumber(b, "zone", zone);
				AiMatchLogWriter.AppendString(b, "center", DemandCell(center));
				AiMatchLogWriter.AppendNumber(b, "until", untilTick);
			});
		}

		void IBotRefineryDemandObserver.RefineryPlaced(OpenRA.Player owner, int tick, int zone, CPos center, CPos site, bool fallbackSpacing, int nearestFriendly)
		{
			ObserverRow(owner, "rd_refinery_placed", b =>
			{
				AiMatchLogWriter.AppendString(b, "cell", DemandCell(site));
				AiMatchLogWriter.AppendNumber(b, "zone", zone);
				AiMatchLogWriter.AppendString(b, "center", DemandCell(center));
				AiMatchLogWriter.AppendNumber(b, "fallback_spacing", fallbackSpacing ? 1 : 0);
				AiMatchLogWriter.AppendNumber(b, "nearest_friendly", nearestFriendly);
			});
		}

		void ObserverRow(OpenRA.Player owner, string kind, Action<System.Text.StringBuilder> fields)
		{
			if (writer == null || ended)
				return;

			try
			{
				var seat = BotSeat(owner);
				if (seat == null)
					return;

				var b = BeginEvent(seat, kind, 1, out _);
				fields(b);
				b.Append('}');
				writer.TryWrite(b.ToString());
			}
			catch (Exception e)
			{
				Fail(e, kind);
			}
		}
	}
}
