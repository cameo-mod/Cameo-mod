#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Production variant that consumes a larva slave (from DroneSpawnerMaster) each time a unit is built.",
		"Supports up to MaxParallel simultaneous builds via LarvaProductionQueue.",
		"While a unit is building, one larva actor will display an egg condition.",
		"When the timer expires the larva is killed and the finished unit exits the hatchery using the normal exit/rally-point logic.")]
	public class LarvaConsumingProductionInfo : ProductionInfo
	{
		[Desc("Condition to grant to a larva to play the egg animation.")]
		public readonly string EggCondition = "sc_zerg_egg";

		public override object Create(ActorInitializer init) { return new LarvaConsumingProduction(init, this); }
	}

	public class LarvaConsumingProduction : Production, ITick
	{
		readonly LarvaConsumingProductionInfo info;

		BaseSpawnerMaster spawnerMaster;
		LarvaProductionQueue queue;

		// One slot per active in-progress item. Actor == null means the slot is free.
		readonly struct EggSlot
		{
			public readonly Actor Larva;
			public readonly int Token;

			public EggSlot(Actor larva, int token) { Larva = larva; Token = token; }
		}

		readonly List<EggSlot> eggSlots = new();

		/// <summary>Number of larva egg-slots currently committed (used by LarvaProductionQueue.TickInner).</summary>
		public int AssignedSlotCount => eggSlots.Count;

		public LarvaConsumingProduction(ActorInitializer init, LarvaConsumingProductionInfo info)
			: base(init, info)
		{
			this.info = info;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			spawnerMaster = self.TraitOrDefault<BaseSpawnerMaster>();
			queue = self.TraitOrDefault<LarvaProductionQueue>();
		}

		void ITick.Tick(Actor self)
		{
			if (spawnerMaster == null || queue == null)
				return;

			// Slot target: every non-paused item in the queue needs a larva, capped at MaxParallel.
			// This includes Done items (slot kept until Produce() consumes it) AND unstarted items
			// (slot must be assigned BEFORE their timer starts, so the timer only runs when a
			// larva is physically available).
			var activeCount = Math.Min(queue.Info.MaxParallel, queue.AllQueued().Count(i => !i.Paused));

			// Purge slots whose larva died (e.g. killed in combat).
			for (var i = eggSlots.Count - 1; i >= 0; i--)
			{
				if (eggSlots[i].Larva.IsDead)
					eggSlots.RemoveAt(i);
			}

			// Release surplus slots when items are cancelled.
			while (eggSlots.Count > activeCount)
			{
				var last = eggSlots[eggSlots.Count - 1];
				eggSlots.RemoveAt(eggSlots.Count - 1);
				RevokeEggCondition(last.Larva, last.Token);
			}

			// Claim a free larva for each new active item that doesn't have one yet.
			// Gather all larva actors that are already used as eggs so we don't double-assign.
			var usedLarvae = new HashSet<Actor>(eggSlots.Select(s => s.Larva));

			while (eggSlots.Count < activeCount)
			{
				var freeLarva = spawnerMaster.SlaveEntries
					.Where(e => e.IsValid && e.Actor.IsInWorld && !usedLarvae.Contains(e.Actor))
					.Select(e => e.Actor)
					.FirstOrDefault();

				if (freeLarva == null)
					break; // No free larva available yet; retry next tick.

				var ec = freeLarva.TraitsImplementing<ExternalCondition>()
					.FirstOrDefault(t => t.Info.Condition == info.EggCondition && t.CanGrantCondition(self));

				if (ec == null)
					break;

				var token = ec.GrantCondition(freeLarva, self);
				eggSlots.Add(new EggSlot(freeLarva, token));
				usedLarvae.Add(freeLarva);
			}
		}

		void RevokeEggCondition(Actor larva, int token)
		{
			if (token == Actor.InvalidConditionToken || larva.IsDead)
				return;

			var ec = larva.TraitsImplementing<ExternalCondition>()
				.FirstOrDefault(t => t.Info.Condition == info.EggCondition);

			ec?.TryRevokeCondition(larva, this, token);
		}

		public override bool Produce(Actor self, ActorInfo producee, string productionType, TypeDictionary inits, int refundableValue)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return false;

			// We need a committed egg larva to consume.
			if (eggSlots.Count == 0)
				return false;

			// Select an exit, ignoring all current egg larvae so none of them blocks the cell.
			var mobileInfo = producee.TraitInfoOrDefault<MobileInfo>();
			var eggActors = new HashSet<Actor>(eggSlots.Select(s => s.Larva).Where(a => !a.IsDead));

			var exit = SelectExit(self, producee, productionType, e =>
			{
				var exitCell = self.Location + e.Info.ExitCell;
				self.NotifyBlocker(exitCell);
				return mobileInfo == null ||
					mobileInfo.CanEnterCell(self.World, self, exitCell,
						ignoreActor: eggActors.FirstOrDefault(a => self.World.Map.CellContaining(a.CenterPosition) == exitCell) ?? eggSlots[0].Larva);
			});

			if (exit == null && self.OccupiesSpace != null && producee.HasTraitInfo<IOccupySpaceInfo>())
				return false;

			// Consume the first available slot.
			var slot = eggSlots[0];
			eggSlots.RemoveAt(0);

			var larvaToKill = slot.Larva;
			self.World.AddFrameEndTask(w =>
			{
				if (!larvaToKill.IsDead)
					larvaToKill.Kill(self);
			});

			ProduceActors(self, producee, productionType, inits, exit?.Info);
			return true;
		}
	}
}

