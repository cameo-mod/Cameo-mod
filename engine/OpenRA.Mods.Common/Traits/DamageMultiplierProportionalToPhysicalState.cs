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
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Modifies incoming damage based on a PhysicalState value.")]
	public class DamageMultiplierProportionalToPhysicalStateInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Name of the PhysicalState to monitor.")]
		public readonly string PhysicalStateName = null;

		[Desc("Damage multiplier at minimum PhysicalState value (percentage).")]
		public readonly int DamageMultiplierAtMinimum = 100;

		[Desc("Damage multiplier at maximum PhysicalState value (percentage).")]
		public readonly int DamageMultiplierAtMaximum = 1000;

		[Desc("Use the deviation from RelaxedValue instead of absolute PhysicalState value.")]
		public readonly bool UseDeviationFromRelaxed = false;

		public override object Create(ActorInitializer init) { return new DamageMultiplierProportionalToPhysicalState(init.Self, this); }
	}

	public class DamageMultiplierProportionalToPhysicalState : ConditionalTrait<DamageMultiplierProportionalToPhysicalStateInfo>, 
		IDamageModifier, INotifyPhysicalStateChanged
	{
		readonly Actor self;
		readonly PhysicalState physicalState;

		int currentDamageMultiplier = 100;

		public DamageMultiplierProportionalToPhysicalState(Actor self, DamageMultiplierProportionalToPhysicalStateInfo info)
			: base(info)
		{
			this.self = self;
			physicalState = self.TraitsImplementing<PhysicalState>()
				.FirstOrDefault(ps => ps.Name == info.PhysicalStateName);

			if (physicalState != null)
				UpdateDamageMultiplier(physicalState.Value);
		}

		void UpdateDamageMultiplier(int physicalStateValue)
		{
			if (physicalState == null)
				return;

			float normalizedValue = GetNormalizedValue(physicalStateValue);

			currentDamageMultiplier = InterpolateMultiplier(
				Info.DamageMultiplierAtMinimum,
				Info.DamageMultiplierAtMaximum,
				normalizedValue);
		}

		float GetNormalizedValue(int physicalStateValue)
		{
			if (Info.UseDeviationFromRelaxed)
			{
				var relaxedValue = ((PhysicalStateInfo)physicalState.Info).RelaxedValue;
				var minValue = physicalState.MinValue;
				var maxValue = physicalState.MaxValue;

				if (physicalStateValue >= relaxedValue)
				{
					var maxDeviation = maxValue - relaxedValue;
					if (maxDeviation == 0)
						return 0f;
					return (float)(physicalStateValue - relaxedValue) / maxDeviation;
				}
				else
				{
					var maxDeviation = relaxedValue - minValue;
					if (maxDeviation == 0)
						return 0f;
					return (float)(relaxedValue - physicalStateValue) / maxDeviation;
				}
			}
			else
			{
				var range = physicalState.MaxValue - physicalState.MinValue;
				if (range == 0)
					return 0f;
				return (float)(physicalStateValue - physicalState.MinValue) / range;
			}
		}

		int InterpolateMultiplier(int minMultiplier, int maxMultiplier, float normalizedValue)
		{
			return (int)(minMultiplier + (maxMultiplier - minMultiplier) * normalizedValue);
		}

		int IDamageModifier.GetDamageModifier(Actor attacker, Damage damage)
		{
			return IsTraitDisabled ? 100 : currentDamageMultiplier;
		}

		void INotifyPhysicalStateChanged.PhysicalStateChanged(Actor self, PhysicalState physicalState, int oldValue, int newValue)
		{
			if (physicalState == this.physicalState)
				UpdateDamageMultiplier(newValue);
		}
	}
}