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
	[TraitLocation(SystemActors.Player)]
	[Desc("Tracks the per-player cooldown for ActorLostNotificationCameo.",
		"Must be added to the Player actor.")]
	public class ActorLostNotificationCameoManagerInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new ActorLostNotificationCameoManager(); }
	}

	public class ActorLostNotificationCameoManager
	{
		public long NextAllowedTime;
	}

	[Desc("Plays a speech notification when this actor is killed.",
		"Supports an optional minimum interval between repeated notifications.",
		"Requires ActorLostNotificationCameoManager on the Player actor.")]
	sealed class ActorLostNotificationCameoInfo : ConditionalTraitInfo
	{
		[NotificationReference("Speech")]
		[Desc("Speech notification to play.")]
		public readonly string Notification = "UnitLost";

		[Desc("Text notification to display.")]
		[FluentReference(optional: true)]
		public readonly string TextNotification = null;

		public readonly bool NotifyAll = false;

		[Desc("Minimum duration (in milliseconds) between notification events.")]
		public readonly int NotifyInterval = 0;

		public override object Create(ActorInitializer init) { return new ActorLostNotificationCameo(this); }
	}

	sealed class ActorLostNotificationCameo : ConditionalTrait<ActorLostNotificationCameoInfo>, INotifyKilled
	{
		public ActorLostNotificationCameo(ActorLostNotificationCameoInfo info)
			: base(info) { }

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled)
				return;

			var localPlayer = self.World.LocalPlayer;

			if (localPlayer == null || localPlayer.Spectating)
				return;

			var player = Info.NotifyAll ? localPlayer : self.Owner;

			var manager = player.PlayerActor.TraitOrDefault<ActorLostNotificationCameoManager>();
			if (manager != null && Game.RunTime < manager.NextAllowedTime)
				return;

			Game.Sound.PlayNotification(self.World.Map.Rules, player, "Speech", Info.Notification, self.Owner.Faction.InternalName);
			TextNotificationsManager.AddTransientLine(player, Info.TextNotification);

			if (manager != null)
				manager.NextAllowedTime = Game.RunTime + Info.NotifyInterval;
		}
	}
}
