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

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Marks this actor as a critical unit for use with CriticalUnitAttackNotifier.")]
	public class CriticalUnitInfo : TraitInfo<CriticalUnit> { }
	public class CriticalUnit { }

	[Desc("Displays a text notification when any critical unit (marked with CriticalUnit) owned by this player is attacked.",
		"Attach this to the player actor.")]
	[TraitLocation(SystemActors.Player)]
	public class CriticalUnitAttackNotifierInfo : TraitInfo
	{
		[Desc("Minimum duration (in milliseconds) between notification events.")]
		public readonly int NotifyInterval = 30000;

		[FluentReference(optional: true)]
		[Desc("Text notification to display.")]
		public readonly string TextNotification = null;

		[Desc("Flash the text notification to draw attention to it.")]
		public readonly bool FlashTextNotification = false;

		public override object Create(ActorInitializer init) { return new CriticalUnitAttackNotifier(this); }
	}

	public class CriticalUnitAttackNotifier : INotifyDamage
	{
		readonly CriticalUnitAttackNotifierInfo info;
		long lastNotifyTime;

		public CriticalUnitAttackNotifier(CriticalUnitAttackNotifierInfo info)
		{
			this.info = info;
			lastNotifyTime = -info.NotifyInterval;
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (e.Attacker != null && e.Attacker.Owner == self.Owner)
				return;

			if (e.Damage.Value < 0)
				return;

			if (!self.Info.HasTraitInfo<CriticalUnitInfo>())
				return;

			if (Game.RunTime > lastNotifyTime + info.NotifyInterval)
			{
				TextNotificationsManager.AddTransientLine(self.Owner, info.TextNotification, info.FlashTextNotification);
				lastNotifyTime = Game.RunTime;
			}
		}
	}
}
