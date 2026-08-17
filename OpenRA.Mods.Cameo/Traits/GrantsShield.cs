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
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Contributes an ADDITIVE amount of shield to the actor's single shield pool.",
		"",
		"W21: shield grants stack. Two upgrades that each give 25% of health produce ONE",
		"bar holding 50%, never two bars of 25% — so every source of toughness reads as a",
		"bigger version of the same thing rather than a pile of separate mechanics. That is",
		"the same law ArmorPlating follows for the armor layer.",
		"",
		"Use this instead of ChangesShield whenever an UPGRADE or ability grants shielding.",
		"ChangesShield's StartIfBelow is a CAP, not a contribution: two sources with",
		"StartIfBelow 25 both stop at 25% and the second one is worth nothing. This trait",
		"sums instead, refilling the shared pool toward the total of every enabled grant.",
		"",
		"⚠ NEVER add a second `Shielded` trait to give a unit more shield. Every unit",
		"already inherits exactly one from ^BasicUnit -> ^ShieldedShieldable, and a",
		"duplicate throws `has multiple traits of type Shielded` when the actor is BUILT —",
		"which parses, lints and boots perfectly cleanly first. That is what this trait is",
		"for. (Guarded by tools/audit/audit_unique_traits.py.)")]
	public class GrantsShieldInfo : ConditionalTraitInfo, Requires<ShieldedInfo>
	{
		[Desc("Shield contributed as a FLAT amount. Added to PercentageStrength.")]
		public readonly int Strength = 0;

		[Desc("Shield contributed as a PERCENTAGE of the actor's max health.",
			"Added to Strength.")]
		public readonly int PercentageStrength = 25;

		[Desc("Grant this source's full contribution the moment it turns on, so researching",
			"an upgrade shields the army you already own instead of making it wait.")]
		public readonly bool GrantImmediately = true;

		[Desc("Ticks after taking damage before the pool starts refilling (W21 R7: shields",
			"use the longest delay of the three layers, then refill fastest).")]
		public readonly int DamageCooldown = 250;

		[Desc("Percentage OF MAX HEALTH refilled each interval. Added to RegenAmount.")]
		public readonly int PercentageRegenAmount = 2;

		[Desc("Flat amount refilled each interval. Added to PercentageRegenAmount.")]
		public readonly int RegenAmount = 0;

		[Desc("Ticks between refill steps.")]
		public readonly int RegenInterval = 25;

		public override object Create(ActorInitializer init) { return new GrantsShield(this); }
	}

	public class GrantsShield : ConditionalTrait<GrantsShieldInfo>, ITick, INotifyDamage
	{
		Shielded shield;
		Health health;

		// Every grant on the actor feeds one pool, so exactly one of them may run the
		// refill — N instances ticking would refill N times as fast. The first one owns
		// the work and reads the others' contributions; the rest are inert but still
		// counted. Same ownership idiom as ArmorPlating, for the same reason.
		GrantsShield[] grants;
		GrantsShield owner;
		bool IsOwner => owner == this;

		int regenTicks;
		int cooldownTicks;

		public GrantsShield(GrantsShieldInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			// Resolved here, never in the constructor: traits are constructed in
			// declaration order, so a constructor lookup returns null whenever the target
			// registers later.
			shield = self.TraitOrDefault<Shielded>();
			health = self.TraitOrDefault<Health>();
			grants = self.TraitsImplementing<GrantsShield>().ToArray();
			owner = grants[0];

			base.Created(self);
		}

		int Contribution => health == null ? Info.Strength
			: Info.Strength + Info.PercentageStrength * health.MaxHP / 100;

		// The additive target: everything every ENABLED grant contributes, capped by the
		// pool the shared Shielded actually has room for.
		int GrantedTotal
		{
			get
			{
				var total = 0;
				foreach (var g in grants)
					if (!g.IsTraitDisabled)
						total += g.Contribution;

				return shield == null ? total : total.Clamp(0, shield.MaxStrength);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (!IsOwner || shield == null || shield.IsTraitDisabled)
				return;

			if (cooldownTicks > 0)
			{
				cooldownTicks--;
				return;
			}

			if (--regenTicks > 0)
				return;

			regenTicks = Info.RegenInterval;

			var target = GrantedTotal;
			if (shield.Strength >= target)
				return;

			var step = Info.RegenAmount;
			if (health != null)
				step += Info.PercentageRegenAmount * health.MaxHP / 100;

			if (step <= 0)
				return;

			// Never overshoot the granted total: a generator filling the same pool to 100%
			// is a separate source and must not be undone or double-counted here.
			shield.Regenerate(self, System.Math.Min(step, target - shield.Strength));
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (!IsOwner || e.Damage.Value <= 0)
				return;

			cooldownTicks = Info.DamageCooldown;
		}

		protected override void TraitEnabled(Actor self)
		{
			regenTicks = Info.RegenInterval;

			if (shield == null || !Info.GrantImmediately)
				return;

			// Top up to the new total rather than adding blindly: if another grant already
			// filled the pool past this one's share, there is nothing to hand over.
			var missing = GrantedTotal - shield.Strength;
			if (missing > 0)
				shield.Regenerate(self, missing);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (shield == null)
				return;

			// Losing a grant shrinks the target; drop any strength that was only there
			// because of it, but never below what the remaining grants still support.
			var excess = shield.Strength - GrantedTotal;
			if (excess > 0)
				shield.Regenerate(self, -excess);
		}
	}
}
