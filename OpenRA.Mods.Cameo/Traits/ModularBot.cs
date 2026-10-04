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
using System.Diagnostics;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	// Shadows OpenRA.Mods.Common.Traits.ModularBotInfo/ModularBot (assembly order puts
	// Cameo before Common). Identical bot behaviour, plus per-module tick timing so the
	// cost of each bot module is measured in the AI logs instead of guessed.
	[Desc("Bot that uses BotModules. Times each module per tick and reports the totals.")]
	[TraitLocation(SystemActors.Player)]
	public sealed class ModularBotInfo : TraitInfo, IBotInfo
	{
		[FieldLoader.Require]
		[Desc("Internal id for this bot.")]
		public readonly string Type = null;

		[FluentReference]
		[Desc("Human-readable name this bot uses.")]
		public readonly string Name = null;

		[Desc("Minimum portion of pending orders to issue each tick (e.g. 5 issues at least 1/5th of all pending orders). " +
			"Excess orders remain queued for subsequent ticks.")]
		public readonly int MinOrderQuotientPerTick = 5;

		[Desc("Cameo-only: report per-module bot tick timings to the bot debug log every N ticks. 0 disables.")]
		public readonly int ModulePerfReportIntervalTicks = 1500;

		[Desc("Cameo-only: how many of the slowest modules to list in each timing report.")]
		public readonly int ModulePerfReportTop = 8;

		[Desc("Cap on queued orders. Once full, the oldest deferred order is dropped —",
			"it refers to the stalest world state. Bounds memory when an action budget",
			"or lag keeps orders pending longer than producers emit them.")]
		public readonly int MaxQueuedOrders = 512;

		[Desc("Cameo-only: not offered in the lobby's bot lists. The type still exists for map-side bots, scripts",
			"and the A/B harness (the `fransbot` donor and the `classic` reference bot, maintainer 2026-09-28).")]
		public readonly bool HiddenInLobby = false;

		string IBotInfo.Type => Type;

		string IBotInfo.Name => Name;

		public override object Create(ActorInitializer init) { return new ModularBot(this, init); }
	}

	public sealed class ModularBot : ITick, IBot, INotifyDamage
	{
		public bool IsEnabled;

		readonly ModularBotInfo info;
		readonly World world;

		// AR-8: a queued order carries the context it was emitted under — the instanced issuer
		// (`Type@ordinal`, or a provider's ambient IssueAs name) and the attack-response flag.
		// The gate judges at ISSUE time, so the context travels with the order.
		readonly LinkedList<(Order Order, string Issuer, bool Emergency)> orders = [];

		OpenRA.Player player;
		IBotActionBudget actionBudget;

		IBotTick[] tickModules;
		IBotRespondToAttack[] attackResponseModules;

		readonly Dictionary<IBotTick, (long Ticks, long Count)> moduleTiming = [];

		// DESIGN §19.6, the order gate: every module's orders pass through QueueOrder, and ModularBot is the one that calls
		// each module, so it knows who is ordering. `issuer` is the instanced identity (`Type@N`) of the module running
		// now (null outside a module call); `emergency` is true inside an attack response. Instance ordinals live only
		// in attribution — verdicts compare TypeOf(issuer) to the type-named lease owner, so they can never desync.
		readonly BotModules.BotOrderGate<Actor> gate = new();
		readonly Dictionary<object, string> issuerOf = [];
		string issuer;
		bool emergency;
		int gatePruneTick;

		public BotModules.BotOrderGate<Actor> OrderGate => gate;

		/// <summary>Orders dropped because the queue was full (MaxQueuedOrders): the oldest first.</summary>
		public int DroppedOrders { get; private set; }
		readonly Stopwatch moduleStopwatch = new();
		int ticksSinceReport;

		IBotInfo IBot.Info => info;
		OpenRA.Player IBot.Player => player;

		public ModularBot(ModularBotInfo info, ActorInitializer init)
		{
			this.info = info;
			world = init.World;
		}

		// Called by the host's player creation code
		public void Activate(OpenRA.Player p)
		{
			// Bot logic is not allowed to affect world state, and can only act by issuing orders
			// These orders are recorded in the replay, so bots shouldn't be enabled during replays
			if (p.World.IsReplay)
				return;

			IsEnabled = true;
			player = p;
			tickModules = p.PlayerActor.TraitsImplementing<IBotTick>().ToArray();
			actionBudget = p.PlayerActor.TraitsImplementing<IBotActionBudget>().FirstEnabledTraitOrDefault();
			attackResponseModules = p.PlayerActor.TraitsImplementing<IBotRespondToAttack>().ToArray();
			foreach (var t in tickModules)
				issuerOf[t] = BotIssuer.Of(t, p.PlayerActor);
			foreach (var t in attackResponseModules)
				issuerOf.TryAdd(t, BotIssuer.Of(t, p.PlayerActor));
			foreach (var ibe in p.PlayerActor.TraitsImplementing<IBotEnabled>())
				ibe.BotEnabled(this);
		}

		void IBot.QueueOrder(Order order)
		{
			// The ambient scope wins: a provider emitting inside another module's call charges itself
			// (BotIssuer.IssueAs), otherwise the module ticking now is the issuer. `emergency` rides along —
			// it describes the emission context, not the world state at issue.
			var item = (Order: order, Issuer: BotIssuer.Current ?? issuer, Emergency: emergency);
			while (orders.Count >= info.MaxQueuedOrders)
			{
				var dropped = orders.First.Value;
				orders.RemoveFirst();
				DroppedOrders++;
				Log.Write("debug", $"AI {player.InternalName}: ORDERGATE DROPPED {dropped.Issuer ?? "?"} queued {dropped.Order.OrderString} (queue full {info.MaxQueuedOrders}; tick {world.WorldTick})");
			}

			orders.AddLast(item);
		}

		/// <summary>
		/// DESIGN §19.6: a unit order from a module that does not hold the unit's lease is refused when the registry
		/// enforces, counted when it only watches; an emergency (an attack response from a listed module) takes the unit
		/// over. Units without a lease, orders on buildings and bots without a registry (`classic`) pass untouched.
		/// A grouped order (a null Subject with a GroupedActors array) resolves per member, so every member is judged
		/// on its own lease — refused members are stripped and the order is rebuilt over the survivors (AR-1, see
		/// <see cref="BotModules.BotOrderGroup"/>). Returns the order to enqueue, or null when it dies at the gate.
		///
		/// Runs at ISSUE time (AR-8): a deferred order is judged against the leases that hold when it would act,
		/// not the ones that held when it was queued — a claim released in between no longer kills it, and one
		/// taken in between no longer slips it through. `issuer`/`emergency` are the emission context captured
		/// at queue time; they say who emitted the order, which world state cannot tell.
		/// </summary>
		Order GateOrder(string issuer, bool emergency, Order order)
		{
			BotModules.BotUnitLeaseRegistry registry = null;
			var registryResolved = false;

			// Judges one member on its own lease. The registry lookup runs once per order and only when a
			// judgeable member exists — dead, foreign-owned and non-moving members pass through unjudged
			// (BotOrderGroup.UnitInScope), exactly as single orders always have. Resolved at use time, never
			// cached across calls: the registry is conditional (AR-6 lesson).
			bool Passes(Actor unit, Order order)
			{
				if (!BotModules.BotOrderGroup.UnitInScope(unit, player))
					return true;

				if (!registryResolved)
				{
					registryResolved = true;
					registry = BotUnitLeases.Of(player) as BotModules.BotUnitLeaseRegistry;
				}

				return PassesMember(unit, order, issuer, emergency, registry);
			}

			var group = order.GroupedActors;
			if (group == null)
				return Passes(order.Subject, order) ? order : null;

			var (members, filtered) = BotModules.BotOrderGroup.Filter(group, unit => Passes(unit, order));
			if (!filtered)
				return order;

			return members.Length == 0 ? null : BotModules.BotOrderGroup.RebuildWithMembers(order, members);
		}

		bool PassesMember(Actor unit, Order order, string issuer, bool emergency, BotModules.BotUnitLeaseRegistry registry)
		{
			if (registry == null)
				return true;

			var leases = (IBotUnitLeases)registry;
			var holder = leases.LeaseOf(unit)?.Owner;
			var ri = registry.Info;
			var (verdict, first) = gate.Judge(issuer, holder, ri.EnforceAtOrderGate, emergency, ri.EmergencyModules);
			if (first)
				Log.Write("debug", $"AI {player.InternalName}: ORDERGATE {verdict.ToString().ToUpperInvariant()} {issuer ?? "?"} ordered {unit.Info.Name} {unit.ActorID} ({order.OrderString}) held by {holder} (tick {world.WorldTick}; first of this pair)");

			if (verdict == BotModules.BotOrderVerdict.Refuse)
				return false;

			if (verdict == BotModules.BotOrderVerdict.Preempt)
				leases.Preempt(unit, BotIssuer.TypeOf(issuer), BotLeasePurpose.Emergency, ri.EmergencyLeaseTicks);

			var earlier = gate.NoteIssued(unit, issuer, world.WorldTick, ri.CrossedOrderWindowTicks, holder);
			if (earlier != null && gate.CrossedPairs[(earlier, issuer)] == 1)
				Log.Write("debug", $"AI {player.InternalName}: ORDERGATE CROSSED {earlier} then {issuer} ordered {unit.Info.Name} {unit.ActorID} within {ri.CrossedOrderWindowTicks} ticks (tick {world.WorldTick}; first of this pair)");

			if (world.WorldTick - gatePruneTick > 500)
			{
				gatePruneTick = world.WorldTick;
				gate.Prune(a => a.IsDead || !a.IsInWorld);
			}

			return true;
		}

		void ITick.Tick(Actor self)
		{
			if (!IsEnabled || self.World.IsLoadingGameSave)
				return;

			var timed = info.ModulePerfReportIntervalTicks > 0;
			using (new PerfSample("bot_tick"))
			{
				// Every module ticks every tick — timers (scan intervals, countdowns)
				// live inside BotTick, so an attention budget must not skip whole
				// modules here. Attention gating needs a clock/decision split first.
				Sync.RunUnsynced(Game.Settings.Debug.SyncCheckBotModuleCode, world, () =>
				{
					foreach (var t in tickModules)
					{
						if (!t.IsTraitEnabled())
							continue;

						issuer = issuerOf.TryGetValue(t, out var n) ? n : t.GetType().Name;
						if (timed)
						{
							moduleStopwatch.Restart();
							t.BotTick(this);
							moduleStopwatch.Stop();

							moduleTiming.TryGetValue(t, out var acc);
							moduleTiming[t] = (acc.Ticks + moduleStopwatch.ElapsedTicks, acc.Count + 1);
						}
						else
							t.BotTick(this);

						issuer = null;
					}
				});
			}

			if (timed && ++ticksSinceReport >= info.ModulePerfReportIntervalTicks)
			{
				ticksSinceReport = 0;
				ReportModuleTiming(self);
			}

			var ordersToIssueThisTick = Math.Min((orders.Count + info.MinOrderQuotientPerTick - 1) / info.MinOrderQuotientPerTick, orders.Count);
			for (var i = 0; i < ordersToIssueThisTick && orders.Count > 0; i++)
			{
				var item = orders.First.Value;
				orders.RemoveFirst();

				// AR-8: the gate runs here, when the order would act — a refused order costs no action.
				var gated = GateOrder(item.Issuer, item.Emergency, item.Order);
				if (gated == null)
					continue;

				if (actionBudget != null && !actionBudget.TryConsumeActions())
				{
					// The budget, not the gate, stopped it: it goes back to the front for next tick
					// instead of vanishing the way a dequeued-then-abandoned order used to.
					orders.AddFirst(item);
					break;
				}

				world.IssueOrder(gated);
			}
		}

		void ReportModuleTiming(Actor self)
		{
			if (moduleTiming.Count == 0)
				return;

			var perModule = moduleTiming
				.Select(kv => (Name: kv.Key.GetType().Name, Ticks: kv.Value.Ticks, Count: kv.Value.Count))
				.OrderByDescending(m => m.Ticks)
				.ToList();

			var totalMs = perModule.Sum(m => m.Ticks) * 1000.0 / Stopwatch.Frequency;
			var calls = perModule.Sum(m => m.Count);
			var top = string.Join(", ", perModule
				.Take(info.ModulePerfReportTop)
				.Select(m => $"{m.Name}={m.Ticks * 1000.0 / Stopwatch.Frequency:F1}ms/{m.Count}t"));

			var line = string.Format("AI ({0}): module timing over {1} calls, total {2:F1} ms — {3}",
				player.ClientIndex, calls, totalMs, top);
			Log.Write("debug", line);
			TextNotificationsManager.Debug(line);

			moduleTiming.Clear();
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (!IsEnabled || self.World.IsLoadingGameSave)
				return;

			using (new PerfSample("bot_attack_response"))
			{
				Sync.RunUnsynced(Game.Settings.Debug.SyncCheckBotModuleCode, world, () =>
				{
					emergency = true;
					foreach (var t in attackResponseModules)
					{
						if (!t.IsTraitEnabled())
							continue;

						issuer = issuerOf.TryGetValue(t, out var n) ? n : t.GetType().Name;
						t.RespondToAttack(this, self, e);
					}

					issuer = null;
					emergency = false;
				});
			}
		}
	}
}
