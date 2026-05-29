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

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This actor flies of the map when the trait is enabled.")]
	public class AutoFliesOffInfo : ConditionalTraitInfo, Requires<AircraftInfo>
	{
		public override object Create(ActorInitializer init) { return new AutoFliesOff(this); }
	}

	public class AutoFliesOff : ConditionalTrait<AutoFliesOffInfo>
	{
		public AutoFliesOff(AutoFliesOffInfo info)
			: base(info) { }

		protected override void TraitEnabled(Actor self)
		{
			self.QueueActivity(false, new FlyOffMap(self));
			self.QueueActivity(new RemoveSelf());
		}
	}
}
