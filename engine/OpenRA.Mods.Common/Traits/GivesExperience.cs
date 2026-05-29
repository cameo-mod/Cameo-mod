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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor gives experience to a GainsExperience actor proportional to the damage they deal.")]
	sealed class GivesExperienceInfo : TraitInfo
	{
		[Desc("If -1, use the value of the unit cost.")]
		public readonly int Experience = -1;

		[Desc("Player relationships the attacking player needs to receive the experience.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		[Desc("Percentage of the `Experience` value that is being granted to the attacking actor.")]
		public readonly int ActorExperienceModifier = 10000;

		[Desc("Percentage of the `Experience` value that is being granted to the player owning the attacking actor.")]
		public readonly int PlayerExperienceModifier = 0;

		[Desc("Percentage of the `Experience` value granted to an actor that heals this actor. Defaults to 0 (disabled).")]
		public readonly int HealerExperienceModifier = 0;

		public override object Create(ActorInitializer init) { return new GivesExperience(this); }
	}

	sealed class GivesExperience : INotifyCreated, INotifyDamage, INotifyKilled
	{
		readonly GivesExperienceInfo info;

		int exp;
		Health health;

		public GivesExperience(GivesExperienceInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();
			exp = info.Experience >= 0 ? info.Experience
				: valued != null ? valued.Cost : 0;

			exp = Util.ApplyPercentageModifiers(exp, self.TraitsImplementing<IGivesExperienceModifier>().Select(m => m.GetGivesExperienceModifier()));

			health = self.TraitOrDefault<Health>();
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (exp == 0 || e.Attacker == null || e.Attacker.Disposed)
				return;

			if (e.Damage.Value > 0)
			{
				if (!info.ValidRelationships.HasRelationship(e.Attacker.Owner.RelationshipWith(self.Owner)))
					return;

				var xp = exp * e.Damage.Value / health.MaxHP;
				if (xp > 0)
					GiveXP(e.Attacker, xp, info.ActorExperienceModifier);
			}
			else if (e.Damage.Value < 0 && info.HealerExperienceModifier > 0)
			{
				var xp = exp * -e.Damage.Value / health.MaxHP;
				if (xp > 0)
					GiveXP(e.Attacker, xp, info.HealerExperienceModifier);
			}
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (e.Attacker == null || e.Attacker.Disposed)
				return;

			if (!info.ValidRelationships.HasRelationship(e.Attacker.Owner.RelationshipWith(self.Owner)))
				return;

			e.Attacker.TraitOrDefault<GainsExperience>()?.IncrementKill();
		}

		void GiveXP(Actor attacker, int xp, int actorModifier)
		{
			var actor = attacker.TraitOrDefault<GainsExperience>();
			if (actor != null)
			{
				var mod = attacker.TraitsImplementing<IGainsExperienceModifier>()
					.Select(x => x.GetGainsExperienceModifier()).Append(actorModifier);
				actor.GiveExperience(Util.ApplyPercentageModifiers(xp, mod));
			}

			attacker.Owner.PlayerActor.TraitOrDefault<PlayerExperience>()
				?.GiveExperience(Util.ApplyPercentageModifiers(xp, [info.PlayerExperienceModifier]));
		}
	}
}
