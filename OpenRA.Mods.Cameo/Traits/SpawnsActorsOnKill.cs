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

using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Spawns an actor for the KILLER when this actor kills something — the mirror of",
		"SpawnActorOnDeath, which fires for the victim.",
		"",
		"Built for Schwarzer Mond's Moon Propaganda: enemy infantry who die to the Reich's",
		"broadcasts have a chance of getting up again on the Reich's side. Any effect of the",
		"form \"killing X gives you Y\" belongs here — the trait sits on the KILLER, so it can",
		"be gated on the killer's own upgrade, which nothing on the victim ever could.")]
	public class SpawnsActorsOnKillInfo : ConditionalTraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Actor to spawn where the victim died.")]
		public readonly string Actor = null;

		[Desc("Percentage chance that a qualifying kill spawns the actor.")]
		public readonly int Probability = 100;

		[Desc("Only kills on victims with one of these target types count.",
			"Empty means any victim qualifies.")]
		public readonly BitSet<TargetableType> ValidTargets = default;

		[Desc("Kills on victims with any of these target types never count, even when they",
			"also match ValidTargets.")]
		public readonly BitSet<TargetableType> InvalidTargets = default;

		[Desc("Relationships to the victim that count. Killing your own troops should not",
			"reinforce you, so allies and your own units are excluded by default.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Enemy;

		[Desc("Death types that count. Empty means any.")]
		public readonly BitSet<DamageType> DeathTypes = default;

		public override object Create(ActorInitializer init) { return new SpawnsActorsOnKill(this); }
	}

	public class SpawnsActorsOnKill : ConditionalTrait<SpawnsActorsOnKillInfo>, INotifyAppliedDamage
	{
		public SpawnsActorsOnKill(SpawnsActorsOnKillInfo info)
			: base(info) { }

		void INotifyAppliedDamage.AppliedDamage(Actor self, Actor damaged, AttackInfo e)
		{
			// AppliedDamage fires for EVERY hit this actor lands, so the kill is identified
			// the way AnnounceOnKill does it — the damage state reaching Dead — and suicides
			// are excluded.
			if (IsTraitDisabled || e.DamageState != DamageState.Dead || damaged == self)
				return;

			if (!Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(damaged.Owner)))
				return;

			if (!Info.DeathTypes.IsEmpty && !e.Damage.DamageTypes.Overlaps(Info.DeathTypes))
				return;

			var targetTypes = damaged.GetEnabledTargetTypes();
			if (!Info.ValidTargets.IsEmpty && !targetTypes.Overlaps(Info.ValidTargets))
				return;

			if (!Info.InvalidTargets.IsEmpty && targetTypes.Overlaps(Info.InvalidTargets))
				return;

			// SharedRandom, never Random: this runs inside the simulation, so every client
			// must roll the same number or the game desyncs.
			if (self.World.SharedRandom.Next(100) >= Info.Probability)
				return;

			// The victim is mid-death; place the recruit on its cell at the end of the frame,
			// once that actor is actually gone and the cell is free.
			var location = damaged.Location;
			var owner = self.Owner;
			self.World.AddFrameEndTask(w => w.CreateActor(Info.Actor, new TypeDictionary
			{
				new LocationInit(location),
				new OwnerInit(owner),
			}));
		}
	}
}
