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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Superweapon plug capacity for the Limited superweapon ruleset. Two synced layers: " +
		"an IValidateOrder admission gate (pending + installed + batch <= cap, evaluated " +
		"owner-wide across all production queues), and a per-tick frame-end reconciliation " +
		"that refunds over-cap pending SW plug items. The gate is inert unless the player " +
		"holds the `global-swlimit` lobby prerequisite, so Unlimited mode is unaffected. " +
		"Reconciliation covers infinite-replenishment (EndProduction re-adds the queued plug " +
		"without calling StartProduction), captures, and ordinary items migrating INTO an " +
		"SW plug via GetReplacement. Inbound migration is detection-only: a migrated item " +
		"can tick at most once before frame-end removal refunds all paid cash/resources.")]
	public class SuperweaponPlugLimitInfo : TraitInfo
	{
		[Desc("Declared occupancy prerequisite tokens that capacity-manage superweapon plugs " +
			"(e.g. ionc). A plug item is capped only when it negates a token declared here " +
			"whose provider wiring verifies as lobby-cap wiring; an empty list leaves every " +
			"plug uncapped.")]
		public readonly string[] OccupancyTokens = [];

		public override object Create(ActorInitializer init) => new SuperweaponPlugLimit(this);
	}

	public class SuperweaponPlugLimit : ITick, IValidateOrder
	{
		readonly SuperweaponPlugLimitInfo info;

		// SW plug item name ("td_gdi_ioncannonuplink") -> declared occupancy
		// prerequisite token ("ionc") provided while its host holds the install
		// condition.
		Dictionary<string, string> swItemToken;

		// dedup: one frame-end sweep is queued per owner per drain; late same-frame
		// installs still resolve because the sweep re-evaluates live counts at run time.
		bool sweepQueued;

		public SuperweaponPlugLimit(SuperweaponPlugLimitInfo info)
		{
			this.info = info;
		}

		public bool OrderValidation(OrderManager orderManager, World world, int clientId, Order order)
		{
			if (order.OrderString != "StartProduction" || order.Subject == null)
				return true;

			var owner = order.Subject.Owner;
			if (owner == null)
				return true;

			EnsureMaps(world);
			var token = TokenForSwItem(order.TargetString);
			if (token == null)
				return true;

			var techTree = owner.PlayerActor.TraitOrDefault<TechTree>();
			if (techTree == null || !techTree.HasPrerequisites(new[] { "global-swlimit" }))
				return true;

			// Pending is counted per occupancy token, not per item name: distinct
			// plug actors negating the same !token share one capacity slot.
			var pending = PendingSwItems(world, owner)
				.Count(kv => TokenForSwItem(kv.Value.Item) == token);
			var installed = techTree.HasPrerequisites(new[] { token });

			return Admit(true, pending, installed, order.ExtraData);
		}

		// Pure admission rule: unlimited mode never gates; limited mode admits only
		// when pending + installed + requested stay within the per-type cap of 1.
		internal static bool Admit(bool limited, int pending, bool installed, long requested)
		{
			return !limited || pending + (installed ? 1 : 0) + requested <= 1;
		}

		// Pending items beyond the per-type cap that reconciliation must remove.
		internal static int ExcessCount(int pending, bool installed)
		{
			return Math.Max(0, pending + (installed ? 1 : 0) - 1);
		}

		public void Tick(Actor self)
		{
			// One deduplicated frame-end sweep per tick. The task runs inside
			// World.Tick's frame-end drain, after all queue ticks and after any
			// PlaceBuilding install callback that ran this frame (EnablePlug and
			// EndProduction replenish both complete before appended tasks run),
			// and before the next tick's order phase and CancelUnbuildableItems.
			if (sweepQueued)
				return;

			sweepQueued = true;
			var world = self.World;
			world.AddFrameEndTask(w =>
			{
				sweepQueued = false;
				Reconcile(w);
			});
		}

		void EnsureMaps(World world)
		{
			if (swItemToken != null)
				return;

			var diagnostics = new List<string>();
			swItemToken = BuildPlugTokenMap(world.Map.Rules.Actors.Values, info.OccupancyTokens, diagnostics);
			foreach (var diagnostic in diagnostics)
				Log.Write("debug", diagnostic);
		}

		// Capacity wiring is declared, not inferred: only tokens in
		// SuperweaponPlugLimitInfo.OccupancyTokens can cap a plug. For each declared
		// token the wiring chain is verified end to end — a ProvidesPrerequisite
		// producing the token must carry `RequiresPrerequisites: global-swlimit` and
		// a positive single-variable RequiresCondition (a bare install condition
		// such as `ionc`; `!x`, `x && y` or `(x)` cannot be install gates); a host
		// Pluggable must grant that condition for some plug type; and a plug actor
		// of that type must negate !token in Buildable.Prerequisites. Missing or
		// ambiguous links emit diagnostics (debug channel) and leave the plug
		// uncapped rather than silently mis-deriving wiring. Behavioural polarity
		// of the real gates is regression-covered at samples 0/1/2/int.MaxValue —
		// no universal expression-validation claim.
		internal static Dictionary<string, string> BuildPlugTokenMap(
			IEnumerable<ActorInfo> actors, IEnumerable<string> occupancyTokens, List<string> diagnostics = null)
		{
			var result = new Dictionary<string, string>();
			var declared = occupancyTokens.OrderBy(t => t, StringComparer.Ordinal).ToArray();
			if (declared.Length == 0)
				return result;

			var all = actors.ToArray();
			foreach (var token in declared)
			{
				var conditions = new List<string>();
				var sawProvider = false;
				foreach (var ai in all)
				{
					foreach (var p in ai.TraitInfos<ProvidesPrerequisiteInfo>())
					{
						if ((p.Prerequisite ?? ai.Name) != token)
							continue;

						sawProvider = true;
						var variables = p.RequiresCondition?.Variables.ToArray();
						var positiveGate = variables != null && variables.Length == 1
							&& p.RequiresCondition.Expression.Trim() == variables[0];
						if (!positiveGate)
						{
							diagnostics?.Add($"swcap: '{token}' provider '{ai.Name}' RequiresCondition " +
								$"'{p.RequiresCondition?.Expression ?? "<none>"}' is not a positive single-variable gate — skipped");
							continue;
						}

						if (!p.RequiresPrerequisites.Contains("global-swlimit"))
						{
							diagnostics?.Add($"swcap: '{token}' provider '{ai.Name}' lacks RequiresPrerequisites: global-swlimit — skipped");
							continue;
						}

						// Echo the resolved gate for review.
						diagnostics?.Add($"swcap: '{token}' <- '{ai.Name}' gated on '{p.RequiresCondition.Expression}' + global-swlimit");
						if (!conditions.Contains(variables[0]))
							conditions.Add(variables[0]);
					}
				}

				if (!sawProvider)
					diagnostics?.Add($"swcap: declared token '{token}' has no ProvidesPrerequisite provider — dangling declaration");

				if (conditions.Count == 0)
				{
					diagnostics?.Add($"swcap: declared token '{token}' has no qualifying lobby-cap provider — nothing capped");
					continue;
				}

				if (conditions.Count > 1)
					diagnostics?.Add($"swcap: declared token '{token}' resolves {conditions.Count} install conditions ({string.Join(", ", conditions)})");

				// Plug types = host Pluggable entries granting a resolved condition.
				var plugTypes = new List<string>();
				foreach (var ai in all)
					foreach (var pluggable in ai.TraitInfos<PluggableInfo>())
						foreach (var cond in pluggable.Conditions)
							if (conditions.Contains(cond.Value) && !plugTypes.Contains(cond.Key))
								plugTypes.Add(cond.Key);

				// The plug itself must negate the declared token in Buildable
				// prerequisites (!token, or hidden ~!token).
				var resolved = 0;
				foreach (var ai in all)
				{
					var plug = ai.TraitInfoOrDefault<PlugInfo>();
					var buildable = ai.TraitInfoOrDefault<BuildableInfo>();
					if (plug == null || buildable == null || !plugTypes.Contains(plug.Type))
						continue;

					if (!buildable.Prerequisites.Any(p => p.Replace("~", "") == "!" + token))
						continue;

					if (result.ContainsKey(ai.Name))
					{
						diagnostics?.Add($"swcap: plug '{ai.Name}' negates multiple declared tokens — first wins");
						continue;
					}

					result[ai.Name] = token;
					resolved++;
				}

				if (resolved == 0)
					diagnostics?.Add($"swcap: declared token '{token}' resolves no plug actor");
				else if (resolved > 1)
					diagnostics?.Add($"swcap: declared token '{token}' resolves {resolved} plug actors — shared-slot cap");
			}

			return result;
		}

		// itemName -> occupancy token for SW plug items, else null.
		string TokenForSwItem(string itemName)
		{
			return swItemToken.TryGetValue(itemName, out var token) ? token : null;
		}

		static IEnumerable<KeyValuePair<ProductionQueue, ProductionItem>> PendingSwItems(World world, OpenRA.Player owner)
		{
			// Deterministic order: queues sorted by producer actor ID, items tail-first later.
			foreach (var tp in world.ActorsWithTrait<ProductionQueue>().OrderBy(tp => tp.Actor.ActorID))
			{
				if (tp.Actor.Owner != owner)
					continue;

				foreach (var item in tp.Trait.AllQueued())
					yield return new KeyValuePair<ProductionQueue, ProductionItem>(tp.Trait, item);
			}
		}

		void Reconcile(World world)
		{
			EnsureMaps(world);
			foreach (var player in world.Players)
			{
				var techTree = player.PlayerActor.TraitOrDefault<TechTree>();
				if (techTree == null || !techTree.HasPrerequisites(new[] { "global-swlimit" }))
					continue;

				// Count pending per occupancy token (distinct plugs sharing a
				// token share one capacity slot).
				var pending = new Dictionary<string, List<(ProductionQueue Queue, ProductionItem Item)>>();
				foreach (var kv in PendingSwItems(world, player))
				{
					var token = TokenForSwItem(kv.Value.Item);
					if (token == null)
						continue;

					if (!pending.TryGetValue(token, out var list))
						pending[token] = list = new List<(ProductionQueue, ProductionItem)>();
					list.Add((kv.Key, kv.Value));
				}

				if (pending.Count == 0)
					continue;

				foreach (var kv in pending)
				{
					var excess = ExcessCount(kv.Value.Count, techTree.HasPrerequisites(new[] { kv.Key }));
					if (excess <= 0)
						continue;

					// Cancel canonical-latest excess: queues already iterated in
					// ActorID order, so later entries are latest; take tail-first.
					for (var i = kv.Value.Count - 1; i >= 0 && excess > 0; i--, excess--)
						Cancel(player, kv.Value[i].Queue, kv.Value[i].Item);
				}
			}
		}

		// Cash share of a cancelled item's refund, matching
		// ProductionQueue.CancelProductionInner: resources are refunded as
		// resources and do NOT also come back as cash. The engine achieves the
		// split by bumping RemainingCost += ResourcesPaid before GiveCash; the
		// arithmetic here is identical (the item is removed right after, so
		// mutating RemainingCost would be dead work).
		internal static int RefundCash(int totalCost, int remainingCost, int resourcesPaid)
		{
			return totalCost - remainingCost - resourcesPaid;
		}

		static void Cancel(OpenRA.Player owner, ProductionQueue queue, ProductionItem item)
		{
			// Replicates ProductionQueue.CancelProductionInner's refund path:
			// clear Infinite first so EndProduction does not re-add a replacement,
			// refund resources and paid cash, then remove the item.
			item.Infinite = false;

			var playerResources = owner.PlayerActor.TraitOrDefault<PlayerResources>();
			if (playerResources != null)
			{
				if (item.ResourcesPaid > 0)
					playerResources.GiveResources(item.ResourcesPaid);

				playerResources.GiveCash(RefundCash(item.TotalCost, item.RemainingCost, item.ResourcesPaid));
			}

			queue.EndProduction(item);
		}
	}
}
