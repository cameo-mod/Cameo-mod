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
	[Desc("Recharges or drains a DamageLayer's pool over time. Attach several (with different conditions)",
		"to the same layer for charge / discharge behaviour. Targets the layer whose Name matches Layer.",
		"Replaces ChangesShield.")]
	sealed class ChangesDamageLayerInfo : ConditionalTraitInfo, Requires<DamageLayerInfo>
	{
		[Desc("Name of the DamageLayer to modify (matches DamageLayer.Name).")]
		public readonly string Layer = "shield";

		[Desc("Absolute amount of strength added in each step.",
			"Use negative values to drain.")]
		public readonly int Step = 5;

		[Desc("Relative percentage of the layer's max strength added in each step.",
			"Use negative values to drain.",
			"When both values are defined, their sum is applied.")]
		public readonly int PercentageStep = 0;

		[Desc("Time in ticks to wait between each strength modification.")]
		public readonly int Delay = 5;

		[Desc("Only recharge while the layer's strength is below this percentage of its max.")]
		public readonly int StartIfBelow = 50;

		[Desc("Time in ticks to wait after taking damage.")]
		public readonly int DamageCooldown = 0;

		public override object Create(ActorInitializer init) { return new ChangesDamageLayer(init.Self, this); }
	}

	sealed class ChangesDamageLayer : ConditionalTrait<ChangesDamageLayerInfo>, ITick, INotifyDamage, ISync
	{
		readonly DamageLayer layer;

		[VerifySync]
		int ticks;

		[VerifySync]
		int damageTicks;

		public ChangesDamageLayer(Actor self, ChangesDamageLayerInfo info)
			: base(info)
		{
			var layers = self.TraitsImplementing<DamageLayer>();

			// Prefer the layer named to match; otherwise fall back to the only layer present
			// (covers the common single-layer case where both names keep their defaults).
			layer = layers.FirstOrDefault(l => l.Info.Name == Info.Layer) ?? layers.FirstOrDefault();
		}

		void ITick.Tick(Actor self)
		{
			if (self.IsDead || IsTraitDisabled || layer == null)
				return;

			// Cast to long to avoid overflow when multiplying by the strength.
			if (layer.Strength >= Info.StartIfBelow * (long)layer.MaxStrength / 100)
				return;

			if (damageTicks > 0)
			{
				--damageTicks;
				return;
			}

			if (--ticks <= 0)
			{
				ticks = Info.Delay;
				layer.Regenerate(self, Info.Step + Info.PercentageStep * layer.MaxStrength / 100);
			}
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage.Value > 0)
				damageTicks = Info.DamageCooldown;
		}
	}
}
