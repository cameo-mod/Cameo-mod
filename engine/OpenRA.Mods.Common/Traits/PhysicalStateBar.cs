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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Displays a status bar for a named PhysicalState value.")]
	public class PhysicalStateBarInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Name of the PhysicalState to display.")]
		public readonly string PhysicalStateName = null;

		[Desc("Color for positive values (above RelaxedValue).")]
		public readonly Color PositiveColor = Color.Red;

		[Desc("Color for negative values (below RelaxedValue).")]
		public readonly Color NegativeColor = Color.Blue;

		[Desc("Color for neutral/relaxed values.")]
		public readonly Color NeutralColor = Color.Gray;

		[Desc("Whether to display the bar when the value equals the relaxed value.")]
		public readonly bool DisplayWhenRelaxed = false;

		[Desc("Whether to always display the bar, even when empty.")]
		public readonly bool DisplayWhenEmpty = false;

		[Desc("Use separate colors for values above and below relaxed value instead of showing deviation from relaxed value.")]
		public readonly bool ShowAbsoluteValues = false;

		public override object Create(ActorInitializer init) { return new PhysicalStateBar(init.Self, this); }
	}

	public class PhysicalStateBar : ConditionalTrait<PhysicalStateBarInfo>, ISelectionBar, INotifyPhysicalStateChanged
	{
		readonly Actor self;
		readonly PhysicalState physicalState;

		public PhysicalStateBar(Actor self, PhysicalStateBarInfo info)
			: base(info)
		{
			this.self = self;

			physicalState = self.TraitsImplementing<PhysicalState>()
				.FirstOrDefault(ps => ps.Name == info.PhysicalStateName);
		}

		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled || physicalState == null)
				return 0f;

			var currentValue = physicalState.Value;
			var minValue = physicalState.MinValue;
			var maxValue = physicalState.MaxValue;
			var relaxedValue = ((PhysicalStateInfo)physicalState.Info).RelaxedValue;

			// Don't display if at relaxed value and not configured to show
			if (currentValue == relaxedValue && !Info.DisplayWhenRelaxed)
				return 0f;

			if (Info.ShowAbsoluteValues)
			{
				// Show absolute position within min/max range
				var range = maxValue - minValue;
				if (range == 0)
					return 0f;
				return (float)(currentValue - minValue) / range;
			}
			else
			{
				// Show deviation from relaxed value
				var deviation = Math.Abs(currentValue - relaxedValue);
				var maxDeviation = Math.Max(maxValue - relaxedValue, relaxedValue - minValue);
				
				if (maxDeviation == 0 || deviation == 0)
					return 0f;
					
				return (float)deviation / maxDeviation;
			}
		}

		Color ISelectionBar.GetColor()
		{
			if (IsTraitDisabled || physicalState == null)
				return Info.NeutralColor;

			var currentValue = physicalState.Value;
			var relaxedValue = ((PhysicalStateInfo)physicalState.Info).RelaxedValue;

			if (currentValue == relaxedValue)
				return Info.NeutralColor;
			else if (currentValue > relaxedValue)
				return Info.PositiveColor;
			else
				return Info.NegativeColor;
		}

		bool ISelectionBar.DisplayWhenEmpty => Info.DisplayWhenEmpty;

		void INotifyPhysicalStateChanged.PhysicalStateChanged(Actor self, PhysicalState physicalState, int oldValue, int newValue)
		{
			// This ensures the bar updates when the physical state changes
			// The actual update is handled by the selection bar rendering system
		}
	}
}