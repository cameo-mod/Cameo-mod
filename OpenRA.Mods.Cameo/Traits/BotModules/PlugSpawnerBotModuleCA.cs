#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Queues plugs for pluggable buildings through normal paid production.",
		"Each entry maps one plug actor to the host actor names it may be built for.",
		"One module covers every plug kind — the upstream AS module needed one variant per plug.")]
	public class PlugSpawnerBotModuleCAInfo : ConditionalTraitInfo
	{
		[Desc("Plug actor name -> comma-separated host actor names the AI may build it for.")]
		public readonly Dictionary<string, string> Plugs = new();

		[Desc("Plug production interval per plug type.")]
		public readonly int Interval = 50;

		[Desc("UPGRADE-QUEUE-02 (add-ons never idle): the add-on service. It is the BuildingAddons producer; the upgrade queue service does not serve it.",
			"False = the module starts no plugs (ablation row pq_addon_service).")]
		public readonly bool AddonService = true;

		public override object Create(ActorInitializer init) { return new PlugSpawnerBotModuleCA(init.Self, this); }
	}

	public class PlugSpawnerBotModuleCA : ConditionalTrait<PlugSpawnerBotModuleCAInfo>, IBotTick, INotifyCreated
	{
		readonly World world;
		TechTree techTree;
		AblationLatch ablateAddonService; // UPGRADE-QUEUE-02 ABLATE hook (DEV-only provider; absent in release = always on)

		// plugActor -> (plug type, host name set, prerequisites, actor info)
		readonly Dictionary<string, (string Type, HashSet<string> Hosts, string[] Prerequisites, ActorInfo Info)> plugs = new();
		// UPGRADE-QUEUE-02: no period. The add-on producer checks on every bot visit, so a finished or newly
		// available add-on is replaced at once (user rule: no delay). Info.Interval is kept for config compatibility.

		public PlugSpawnerBotModuleCA(Actor self, PlugSpawnerBotModuleCAInfo info)
			: base(info)
		{
			world = self.World;
		}

		protected override void Created(Actor self)
		{
			techTree = self.Owner.PlayerActor.TraitOrDefault<TechTree>();

			foreach (var kv in Info.Plugs)
			{
				if (!world.Map.Rules.Actors.TryGetValue(kv.Key, out var actorInfo))
					continue;

				var plugInfo = actorInfo.TraitInfoOrDefault<PlugInfo>();
				if (plugInfo == null)
					continue;

				var hosts = kv.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
					.ToHashSet();
				var prereqs = actorInfo.TraitInfos<BuildableInfo>()
					.SelectMany(b => b.Prerequisites)
					.Distinct()
					.ToArray();

				plugs[kv.Key] = (plugInfo.Type, hosts, prereqs, actorInfo);
			}

			base.Created(self);
		}

		bool PrerequisitesMet(string[] prereqs)
		{
			return prereqs.Length == 0 || techTree == null || techTree.HasPrerequisites(prereqs);
		}

		void IBotTick.BotTick(IBot bot)
		{
			var player = bot.Player;
			if (!Info.AddonService || ablateAddonService.Off(player, AblationIds.AddonService))
				return;

			// AR-4 (fleet orders 2026-10-04b): the owned-host scan is lazy and reads only Pluggable actors, and runs
			// only for a real candidate (not pending, prerequisites met, queue present), so an idle check stays cheap.
			List<Actor> owned = null;
			var queues = player.PlayerActor.TraitsImplementing<ProductionQueue>(); // UPGRADE-QUEUE-02 (Sonnet LOW): lazy, no per-visit array

			foreach (var (plugActor, plug) in plugs)
			{
				// Never queue a plug the tech tree still gates off — plugs that bypass
				// Buildable prerequisites would skip the faction's tech path entirely.
				if (!PrerequisitesMet(plug.Prerequisites))
					continue;

				// Bots buy plugs like players: ordinary StartProduction into a real queue,
				// paid over the plug's build duration, then completed-item PlacePlug via
				// the base builder's Done branch. Demand only fires while an owned host
				// still accepts the plug type, and never duplicates a pending item —
				// ordinary plugs may oversubscribe by design, but a second identical
				// order before the first finishes is pure waste. Superweapon plugs are
				// additionally admission-gated owner-wide by SuperweaponPlugLimit, so a
				// rejected order here is correct behaviour, not an error.
				if (ItemPending(queues, plugActor))
					continue;

				owned ??= CollectOwnedActors(world.ActorsHavingTrait<Pluggable>(), player);
				var hasEligibleHost = owned
					.Where(x => plug.Hosts.Contains(x.Info.Name))
					.Any(x => x.TraitsImplementing<Pluggable>().Any(p => p.AcceptsPlug(plug.Type)));

				if (!hasEligibleHost)
					continue;

				// Plugs without a production queue (e.g. the dormant droppod uplink, whose
				// Queue is deliberately unset) resolve no producer and stay unreachable —
				// migrating this module must not silently activate them.
				var queue = QueueForPlug(queues, plug.Info);
				if (queue == null)
					continue;

				bot.QueueOrder(Order.StartProduction(queue.Actor, plugActor, 1));
			}
		}

		// The first owner queue that can currently produce the plug, else null. Pure seam
		// so queue selection is unit-testable without a World.
		internal static ProductionQueue QueueForPlug(IEnumerable<ProductionQueue> queues, ActorInfo actorInfo)
		{
			return queues.FirstOrDefault(q => q.CanBuild(actorInfo));
		}

		// True while any of the owner's queues already holds the plug item (in progress or
		// Done-held). Pure seam so the pending dedup is unit-testable without a World.
		internal static bool ItemPending(IEnumerable<ProductionQueue> queues, string itemName)
		{
			return queues.Any(q => q.AllQueued().Any(i => i.Item == itemName));
		}

		// AR-4: the interval scan collects the bot's live own actors once for all plug kinds.
		internal static List<Actor> CollectOwnedActors(IEnumerable<Actor> actors, OpenRA.Player owner)
		{
			var owned = new List<Actor>();
			foreach (var actor in actors)
				if (actor.IsInWorld && !actor.IsDead && actor.Owner == owner)
					owned.Add(actor);
			return owned;
		}

	}
}
