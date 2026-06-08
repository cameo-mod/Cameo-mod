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
	public class DeterministicOffsetSmokeParticleEmitterInfo : ConditionalTraitInfo, ISmokeParticleInfo, IRulesetLoaded,
		Requires<DeterministicCellOffsetInfo>
	{
		[FieldLoader.Require]
		[Desc("The duration of an individual particle. Two values mean actual lifetime will vary between them.")]
		public readonly int[] Duration;

		[Desc("Additional offset for the particle emitter, applied on top of DeterministicCellOffset.")]
		public readonly WVec Offset = WVec.Zero;

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

		public override object Create(ActorInitializer init) { return new DeterministicOffsetSmokeParticleEmitter(init.Self, this); }

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

	public class DeterministicOffsetSmokeParticleEmitter : ConditionalTrait<DeterministicOffsetSmokeParticleEmitterInfo>, ITick
	{
		readonly MersenneTwister random;
		readonly DeterministicCellOffset cellOffset;

		IFacing facing;
		int ticks;

		public DeterministicOffsetSmokeParticleEmitter(Actor self, DeterministicOffsetSmokeParticleEmitterInfo info)
			: base(info)
		{
			random = self.World.SharedRandom;
			cellOffset = self.Trait<DeterministicCellOffset>();
		}

		protected override void Created(Actor self)
		{
			facing = self.TraitOrDefault<IFacing>();
			base.Created(self);
		}

		void ITick.Tick(Actor self)
		{
			if (!self.IsInWorld || IsTraitDisabled)
				return;

			if (--ticks < 0)
			{
				ticks = Info.SpawnFrequency.Length == 2 ? random.Next(Info.SpawnFrequency[0], Info.SpawnFrequency[1]) : Info.SpawnFrequency[0];
				var spawnFacing = (!Info.RandomFacing && facing != null) ? facing.Facing.Facing : -1;
				var offset = cellOffset.Offset + Info.Offset;

				self.World.AddFrameEndTask(w => w.Add(new DeterministicSmokeParticle(self, Info, self.CenterPosition + offset, spawnFacing)));
			}
		}
	}
}
