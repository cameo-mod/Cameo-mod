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
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Modifies movement speed, turn speed, turret turn speed, and reload delay based on a PhysicalState value.")]
	public class SlowsProportionalToPhysicalStateInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Name of the PhysicalState to monitor.")]
		public readonly string PhysicalStateName = null;

		[Desc("Movement speed modifier at minimum PhysicalState value (percentage).")]
		public readonly int SpeedModifierAtMinimum = 100;

		[Desc("Movement speed modifier at maximum PhysicalState value (percentage).")]
		public readonly int SpeedModifierAtMaximum = 50;

		[Desc("Turn speed modifier at minimum PhysicalState value (percentage).")]
		public readonly int TurnSpeedModifierAtMinimum = 100;

		[Desc("Turn speed modifier at maximum PhysicalState value (percentage).")]
		public readonly int TurnSpeedModifierAtMaximum = 50;

		[Desc("Turret turn speed modifier at minimum PhysicalState value (percentage).")]
		public readonly int TurretTurnSpeedModifierAtMinimum = 100;

		[Desc("Turret turn speed modifier at maximum PhysicalState value (percentage).")]
		public readonly int TurretTurnSpeedModifierAtMaximum = 50;

		[Desc("Reload delay modifier at minimum PhysicalState value (percentage). Higher = faster reload.")]
		public readonly int ReloadDelayModifierAtMinimum = 100;

		[Desc("Reload delay modifier at maximum PhysicalState value (percentage). Lower = slower reload.")]
		public readonly int ReloadDelayModifierAtMaximum = 50;

		[Desc("Turret types to affect. Leave empty to affect all turrets.")]
		public readonly HashSet<string> TurretTypes = new();

		[Desc("Weapon types to affect. Leave empty to affect all weapons.")]
		public readonly HashSet<string> WeaponTypes = new();

		[Desc("Use the deviation from RelaxedValue instead of absolute PhysicalState value.")]
		public readonly bool UseDeviationFromRelaxed = true;

		[Desc("Only apply effects when PhysicalState value is below RelaxedValue (negative deviation).")]
		public readonly bool OnlyNegativeValues = false;

		[Desc("Only apply effects when PhysicalState value is above RelaxedValue (positive deviation).")]
		public readonly bool OnlyPositiveValues = false;

		public override object Create(ActorInitializer init) { return new SlowsProportionalToPhysicalState(init.Self, this); }
	}

	public class SlowsProportionalToPhysicalState : ConditionalTrait<SlowsProportionalToPhysicalStateInfo>, 
		ISpeedModifier, ITurnSpeedModifier, ITurretTurnSpeedModifier, IReloadModifier, INotifyPhysicalStateChanged
	{
		readonly Actor self;
		readonly PhysicalState physicalState;

		int currentSpeedModifier = 100;
		int currentTurnSpeedModifier = 100;
		int currentTurretTurnSpeedModifier = 100;
		int currentReloadDelaySpeedPercent = 100;

		public SlowsProportionalToPhysicalState(Actor self, SlowsProportionalToPhysicalStateInfo info)
			: base(info)
		{
			this.self = self;
			physicalState = self.TraitsImplementing<PhysicalState>()
				.FirstOrDefault(ps => ps.Name == info.PhysicalStateName);

			if (physicalState != null)
				UpdateModifiers(physicalState.Value);
		}

		void UpdateModifiers(int physicalStateValue)
		{
			if (physicalState == null)
				return;

			float normalizedValue = GetNormalizedValue(physicalStateValue);

			currentSpeedModifier = InterpolateModifier(
				Info.SpeedModifierAtMinimum,
				Info.SpeedModifierAtMaximum,
				normalizedValue);

			currentTurnSpeedModifier = InterpolateModifier(
				Info.TurnSpeedModifierAtMinimum,
				Info.TurnSpeedModifierAtMaximum,
				normalizedValue);

			currentTurretTurnSpeedModifier = InterpolateModifier(
				Info.TurretTurnSpeedModifierAtMinimum,
				Info.TurretTurnSpeedModifierAtMaximum,
				normalizedValue);

			currentReloadDelaySpeedPercent = InterpolateModifier(
				Info.ReloadDelayModifierAtMinimum,
				Info.ReloadDelayModifierAtMaximum,
				normalizedValue);
		}

		float GetNormalizedValue(int physicalStateValue)
		{
			var relaxedValue = ((PhysicalStateInfo)physicalState.Info).RelaxedValue;

			// Check if we should only apply effects to specific value ranges
			if (Info.OnlyNegativeValues && physicalStateValue >= relaxedValue)
				return 0f; // No effect for positive values
			if (Info.OnlyPositiveValues && physicalStateValue <= relaxedValue)
				return 0f; // No effect for negative values

			if (Info.UseDeviationFromRelaxed)
			{
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

		int InterpolateModifier(int minModifier, int maxModifier, float normalizedValue)
		{
			return Math.Max(0, (int)(minModifier + (maxModifier - minModifier) * normalizedValue));
		}

		int ISpeedModifier.GetSpeedModifier()
		{
			if (IsTraitDisabled)
				return 100;

			return currentSpeedModifier;
		}

		int ITurnSpeedModifier.GetTurnSpeedModifier()
		{
			return IsTraitDisabled ? 100 : currentTurnSpeedModifier;
		}

		int ITurretTurnSpeedModifier.GetTurretTurnSpeedModifier(string turretName)
		{
			if (IsTraitDisabled)
				return 100;

			return Info.TurretTypes.Count == 0 || (!string.IsNullOrEmpty(turretName) && Info.TurretTypes.Contains(turretName))
				? currentTurretTurnSpeedModifier : 100;
		}

		int IReloadModifier.GetReloadModifier(string armamentName)
		{
			if (IsTraitDisabled)
				return 100;

			return Info.WeaponTypes.Count == 0 || (!string.IsNullOrEmpty(armamentName) && Info.WeaponTypes.Contains(armamentName))
				? ConvertReloadSpeedToDelayPercent(currentReloadDelaySpeedPercent) : 100;
		}

		void INotifyPhysicalStateChanged.PhysicalStateChanged(Actor self, PhysicalState physicalState, int oldValue, int newValue)
		{
			if (physicalState == this.physicalState)
				UpdateModifiers(newValue);
		}

		static int ConvertReloadSpeedToDelayPercent(int speedPercent)
		{
			if (speedPercent <= 0)
				return int.MaxValue;

			var delayPercent = (int)Math.Ceiling(10000m / speedPercent);
			return Math.Max(1, delayPercent);
		}

	}
}
