#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Attach to the player. Plays a notification a short delay after one of the player's actors",
		"deploys (transforms) into something that unlocks new buildable options - e.g. an MCV into a",
		"Construction Yard. Mirrors PlaceBuilding's NewOptions behaviour for the transform path:",
		"deploying a structure that grants nothing new (a second identical Construction Yard) stays",
		"silent, and several deploys within the delay window announce only once.",
		"Requires NewConstructionOptionsOnDeploy on the deploying actors to fire.")]
	public class NewConstructionOptionsNotificationInfo : TraitInfo
	{
		[Desc("Ticks to wait after the deploy before checking for new options and announcing.",
			"Needed because production queues only refresh their buildable list on their own tick,",
			"one tick after the new producer is created.")]
		public readonly int Delay = 10;

		[NotificationReference("Speech")]
		[Desc("Speech notification to play when the deploy unlocks new build options.")]
		public readonly string Notification = null;

		[FluentReference(optional: true)]
		[Desc("Text notification to display when the deploy unlocks new build options.")]
		public readonly string TextNotification = null;

		public override object Create(ActorInitializer init) { return new NewConstructionOptionsNotification(init.Self, this); }
	}

	public class NewConstructionOptionsNotification : ITick
	{
		readonly NewConstructionOptionsNotificationInfo info;
		readonly OpenRA.Player owner;

		bool pending;
		int buildablesBefore;
		int countdown;

		public NewConstructionOptionsNotification(Actor self, NewConstructionOptionsNotificationInfo info)
		{
			this.info = info;
			owner = self.Owner;
		}

		// Called by NewConstructionOptionsOnDeploy when one of this player's actors begins transforming.
		// Called inside the transform's frame-end task, before the new actor is created, so the count
		// captured here is the pre-deploy baseline. Coalesces concurrent/closely-spaced deploys: the
		// first one arms the check, the rest are folded into it so we announce at most once per window.
		public void NotifyDeploy()
		{
			if (pending || owner != owner.World.LocalPlayer)
				return;

			buildablesBefore = GetNumBuildables(owner);
			countdown = info.Delay;
			pending = true;
		}

		void ITick.Tick(Actor self)
		{
			if (!pending)
				return;

			if (--countdown > 0)
				return;

			pending = false;

			// Only announce when the deploy actually made new options available.
			if (GetNumBuildables(owner) <= buildablesBefore)
				return;

			Game.Sound.PlayNotification(self.World.Map.Rules, owner, "Speech", info.Notification, owner.Faction.InternalName);
			TextNotificationsManager.AddTransientLine(owner, info.TextNotification);
		}

		static int GetNumBuildables(OpenRA.Player p)
		{
			// This only matters for the local player.
			if (p != p.World.LocalPlayer)
				return 0;

			return p.World.ActorsWithTrait<ProductionQueue>()
				.Where(a => a.Actor.Owner == p)
				.SelectMany(a => a.Trait.BuildableItems()).Distinct().Count();
		}
	}

	[Desc("Attach to a transforming actor (e.g. an MCV). On deploy, asks the owner's",
		"NewConstructionOptionsNotification to announce if the deploy unlocked new build options.")]
	public class NewConstructionOptionsOnDeployInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new NewConstructionOptionsOnDeploy(); }
	}

	public class NewConstructionOptionsOnDeploy : INotifyTransform
	{
		void INotifyTransform.BeforeTransform(Actor self) { }

		void INotifyTransform.OnTransform(Actor self)
		{
			self.Owner.PlayerActor.TraitOrDefault<NewConstructionOptionsNotification>()?.NotifyDeploy();
		}

		void INotifyTransform.AfterTransform(Actor toActor) { }
	}
}
