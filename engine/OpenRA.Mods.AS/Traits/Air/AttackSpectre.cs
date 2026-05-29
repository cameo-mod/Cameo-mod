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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Aircraft with tageting limited to a circular area. Actor must be created via `AirstrikePowerAS` ability.")]
	public class AttackSpectreInfo : AttackAircraftInfo, Requires<AircraftInfo>
	{
		[Desc("Radius of the area which the aircraft can attack in.")]
		public readonly WDist TargetRadius = WDist.FromCells(5);

		public override object Create(ActorInitializer init) { return new AttackSpectre(init, this); }
	}

	public class AttackSpectre : AttackAircraft, IIssueOrder
	{
		public new readonly AttackSpectreInfo Info;
		public readonly WPos TargetLocation;

		public AttackSpectre(ActorInitializer init, AttackSpectreInfo info)
			: base(init.Self, info)
		{
			Info = info;

			var spectreTargetPositionInit = init.GetOrDefault<SpectreTargetPositionInit>();
			if (spectreTargetPositionInit != null)
				TargetLocation = spectreTargetPositionInit.Value;
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (IsTraitDisabled)
					yield break;

				if (!Armaments.Any())
					yield break;

				yield return new SpectreAttackOrderTargeter(this, 6);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order is SpectreAttackOrderTargeter)
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		protected override bool CanAttack(Actor self, in Target target)
		{
			if (!target.IsInRange(TargetLocation, Info.TargetRadius))
				return false;

			return base.CanAttack(self, target);
		}

		sealed class SpectreAttackOrderTargeter : AttackOrderTargeter
		{
			readonly AttackSpectre attackSpectre;

			public SpectreAttackOrderTargeter(AttackSpectre attackSpectre, int priority)
				: base(attackSpectre, priority)
			{
				this.attackSpectre = attackSpectre;
			}

			public override bool CanTarget(Actor self, in Target target, ref TargetModifiers modifiers, ref string cursor)
			{
				if (!target.IsInRange(attackSpectre.TargetLocation, attackSpectre.Info.TargetRadius))
					return false;

				return base.CanTarget(self, target, ref modifiers, ref cursor);
			}
		}
	}

	public class SpectreTargetPositionInit(WPos value) : ValueActorInit<WPos>(value), ISingleInstanceInit { }
}
