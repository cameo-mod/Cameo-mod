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

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Modifies the turn speed of this turret.")]
	public class TurretTurnSpeedMultiplierInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Percentage modifier to apply.")]
		public readonly int Modifier = 100;

		[Desc("Turret types to applies to. Leave empty to apply to all turrets.")]
		public readonly HashSet<string> Types = new();

		public override object Create(ActorInitializer init) { return new TurretTurnSpeedMultiplier(this); }
	}

	public class TurretTurnSpeedMultiplier : ConditionalTrait<TurretTurnSpeedMultiplierInfo>, ITurretTurnSpeedModifier
	{
		public TurretTurnSpeedMultiplier(TurretTurnSpeedMultiplierInfo info)
			: base(info) { }

		int ITurretTurnSpeedModifier.GetTurretTurnSpeedModifier(string turretName)
		{
			return !IsTraitDisabled && (Info.Types.Count == 0 || (!string.IsNullOrEmpty(turretName) && Info.Types.Contains(turretName))) ? Info.Modifier : 100;
		}
	}
}
