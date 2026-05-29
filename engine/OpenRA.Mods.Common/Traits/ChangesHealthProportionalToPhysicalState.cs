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

using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Applies continuous health changes based on a PhysicalState value.")]
	public class ChangesHealthProportionalToPhysicalStateInfo : ConditionalTraitInfo, Requires<IHealthInfo>
	{
		[FieldLoader.Require]
		[Desc("Name of the PhysicalState to monitor.")]
		public readonly string PhysicalStateName = null;

		[Desc("Damage per tick at maximum PhysicalState value.")]
		public readonly int DamageAtMaximum = 10;

		[Desc("Damage per tick at minimum PhysicalState value.")]
		public readonly int DamageAtMinimum = 0;

		[Desc("Damage types to apply.")]
		public readonly BitSet<DamageType> DamageTypes = new("DefaultDeath");

		[Desc("Ticks between damage applications.")]
		public readonly int DamageInterval = 25;

		[Desc("Only apply damage when PhysicalState value is above this threshold.")]
		public readonly int DamageThreshold = 0;

		[Desc("Use percentage-based damage relative to max health instead of absolute values.")]
		public readonly bool UsePercentageDamage = false;

		public override object Create(ActorInitializer init) { return new ChangesHealthProportionalToPhysicalState(init.Self, this); }
	}

	public class ChangesHealthProportionalToPhysicalState : ConditionalTrait<ChangesHealthProportionalToPhysicalStateInfo>, ITick, INotifyPhysicalStateChanged, ISync
	{
		readonly Actor self;
		readonly IHealth health;
		readonly PhysicalState physicalState;

		[Sync]
		int damageTick;

		public ChangesHealthProportionalToPhysicalState(Actor self, ChangesHealthProportionalToPhysicalStateInfo info)
			: base(info)
		{
			this.self = self;
			health = self.Trait<IHealth>();
			physicalState = self.TraitsImplementing<PhysicalState>()
				.FirstOrDefault(ps => ps.Name == info.PhysicalStateName);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || health == null || physicalState == null)
				return;

			if (++damageTick >= Info.DamageInterval)
			{
				damageTick = 0;
				ApplyHealthChange();
			}
		}

		void ApplyHealthChange()
		{
			var currentValue = physicalState.Value;
			
			if (currentValue <= Info.DamageThreshold)
				return;

			var minValue = physicalState.MinValue;
			var maxValue = physicalState.MaxValue;
			var range = maxValue - minValue;

			if (range == 0)
				return;

			var normalizedValue = (float)(currentValue - minValue) / range;
			var baseDamageAmount = Info.DamageAtMinimum + (Info.DamageAtMaximum - Info.DamageAtMinimum) * normalizedValue;

			int actualDamageAmount;
			if (Info.UsePercentageDamage)
			{
				// Percentage-based damage relative to max health
				actualDamageAmount = (int)(health.MaxHP * baseDamageAmount / 100f);
			}
			else
			{
				// Absolute damage
				actualDamageAmount = (int)baseDamageAmount;
			}

			if (actualDamageAmount > 0)
			{
				var attacker = physicalState.SourceActor;
				if (attacker == null || attacker.Disposed || !attacker.IsInWorld || attacker.IsDead)
					attacker = self;

				self.InflictDamage(attacker, new Damage(actualDamageAmount, Info.DamageTypes));
			}
		}

		void INotifyPhysicalStateChanged.PhysicalStateChanged(Actor self, PhysicalState physicalState, int oldValue, int newValue)
		{
			// Health changes are applied on tick, not immediately on state change
		}
	}
}
