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
	[Desc("Coordinates an actor's DamageLayer pools. Implements the engine's existing IPhysicalStateShield",
		"damage hook, so layered damage absorption works with no engine changes: incoming damage cascades",
		"through the active layers outermost-first (highest Priority) and only the overflow reaches Health.",
		"Attach once to any actor that carries one or more DamageLayer traits.")]
	public class DamageLayerManagerInfo : TraitInfo, Requires<DamageLayerInfo>
	{
		public override object Create(ActorInitializer init) { return new DamageLayerManager(); }
	}

	public class DamageLayerManager : IPhysicalStateShield, INotifyCreated
	{
		DamageLayer[] layers;

		void INotifyCreated.Created(Actor self)
		{
			// Cache every layer once, ordered outermost-first. Activation (enabled / strength > 0)
			// is re-checked per hit inside DamageLayer.Absorb, so disabled layers are simply skipped.
			layers = self.TraitsImplementing<DamageLayer>()
				.OrderByDescending(l => l.Priority)
				.ToArray();
		}

		Damage IPhysicalStateShield.AbsorbDamage(Actor self, Actor attacker, Damage damage)
		{
			if (layers == null || layers.Length == 0 || damage.Value <= 0 || attacker == self)
				return damage;

			var remaining = damage.Value;
			foreach (var layer in layers)
			{
				remaining = layer.Absorb(remaining, damage.DamageTypes);
				if (remaining <= 0)
					return new Damage(0, damage.DamageTypes, damage.ProjectileType);
			}

			if (remaining == damage.Value)
				return damage;

			return new Damage(remaining, damage.DamageTypes, damage.ProjectileType);
		}
	}
}
