#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Immutable;
using System.Reflection;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Projectiles;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Projectiles
{
	[Desc("A Bullet whose Inaccuracy and Speed are DERIVED from the firing weapon's own Range,",
		"so a projectile template can state the rule once instead of every weapon restating a",
		"number. Maintainer 2026-08-17: \"this should be a function for range ... the default",
		"value if you have no inaccuracy selected should be 1% of maximum range ... a field",
		"called ProjectileSpeedPercentage, and then we put 10% for Cannons and 2% for Artillery\".",
		"",
		"⚠ NOT a shadow of `Bullet`. Replacing the engine type by name would silently change",
		"EVERY weapon that says `Projectile: Bullet` (the most-used projectile in the mod);",
		"this is a separate type, so a template opts in by writing `Projectile: ScaledBullet`",
		"and everything else is untouched. It inherits BulletInfo, so every existing field",
		"(Image, Sequences, Shadow, TrailImage, InaccuracyType, ...) works exactly as before.")]
	public class ScaledBulletInfo : BulletInfo, IRulesetLoaded<WeaponInfo>
	{
		[Desc("Inaccuracy as a PERCENTAGE of the weapon's Range. 0 disables the derivation.",
			"Cannons: 1. Note `InaccuracyType` still applies on top — at its `Maximum` default",
			"the engine ALREADY scales the value with each shot's distance, so this sets the",
			"value that scaling works from, not the scaling itself.")]
		public readonly int InaccuracyPercentage = 0;

		[Desc("Projectile speed as a PERCENTAGE of the weapon's Range, in WDist/tick.",
			"0 disables the derivation. Cannons: 10 — which is what the shipped Shell templates",
			"already encode by hand (Speed 500 on a 5000-range cannon). Artillery: 2.")]
		public readonly int ProjectileSpeedPercentage = 0;

		// The base declares these `public readonly` and FieldLoader writes them by reflection at
		// load time; deriving one of them is the same operation a beat later, so it uses the same
		// mechanism rather than duplicating 367 lines of Bullet to change two numbers.
		static readonly FieldInfo InaccuracyField =
			typeof(BulletInfo).GetField(nameof(Inaccuracy), BindingFlags.Public | BindingFlags.Instance);

		static readonly FieldInfo SpeedField =
			typeof(BulletInfo).GetField(nameof(Speed), BindingFlags.Public | BindingFlags.Instance);

		void IRulesetLoaded<WeaponInfo>.RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			var range = info.Range.Length;
			if (range <= 0)
				return;

			// ⚠ AN EXPLICIT YAML VALUE ALWAYS WINS, so this is a DEFAULT and never a constraint —
			// which is what makes it safe to roll out one family at a time. "Explicit" is detected
			// as "differs from BulletInfo's own default", because FieldLoader does not report which
			// keys the yaml actually contained. The edge case is therefore real and small: a weapon
			// that deliberately writes `Inaccuracy: 0` or `Speed: 17` AND sets a percentage gets the
			// derived value. Set the percentage to 0 on that weapon if you need those exact numbers.
			if (InaccuracyPercentage > 0 && Inaccuracy == WDist.Zero)
				InaccuracyField.SetValue(this, new WDist(range * InaccuracyPercentage / 100));

			if (ProjectileSpeedPercentage > 0 && Speed.Length == 1 && Speed[0] == new WDist(17))
				SpeedField.SetValue(this, ImmutableArray.Create(
					new WDist(range * ProjectileSpeedPercentage / 100)));
		}
	}
}
