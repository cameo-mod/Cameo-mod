#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Manages AI load unit related with " + nameof(Garrisonable) + " and " + nameof(Garrisoner) + " traits.")]
	public class LoadGarrisonerBotModuleCAInfo : ConditionalTraitInfo
	{
		[Desc("Actor types that can be targeted for load, must have " + nameof(Garrisonable) + ".",
			"Leave this empty to include all actors.")]
		public readonly HashSet<string> GarrisonableUnit = null;

		[Desc("Actor types that used for loading, must have " + nameof(Passenger) + ".",
			"Leave this empty to include all actors.")]
		public readonly HashSet<string> GarrisonerUnit = null;

		[Desc("Scan suitable actors and target in this interval.")]
		public readonly int ScanTick = 457;

		[Desc("Don't load Garrisoner to this actor if damage state is worse than this.")]
		public readonly DamageState ValidDamageState = DamageState.Heavy;

		[Desc("Load passengers max to this amount per scan.")]
		public readonly int PassengersPerScan = 2;

		public override object Create(ActorInitializer init) { return new LoadGarrisonerBotModuleCA(init.Self, this); }
	}

	public class LoadGarrisonerBotModuleCA : ConditionalTrait<LoadGarrisonerBotModuleCAInfo>, IBotTick
	{
		// AR-9 (§19.6): the loader holds a BotLeasePurpose.Garrison claim on every walking
		// garrisoner — an idle infantry unit is squad-draftable, so without the claim a squad
		// could draft a unit already marching to a garrison. Renewed each scan; released the
		// tick the unit boards, dies, is captured or goes idle; Stopped then released on a
		// stuck march. TraitDisabled Stops every still-orderable marcher and releases all.
		// Under classicbot the registry is absent and every helper is a no-op — bit-identical.
		const string LeaseOwner = nameof(LoadGarrisonerBotModuleCA);

		readonly World world;
		readonly Player player;
		readonly Predicate<Actor> unitCannotBeOrdered;
		readonly Predicate<Actor> unitCannotBeOrderedOrIsBusy;
		readonly Predicate<Actor> unitCannotBeOrderedOrIsIdle;
		readonly Predicate<Actor> invalidTransport;

		readonly List<UnitWposWrapper> activeGarrisoner = new();

		// Stuck units are ignored only for a while — a unit blocked by traffic should
		// get another attempt instead of being blacklisted until it dies.
		readonly Dictionary<Actor, int> stuckGarrisoner = new();
		const int StuckExpiryTicks = 9144;
		int minAssignRoleDelayTicks;

		public LoadGarrisonerBotModuleCA(Actor self, LoadGarrisonerBotModuleCAInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			invalidTransport = a => a == null || a.IsDead || !a.IsInWorld || (a.Owner.RelationshipWith(player) != PlayerRelationship.Neutral && a.Owner != player);
			unitCannotBeOrdered = a => a == null || a.IsDead || !a.IsInWorld || a.Owner != player;
			unitCannotBeOrderedOrIsBusy = a => unitCannotBeOrdered(a) || !(a.IsIdle || a.CurrentActivity is FlyIdle);
			unitCannotBeOrderedOrIsIdle = a => unitCannotBeOrdered(a) || a.IsIdle || a.CurrentActivity is FlyIdle;
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			minAssignRoleDelayTicks = world.LocalRandom.Next(0, Info.ScanTick);
		}

		protected override void TraitDisabled(Actor self)
		{
			var leases = BotUnitLeases.Of(player);

			// Under the lease regime a still-marching unit is Stopped before its claim ends,
			// so a freed unit goes idle-draftable instead of finishing a march nobody owns.
			// Classic never had leases — no Stop, no release, the march continues exactly as
			// before. stuckGarrisoner deliberately survives the disable: its expiry stamps
			// self-purge, and re-enabling should not resend units into known-blocked paths.
			DisableRelease(bot, leases, activeGarrisoner, LeaseOwner, unitCannotBeOrdered);
			activeGarrisoner.Clear();
		}

		public static int LeaseHeartbeatTicks(int scanTick) => Math.Max(200, scanTick * 4);

		/// <summary>
		/// GC-1 order-before-release: the holder's last act on a unit it gives up — queue the
		/// Stop, then end the claim. Under the issue-time gate (§19.6, AR-8) the Stop passes
		/// because nobody holds the unit when it acts; the ordering is the convention that
		/// keeps the Stop attributed to the module that owned the march.
		/// </summary>
		public static void StopAndRelease(IBot bot, IBotUnitLeases leases, Actor unit, string owner)
		{
			bot.QueueOrder(new Order("Stop", unit, false));
			leases?.Release(unit, owner);
		}

		/// <summary>
		/// Per-tick release: drop every tracked unit matching `goneOrIdle` (boarded, dead,
		/// captured or idle) and end its claim. Returns the number released.
		/// </summary>
		public static int ReleaseGoneOrIdle(List<UnitWposWrapper> active, Predicate<Actor> goneOrIdle, IBotUnitLeases leases, string owner)
		{
			return active.RemoveAll(u =>
			{
				if (!goneOrIdle(u.Actor))
					return false;
				leases?.Release(u.Actor, owner);
				return true;
			});
		}

		/// <summary>
		/// Per-scan heartbeat: renew the claim on a unit that is still marching. A unit the
		/// table now refuses — its lease lapsed into another owner's hands — is released from
		/// our handle and reported lost so the caller drops it.
		/// </summary>
		public static bool LostRenewal(IBotUnitLeases leases, Actor unit, string owner, int heartbeatTicks)
		{
			var lost = !BotUnitLeases.TryClaim(leases, unit, owner, BotLeasePurpose.Garrison, heartbeatTicks);
			if (lost)
				leases?.Release(unit, owner);
			return lost;
		}

		/// <summary>
		/// §19.6 claim-before-order for a new marcher: the claim lands before any capacity is
		/// charged, so a unit another module took mid-scan costs the load nothing.
		/// </summary>
		public static int ClaimedWeight(IBotUnitLeases leases, Actor actor, int weight, string owner, int heartbeatTicks) =>
			BotUnitLeases.TryClaim(leases, actor, owner, BotLeasePurpose.Garrison, heartbeatTicks) ? weight : 0;

		/// <summary>
		/// Disable-time teardown: Stop each still-orderable unit while we hold it (only under
		/// the lease regime — `leases == null` means classic, where nothing is stopped and the
		/// old march-to-the-end behaviour is preserved), then release every claim.
		/// </summary>
		public static void DisableRelease(IBot bot, IBotUnitLeases leases, List<UnitWposWrapper> active, string owner, Predicate<Actor> orderable)
		{
			foreach (var g in active)
			{
				if (leases != null && bot != null && orderable(g.Actor))
					StopAndRelease(bot, leases, g.Actor, owner);
				else
					leases?.Release(g.Actor, owner);
			}
		}

		IBot bot;

		void IBotTick.BotTick(IBot bot)
		{
			this.bot = bot;
			var leases = BotUnitLeases.Of(player);

			// Every tick: a garrisoner that boarded, died, was captured or went idle is released
			// now — parked until the next scan it would stay squad-undraftable for up to
			// ScanTick (~19 s) while nobody is steering it. Claim renewal, the stuck check and
			// new assignments all stay on the scan cadence below.
			if (activeGarrisoner.Count > 0)
				ReleaseGoneOrIdle(activeGarrisoner, unitCannotBeOrderedOrIsIdle, leases, LeaseOwner);

			if (--minAssignRoleDelayTicks <= 0)
			{
				minAssignRoleDelayTicks = Info.ScanTick;

				// Heartbeat: a garrisoner that is still ours renews its lease; one whose
				// renewal lost the claim to another owner is released and dropped.
				activeGarrisoner.RemoveAll(u => LostRenewal(leases, u.Actor, LeaseOwner, LeaseHeartbeatTicks(Info.ScanTick)));

				foreach (var a in stuckGarrisoner.Keys.Where(a => unitCannotBeOrdered(a) || stuckGarrisoner[a] <= world.WorldTick).ToList())
					stuckGarrisoner.Remove(a);
				for (var i = 0; i < activeGarrisoner.Count; i++)
				{
					var p = activeGarrisoner[i];
					if (p.Actor.CurrentActivity.ChildActivity != null
						&& p.Actor.CurrentActivity.ChildActivity.ActivityType == ActivityType.Move
						&& p.Actor.CenterPosition == p.WPos)
					{
						stuckGarrisoner[p.Actor] = world.WorldTick + StuckExpiryTicks;
						StopAndRelease(bot, leases, p.Actor, LeaseOwner);
						activeGarrisoner.RemoveAt(i);
						i--;
					}

					p.WPos = p.Actor.CenterPosition;
				}

				var tcs = world.ActorsWithTrait<Garrisonable>().Where(
				at =>
				{
					var health = at.Actor.TraitOrDefault<IHealth>()?.DamageState;
					return (Info.GarrisonableUnit == null || Info.GarrisonableUnit.Contains(at.Actor.Info.Name)) && !invalidTransport(at.Actor)
					&& at.Trait.HasSpace(1) && (health == null || health < Info.ValidDamageState);
				}).ToArray();

				if (tcs.Length == 0)
					return;

				var tc = tcs.Random(world.LocalRandom);
				var garrisonable = tc.Trait;
				var transport = tc.Actor;
				var spaceTaken = 0;

				var garrisoner = world.ActorsWithTrait<Garrisoner>().Where(at => !unitCannotBeOrderedOrIsBusy(at.Actor)
					&& (Info.GarrisonerUnit == null || Info.GarrisonerUnit.Contains(at.Actor.Info.Name))
					&& !stuckGarrisoner.ContainsKey(at.Actor)
					&& !BotUnitLeases.IsClaimedByOther(leases, at.Actor, LeaseOwner)
					&& garrisonable.HasSpace(at.Trait.Info.Weight))
						.OrderBy(at => (at.Actor.CenterPosition - transport.CenterPosition).HorizontalLengthSquared);

				var orderedActors = new List<Actor>();

				var passengerCount = 0;
				foreach (var g in garrisoner)
				{
					if (!AIUtils.PathExist(g.Actor, transport.Location, transport))
						continue;

					if (garrisonable.HasSpace(spaceTaken + g.Trait.Info.Weight))
					{
						// §19.6: claim before ordering — a garrisoner another module just took stays theirs, and costs no space.
						var weight = ClaimedWeight(leases, g.Actor, g.Trait.Info.Weight, LeaseOwner, LeaseHeartbeatTicks(Info.ScanTick));
						if (weight == 0)
							continue;

						spaceTaken += weight;
						orderedActors.Add(g.Actor);
						activeGarrisoner.Add(new UnitWposWrapper(g.Actor));
						passengerCount++;
					}

					if (!garrisonable.HasSpace(spaceTaken + 1) || passengerCount >= Info.PassengersPerScan)
						break;
				}

				if (orderedActors.Count > 0)
				{
					bot.QueueOrder(new Order("AttackMove", null, Target.FromCell(transport.World, transport.Location), false, groupedActors: orderedActors.ToArray()));
					bot.QueueOrder(new Order("EnterGarrison", null, Target.FromActor(transport), true, groupedActors: orderedActors.ToArray()));
				}
			}
		}
	}
}
