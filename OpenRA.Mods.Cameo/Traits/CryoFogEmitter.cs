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

using OpenRA.GameRules;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Cameo.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Emits rising cryo-fog particles while a condition holds (e.g. a cryo/freeze effect), ",
		"subject to a world-wide concurrent cap from CryoFogLimiter.")]
	public class CryoFogEmitterInfo : ConditionalTraitInfo, ISmokeParticleInfo, IRulesetLoaded
	{
		[FieldLoader.Require]
		[Desc("The duration of an individual particle. Two values mean actual lifetime will vary between them.")]
		public readonly int[] Duration;

		[Desc("Offset for the particle emitter.")]
		public readonly WVec[] Offset = [WVec.Zero];

		[Desc("Randomize particle forward movement.")]
		public readonly WDist[] Speed = [WDist.Zero];

		[Desc("Randomize particle gravity.")]
		public readonly WDist[] Gravity = [WDist.Zero];

		[Desc("Randomize particle facing.")]
		public readonly bool RandomFacing = true;

		[Desc("Randomize particle turnrate.")]
		public readonly int TurnRate = 0;

		[Desc("Rate to reset particle movement properties.")]
		public readonly int RandomRate = 4;

		[Desc("How often particles should spawn, in ticks.")]
		public readonly int[] SpawnFrequency = [100, 150];

		[Desc("Which image to use.")]
		public readonly string Image = "particles";

		[SequenceReference(nameof(Image))]
		[Desc("Which sequence to use when the smoke starts.")]
		public readonly string[] StartSequences = [];

		[FieldLoader.Require]
		[SequenceReference(nameof(Image))]
		[Desc("Which sequence to use while smoke is active.")]
		public readonly string[] Sequences = [];

		[SequenceReference(nameof(Image))]
		[Desc("Which sequence to use when the smoke ends.")]
		public readonly string[] EndSequences = [];

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Which palette to use.")]
		public readonly string Palette = null;

		public readonly bool IsPlayerPalette = false;

		[WeaponReference]
		[Desc("Has to be defined in weapons.yaml, if defined, as well.")]
		public readonly string Weapon = null;

		public WeaponInfo WeaponInfo { get; private set; }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (string.IsNullOrEmpty(Weapon))
				return;

			var weaponToLower = Weapon.ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out var weaponInfo))
				throw new YamlException($"Weapons Ruleset does not contain an entry '{weaponToLower}'");

			WeaponInfo = weaponInfo;
		}

		public override object Create(ActorInitializer init) { return new CryoFogEmitter(init.Self, this); }

		string ISmokeParticleInfo.Image => Image;
		string[] ISmokeParticleInfo.StartSequences => StartSequences;
		string[] ISmokeParticleInfo.Sequences => Sequences;
		string[] ISmokeParticleInfo.EndSequences => EndSequences;
		string ISmokeParticleInfo.Palette => Palette;
		bool ISmokeParticleInfo.IsPlayerPalette => IsPlayerPalette;
		WDist[] ISmokeParticleInfo.Speed => Speed;
		WDist[] ISmokeParticleInfo.Gravity => Gravity;
		int[] ISmokeParticleInfo.Duration => Duration;
		WeaponInfo ISmokeParticleInfo.Weapon => WeaponInfo;
		int ISmokeParticleInfo.TurnRate => TurnRate;
		int ISmokeParticleInfo.RandomRate => RandomRate;
	}

	public class CryoFogEmitter : ConditionalTrait<CryoFogEmitterInfo>, ITick
	{
		readonly MersenneTwister random;
		readonly WVec offset;
		readonly int reserveDuration;

		IFacing facing;
		CryoFogLimiter limiter;
		int ticks;

		public CryoFogEmitter(Actor self, CryoFogEmitterInfo info)
			: base(info)
		{
			random = self.World.SharedRandom;

			offset = Info.Offset.Length == 2
				? new WVec(
					random.Next(Info.Offset[0].X, Info.Offset[1].X),
					random.Next(Info.Offset[0].Y, Info.Offset[1].Y),
					random.Next(Info.Offset[0].Z, Info.Offset[1].Z))
				: Info.Offset[0];

			// Reserve the longest possible lifetime so the global cap stays conservative.
			reserveDuration = Info.Duration.Length == 2 ? Info.Duration[1] : Info.Duration[0];
		}

		protected override void Created(Actor self)
		{
			facing = self.TraitOrDefault<IFacing>();
			limiter = self.World.WorldActor.TraitOrDefault<CryoFogLimiter>();
			base.Created(self);
		}

		void ITick.Tick(Actor self)
		{
			if (!self.IsInWorld || IsTraitDisabled)
				return;

			if (--ticks < 0)
			{
				ticks = Info.SpawnFrequency.Length == 2 ? random.Next(Info.SpawnFrequency[0], Info.SpawnFrequency[1]) : Info.SpawnFrequency[0];

				// Global concurrent cap: drop this spawn if the budget is full. The spawn timer
				// already advanced deterministically above, so all clients stay in sync.
				if (limiter != null && !limiter.TryReserve(reserveDuration))
					return;

				var spawnFacing = (!Info.RandomFacing && facing != null) ? facing.Facing.Facing : -1;

				// Spread spawns across the actor's whole solid footprint so multi-cell buildings
				// emit everywhere, not just at the centre. Single-cell units use their one cell.
				var pos = self.CenterPosition;
				var cells = self.OccupiesSpace?.OccupiedCells();
				if (cells != null && cells.Length > 0)
					pos = self.World.Map.CenterOfCell(cells[random.Next(cells.Length)].Cell);

				pos += offset;
				self.World.AddFrameEndTask(w => w.Add(new DeterministicSmokeParticle(self, Info, pos, spawnFacing)));
			}
		}
	}
}
