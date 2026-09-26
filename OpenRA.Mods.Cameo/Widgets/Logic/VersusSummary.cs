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

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cameo.Widgets.Logic
{
	/// <summary>
	/// What a unit's weapons do to each armour class, read from the RESOLVED rules at runtime, so the
	/// production tooltip can never disagree with the warheads (maintainer ruling 2026-09-23, ROADMAP
	/// "three ideas from Combined Arms' damage model").
	///
	/// The armour ladders are DESIGN §12.0d's, each ordered lightest -> heaviest. `Heroic` is left out:
	/// it is a DERIVED cell (§12.0b), not a rung. Protection layers (Shield, HAZMAT, ...) are not
	/// ladders either.
	/// </summary>
	public sealed class VersusSummary
	{
		public enum LadderKind { Infantry, Vehicles, Buildings, Aircraft }

		public static readonly (LadderKind Kind, string[] Rungs, string[] TargetTypes)[] Ladders =
		[
			(LadderKind.Infantry, ["None", "Flak", "Plate"], ["Ground", "Infantry"]),
			(LadderKind.Vehicles, ["Scout", "Light", "Medium", "Heavy", "Superheavy"], ["Ground", "Water", "Vehicle"]),
			(LadderKind.Buildings, ["Wood", "Steel", "Concrete"], ["Ground", "Structure"]),
			(LadderKind.Aircraft, ["Fighter", "Helicopter", "Bomber", "Spaceship"], ["Air"]),
		];

		/// <summary>A profile whose strongest rung beats its weakest by less than this is FLAT (§12.0d "Super").</summary>
		const int FlatRatioPercent = 115;

		public readonly struct Ladder
		{
			public readonly LadderKind Kind;

			/// <summary>Null when no weapon of the unit can target this ladder.</summary>
			public readonly (string Armor, int Percent)[] Rungs;

			public Ladder(LadderKind kind, (string, int)[] rungs)
			{
				Kind = kind;
				Rungs = rungs;
			}

			public bool CanAttack => Rungs != null;
		}

		public readonly Ladder[] Rows;

		/// <summary>
		/// The vehicle ladder peaks on its HEAVIEST rungs (`Heavy`/`Superheavy`, §12.0d's heavy end) and is
		/// not flat. Maintainer 2026-09-23: show "Armor Piercing" for anti-heavy, and no tag otherwise.
		/// </summary>
		public readonly bool ArmorPiercing;

		/// <summary>Target DOMAINS any of the unit's weapons can hit, in <see cref="Domains"/> order.</summary>
		public readonly string[] Targets;

		/// <summary>The target types shown on the tooltip's "Targets" line, in display order.</summary>
		public static readonly string[] Domains = ["Ground", "Water", "Underwater", "Air"];

		/// <summary>Ground target classes, listed only when a unit cannot hit `Ground` as a whole.</summary>
		public static readonly string[] GroundClasses = ["Infantry", "Vehicle", "Structure"];

		public bool HasWeapons => Rows.Any(r => r.CanAttack);

		VersusSummary(Ladder[] rows, bool armorPiercing, string[] targets)
		{
			Rows = rows;
			ArmorPiercing = armorPiercing;
			Targets = targets;
		}

		public static VersusSummary For(ActorInfo actor, Ruleset rules)
		{
			var weapons = Weapons(actor, rules);
			var rows = new Ladder[Ladders.Length];
			var armorPiercing = false;

			for (var i = 0; i < Ladders.Length; i++)
			{
				var (kind, rungs, targetTypes) = Ladders[i];

				// The strongest weapon that can hit this ladder speaks for it: a sidearm's profile must not
				// mask the main gun's.
				var best = weapons
					.Where(w => CanTarget(w.Weapon, targetTypes))
					.OrderByDescending(w => w.Potency)
					.FirstOrDefault();

				if (best.Weapon == null)
				{
					rows[i] = new Ladder(kind, null);
					continue;
				}

				var values = rungs.Select(r => (r, best.Warhead.Versus.GetValueOrDefault(r, 100))).ToArray();
				rows[i] = new Ladder(kind, values);

				if (kind == LadderKind.Vehicles)
				{
					var max = values.Max(v => v.Item2);
					var min = values.Min(v => v.Item2);
					var peak = System.Array.FindLastIndex(values, v => v.Item2 == max);
					armorPiercing = max * 100 > min * FlatRatioPercent && peak >= rungs.Length - 2;
				}
			}

			// Domains first; a weapon that cannot hit `Ground` as a whole (a sniper rifle targets `Infantry`)
			// names the ground classes it CAN hit instead, so the line is never empty for an armed unit.
			var targets = Domains.Where(d => weapons.Any(w => CanTarget(w.Weapon, [d]))).ToList();
			if (!targets.Contains("Ground"))
				targets.InsertRange(0, GroundClasses.Where(c => weapons.Any(w => CanTarget(w.Weapon, [c]))));
			return new VersusSummary(rows, armorPiercing, targets.ToArray());
		}

		static List<(WeaponInfo Weapon, DamageWarhead Warhead, long Potency)> Weapons(ActorInfo actor, Ruleset rules)
		{
			var armaments = actor.TraitInfos<ArmamentInfo>().ToList();
			var innate = armaments.Where(a => a.EnabledByDefault).ToList();

			// A unit whose every armament is condition-gated (deploy modes, garrisons) still has weapons.
			var chosen = innate.Count > 0 ? innate : armaments;
			var result = new List<(WeaponInfo, DamageWarhead, long)>();
			foreach (var a in chosen)
			{
				if (a.Weapon == null || !rules.Weapons.TryGetValue(a.Weapon.ToLowerInvariant(), out var weapon))
					continue;

				// W24: one MAIN damage warhead per weapon. Where a weapon still stacks several, the largest
				// is its main one; twins and effect warheads are smaller or deal no damage.
				var main = weapon.Warheads.OfType<DamageWarhead>().Where(d => d.Damage > 0)
					.OrderByDescending(d => d.Damage).FirstOrDefault();
				if (main == null)
					continue;

				var potency = (long)main.Damage * weapon.Burst * 1000 / System.Math.Max(1, weapon.ReloadDelay);
				result.Add((weapon, main, potency));
			}

			return result;
		}

		static bool CanTarget(WeaponInfo weapon, string[] targetTypes)
		{
			return targetTypes.Any(t => weapon.ValidTargets.Contains(t) && !weapon.InvalidTargets.Contains(t));
		}

		/// <summary>Colour band for one percentage. MEAN-100 profiles centre on 100 (§12.0h).</summary>
		public static Color ColorFor(int percent)
		{
			if (percent >= 150)
				return Color.FromArgb(0x33, 0xEE, 0x55);
			if (percent >= 115)
				return Color.FromArgb(0x99, 0xDD, 0x66);
			if (percent >= 85)
				return Color.FromArgb(0xDD, 0xDD, 0xDD);
			if (percent >= 50)
				return Color.FromArgb(0xFF, 0xAA, 0x33);
			return Color.FromArgb(0xEE, 0x55, 0x55);
		}

		/// <summary>Rungs this unit is strong against (>= StrongPercent), in ladder order.</summary>
		public IEnumerable<string> Strong(int strongPercent = 130)
		{
			return Rows.Where(r => r.CanAttack).SelectMany(r => r.Rungs).Where(v => v.Percent >= strongPercent).Select(v => v.Armor);
		}

		/// <summary>Rungs this unit is weak against (&lt;= WeakPercent), in ladder order.</summary>
		public IEnumerable<string> Weak(int weakPercent = 50)
		{
			return Rows.Where(r => r.CanAttack).SelectMany(r => r.Rungs).Where(v => v.Percent <= weakPercent).Select(v => v.Armor);
		}

		public IEnumerable<LadderKind> CannotAttack()
		{
			return Rows.Where(r => !r.CanAttack).Select(r => r.Kind);
		}
	}
}
