#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-03 P7 phase (a) (design 01 5e100b94 + Opus review): the write-only army duty ledger of this seat, DEV only. Every claim and release of units by a squad-manager duty calls one hook here; the hook records the unit
	// ids, the duty, the caller method and the reason in the ledger and writes one row to army_duty_trace.csv (own file, no header, all seats append): tick,seat,event,duty,caller,detail with event = claim | release | pool |
	// lend | heartbeat | counters. [Conditional]: every call and its arguments are removed from a release build, so release adds no state, RNG use or allocation (the ledger field stays null). NOTHING reads the ledger in phase (a): no
	// decision, order or random draw changes. The pool hash in a row is the order-sensitive digest of the idle pool right AFTER the change (each hook sits directly behind the pool write it records).
	partial class SquadManagerBotModuleCA
	{
		ArmyDutyLedger dutyLedger;
		int dutyCountersNext = 5000;

		[Conditional("CAMEO_DEV_LOGGING")]
		void LedgerClaim(ArmyDuty duty, string caller, string reason, IEnumerable<UnitWposWrapper> units) => LedgerClaim(duty, caller, reason, units.Select(u => u.Actor));

		[Conditional("CAMEO_DEV_LOGGING")]
		void LedgerClaim(ArmyDuty duty, string caller, string reason, IEnumerable<Actor> units)
		{
			if (!Info.ArmyDutyTrace)
				return;

			dutyLedger ??= new ArmyDutyLedger();
			var kinds = new Dictionary<ArmyDutyLedger.ClaimKind, int>();
			var displaced = new Dictionary<ArmyDuty, int>();
			var ids = new List<uint>();
			var steals = 0;
			var draftBacks = 0;
			foreach (var actor in units)
			{
				if (actor == null)
					continue;

				var result = dutyLedger.Claim(actor.ActorID, duty, World.WorldTick, reason, caller);
				kinds[result.Kind] = kinds.GetValueOrDefault(result.Kind) + 1;
				if (result.Kind != ArmyDutyLedger.ClaimKind.New)
					displaced[result.Previous.Duty] = displaced.GetValueOrDefault(result.Previous.Duty) + 1;

				if (result.Steal)
					steals++;

				if (result.DraftBack)
					draftBacks++;

				ids.Add(actor.ActorID);
			}

			if (ids.Count == 0)
				return;

			DutyWrite("claim", duty.ToString(), caller, ArmyDutyLedger.ClaimDetail(duty, caller, reason, kinds, displaced, steals, draftBacks, ids, DutyPoolHash()));
			DutyCounters();
		}

		// The units leave their duty and hold none (squad dissolved on a death, handed to another module, dropped).
		[Conditional("CAMEO_DEV_LOGGING")]
		void LedgerRelease(string caller, string reason, IEnumerable<UnitWposWrapper> units) => LedgerRelease(caller, reason, units.Select(u => u.Actor));

		[Conditional("CAMEO_DEV_LOGGING")]
		void LedgerRelease(string caller, string reason, IEnumerable<Actor> units)
		{
			if (!Info.ArmyDutyTrace)
				return;

			dutyLedger ??= new ArmyDutyLedger();
			var previous = new Dictionary<ArmyDuty, int>();
			var ids = new List<uint>();
			foreach (var actor in units)
			{
				if (actor == null)
					continue;

				if (dutyLedger.Release(actor.ActorID, World.WorldTick, out var entry))
					previous[entry.Duty] = previous.GetValueOrDefault(entry.Duty) + 1;

				ids.Add(actor.ActorID);
			}

			if (ids.Count == 0)
				return;

			DutyWrite("release", "-", caller, ArmyDutyLedger.ReleaseDetail(caller, reason, previous, ids, DutyPoolHash()));
			DutyCounters();
		}

		// The units went back to the idle pool (R7): their old duty ends and they hold Idle (a hand-back, never a take).
		[Conditional("CAMEO_DEV_LOGGING")]
		void LedgerToPool(string caller, string reason, IEnumerable<UnitWposWrapper> units) => LedgerToPool(caller, reason, units.Select(u => u.Actor));

		[Conditional("CAMEO_DEV_LOGGING")]
		void LedgerToPool(string caller, string reason, IEnumerable<Actor> units)
		{
			if (!Info.ArmyDutyTrace)
				return;

			dutyLedger ??= new ArmyDutyLedger();
			var previous = new Dictionary<ArmyDuty, int>();
			var ids = new List<uint>();
			foreach (var actor in units)
			{
				if (actor == null)
					continue;

				if (dutyLedger.ToPool(actor.ActorID, World.WorldTick, reason, caller, out var entry))
					previous[entry.Duty] = previous.GetValueOrDefault(entry.Duty) + 1;

				ids.Add(actor.ActorID);
			}

			if (ids.Count == 0)
				return;

			DutyWrite("pool", ArmyDuty.Idle.ToString(), caller, ArmyDutyLedger.ReleaseDetail(caller, reason, previous, ids, DutyPoolHash()));
			DutyCounters();
		}

		// A guard-lend: the units leave the home guard for the pool. The lend is a claim of the guard-lend duty (it can displace a higher duty: that is a baseline finding), then the units sit in the pool; a defend claim within 3000 ticks of
		// it is a draft-back.
		[Conditional("CAMEO_DEV_LOGGING")]
		void LedgerLend(string caller, string reason, IEnumerable<Actor> units)
		{
			if (!Info.ArmyDutyTrace)
				return;

			dutyLedger ??= new ArmyDutyLedger();
			var kinds = new Dictionary<ArmyDutyLedger.ClaimKind, int>();
			var displaced = new Dictionary<ArmyDuty, int>();
			var ids = new List<uint>();
			var steals = 0;
			foreach (var actor in units)
			{
				if (actor == null)
					continue;

				var result = dutyLedger.Claim(actor.ActorID, ArmyDuty.GuardLend, World.WorldTick, reason, caller);
				kinds[result.Kind] = kinds.GetValueOrDefault(result.Kind) + 1;
				if (result.Kind != ArmyDutyLedger.ClaimKind.New)
					displaced[result.Previous.Duty] = displaced.GetValueOrDefault(result.Previous.Duty) + 1;

				if (result.Steal)
					steals++;

				dutyLedger.NoteLend(actor.ActorID, World.WorldTick);
				dutyLedger.ToPool(actor.ActorID, World.WorldTick, reason, caller, out _);
				ids.Add(actor.ActorID);
			}

			if (ids.Count == 0)
				return;

			DutyWrite("lend", ArmyDuty.GuardLend.ToString(), caller, ArmyDutyLedger.ClaimDetail(ArmyDuty.GuardLend, caller, reason, kinds, displaced, steals, 0, ids, DutyPoolHash()));
			DutyCounters();
		}

		// The heartbeat (ReconcileSquadLeasesCore) took one unit out of the pool or a squad because another module holds its lease (the unit was owned twice). No pool hash in this row: the pool call site runs inside a List.RemoveAll
		// predicate, where the list is half compacted.
		[Conditional("CAMEO_DEV_LOGGING")]
		void LedgerHeartbeat(string why, Actor unit)
		{
			if (!Info.ArmyDutyTrace || unit == null)
				return;

			dutyLedger ??= new ArmyDutyLedger();
			dutyLedger.NoteHeartbeatDoubles(1);
			dutyLedger.Release(unit.ActorID, World.WorldTick, out var entry);
			DutyWrite("heartbeat", "-", nameof(ReconcileSquadLeasesCore), $"why={why};prev={entry.Duty};units={unit.ActorID}");
		}

		// Every 5000 ticks: drop the entries of units that died or left the world (no hook sits at every death), then write the baseline counters.
		void DutyCounters()
		{
			if (World.WorldTick < dutyCountersNext)
				return;

			dutyCountersNext = World.WorldTick + 5000;
			dutyLedger.Prune(id => World.GetActorById(id) is not { IsInWorld: true, IsDead: false });
			DutyWrite("counters", "-", "-", dutyLedger.CountersDetail());
		}

		string DutyPoolHash() => ArmyDutyLedger.PoolHash(unitsHangingAroundTheBase.Where(u => u.Actor != null).Select(u => u.Actor.ActorID));

		void DutyWrite(string ev, string duty, string caller, string detail)
		{
			var hub = World.WorldActor.TraitOrDefault<CameoLogHub>();
			if (hub == null || !hub.Sink.Enabled)
				return;

			var seat = Array.IndexOf(World.Players, Player);
			hub.Sink.InfantryRejectRow(seat, "army_duty_trace.csv", $"{World.WorldTick},{seat},{ev},{duty},{caller},{detail}", World.WorldTick);
		}
	}
}
