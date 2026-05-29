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
	[RequireExplicitImplementation]
	public interface INotifyPhysicalStateChanged
	{
		void PhysicalStateChanged(Actor self, PhysicalState physicalState, int oldValue, int newValue);
	}

	[Desc("Tracks a named physical state value with configurable relaxation behavior.")]
	public class PhysicalStateInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Name of this physical state. Used to reference it from other traits.")]
		public readonly string Name = null;

		[Desc("Minimum value for this physical state.")]
		public readonly int MinValue = 0;

		[Desc("Maximum value for this physical state.")]
		public readonly int MaxValue = 100;

		[Desc("Whether incoming value changes should be scaled relative to unit health (divided by HP/10000).")]
		public readonly bool RelativeToHealth = false;

		[Desc("Apply damage modifiers when this PhysicalState receives external changes.")]
		public readonly bool ApplyDamageModifiers = false;

		[Desc("Linear relaxation amount per tick towards RelaxedValue.")]
		public readonly int RelaxationLinear = 0;

		[Desc("Health-scaled relaxation amount per tick towards RelaxedValue.")]
		public readonly int RelaxationScaled = 0;

		[Desc("Exponential relaxation rate towards RelaxedValue (0.0 = no exponential relaxation, 1.0 = instant).")]
		public readonly float RelaxationExp = 0f;

		[Desc("Delay in ticks before relaxation starts after external value change.")]
		public readonly int RelaxationDelay = 0;

		[Desc("Interval in ticks between relaxation applications.")]
		public readonly int RelaxationInterval = 1;

		[Desc("Target value for relaxation.")]
		public readonly int RelaxedValue = 0;

		[Desc("Initial value when the trait is created.")]
		public readonly int InitialValue = 0;

		public override object Create(ActorInitializer init) { return new PhysicalState(init.Self, this); }
	}

	public class PhysicalState : ConditionalTrait<PhysicalStateInfo>, ITick, ISync
	{
		readonly Actor self;
		IHealth health;
		Actor sourceActor;
		INotifyPhysicalStateChanged[] notifyPhysicalStateChanged = Array.Empty<INotifyPhysicalStateChanged>();
		int range;

		[Sync]
		int relaxationDelayTicks;
		int relaxationIntervalTicks;
		int currentValue;

		public string Name => Info.Name;

		public int Value
		{
			get => currentValue;
			set => SetValue(value, true);
		}

		public int MinValue => Info.MinValue;
		public int MaxValue => Info.MaxValue;

		public int RelaxationDelayTicks => relaxationDelayTicks;

		public Actor SourceActor
		{
			get
			{
				ClearInvalidSource();
				return sourceActor;
			}
		}

		public PhysicalState(Actor self, PhysicalStateInfo info)
			: base(info)
		{
			this.self = self;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			health = self.TraitOrDefault<IHealth>();
			currentValue = Math.Clamp(Info.InitialValue, Info.MinValue, Info.MaxValue);
			range = Info.MaxValue - Info.MinValue;
			notifyPhysicalStateChanged = self.TraitsImplementing<INotifyPhysicalStateChanged>().ToArray();
		}

		void SetValue(int newValue, bool isExternalChange, Actor source = null)
		{
			ClearInvalidSource();

			var clampedValue = Math.Clamp(newValue, Info.MinValue, Info.MaxValue);
			var oldValue = currentValue;

			if (isExternalChange)
				UpdateSource(oldValue, clampedValue, source);

			if (clampedValue == currentValue)
			{
				if (clampedValue == Info.RelaxedValue)
					sourceActor = null;

				return;
			}

			currentValue = clampedValue;

			if (currentValue == Info.RelaxedValue)
				sourceActor = null;

			if (isExternalChange && Info.RelaxationDelay > 0)
				relaxationDelayTicks = Info.RelaxationDelay;

			foreach (var notify in notifyPhysicalStateChanged)
				notify.PhysicalStateChanged(self, this, oldValue, currentValue);
		}

		public void ApplyChange(int amount, Actor source = null, bool applyModifiers = true, bool scaleToHealth = true)
		{
			var scaledAmount = amount;

			if (scaleToHealth)
				scaledAmount = ScaleChangeToHealth(scaledAmount);

			if (Info.ApplyDamageModifiers && scaledAmount != 0 && applyModifiers)
			{
				var adjustedAmount = (decimal)scaledAmount;
				var damageContext = new Damage(scaledAmount);

				foreach (var modifier in self.TraitsImplementing<IDamageModifier>())
				{
					var modifierPercent = modifier.GetDamageModifier(source, damageContext);
					if (modifierPercent != 100)
						adjustedAmount *= modifierPercent / 100m;
				}

				var owner = self.Owner;
				var playerActor = owner?.PlayerActor;
				if (playerActor != null)
				{
					foreach (var modifier in playerActor.TraitsImplementing<IDamageModifier>())
					{
						var modifierPercent = modifier.GetDamageModifier(source, damageContext);
						if (modifierPercent != 100)
							adjustedAmount *= modifierPercent / 100m;
					}
				}

				scaledAmount = (int)adjustedAmount;
			}

			SetValue(currentValue + scaledAmount, true, source);
		}

		void UpdateSource(int oldValue, int newValue, Actor source)
		{
			if (source == null || source.Disposed || !source.IsInWorld || source.IsDead)
				return;

			if (sourceActor != null && (sourceActor.Disposed || !sourceActor.IsInWorld || sourceActor.IsDead))
				sourceActor = null;

			if (sourceActor == source)
				return;

			var baseline = Info.RelaxedValue;
			var oldOffset = oldValue - baseline;
			var newOffset = newValue - baseline;

			if (sourceActor == null)
			{
				if (newValue != baseline)
					sourceActor = source;

				return;
			}

			if (oldOffset == 0 && newOffset != 0)
			{
				sourceActor = source;
				return;
			}

			if ((oldOffset > 0 && newOffset <= 0) || (oldOffset < 0 && newOffset >= 0))
			{
				if (newOffset == 0)
					sourceActor = null;
				else
					sourceActor = source;
			}
		}

		void ClearInvalidSource()
		{
			if (sourceActor != null && (sourceActor.Disposed || !sourceActor.IsInWorld || sourceActor.IsDead))
				sourceActor = null;
		}

		void ITick.Tick(Actor self)
		{
			ClearInvalidSource();

			if (IsTraitDisabled)
				return;

			if (relaxationDelayTicks > 0)
			{
				relaxationDelayTicks--;
				return;
			}

			if (++relaxationIntervalTicks >= Info.RelaxationInterval)
			{
				relaxationIntervalTicks = 0;
				ApplyRelaxation();
			}
		}

		void ApplyRelaxation()
		{
			if (currentValue == Info.RelaxedValue)
				return;

			var difference = Info.RelaxedValue - currentValue;

			if (Info.RelaxationLinear > 0)
				ApplyLinearRelaxation(difference, Info.RelaxationLinear);

			if (Info.RelativeToHealth && Info.RelaxationScaled > 0)
				ApplyLinearRelaxation(difference, ScaleChangeToHealth(Info.RelaxationScaled));

			if (Info.RelaxationExp > 0f && currentValue != Info.RelaxedValue)
			{
				var expChange = (int)(difference * Info.RelaxationExp);
				if (expChange != 0)
					SetValue(currentValue + expChange, false);
			}
		}

		public void ApplyLinearRelaxation(int difference, int amount)
		{
			var linearChange = Math.Sign(difference) * Math.Min(amount, Math.Abs(difference));
			SetValue(currentValue + linearChange, false);
		}

		public int ScaleChangeToHealth(int amount)
		{
			if (health == null || health.MaxHP == 0 || !Info.RelativeToHealth) return amount;
			return amount * range / health.MaxHP;
		}
	}
}
