#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Warheads
{
	[Desc("Percentage-of-max-health version of AreaDamage: the SAME expanding-ring / damage-over-time",
		"/ baked friendly-fire spatial pass, but each hit removes a PERCENTAGE of the victim's maximum",
		"health instead of a flat amount (Damage is the percentage, scaled by Falloff, per-tick weight",
		"and Versus). One of these with a Falloff gradient replaces a whole stack of concentric",
		"HealthPercentageDamage rings (the old nuke '% big bang' bandaid). Set UpdatesUnitStatistics:",
		"false so it is not counted for unit damage stats.")]
	public class AreaDamagePercentageWarhead : AreaDamageWarhead
	{
		protected override void InflictDamage(Actor victim, Actor firedBy, HitShape shape, WarheadArgs args)
		{
			var healthInfo = victim.Info.TraitInfo<HealthInfo>();
			var damage = Util.ApplyPercentageModifiers(healthInfo.HP, args.DamageModifiers.Append(Damage, DamageVersus(victim, shape, args)));
			victim.InflictDamage(firedBy, new Damage(damage, DamageTypes, GetProjectileType(args)));
		}
	}
}
