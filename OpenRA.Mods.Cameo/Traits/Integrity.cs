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

using System;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("An ELECTRONICS pool — an actor's resistance to electrical / EMP attack. NOT a shield.",
		"Unlike a shield it is SELECTIVE: it only absorbs the damage types listed in",
		"AffectedByDamageTypes (Cameo's defaults use Tesla), and every other damage type goes",
		"straight to health as if this trait were not there. It pairs with the EMP condition bar",
		"and is what a warhead's IntegrityScale drains.",
		"⚠ Do not confuse this with OpenRA.Mods.AS's `Shielded` trait, which IS the general",
		"protective shield layer that absorbs everything. The two are separate systems: an actor",
		"can carry both, and roughly twice as many actors use Shielded as use this.",
		"(This file's descriptions were copied verbatim from Shielded and called this a shield",
		"for a long time — corrected 2026-08-12.)")]
	public class IntegrityInfo : PausableConditionalTraitInfo
	{
		[Desc("Electronics pool as a FLAT amount of damage absorbed. Added to MaxPercentageStrength.")]
		public readonly int MaxStrength = 0;

		[Desc("Electronics pool as a PERCENTAGE of the actor's max health. Added to MaxStrength.")]
		public readonly int MaxPercentageStrength = 100;

		[Desc("Flat pool the actor starts with when the trait is enabled.")]
		public readonly int InitialStrength = 0;

		[Desc("Starting pool as a PERCENTAGE of the maximum, when the trait is enabled.")]
		public readonly int InitialPercentageStrength = 100;

		[Desc("Delay in ticks before the pool recharges for the first time after the trait is enabled.")]
		public readonly int InitialRegenDelay = 0;

		[Desc("Delay in ticks after absorbing damage before the pool starts recharging again.")]
		public readonly int DamageRegenDelay = 0;

		[Desc("FLAT amount recharged each interval. Applied IN ADDITION to PercentageRegenAmount,",
			"not instead of it — set one of the two to 0 unless both are wanted.")]
		public readonly int RegenAmount = 0;

		[Desc("Percentage OF THE MAXIMUM pool recharged each interval. Applied IN ADDITION to",
			"RegenAmount (see above).")]
		public readonly int PercentageRegenAmount = 1;

		[Desc("Number of ticks between recharge steps.")]
		public readonly int RegenInterval = 25;

		[Desc("The ONLY damage types this pool absorbs. Everything else bypasses it entirely and",
			"hits health directly — this is what makes it electronics rather than a shield.")]
		public readonly BitSet<DamageType> AffectedByDamageTypes = default;

		[GrantedConditionReference]
		[Desc("Condition to grant while the electronics pool still has strength left.")]
		public readonly string ActiveCondition = null;

		[Desc("Hides the selection bar while the pool is at full strength.")]
		public readonly bool HideBarWhenFull = true;

		public readonly bool ShowSelectionBar = true;
		public readonly Color SelectionBarColor = Color.FromArgb(0, 148, 128);
		public readonly Color DisabledSelectionBarColor = Color.FromArgb(173, 216, 230);

		public override object Create(ActorInitializer init) { return new Integrity(init, this); }
	}

	public class Integrity : PausableConditionalTrait<IntegrityInfo>, ITick, ISync, ISelectionBar, INotifyDamage
	{
		int conditionToken = Actor.InvalidConditionToken;
		readonly Actor self;

		[VerifySync]
		public int Strength;
		public int MaxStrength;
		int ticks;

		IHealth health;

		public Integrity(ActorInitializer init, IntegrityInfo info)
			: base(info)
		{
			self = init.Self;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			health = self.TraitOrDefault<IHealth>();
			MaxStrength = Info.MaxStrength + Info.MaxPercentageStrength * health.MaxHP / 100;
			Strength = Info.InitialStrength + Info.InitialPercentageStrength * health.MaxHP / 100;
			if (Strength > 0 && conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.ActiveCondition);
			ticks = Info.InitialRegenDelay;
		}

		void ITick.Tick(Actor self)
		{
			Regenerate(self);
		}

		protected void Regenerate(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (Strength == MaxStrength)
				return;

			if (--ticks > 0)
				return;

			Strength += Info.RegenAmount + (Info.PercentageRegenAmount * MaxStrength / 100);

			if (Strength > MaxStrength)
				Strength = MaxStrength;

			if (Strength <= -MaxStrength)
				Strength = -MaxStrength;

			if (Strength > 0 && conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.ActiveCondition);

			ticks = Info.RegenInterval;
		}

		public void Regenerate(Actor self, int amount)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (amount < 0 && ticks < Info.DamageRegenDelay)
				ticks = Info.DamageRegenDelay;

			Strength += amount;

			if (Strength > 0 && conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.ActiveCondition);

			if (Strength <= 0 && conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);

			if (Strength <= -MaxStrength)
				Strength = -MaxStrength;
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled)
				return;

			if (e.Damage.Value == 0 || e.Attacker == self)
				return;

			if (e.Damage.Value < 0 || (!Info.AffectedByDamageTypes.IsEmpty && !e.Damage.DamageTypes.Overlaps(Info.AffectedByDamageTypes)))
				return;

			if (ticks < Info.DamageRegenDelay)
				ticks = Info.DamageRegenDelay;

			var damageAmt = Convert.ToInt32(e.Damage.Value);
			Strength -= damageAmt;

			if (Strength <= 0 && conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);

			if (Strength <= -MaxStrength)
				Strength = -MaxStrength;
		}

		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled || !Info.ShowSelectionBar || Strength == 0 || (Strength == MaxStrength && Info.HideBarWhenFull))
				return 0;

			var selected = self.World.Selection.Contains(self);
			var rollover = self.World.Selection.RolloverContains(self);
			var regularWorld = self.World.Type == WorldType.Regular;
			var statusBars = Game.Settings.Game.StatusBars;

			var displayHealth = selected || rollover || (regularWorld && statusBars == StatusBarsType.AlwaysShow)
				|| (regularWorld && statusBars == StatusBarsType.DamageShow && Strength < MaxStrength);

			if (!displayHealth)
				return 0;

			return Math.Abs((float)Strength / MaxStrength);
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		Color ISelectionBar.GetColor() { return Strength > 0 ? Info.SelectionBarColor : Info.DisabledSelectionBarColor; }

		protected override void TraitEnabled(Actor self)
		{
			ticks = Info.InitialRegenDelay;
			Strength = Info.InitialStrength;

			if (conditionToken == Actor.InvalidConditionToken && Strength > 0)
				conditionToken = self.GrantCondition(Info.ActiveCondition);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (conditionToken == Actor.InvalidConditionToken)
				return;

			conditionToken = self.RevokeCondition(conditionToken);
		}
	}
}
