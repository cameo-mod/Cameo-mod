using System;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Allows an actor to receive shield protection from nearby ProvidesShieldFromPhysicalState providers.")]
	public class ReceivesShieldFromProviderInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Name of the PhysicalState shield to consume when protected.")]
		public readonly string PhysicalStateName = null;

		[Desc("Condition granted while the actor is shielded.")]
		[GrantedConditionReference]
		public readonly string ShieldedCondition = null;

		[Desc("Ticks between shield status checks.")]
		public readonly int StatusCheckInterval = 15;

		public override object Create(ActorInitializer init) { return new ReceivesShieldFromProvider(init.Self, this); }
	}

	public class ReceivesShieldFromProvider : ConditionalTrait<ReceivesShieldFromProviderInfo>, IPhysicalStateShield, ITick
	{
		readonly Actor self;
		readonly PhysicalStateShieldManager manager;

		int conditionToken = Actor.InvalidConditionToken;
		int statusTicker;

		public ReceivesShieldFromProvider(Actor self, ReceivesShieldFromProviderInfo info)
			: base(info)
		{
			this.self = self;
			manager = PhysicalStateShieldManager.ForWorld(self.World);
			statusTicker = 1;
		}

		public Damage AbsorbDamage(Actor victim, Actor attacker, Damage damage)
		{
			if (victim != self || damage.Value <= 0)
				return damage;

			if (IsTraitDisabled)
				return damage;

			var remaining = manager.AbsorbDamage(victim, attacker, damage.Value, damage.ProjectileType, Info.PhysicalStateName, null);

			if (remaining == damage.Value)
				return damage;

			return new Damage(remaining, damage.DamageTypes, damage.ProjectileType);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
			{
				RevokeCondition();
				return;
			}

			if (--statusTicker <= 0)
			{
				statusTicker = Math.Max(1, Info.StatusCheckInterval);
				UpdateShieldStatus();
			}
		}

		protected override void TraitEnabled(Actor self)
		{
			statusTicker = 1;
		}

		protected override void TraitDisabled(Actor self)
		{
			RevokeCondition();
		}
		void UpdateShieldStatus()
		{
			var status = manager.GetShieldStatus(self, Info.PhysicalStateName);
			var hasShield = status.HasShield;

			if (hasShield && conditionToken == Actor.InvalidConditionToken && !string.IsNullOrEmpty(Info.ShieldedCondition))
				conditionToken = self.GrantCondition(Info.ShieldedCondition);
			else if (!hasShield && conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);
		}

		void RevokeCondition()
		{
			if (conditionToken == Actor.InvalidConditionToken)
				return;

			conditionToken = self.RevokeCondition(conditionToken);
		}
	}
}
