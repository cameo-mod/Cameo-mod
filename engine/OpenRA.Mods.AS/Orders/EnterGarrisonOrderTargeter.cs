#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Orders
{
	public class EnterGarrisonOrderTargeter<GarrisonableInfo> : UnitOrderTargeter where GarrisonableInfo : TraitInfo
	{
		readonly Func<Actor, TargetModifiers, bool> canTarget;
		readonly Func<Actor, bool> useEnterCursor;
		readonly GarrisonerInfo garrisonerInfo;

		public EnterGarrisonOrderTargeter(string order, int priority,
			Func<Actor, TargetModifiers, bool> canTarget, Func<Actor, bool> useEnterCursor, GarrisonerInfo garrisonerInfo)
			: base(order, priority, "enter", true, true)
		{
			this.canTarget = canTarget;
			this.useEnterCursor = useEnterCursor;
			this.garrisonerInfo = garrisonerInfo;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			if (target == null)
				return false;

			if (garrisonerInfo.TargetRelationships.HasRelationship(self.Owner.RelationshipWith(target.Owner))
				&& target.Info.HasTraitInfo<GarrisonableInfo>()
				&& canTarget(target, modifiers))
			{
				cursor = useEnterCursor(target) ? "enter" : "enter-blocked";
				return true;
			}
			else
				return false;
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			return CanTargetActor(self, target.Actor, modifiers, ref cursor);
		}
	}
}
