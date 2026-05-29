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
using OpenRA;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Changes the owner when a PhysicalState crosses a threshold.",
				"The new owner will be the owner of the unit that first changed the Physical State")]
	public class ChangeOwnerOnPhysicalStateInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Name of the PhysicalState to monitor.")]
		public readonly string PhysicalStateName = null;

		[Desc("Threshold value that must be met to trigger the owner change.")]
		public readonly int Threshold = 0;

		[Desc("Change owner when the PhysicalState is greater than or equal to the threshold (true) or less than or equal to the threshold (false).")]
		public readonly bool TriggerWhenAbove = true;

		[Desc("Keep the new owner even after the PhysicalState moves back past the threshold.")]
		public readonly bool Permanent = false;

		public override object Create(ActorInitializer init) { return new ChangeOwnerOnPhysicalState(init.Self, this); }
	}

	public class ChangeOwnerOnPhysicalState : ConditionalTrait<ChangeOwnerOnPhysicalStateInfo>,
		INotifyPhysicalStateChanged, INotifyOwnerChanged, INotifyCreated
	{
		readonly Actor self;
		readonly PhysicalState physicalState;

		Player baselineOwner;
		Player capturingOwner;
		bool thresholdActive;
		bool suppressOwnerChanged;

		public ChangeOwnerOnPhysicalState(Actor self, ChangeOwnerOnPhysicalStateInfo info)
			: base(info)
		{
			this.self = self;
			physicalState = self.TraitsImplementing<PhysicalState>()
				.FirstOrDefault(ps => ps.Name == info.PhysicalStateName);
			baselineOwner = self.Owner;
		}

		void INotifyCreated.Created(Actor self)
		{
			if (physicalState != null)
				EvaluateState(physicalState.Value);
		}

		void INotifyPhysicalStateChanged.PhysicalStateChanged(Actor self, PhysicalState state, int oldValue, int newValue)
		{
			if (state != physicalState)
				return;

			EvaluateState(newValue);
		}

		void EvaluateState(int currentValue)
		{
			if (physicalState == null || IsTraitDisabled)
				return;

			var thresholdReached = Info.TriggerWhenAbove
				? currentValue >= Info.Threshold
				: currentValue <= Info.Threshold;

			if (thresholdReached)
			{
				var source = physicalState.SourceActor;
				if (source == null || source.Disposed || !source.IsInWorld || source.IsDead)
					return;

				var newOwner = source.Owner;
				if (newOwner == null)
					return;

				if (!Info.Permanent && !thresholdActive)
					baselineOwner = self.Owner;

				if (self.Owner != newOwner || capturingOwner != newOwner)
					ChangeOwnerInternal(newOwner);

				thresholdActive = true;
				capturingOwner = newOwner;
			}
			else
			{
				if (thresholdActive && !Info.Permanent && baselineOwner != null && self.Owner != baselineOwner)
					ChangeOwnerInternal(baselineOwner);

				thresholdActive = false;
				capturingOwner = null;
			}
		}

		protected override void TraitEnabled(Actor self)
		{
			if (physicalState != null)
				EvaluateState(physicalState.Value);
		}

		void ChangeOwnerInternal(Player newOwner)
		{
			if (newOwner == null || self.Owner == newOwner)
				return;

			suppressOwnerChanged = true;
			self.ChangeOwner(newOwner);
			self.World.AddFrameEndTask(_ => suppressOwnerChanged = false);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (suppressOwnerChanged)
				return;

			if (!thresholdActive || Info.Permanent)
				baselineOwner = newOwner;
		}

		protected override void TraitDisabled(Actor self)
		{
			if (!Info.Permanent && thresholdActive && baselineOwner != null && self.Owner != baselineOwner)
				ChangeOwnerInternal(baselineOwner);

			thresholdActive = false;
			capturingOwner = null;
		}
	}
}
