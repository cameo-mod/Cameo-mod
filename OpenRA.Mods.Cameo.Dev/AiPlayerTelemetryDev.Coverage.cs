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
	// Record-only: nothing here writes synced state, orders or random draws. A fault ends the channel through Fail(), never the game.
	public sealed partial class AiPlayerTelemetryDev : IBotBuildingPickObserver
	{
		const int CoveragePollTicks = 50;
		const int RefineryHeldRepeatTicks = 750;

		sealed class CoverageSeat
		{
			public HashSet<string> Buildings;
			public HashSet<string> Units;
			public readonly HashSet<string> Available = new(StringComparer.Ordinal);
			public string LastRefineryGate;
			public int LastRefineryGateTick = -1;
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

			var buildingWeights = MergeWeights(bases.Select(b => b.Info.BuildingFractions));
			var unitWeights = MergeWeights(units.Select(u => u.Info.UnitsToBuild));
			cov = new CoverageSeat
			{
				Buildings = new HashSet<string>(buildingWeights.Keys, StringComparer.Ordinal),
				Units = new HashSet<string>(unitWeights.Keys, StringComparer.Ordinal),
			};
			coverage[seat.Player] = cov;

			EmitListedSet(seat, "bb_listed_set", buildingWeights);
			EmitListedSet(seat, "ub_listed_set", unitWeights);
			return cov;
		}

		// The largest weight per name over the enabled modules (one generic module per bot in practice).
		static SortedDictionary<string, int> MergeWeights(IEnumerable<Dictionary<string, int>> tables)
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

				foreach (var item in queue.BuildableItems())
				{
					var name = item.Name;
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

		void IBotBuildingPickObserver.RefineryHeld(OpenRA.Player owner, int tick, string gate, int refineries)
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

			if (cov == null || (cov.LastRefineryGate == gate && cov.LastRefineryGateTick >= 0 && tick - cov.LastRefineryGateTick < RefineryHeldRepeatTicks))
				return;

			cov.LastRefineryGate = gate;
			cov.LastRefineryGateTick = tick;
			ObserverRow(owner, "bb_refinery_held", b =>
			{
				AiMatchLogWriter.AppendString(b, "gate", gate);
				AiMatchLogWriter.AppendNumber(b, "refineries", refineries);
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
