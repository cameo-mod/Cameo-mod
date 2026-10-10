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
		// token the wiring chain is verified end to end on a single host — a
		// ProvidesPrerequisite producing the token must carry
		// `RequiresPrerequisites: global-swlimit` and a positive single-variable
		// RequiresCondition (a bare install condition such as `ionc`; `!x`,
		// `x && y` or `(x)` cannot be install gates); a Pluggable socket ON THE
		// SAME host actor must grant that condition for some plug type; and a
		// plug actor of that type must negate !token in Buildable.Prerequisites.
		// Provider/socket pairs on different actors never join: installing the
		// other host's plug would not publish this token. Missing or ambiguous
		// links emit diagnostics (debug channel) and leave the plug uncapped —
		// an item negating multiple resolving tokens is rejected, never
		// first-wins. Behavioural polarity of the real gates is
		// regression-covered at samples 0/1/2/int.MaxValue — no universal
		// expression-validation claim.
		internal static Dictionary<string, string> BuildPlugTokenMap(
			IEnumerable<ActorInfo> actors, IEnumerable<string> occupancyTokens, List<string> diagnostics = null)
		{
			var result = new Dictionary<string, string>();
			var declared = new HashSet<string>(occupancyTokens ?? [], StringComparer.Ordinal);
			if (declared.Count == 0)
				return result;

			var all = actors.ToArray();

			// Pass 1: verify each declared token's provider chain and pair the
			// install condition with a plug type ON THE SAME HOST. Joining
			// providers and sockets globally would fabricate wiring: a provider
			// on host A must never pair with a socket on host B — installing
			// that socket's plug would not publish the token.
			var tuples = new List<(string Token, string Type)>();
			var sawProvider = new HashSet<string>(StringComparer.Ordinal);
			var qualified = new HashSet<string>(StringComparer.Ordinal);
			foreach (var host in all)
			{
				var hostProviders = new List<(string Token, string Condition)>();
				foreach (var p in host.TraitInfos<ProvidesPrerequisiteInfo>())
				{
					var token = p.Prerequisite ?? host.Name;
					if (!declared.Contains(token))
						continue;

					sawProvider.Add(token);
					var variables = p.RequiresCondition?.Variables.ToArray();
					if (variables == null || variables.Length != 1
						|| p.RequiresCondition.Expression.Trim() != variables[0])
					{
						diagnostics?.Add($"swcap: '{token}' provider '{host.Name}' RequiresCondition " +
							$"'{p.RequiresCondition?.Expression ?? "<none>"}' is not a positive single-variable gate — skipped");
						continue;
					}

					if (!p.RequiresPrerequisites.Contains("global-swlimit"))
					{
						diagnostics?.Add($"swcap: '{token}' provider '{host.Name}' lacks RequiresPrerequisites: global-swlimit — skipped");
						continue;
					}

					// Echo the resolved gate for review.
					diagnostics?.Add($"swcap: '{token}' <- '{host.Name}' gated on '{p.RequiresCondition.Expression}' + global-swlimit");
					qualified.Add(token);
					hostProviders.Add((token, variables[0]));
				}

				foreach (var hp in hostProviders)
				{
					var matched = false;
					foreach (var pluggable in host.TraitInfos<PluggableInfo>())
						foreach (var cond in pluggable.Conditions)
							if (cond.Value == hp.Condition)
							{
								tuples.Add((hp.Token, cond.Key));
								matched = true;
							}

					if (!matched)
						diagnostics?.Add($"swcap: '{hp.Token}' provider on '{host.Name}' has no same-host Pluggable socket granting '{hp.Condition}' — uncapped");
				}
			}

			foreach (var token in declared.OrderBy(t => t, StringComparer.Ordinal))
			{
				if (!sawProvider.Contains(token))
					diagnostics?.Add($"swcap: declared token '{token}' has no ProvidesPrerequisite provider — dangling declaration");
				if (!qualified.Contains(token))
					diagnostics?.Add($"swcap: declared token '{token}' has no qualifying lobby-cap provider — nothing capped");
			}

			// Pass 2: plug items. A candidate token must both be negated by the
			// item (!token / ~!token in Buildable.Prerequisites) and resolve to
			// the item's own plug type via a verified same-host pair. Negating
			// more than one resolving token is ambiguous — the item is rejected
			// outright rather than taking whichever token sorts first.
			var resolvedPerToken = new Dictionary<string, int>(StringComparer.Ordinal);
			foreach (var ai in all)
			{
				var plug = ai.TraitInfoOrDefault<PlugInfo>();
				var buildable = ai.TraitInfoOrDefault<BuildableInfo>();
				if (plug == null || buildable == null)
					continue;

				var negated = buildable.Prerequisites
					.Select(NegatedToken)
					.Where(t => t != null && declared.Contains(t))
					.Distinct(StringComparer.Ordinal)
					.ToArray();
				if (negated.Length == 0)
					continue;

				var candidates = negated
					.Where(t => tuples.Any(tp => tp.Token == t && tp.Type == plug.Type))
					.ToArray();

				if (candidates.Length == 0)
				{
					diagnostics?.Add($"swcap: plug '{ai.Name}' negates declared token(s) '{string.Join("', '", negated)}' " +
						$"with no verified provider+socket chain for plug type '{plug.Type}' — uncapped");
					continue;
				}

				if (candidates.Length > 1)
				{
					diagnostics?.Add($"swcap: plug '{ai.Name}' negates multiple declared tokens " +
						$"({string.Join(", ", candidates)}) resolving for plug type '{plug.Type}' — ambiguous, uncapped");
					continue;
				}

				result[ai.Name] = candidates[0];
				resolvedPerToken[candidates[0]] = resolvedPerToken.GetValueOrDefault(candidates[0]) + 1;
			}

			foreach (var token in declared.OrderBy(t => t, StringComparer.Ordinal))
			{
				var resolved = resolvedPerToken.GetValueOrDefault(token);
				if (qualified.Contains(token) && resolved == 0)
					diagnostics?.Add($"swcap: declared token '{token}' resolves no plug actor");
				else if (resolved > 1)
					diagnostics?.Add($"swcap: declared token '{token}' resolves {resolved} plug actors — shared-slot cap");
			}

			return result;
		}

		// "!token" / "~!token" -> "token"; anything else -> null.
		static string NegatedToken(string prerequisite)
		{
			var p = prerequisite.Trim().TrimStart('~');
			return p.StartsWith("!", StringComparison.Ordinal) ? p.Substring(1) : null;
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
