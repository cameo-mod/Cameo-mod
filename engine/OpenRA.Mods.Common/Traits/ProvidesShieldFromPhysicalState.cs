using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Provides shield energy from a PhysicalState pool and optionally shares it with nearby allies.")]
	public class ProvidesShieldFromPhysicalStateInfo : ConditionalTraitInfo, Requires<PhysicalStateInfo>
	{
		[FieldLoader.Require]
		[Desc("Name of the PhysicalState to consume for shield energy.")]
		public readonly string PhysicalStateName = null;

		[Desc("Share shield with allied actors within this range. Zero disables sharing.")]
		public readonly WDist ShareRange = WDist.Zero;

		[Desc("Allow this trait to protect the owner actor.")]
		public readonly bool ProtectSelf = true;

		[Desc("Projectile types that bypass this shield. Uses projectile class names without the 'Info' suffix.")]
		public readonly string[] ExcludeProjectileTypes = Array.Empty<string>();

		public override object Create(ActorInitializer init) { return new ProvidesShieldFromPhysicalState(init.Self, this); }
	}

	public class ProvidesShieldFromPhysicalState : ConditionalTrait<ProvidesShieldFromPhysicalStateInfo>, IPhysicalStateShield,
		INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly Actor self;
		readonly PhysicalState physicalState;
		readonly HashSet<string> excludedProjectileTypes;

		public ProvidesShieldFromPhysicalState(Actor self, ProvidesShieldFromPhysicalStateInfo info)
			: base(info)
		{
			this.self = self;
			physicalState = self.TraitsImplementing<PhysicalState>().FirstOrDefault(ps => ps.Name == info.PhysicalStateName);
			excludedProjectileTypes = new HashSet<string>(info.ExcludeProjectileTypes ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
		}

		internal Actor Actor => self;

		public Damage AbsorbDamage(Actor victim, Actor attacker, Damage damage)
		{
			if (victim != self || damage.Value <= 0 || !Info.ProtectSelf)
				return damage;

			if (!CanShareWith(self.Owner))
				return damage;

			var manager = PhysicalStateShieldManager.ForWorld(self.World);
			var remaining = manager.AbsorbDamage(victim, attacker, damage.Value, damage.ProjectileType, Info.PhysicalStateName, this);

			if (remaining == damage.Value)
				return damage;

			return new Damage(remaining, damage.DamageTypes, damage.ProjectileType);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			PhysicalStateShieldManager.ForWorld(self.World).RegisterProvider(this);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			PhysicalStateShieldManager.ForWorld(self.World).UnregisterProvider(this);
		}

		internal bool CanShareWith(Player owner)
		{
			if (IsTraitDisabled || physicalState == null)
				return false;

			if (self.IsDead || self.Disposed || !self.IsInWorld)
				return false;

			if (self.Owner == null || owner == null)
				return false;

			return self.Owner.IsAlliedWith(owner);
		}

		internal bool CanProtectActor(Actor target)
		{
			if (target == null)
				return false;

			if (!CanShareWith(target.Owner))
				return false;

			if (target == self)
				return Info.ProtectSelf;

			if (Info.ShareRange <= WDist.Zero)
				return false;

			var delta = target.CenterPosition - self.CenterPosition;
			return delta.HorizontalLengthSquared <= Info.ShareRange.LengthSquared;
		}

		internal bool CanLinkWith(ProvidesShieldFromPhysicalState other, Player owner)
		{
			if (other == null || other == this)
				return false;

			if (!CanShareWith(owner) || !other.CanShareWith(owner))
				return false;

			return IsWithinRange(other.self) || other.IsWithinRange(self);
		}

		bool IsWithinRange(Actor actor)
		{
			if (Info.ShareRange <= WDist.Zero || actor == null)
				return false;

			var delta = actor.CenterPosition - self.CenterPosition;
			return delta.HorizontalLengthSquared <= Info.ShareRange.LengthSquared;
		}

		internal bool AllowsProjectile(string projectileType)
		{
			if (string.IsNullOrEmpty(projectileType) || excludedProjectileTypes.Count == 0)
				return true;

			return !excludedProjectileTypes.Contains(projectileType);
		}

		internal int GetAvailableShield()
		{
			if (physicalState == null)
				return 0;

			return Math.Max(0, physicalState.Value - physicalState.MinValue);
		}

		internal int GetMaximumShield()
		{
			if (physicalState == null)
				return 0;

			return Math.Max(0, physicalState.MaxValue - physicalState.MinValue);
		}

		internal void DrainShield(int amount, Actor attacker)
		{
			if (amount <= 0 || physicalState == null)
				return;

			var available = GetAvailableShield();
			var consume = Math.Min(amount, available);
			if (consume <= 0)
				return;

			physicalState.ApplyChange(-consume, attacker);
		}
	}
}
