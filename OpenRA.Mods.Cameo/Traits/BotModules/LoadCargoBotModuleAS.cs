#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
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
using OpenRA.Activities;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.CA.Traits;
using BotRng = OpenRA.Mods.CA.BotRng;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	// AR-9 port of the engine's AS LoadCargoBotModule (engine/, OpenRA.Mods.AS — cannot be shadowed: AS precedes
	// Cameo in mod.yaml's Assemblies). The genericbot copy adds the §19.6 lease contract the engine one lacks:
	// its "All" load requirement skips even the busy check, so on the unported module a squad's infantry could be
	// ordered into an allied transport mid-mission. Here every walking passenger holds a BotLeasePurpose.Mission
	// lease (claimed at pick, heartbeated each scan, released on arrival/stuck/loss), and the passenger filter
	// skips units another module already owns. classic keeps the engine copy (ai.yaml gates).
	[TraitLocation(SystemActors.Player)]
	[Desc("Lease-aware port of AS LoadCargoBotModule for genericbot: loads own infantry into allied transports",
		"while holding a Mission lease on every walking passenger so squads cannot re-draft it mid-run.")]
	public class LoadCargoBotModuleASInfo : ConditionalTraitInfo
	{
		// GC-1 roster fallback (same convention as EngineerBotModule → CaptureManagerBotModuleCA):
		// when left empty, both lists resolve from the engine LoadCargoBotModuleInfo block carrying the
		// same @InstanceName on this actor, so the passenger/transport rosters stay single-source. Scalars
		// below are per-instance and must be copied into the twin block when they differ from defaults.
		[Desc("Actor types that can be targeted for load, must have " + nameof(Cargo) + ".",
			"The flag represents if this transport only requires idle unit. Possible values are: All, IdleUnit.",
			"Empty resolves from the same-named LoadCargoBotModuleInfo block.")]
		public readonly Dictionary<string, LoadRequirement> TransportTypesAndLoadRequirement = default;

		[Desc("Actor types that used for loading, must have " + nameof(Passenger) + ".",
			"Empty resolves from the same-named LoadCargoBotModuleInfo block.")]
		public readonly HashSet<string> PassengerTypes = default;

		[Desc("The type of relationship that can be targeted for load. Possible values are: Self, AlliedBot and Allies",
			"AlliedBot means AI will load transport for another allied bot player.")]
		public readonly TransportOwner ValidTransportOwner = TransportOwner.Self;

		[Desc("Scan suitable actors and target in this interval.")]
		public readonly int ScanTick = 317;

		[Desc("Don't load passengers to this actor if damage state is worse than this.")]
		public readonly DamageState ValidDamageState = DamageState.Heavy;

		[Desc("Don't load passengers that are further than this distance to this actor.")]
		public readonly WDist MaxDistance = WDist.FromCells(20);

		public override object Create(ActorInitializer init) { return new LoadCargoBotModuleAS(init.Self, this); }
	}

	public class LoadCargoBotModuleAS : ConditionalTrait<LoadCargoBotModuleASInfo>, IBotTick
	{
		const string LeaseOwner = nameof(LoadCargoBotModuleAS);

		readonly World world;
		readonly OpenRA.Player player;
		readonly Predicate<Actor> unitCannotBeOrdered;
		readonly Predicate<Actor> unitCannotBeOrderedOrIsBusy;
		readonly Predicate<Actor> unitCannotBeOrderedOrIsIdle;
		readonly Predicate<Actor> invalidTransport;

		readonly Dictionary<string, LoadRequirement> transportTypesAndLoadRequirement;
		readonly HashSet<string> passengerTypes;

		readonly List<UnitWposWrapper> activePassengers = [];
		readonly List<Actor> stuckPassengers = [];
		int minAssignRoleDelayTicks;

		public LoadCargoBotModuleAS(Actor self, LoadCargoBotModuleASInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;

			// GC-1 roster fallback: empty lists resolve from the engine block carrying the same @InstanceName.
			// A missing peer leaves the sets empty — the module is inert, same as an empty transport table.
			var peer = self.Info.TraitInfos<LoadCargoBotModuleInfo>()
				.FirstOrDefault(t => t.InstanceName == info.InstanceName);
			transportTypesAndLoadRequirement = ResolveRoster(info.TransportTypesAndLoadRequirement, peer?.TransportTypesAndLoadRequirement);
			passengerTypes = ResolveRoster(info.PassengerTypes, peer?.PassengerTypes);
			switch (info.ValidTransportOwner)
			{
				case TransportOwner.Self:
					invalidTransport = a => a == null || a.IsDead || !a.IsInWorld || a.Owner != player;
					break;
				case TransportOwner.AlliedBot:
					invalidTransport = a => a == null || a.IsDead || !a.IsInWorld || !a.Owner.IsBot || a.Owner.RelationshipWith(player) != PlayerRelationship.Ally;
					break;
				case TransportOwner.Allies:
					invalidTransport = a => a == null || a.IsDead || !a.IsInWorld || a.Owner.RelationshipWith(player) != PlayerRelationship.Ally;
					break;
			}

			unitCannotBeOrdered = a => a == null || a.IsDead || !a.IsInWorld || a.Owner != player;
			unitCannotBeOrderedOrIsBusy = a => unitCannotBeOrdered(a) || !(a.IsIdle || a.CurrentActivity is FlyIdle);
			unitCannotBeOrderedOrIsIdle = a => unitCannotBeOrdered(a) || a.IsIdle || a.CurrentActivity is FlyIdle;
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			minAssignRoleDelayTicks = BotRng.For(player, nameof(LoadCargoBotModuleAS)).Next(0, Info.ScanTick);
		}

		protected override void TraitDisabled(Actor self)
		{
			var leases = BotUnitLeases.Of(player);
			foreach (var p in activePassengers)
				leases?.Release(p.Actor, LeaseOwner);

			activePassengers.Clear();
			stuckPassengers.Clear();
		}

		int LeaseHeartbeatTicks() => Math.Max(200, Info.ScanTick * 4);

		// GC-1: a populated own collection wins; an unset/empty one falls back to the paired classic block's.
		public static Dictionary<string, LoadRequirement> ResolveRoster(
			Dictionary<string, LoadRequirement> own, Dictionary<string, LoadRequirement> peer)
			=> own is { Count: > 0 } ? own : peer ?? new Dictionary<string, LoadRequirement>();

		public static HashSet<string> ResolveRoster(HashSet<string> own, HashSet<string> peer)
			=> own is { Count: > 0 } ? own : peer ?? new HashSet<string>();

		void IBotTick.BotTick(IBot bot)
		{
			if (--minAssignRoleDelayTicks > 0)
				return;

			minAssignRoleDelayTicks = Info.ScanTick;

			var leases = BotUnitLeases.Of(player);

			// Heartbeat: a passenger that is still ours renews its lease; one that is done (idle again, gone,
			// dead, stolen) or whose renewal lost the claim is released and dropped from the active set.
			activePassengers.RemoveAll(u =>
			{
				var lost = unitCannotBeOrderedOrIsIdle(u.Actor)
					|| !BotUnitLeases.TryClaim(leases, u.Actor, LeaseOwner, BotLeasePurpose.Mission, LeaseHeartbeatTicks());
				if (lost)
					leases?.Release(u.Actor, LeaseOwner);

				return lost;
			});
			stuckPassengers.RemoveAll(a => unitCannotBeOrdered(a));
			for (var i = 0; i < activePassengers.Count; i++)
			{
				var p = activePassengers[i];
				if (p.Actor.CurrentActivity.ChildActivity != null
					&& p.Actor.CurrentActivity.ChildActivity.ActivityType == ActivityType.Move
					&& p.Actor.CenterPosition == p.WPos)
				{
					stuckPassengers.Add(p.Actor);

					// Order before release (GC-1 convention): the Stop is ours while the lease is still held.
					bot.QueueOrder(new Order("Stop", p.Actor, false));
					leases?.Release(p.Actor, LeaseOwner);
					activePassengers.RemoveAt(i);
					i--;
				}

				p.WPos = p.Actor.CenterPosition;
			}

			var tcs = world.ActorsWithTrait<Cargo>().Where(
			at =>
			{
				var health = at.Actor.TraitOrDefault<IHealth>()?.DamageState;
				return transportTypesAndLoadRequirement.ContainsKey(at.Actor.Info.Name) && !invalidTransport(at.Actor)
				&& !at.Trait.IsTraitDisabled && at.Trait.HasSpace(1) && (health == null || health < Info.ValidDamageState);
			}).ToList();

			if (tcs.Count == 0)
				return;

			var tc = tcs.Random(BotRng.For(player, nameof(LoadCargoBotModuleAS)));
			var cargo = tc.Trait;
			var transport = tc.Actor;
			var spaceTaken = 0;

			Predicate<Actor> invalidPassenger;
			if (transportTypesAndLoadRequirement[transport.Info.Name] == LoadRequirement.IdleUnit)
				invalidPassenger = unitCannotBeOrderedOrIsBusy;
			else
				invalidPassenger = unitCannotBeOrdered;

			var passengers = world.ActorsWithTrait<Passenger>().Where(at => passengerTypes.Contains(at.Actor.Info.Name)
				&& !invalidPassenger(at.Actor)
				&& !BotUnitLeases.IsClaimedByOther(leases, at.Actor, LeaseOwner)
				&& cargo.HasSpace(at.Trait.Info.Weight)
				&& (at.Actor.CenterPosition - transport.CenterPosition).HorizontalLengthSquared <= Info.MaxDistance.LengthSquared)
					.OrderBy(at => (at.Actor.CenterPosition - transport.CenterPosition).HorizontalLengthSquared);

			var orderedActors = new List<Actor>();

			foreach (var p in passengers)
			{
				if (!AIUtils.PathExist(p.Actor, transport.Location, transport))
					continue;

				if (cargo.HasSpace(spaceTaken + p.Trait.Info.Weight))
				{
					// LC1: claim before ordering — a passenger another module just took stays theirs.
					if (!BotUnitLeases.TryClaim(leases, p.Actor, LeaseOwner, BotLeasePurpose.Mission, LeaseHeartbeatTicks()))
						continue;

					spaceTaken += p.Trait.Info.Weight;
					orderedActors.Add(p.Actor);
					activePassengers.Add(new UnitWposWrapper(p.Actor));
				}

				if (!cargo.HasSpace(spaceTaken + 1))
					break;
			}

			if (orderedActors.Count > 0)
				bot.QueueOrder(new Order("EnterTransport", null, Target.FromActor(transport), false, groupedActors: orderedActors.ToArray()));
		}
	}
}
