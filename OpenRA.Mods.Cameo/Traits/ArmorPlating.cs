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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("An ARMOR PLATING pool — the middle layer of Cameo's Shield -> Integrity -> Armor",
		"-> Health stack (BALANCE_PROGRAM_PLAN W21). Absorbs damage that would otherwise reach",
		"HEALTH, and protects nothing else: an intact shield stops everything, Integrity eats",
		"electrical damage, and this bar only stands between what is left and the health pool.",
		"",
		"Every 'this unit is tougher now' effect in Cameo is meant to be one of these, granted",
		"as a CONDITIONAL trait — cyborgs and droids carry one from the start, upgrades and",
		"veterancy add more, and they stack additively. That is the R1 law: 1 HP is 1 HP, so",
		"toughness is a visible bar rather than an invisible DamageMultiplier.",
		"",
		"Pair FullCondition/EmptyCondition with conditional Armor traits to make the ACTIVE",
		"layer decide the Versus lookup — that is what stops two armor types multiplying.")]
	public class ArmorPlatingInfo : PausableConditionalTraitInfo
	{
		[Desc("Pool as a FLAT amount of damage absorbed. Added to MaxPercentageStrength.")]
		public readonly int MaxStrength = 0;

		[Desc("Pool as a PERCENTAGE of the actor's max health. Added to MaxStrength.",
			"W21 R6: 50 for a unit that starts with plating or gains a full bar from an",
			"upgrade; smaller additive grants stack on top of it.")]
		public readonly int MaxPercentageStrength = 50;

		[Desc("Flat pool the actor starts with. -1 means start at the maximum.")]
		public readonly int InitialStrength = -1;

		[Desc("Flat amount repaired each interval, once the ramp is at full rate.",
			"Added to PercentageRegenAmount — set one of the two to 0 unless both are wanted.")]
		public readonly int RegenAmount = 0;

		[Desc("Percentage OF THE MAXIMUM pool repaired each interval, once the ramp is at full",
			"rate. Added to RegenAmount (see above).")]
		public readonly int PercentageRegenAmount = 0;

		[Desc("Ticks between repair steps.")]
		public readonly int RegenInterval = 25;

		[Desc("W21 R7 — the RAMP. Repair rate scales with how long the actor has gone undamaged:",
			"rate = base * min(1, ticks_since_damage / RampTicks), so a unit under fire repairs",
			"nothing and one left alone winds up to full rate. 125 for armor (health uses 25, or",
			"50 for infantry; shields use 250). Set 0 to disable the ramp and repair at full rate",
			"immediately.")]
		public readonly int RampTicks = 125;

		[Desc("Damage types this plating does NOT absorb — they pass straight through to health.",
			"Leave empty and the plating absorbs everything that reaches it.")]
		public readonly BitSet<DamageType> BypassDamageTypes = default;

		[GrantedConditionReference]
		[Desc("Condition granted while the plating still has strength left. Gate an Armor trait",
			"on this to give the plating its own armor TYPE.")]
		public readonly string FullCondition = null;

		[GrantedConditionReference]
		[Desc("Condition granted while the plating is depleted. Gate the actor's BODY Armor trait",
			"on this so exactly one armor type is ever enabled — otherwise the engine MULTIPLIES",
			"them and squares the weapon's profile (see W20).")]
		public readonly string EmptyCondition = null;

		public readonly bool ShowSelectionBar = true;

		[Desc("W21 R13: armor reads yellow-orange, distinct from the health bar and the shield's purple.")]
		public readonly Color SelectionBarColor = Color.FromArgb(255, 176, 64);

		public override object Create(ActorInitializer init) { return new ArmorPlating(init, this); }
	}

	public class ArmorPlating : PausableConditionalTrait<ArmorPlatingInfo>, ITick, ISync,
		ISelectionBar, IDamageModifier, INotifyDamage
	{
		// Resolved in Created(), NOT in the constructor. Traits are constructed in declaration
		// order, so TraitOrDefault<Health>() from a constructor returns null whenever Health
		// happens to be registered later — and a null health means MaxStrength collapses to
		// Info.MaxStrength (0 by default), the plating starts empty, EmptyCondition fires
		// immediately, and the whole layer silently does nothing. Created() runs after every
		// trait exists, so the lookup is always valid there.
		Health health;

		[VerifySync]
		public int Strength { get; private set; }

		public int MaxStrength { get; private set; }

		int regenTicks;
		int ticksSinceDamage;
		int fullToken = Actor.InvalidConditionToken;
		int emptyToken = Actor.InvalidConditionToken;

		// The pre-modifier damage, stashed by GetDamageModifier so Damaged() can charge the
		// pool at FULL PRECISION. AS's Shielded instead recovers the value by dividing the
		// 1%-scaled result by 0.01, which is integer-lossy: a 5032 hit becomes 50 and comes
		// back as 5000, forgiving up to 99 damage per hit, and anything under 100 damage
		// scales to 0 and costs the shield nothing at all. That matters most for exactly the
		// small hits this stack now has to absorb — DoT ticks, physical-state chip damage and
		// %-twin damage (W21 R9). Stashing the real number avoids the round trip entirely.
		int pendingDamage;

		public ArmorPlating(ActorInitializer init, ArmorPlatingInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			health = self.TraitOrDefault<Health>();

			// Before base.Created, which is what fires TraitEnabled for an unconditional
			// trait (ConditionalTrait.cs:63) — TraitEnabled sets Strength from MaxStrength,
			// so MaxStrength has to be real by then.
			RecalculateMax();
			base.Created(self);
		}

		void RecalculateMax()
		{
			var fromHealth = health != null ? Info.MaxPercentageStrength * health.MaxHP / 100 : 0;
			MaxStrength = Info.MaxStrength + fromHealth;
		}

		void UpdateConditions(Actor self)
		{
			if (Strength > 0)
			{
				if (emptyToken != Actor.InvalidConditionToken)
					emptyToken = self.RevokeCondition(emptyToken);

				if (fullToken == Actor.InvalidConditionToken && !string.IsNullOrEmpty(Info.FullCondition))
					fullToken = self.GrantCondition(Info.FullCondition);
			}
			else
			{
				if (fullToken != Actor.InvalidConditionToken)
					fullToken = self.RevokeCondition(fullToken);

				if (emptyToken == Actor.InvalidConditionToken && !string.IsNullOrEmpty(Info.EmptyCondition))
					emptyToken = self.GrantCondition(Info.EmptyCondition);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (ticksSinceDamage < int.MaxValue)
				ticksSinceDamage++;

			if (Strength >= MaxStrength || --regenTicks > 0)
				return;

			regenTicks = Info.RegenInterval;

			var step = Info.RegenAmount + Info.PercentageRegenAmount * MaxStrength / 100;
			if (step <= 0)
				return;

			// W21 R7: the ramp. Integer maths deliberately — a unit still under fire gets 0.
			if (Info.RampTicks > 0 && ticksSinceDamage < Info.RampTicks)
				step = step * ticksSinceDamage / Info.RampTicks;

			if (step <= 0)
				return;

			Strength = (Strength + step).Clamp(0, MaxStrength);
			UpdateConditions(self);
		}

		int IDamageModifier.GetDamageModifier(Actor attacker, Damage damage)
		{
			if (!Absorbs(damage))
				return 100;

			// 1, never 0: a hit scaled to nothing fires no damage event, so Damaged() would
			// never run and the plating would absorb the shot for free. The 1% that leaks
			// through is healed back below.
			pendingDamage = damage.Value;
			return 1;
		}

		bool Absorbs(Damage damage)
		{
			return !IsTraitDisabled && Strength > 0 && damage.Value > 0
				&& !(!Info.BypassDamageTypes.IsEmpty && damage.DamageTypes.Overlaps(Info.BypassDamageTypes));
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled || Strength == 0 || e.Damage.Value <= 0 || e.Attacker == self)
				return;

			if (!Info.BypassDamageTypes.IsEmpty && e.Damage.DamageTypes.Overlaps(Info.BypassDamageTypes))
				return;

			ticksSinceDamage = 0;

			// Recover what the actor WOULD have taken without this layer.
			//
			// The stashed pre-modifier value is exact, but it ignores every OTHER damage
			// modifier on the actor (a promotion's DamageMultiplier, say) — using it blindly
			// would make plating absorb the unreduced hit and deplete too fast. So: if the
			// engine's result is exactly this trait's own 1% of the stash, nothing else
			// modified the damage and the exact value is safe. Otherwise something else had a
			// say, and undoing only our own 1% (x100) preserves it, at the cost of the integer
			// residue Shielded always pays.
			//
			// R1 abolishes DamageMultiplier, after which the exact branch is the only one that
			// ever runs — but this must be correct BEFORE that pass lands, not after.
			var mineOnly = pendingDamage / 100;
			var incoming = pendingDamage > 0 && e.Damage.Value == mineOnly
				? pendingDamage
				: e.Damage.Value * 100;
			pendingDamage = 0;

			var excess = incoming - Strength;
			Strength = (Strength - incoming).Clamp(0, MaxStrength);

			// Undo the 1% that reached health while the plating was standing.
			if (health != null && e.Damage.Value > 0)
				health.InflictDamage(self, self,
					new Damage(-e.Damage.Value, e.Damage.DamageTypes, e.Damage.ProjectileType), true);

			// W21 R3: damage always cascades into the next layer in the same shot.
			if (excess > 0 && health != null)
				health.InflictDamage(self, e.Attacker,
					new Damage(excess, e.Damage.DamageTypes, e.Damage.ProjectileType), true);

			UpdateConditions(self);
		}

		protected override void TraitEnabled(Actor self)
		{
			RecalculateMax();
			Strength = Info.InitialStrength < 0
				? MaxStrength
				: Info.InitialStrength.Clamp(0, MaxStrength);
			regenTicks = Info.RegenInterval;
			ticksSinceDamage = Info.RampTicks;
			UpdateConditions(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			Strength = 0;
			UpdateConditions(self);
		}

		float ISelectionBar.GetValue()
		{
			if (!Info.ShowSelectionBar || IsTraitDisabled || MaxStrength == 0)
				return 0f;

			return (float)Strength / MaxStrength;
		}

		// W21 R13: all three bars are always visible, so an empty plating still shows its
		// (empty) bar rather than silently vanishing from the unit's silhouette.
		bool ISelectionBar.DisplayWhenEmpty => Info.ShowSelectionBar && !IsTraitDisabled;

		Color ISelectionBar.GetColor() { return Info.SelectionBarColor; }
	}
}
