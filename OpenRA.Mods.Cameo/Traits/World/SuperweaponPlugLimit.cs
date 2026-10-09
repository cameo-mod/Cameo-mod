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
	public class SuperweaponPlugLimitInfo : TraitInfo<SuperweaponPlugLimit> { }

	public class SuperweaponPlugLimit : ITick, IValidateOrder
	{
		// SW plug item name ("td_gdi_ioncannonuplink") -> occupancy prerequisite
		// token ("ionc") provided while its host holds the install condition.
		Dictionary<string, string> swItemToken;

		// dedup: one frame-end sweep is queued per owner per drain; late same-frame
		// installs still resolve because the sweep re-evaluates live counts at run time.
		bool sweepQueued;

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
			swItemToken ??= BuildPlugTokenMap(world.Map.Rules.Actors.Values);
		}

		// Derived from the rules wiring, not hardcoded: a host Pluggable accepts plug
		// type T and grants condition C for it; the same actor's ProvidesPrerequisite
		// gated on C yields the token. An item is SW-capped iff it has Plug.Type == T
		// and its own Buildable prerequisites contain !token (installed-only gating).
		internal static Dictionary<string, string> BuildPlugTokenMap(IEnumerable<ActorInfo> actors)
		{
			// plug type -> candidate occupancy tokens (a condition may feed several
			// ProvidesPrerequisite traits, e.g. cabalnuke and cabalnuke_swlimit)
			var typeToTokens = new Dictionary<string, List<string>>();
			foreach (var ai in actors)
			{
				var providedByCondition = new Dictionary<string, List<string>>();
				foreach (var p in ai.TraitInfos<ProvidesPrerequisiteInfo>())
				{
					// Only single-variable gates identify a plug-granted condition
					// (e.g. RequiresCondition: nuke); multi-variable expressions are
					// skipped so an unrelated gating variable can't be misattributed.
					if (p.RequiresCondition == null)
						continue;

					var variables = p.RequiresCondition.Variables.ToArray();
					if (variables.Length != 1)
						continue;

					var condition = variables[0];
					if (!providedByCondition.TryGetValue(condition, out var tokens))
						providedByCondition[condition] = tokens = new List<string>();
					tokens.Add(p.Prerequisite ?? ai.Name);
				}

				if (providedByCondition.Count == 0)
					continue;

				foreach (var pluggable in ai.TraitInfos<PluggableInfo>())
					foreach (var cond in pluggable.Conditions)
						if (providedByCondition.TryGetValue(cond.Value, out var tokens))
						{
							// Merge: several hosts may accept the same plug type; the
							// plug's own negated prerequisite disambiguates the token.
							if (!typeToTokens.TryGetValue(cond.Key, out var merged))
								typeToTokens[cond.Key] = merged = new List<string>();
							foreach (var t in tokens)
								if (!merged.Contains(t))
									merged.Add(t);
						}
			}

			var result = new Dictionary<string, string>();
			foreach (var ai in actors)
			{
				var plug = ai.TraitInfoOrDefault<PlugInfo>();
				var buildable = ai.TraitInfoOrDefault<BuildableInfo>();
				if (plug == null || buildable == null || !typeToTokens.TryGetValue(plug.Type, out var tokens))
					continue;

				// The occupancy token is the candidate the plug itself negates in
				// Buildable prerequisites (!token). A candidate provided by the same
				// install condition but not negated (e.g. cabalnuke) is unrelated.
				foreach (var p in buildable.Prerequisites)
				{
					var normalized = p.Replace("~", "");
					foreach (var token in tokens)
						if (normalized == "!" + token)
						{
							result[ai.Name] = token;
							break;
						}

					if (result.ContainsKey(ai.Name))
						break;
				}
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

		static void Cancel(OpenRA.Player owner, ProductionQueue queue, ProductionItem item)
		{
			// Replicates ProductionQueue.CancelProductionInner's refund path:
			// clear Infinite first so EndProduction does not re-add a replacement,
			// refund resources and paid cash, then remove the item.
			item.Infinite = false;

			var playerResources = owner.PlayerActor.TraitOrDefault<PlayerResources>();
			if (item.ResourcesPaid > 0)
				playerResources?.GiveResources(item.ResourcesPaid);

			var payment = item.TotalCost - item.RemainingCost;
			if (payment > 0)
				playerResources?.GiveCash(payment);

			queue.EndProduction(item);
		}
	}
}
