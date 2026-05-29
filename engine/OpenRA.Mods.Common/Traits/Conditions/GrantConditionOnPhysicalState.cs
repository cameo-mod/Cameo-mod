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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Conditions
{
	[Desc("Grants a condition when a named PhysicalState value is within specified bounds.")]
	public class GrantConditionOnPhysicalStateInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Name of the PhysicalState to monitor.")]
		public readonly string PhysicalStateName = null;

		[FieldLoader.Require]
		[Desc("Condition to grant when the PhysicalState is within bounds.")]
		public readonly string Condition = null;

		[Desc("Lower bound (inclusive). If the PhysicalState value is >= this value and <= UpperValue, condition is granted.")]
		public readonly int LowerValue = 0;

		[Desc("Upper bound (inclusive). If the PhysicalState value is <= this value and >= LowerValue, condition is granted.")]
		public readonly int UpperValue = 100;

		[GrantedConditionReference]
		public string LinterCondition => Condition;

		public override object Create(ActorInitializer init) { return new GrantConditionOnPhysicalState(init.Self, this); }
	}

	public class GrantConditionOnPhysicalState : ConditionalTrait<GrantConditionOnPhysicalStateInfo>, INotifyPhysicalStateChanged
	{
		readonly Actor self;
		readonly PhysicalState physicalState;

		int conditionToken = Actor.InvalidConditionToken;

		public GrantConditionOnPhysicalState(Actor self, GrantConditionOnPhysicalStateInfo info)
			: base(info)
		{
			this.self = self;

			physicalState = self.TraitsImplementing<PhysicalState>()
				.FirstOrDefault(ps => ps.Name == info.PhysicalStateName);

			if (physicalState != null && IsWithinBounds(physicalState.Value))
				GrantCondition();
		}

		bool IsWithinBounds(int value)
		{
			return value >= Info.LowerValue && value <= Info.UpperValue;
		}

		void GrantCondition()
		{
			if (conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.Condition);
		}

		void RevokeCondition()
		{
			if (conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);
		}

		void INotifyPhysicalStateChanged.PhysicalStateChanged(Actor self, PhysicalState physicalState, int oldValue, int newValue)
		{
			if (IsTraitDisabled || physicalState != this.physicalState)
				return;

			var wasWithinBounds = IsWithinBounds(oldValue);
			var isWithinBounds = IsWithinBounds(newValue);

			if (wasWithinBounds != isWithinBounds)
			{
				if (isWithinBounds)
					GrantCondition();
				else
					RevokeCondition();
			}
		}

		protected override void TraitDisabled(Actor self)
		{
			RevokeCondition();
		}
	}
}
