#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("An absorbing layer with its own health pool that sits above the actor's Health (and above any",
		"lower-priority layers). Damage drains this pool first; only the overflow continues inward. Stack",
		"several with different Priority for shields, ablative armor, etc. Replaces the old Shielded trait;",
		"unlike it, multiple layers coexist instead of overwriting each other. Requires a DamageLayerManager",
		"on the actor, which drains the layers through the engine's existing IPhysicalStateShield damage hook",
		"(so no engine change is needed).")]
	public class DamageLayerInfo : PausableConditionalTraitInfo
	{
		[Desc("Identifier so ChangesDamageLayer (and future traits) can target this specific layer.")]
		public readonly string Name = "shield";

		[Desc("Layers with a higher Priority absorb first (outermost). Damage that exceeds this layer's",
			"pool cascades to the next-lower layer and finally to Health.")]
		public readonly int Priority = 0;

		[Desc("The strength of the layer (amount of damage it will absorb).")]
		public readonly int MaxStrength = 1000;

		[Desc("The strength of the layer (amount of damage it will absorb) in percentage of health.")]
		public readonly int MaxPercentageStrength = 0;

		[Desc("Strength of the layer when the trait is enabled.")]
		public readonly int InitialStrength = 1000;

		[Desc("The strength of the layer (amount of damage it will absorb) in percentage of health.")]
		public readonly int InitialPercentageStrength = 0;

		[Desc("Delay in ticks before the layer regenerates for the first time after the trait is enabled.")]
		public readonly int InitialRegenDelay = 0;

		[Desc("Delay in ticks after absorbing damage before the layer will regenerate.")]
		public readonly int DamageRegenDelay = 0;

		[Desc("Amount to recharge at each interval.")]
		public readonly int RegenAmount = 0;

		[Desc("Number of ticks between recharging.")]
		public readonly int RegenInterval = 25;

		[Desc("Block the remaining damage after the layer breaks (overflow does not cascade inward).")]
		public readonly bool BlockExcessDamage = false;

		[Desc("Damage types that ignore this layer (pass straight through to the next layer).")]
		public readonly BitSet<DamageType> IgnoreShieldDamageTypes = default;

		[GrantedConditionReference]
		[Desc("Condition to grant while the layer has strength remaining.")]
		public readonly string ShieldsUpCondition = null;

		[Desc("Hides the selection bar when the layer is at max strength.")]
		public readonly bool HideBarWhenFull = false;

		public readonly bool ShowSelectionBar = true;
		public readonly Color SelectionBarColor = Color.FromArgb(128, 200, 255);

		public override object Create(ActorInitializer init) { return new DamageLayer(init, this); }
	}

	public class DamageLayer : PausableConditionalTrait<DamageLayerInfo>, ITick, ISync, ISelectionBar
	{
		int conditionToken = Actor.InvalidConditionToken;
		readonly Actor self;

		[VerifySync]
		public int Strength;
		public int MaxStrength;
		public int InitialStrength;
		int ticks;

		IHealth health;

		public DamageLayer(ActorInitializer init, DamageLayerInfo info)
			: base(info)
		{
			self = init.Self;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			health = self.TraitOrDefault<IHealth>();
			MaxStrength = Info.MaxStrength + Info.MaxPercentageStrength * health.MaxHP / 100;
			InitialStrength = Info.InitialStrength + Info.InitialPercentageStrength * health.MaxHP / 100;
			Strength = InitialStrength;
			ticks = Info.InitialRegenDelay;
		}

		public int Priority => Info.Priority;

		public bool IsActive => !IsTraitDisabled && !IsTraitPaused && Strength > 0;

		// Drains this layer's pool by up to the incoming damage and returns the overflow that
		// should cascade to the next layer (or Health). Called by DamageLayerManager.
		public int Absorb(int damageValue, BitSet<DamageType> damageTypes)
		{
			if (!IsActive || damageValue <= 0)
				return damageValue;

			if (!Info.IgnoreShieldDamageTypes.IsEmpty && damageTypes.Overlaps(Info.IgnoreShieldDamageTypes))
				return damageValue;

			// Pause regeneration for a while after taking a hit.
			if (ticks < Info.DamageRegenDelay)
				ticks = Info.DamageRegenDelay;

			var absorbed = Math.Min(Strength, damageValue);
			Strength -= absorbed;

			if (Strength <= 0 && conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);

			return Info.BlockExcessDamage ? 0 : damageValue - absorbed;
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

			Strength += Info.RegenAmount;

			if (Strength > MaxStrength)
				Strength = MaxStrength;

			if (Strength > 0 && conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.ShieldsUpCondition);

			ticks = Info.RegenInterval;
		}

		public void Regenerate(Actor self, int amount)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			Strength += amount;

			if (Strength > MaxStrength)
				Strength = MaxStrength;

			if (Strength > 0 && conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.ShieldsUpCondition);

			if (Strength <= 0 && conditionToken != Actor.InvalidConditionToken)
			{
				Strength = 0;
				conditionToken = self.RevokeCondition(conditionToken);
			}
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

			return (float)Strength / MaxStrength;
		}

		bool ISelectionBar.DisplayWhenEmpty => false;

		Color ISelectionBar.GetColor() { return Info.SelectionBarColor; }

		protected override void TraitEnabled(Actor self)
		{
			ticks = Info.InitialRegenDelay;
			Strength = InitialStrength;

			if (conditionToken == Actor.InvalidConditionToken && Strength > 0)
				conditionToken = self.GrantCondition(Info.ShieldsUpCondition);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (conditionToken == Actor.InvalidConditionToken)
				return;

			conditionToken = self.RevokeCondition(conditionToken);
		}
	}
}
