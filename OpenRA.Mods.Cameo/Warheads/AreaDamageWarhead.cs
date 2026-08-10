#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Warheads
{
	[Desc("Area-of-effect damage that can expand outward over several ticks (a shockwave or a",
		"damage-over-time cloud) and bakes in friendly fire (allies take reduced damage within a",
		"reduced radius). At defaults (Ticks: 1, MaxRadius: 0, no scheduling) it behaves exactly",
		"like SpreadDamage plus baked friendly fire. The authored Damage is the TOTAL dealt across",
		"ALL ticks, so the balance pipeline reads a single number. Replaces the SpreadDamage main",
		"warhead + its _FriendlyFire twin on the AoE families; the _Percentage and per-weapon",
		"_ExtraDamage warheads keep their own bespoke Versus and stay separate.")]
	public class AreaDamageWarhead : DamageWarhead, IRulesetLoaded<WeaponInfo>
	{
		[Desc("Range between falloff steps.")]
		public readonly WDist Spread = new(43);

		[Desc("Damage percentage at each range step.")]
		public readonly ImmutableArray<int> Falloff = [100, 37, 14, 5, 0];

		[Desc("Ranges at which each Falloff step is defined. Overrides Spread.")]
		public readonly ImmutableArray<WDist> Range = default;

		[Desc("Controls the way damage is calculated. Possible values are 'HitShape',",
			"'ClosestTargetablePosition' and 'CenterPosition'.")]
		public readonly DamageCalculationType DamageCalculationType = DamageCalculationType.HitShape;

		[Desc("Number of damage applications. 1 = a single instant hit (identical to SpreadDamage).",
			">1 spreads the TOTAL Damage across that many applications (damage over time).")]
		public readonly int Ticks = 1;

		[Desc("Delay in engine ticks between each application. 0 = every tick lands instantly.")]
		public readonly int TickDelay = 0;

		[Desc("Inner radius of the FIRST ring, used only when MaxRadius is set (the shockwave origin).")]
		public readonly WDist MinRadius = WDist.Zero;

		[Desc("Outer radius of the FINAL ring. When 0 every tick covers the full Falloff range (a",
			"static DoT cloud); when set, the damaged radius grows from MinRadius to MaxRadius across",
			"the ticks (an expanding shockwave).")]
		public readonly WDist MaxRadius = WDist.Zero;

		[Desc("Percentage of Damage dealt to ALLIED actors (baked-in friendly fire; Cameo law: 50).",
			"0 disables friendly fire entirely (allies are never hit).")]
		public readonly int FriendlyFireDamage = 50;

		[Desc("Percentage of the tick radius within which allied actors can be hit (Cameo law: 50).")]
		public readonly int FriendlyFireSpread = 50;

		[Desc("Optional PhysicalState (e.g. Temperature, Corrosion) to change on hit, SCALED BY the damage",
			"this warhead deals (unlike ApplyPhysicalStateWarhead's fixed Amount). Empty = off.")]
		public readonly string PhysicalStateName = null;

		[Desc("PhysicalState change = damage dealt x this percentage (signed): 100 = full heat/corrosion,",
			"-100 = full cold, 50 = half. 0 disables. Put on the main + _Percentage warheads, NOT the",
			"_ExtraDamage chip, so the chip is excluded and the %-twin also feeds the meter.")]
		public readonly int PhysicalStateScale = 0;

		[Desc("Multiple PhysicalState changes on one warhead, {StateName: Scale%}, applied IN ADDITION to",
			"the single PhysicalStateName/Scale above. For a blend, e.g. Plasma: Temperature: 50, Corrosion: 50.")]
		public readonly Dictionary<string, int> PhysicalStates = new();

		[Desc("Drain the victim's Integrity (shield/EMP) pool by the damage this warhead deals x this",
			"percentage, SCALED exactly like PhysicalStateScale (auto-tracks the real post-armor/falloff",
			"damage, so no flat EMP number is hand-set). Cameo Tesla-content law: Tesla 100, Storm 50,",
			"Quantum 33 (Tesla-parents / total-parents). 0 = off. Put it on the main + _Percentage warheads,",
			"NOT the _ExtraDamage chip. Stack a flat AffectsIntegrity warhead on top for upgrade bonuses,",
			"or give the upgraded weapon a higher IntegrityScale so its bonus EMP scales too.")]
		public readonly int IntegrityScale = 0;

		[Desc("Relative damage weight per tick. Length must equal Ticks (omit for an even split).",
			"Weights are NORMALISED so the total across all ticks always equals the authored Damage,",
			"keeping the balance figure a single number. For the nuclear shockwave use a DECREASING",
			"profile (e.g. 5, 4, 3, 2, 1) together with MinRadius/MaxRadius: the first ring is small",
			"and hits hard, later rings are larger and weaker. An INCREASING profile builds up instead.")]
		public readonly ImmutableArray<int> TickDamage = default;

		ImmutableArray<WDist> effectiveRange;
		int tickDamageTotal;

		void IRulesetLoaded<WeaponInfo>.RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (Range != null)
			{
				if (Range.Length != 1 && Range.Length != Falloff.Length)
					throw new YamlException("Number of range values must be 1 or equal to the number of Falloff values.");

				for (var i = 0; i < Range.Length - 1; i++)
					if (Range[i] > Range[i + 1])
						throw new YamlException("Range values must be specified in an increasing order.");

				effectiveRange = Range;
			}
			else
				effectiveRange = Exts.MakeArray(Falloff.Length, i => i * Spread).ToImmutableArray();

			if (TickDamage != null)
			{
				if (TickDamage.Length != Ticks)
					throw new YamlException("Number of TickDamage weights must equal Ticks.");

				tickDamageTotal = TickDamage.Sum();
			}
		}

		protected override void DoImpact(WPos pos, Actor firedBy, WarheadArgs args)
		{
			var world = firedBy.World;

			for (var tick = 0; tick < Ticks; tick++)
			{
				// Copy the loop variable so each scheduled lambda captures its own tick index.
				var t = tick;
				if (t == 0 || TickDelay <= 0)
					ApplyRing(world, pos, firedBy, args, t);
				else
					world.AddFrameEndTask(w => w.Add(new DelayedAction(t * TickDelay, () => ApplyRing(world, pos, firedBy, args, t))));
			}
		}

		void ApplyRing(World world, WPos pos, Actor firedBy, WarheadArgs args, int tick)
		{
			// Expanding shockwave: grow the damaged radius from MinRadius to MaxRadius across the ticks.
			// Static DoT cloud (MaxRadius == 0): every tick covers the full Falloff range.
			var outer = effectiveRange[^1];
			if (MaxRadius.Length > 0 && Ticks > 1)
				outer = new WDist(MinRadius.Length + (MaxRadius.Length - MinRadius.Length) * (tick + 1) / Ticks);

			// The authored Damage is the TOTAL across all ticks. Split it by the per-tick weights
			// (TickDamage) when given, otherwise evenly. Normalised so the ticks always sum to Damage.
			var perTickModifier = Ticks > 1 ? 100 / Ticks : 100;
			if (TickDamage != null && tickDamageTotal > 0)
				perTickModifier = 100 * TickDamage[tick] / tickDamageTotal;

			foreach (var victim in world.FindActorsOnCircle(pos, outer))
			{
				if (!IsValidAgainst(victim, firedBy))
					continue;

				var isAlly = victim.Owner.RelationshipWith(firedBy.Owner) == PlayerRelationship.Ally;
				if (isAlly && FriendlyFireDamage <= 0)
					continue;

				// Friendly fire covers only a fraction of the tick radius.
				var victimOuter = isAlly ? new WDist(outer.Length * FriendlyFireSpread / 100) : outer;

				// PERF: Avoid using TraitsImplementing<HitShape> that needs to find the actor in the trait dictionary.
				HitShape closestActiveShape = null;
				var closestDistance = int.MaxValue;

				foreach (var targetPos in victim.EnabledTargetablePositions)
				{
					if (targetPos is HitShape h)
					{
						var distance = h.DistanceFromEdge(victim, pos).Length;
						if (distance < closestDistance)
						{
							closestDistance = distance;
							closestActiveShape = h;
						}
					}
				}

				// Cannot be damaged without an active HitShape.
				if (closestActiveShape == null)
					continue;

				var falloffDistance = 0;
				switch (DamageCalculationType)
				{
					case DamageCalculationType.HitShape:
						falloffDistance = closestDistance;
						break;
					case DamageCalculationType.ClosestTargetablePosition:
						falloffDistance = victim.GetTargetablePositions().Min(x => (x - pos).Length);
						break;
					case DamageCalculationType.CenterPosition:
						falloffDistance = (victim.CenterPosition - pos).Length;
						break;
				}

				// Outside this tick's (friendly-fire-adjusted) radius: no damage.
				if (falloffDistance > victimOuter.Length)
					continue;

				var localModifiers = args.DamageModifiers.Append(GetDamageFalloff(falloffDistance)).Append(perTickModifier);
				if (isAlly)
					localModifiers = localModifiers.Append(FriendlyFireDamage);

				var impactOrientation = args.ImpactOrientation;

				// If a warhead lands outside the victim's HitShape, we need to calculate the vertical and horizontal impact angles
				// from impact position, rather than last projectile facing/angle.
				if (falloffDistance > 0)
				{
					var towardsTargetYaw = (victim.CenterPosition - args.ImpactPosition).Yaw;
					var impactAngle = Util.GetVerticalAngle(args.ImpactPosition, victim.CenterPosition);
					impactOrientation = new WRot(WAngle.Zero, impactAngle, towardsTargetYaw);
				}

				var updatedWarheadArgs = new WarheadArgs(args)
				{
					DamageModifiers = localModifiers.ToArray(),
					ImpactOrientation = impactOrientation,
				};

				InflictDamage(victim, firedBy, closestActiveShape, updatedWarheadArgs);
			}
		}

		protected override void InflictDamage(Actor victim, Actor firedBy, HitShape shape, WarheadArgs args)
		{
			var damage = Util.ApplyPercentageModifiers(Damage, args.DamageModifiers.Append(DamageVersus(victim, shape, args)));
			victim.InflictDamage(firedBy, new Damage(damage, DamageTypes, GetProjectileType(args)));
			ApplyPhysicalState(victim, firedBy, damage);
			ApplyIntegrityScale(victim, firedBy, damage);
		}

		// Scale a named PhysicalState by the damage just dealt (heat / cold / corrosion meters). Shared
		// with the _Percentage subclass so both the flat main and the %HP twin feed the meter; the
		// separate _ExtraDamage chip warhead never calls this, so it is excluded (maintainer rule).
		// ApplyChange(..., true) lets the TARGET apply its own damage modifiers, so the meter tracks the
		// final effective damage (armor + falloff already baked into `damage`).
		protected void ApplyPhysicalState(Actor victim, Actor firedBy, int damage)
		{
			if (damage == 0)
				return;

			if (!string.IsNullOrEmpty(PhysicalStateName) && PhysicalStateScale != 0)
				ApplyOneState(victim, firedBy, PhysicalStateName, damage * PhysicalStateScale / 100);

			foreach (var kv in PhysicalStates)
				if (kv.Value != 0)
					ApplyOneState(victim, firedBy, kv.Key, damage * kv.Value / 100);
		}

		static void ApplyOneState(Actor victim, Actor firedBy, string name, int change)
		{
			if (change == 0)
				return;

			var physicalState = victim.TraitsImplementing<PhysicalState>()
				.FirstOrDefault(ps => ps.Name == name);
			physicalState?.ApplyChange(change, firedBy, true);
		}

		// Drain the victim's Integrity (shield/EMP) pool proportional to the damage just dealt, the same
		// way ApplyPhysicalState scales a heat/corrosion meter. Auto-tracks the final effective damage
		// (armor + falloff already baked into `damage`), so the "EMP" self-adjusts with the weapon's
		// output and never needs a hand-set number. Shared with the _Percentage subclass so both the flat
		// main and the %HP twin drain the pool; the _ExtraDamage chip never calls this (excluded). No
		// Integrity trait on the victim (most units have no shield) => a harmless no-op.
		protected void ApplyIntegrityScale(Actor victim, Actor firedBy, int damage)
		{
			if (damage == 0 || IntegrityScale == 0)
				return;

			var change = damage * IntegrityScale / 100;
			if (change == 0)
				return;

			victim.TraitsImplementing<Integrity>()
				.FirstOrDefault(t => !t.IsTraitPaused && !t.IsTraitDisabled)
				?.Regenerate(victim, -change);
		}

		int GetDamageFalloff(int distance)
		{
			var inner = effectiveRange[0].Length;
			for (var i = 1; i < effectiveRange.Length; i++)
			{
				var outer = effectiveRange[i].Length;
				if (outer > distance)
					return int2.Lerp(Falloff[i - 1], Falloff[i], distance - inner, outer - inner);

				inner = outer;
			}

			return 0;
		}
	}
}
