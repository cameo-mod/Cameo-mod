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
using OpenRA.Mods.Common.Traits;
using System;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Applies linear changes to PhysicalState.")]
	public class ChangesPhysicalStateInfo : ConditionalTraitInfo, Requires<PhysicalStateInfo>
	{
		[FieldLoader.Require]
		[Desc("Name of the PhysicalState to change.")]
		public readonly string PhysicalStateName = null;

		[Desc("Change value.")]
		public readonly int Amount = 10;

		[Desc("Ticks between change applications.")]
		public readonly int Delay = 25;

		[Desc("Cap off change if it passes RelaxedValue.")]
		public readonly bool IsRelaxation = false;

		[Desc("Scales absolute change against health.")]
		public readonly bool RelativeToHealth = false;

		[Desc("Damage modifiers will affect the change. Has no effect if IsRelaxation is enabled.")]
		public readonly bool AffectedByModifiers = true;

		public override object Create(ActorInitializer init) { return new ChangesPhysicalState(init.Self, this); }
	}

	public class ChangesPhysicalState : ConditionalTrait<ChangesPhysicalStateInfo>, ITick, ISync
	{
		readonly Actor self;
		readonly PhysicalState physicalState;
		int relaxedValue;

		[Sync]
		int ticks;

		int amount;

		public ChangesPhysicalState(Actor self, ChangesPhysicalStateInfo info)
			: base(info)
		{
			this.self = self;
			physicalState = self.TraitsImplementing<PhysicalState>()
				.FirstOrDefault(ps => ps.Name == info.PhysicalStateName);
			relaxedValue = physicalState.Info.RelaxedValue;
			amount = Info.RelativeToHealth ? physicalState.ScaleChangeToHealth(Info.Amount) : Info.Amount;
			if (Info.IsRelaxation) amount = Math.Abs(amount);
		}

		void ApplyChange()
		{
			if (Info.IsRelaxation)
			{
				if (physicalState.RelaxationDelayTicks != 0)
					return;
				physicalState.ApplyLinearRelaxation(relaxedValue - physicalState.Value, amount);
			}
			else physicalState.ApplyChange(amount, self, Info.AffectedByModifiers, false); // already scaled if yes
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || physicalState == null)
				return;

			if (++ticks >= Info.Delay)
			{
				ticks = 0;
				ApplyChange();
			}
		}
	}
}
