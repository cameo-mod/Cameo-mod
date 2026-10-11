using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace OpenRA.Mods.Cameo.Traits
{
	internal readonly record struct McvHealthObservation(uint ActorId, string Type, string Role,
		string TransformTarget, int X, int Y, bool Live, bool InWorld, string Activity, string LeaseOwner);
	internal readonly record struct McvHealthSnapshot(McvHealthObservation Facts, int? IdleSinceTick, int LastCellChangeTick);

	internal sealed class McvHealthState
	{
		internal const int MaximumActors = 64;
		readonly Dictionary<uint, McvHealthSnapshot> tracked = [];
		internal bool Complete { get; private set; } = true;
		int lastTick = -1;
		internal IReadOnlyCollection<McvHealthSnapshot> Snapshots => tracked.Values;

		internal bool Observe(int tick, IReadOnlyList<McvHealthObservation> observations)
		{
			if (tick < 0 || (lastTick < 0 ? tick != 0 : tick != lastTick && tick != lastTick + 1)
				|| observations.Count > MaximumActors || observations.Select(o => o.ActorId).Distinct().Count() != observations.Count)
			{
				Complete = false;
				lastTick = System.Math.Max(lastTick, tick);
				var hadRows = tracked.Count > 0;
				tracked.Clear();
				return hadRows;
			}
			lastTick = tick;
			var next = new Dictionary<uint, McvHealthSnapshot>(observations.Count);
			var changed = observations.Count != tracked.Count;
			foreach (var o in observations)
			{
				if (o.ActorId == 0 || o.Role is not ("construction_mcv" or "field_refinery_vehicle" or "base_building_vehicle")
					|| o.Activity is not ("idle" or "busy" or "mobile" or "deploying"))
					Complete = false;
				var known = tracked.TryGetValue(o.ActorId, out var old) && old.Facts.Type == o.Type;
				var moved = known && (old.Facts.X != o.X || old.Facts.Y != o.Y);
				int? idleSince = o.Live && o.InWorld && o.Activity == "idle"
					? known && !moved && old.Facts.Activity == "idle" ? old.IdleSinceTick : tick : null;
				var row = new McvHealthSnapshot(o, idleSince, !known || moved ? tick : old.LastCellChangeTick);
				next.Add(o.ActorId, row);
				changed |= !known || old != row;
			}
			tracked.Clear();
			foreach (var row in next)
				tracked.Add(row.Key, row.Value);
			return changed;
		}

		internal static string ResolveRole(bool configured, bool construction, bool targetKnown,
			bool targetYard, bool targetRefinery, bool targetBuilding)
		{
			if (!configured || !targetKnown || !targetBuilding || (construction && !targetYard))
				return "unknown";
			if (targetYard && targetRefinery)
				return "unknown";
			return targetYard ? "construction_mcv" : targetRefinery ? "field_refinery_vehicle" : "base_building_vehicle";
		}
	}

	internal static class McvHealthSchema
	{
		internal static string Row(string gameUid, string player, int seq, int tick, string kind,
			bool active, bool supported, bool complete, IEnumerable<McvHealthSnapshot> snapshots,
			IReadOnlyDictionary<string, string> roleMap) => JsonSerializer.Serialize(new
			{
				schema = "cameo-mcv-health", schema_version = 1, game_uid = gameUid, player,
				seq, world_tick = tick, kind, player_active = active, supported,
				observation_complete = complete, role_map = roleMap, dropped = complete ? 0 : 1,
				// Missing outcome/hold hooks are explicit; no inferred healthy deployment coverage.
				intent_complete = false, order_complete = false, hold_complete = false, transform_complete = false,
				complete = false,
				actors = snapshots.OrderBy(s => s.Facts.ActorId).Select(s => new
				{
					actor_id = s.Facts.ActorId, actor_type = s.Facts.Type, role = s.Facts.Role,
					transform_target = s.Facts.TransformTarget, role_source = "configured_mcv_types+loaded_ruleset",
					x = s.Facts.X, y = s.Facts.Y, live = s.Facts.Live, in_world = s.Facts.InWorld,
					activity = s.Facts.Activity, idle_since_tick = s.IdleSinceTick,
					last_cell_change_tick = s.LastCellChangeTick, observation_tick = tick,
					hold = "unknown", lease_owner = s.Facts.LeaseOwner,
					order_requested = (bool?)null, order_accepted = (bool?)null,
					site_intent = (bool?)null, proven_transform_actor_id = (uint?)null
				})
			});
	}
}
