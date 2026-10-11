#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>One-release alias of independent canonical engine fire stations.</summary>
	[Obsolete("Use AttackGarrisoned instead.")]
	public class AttackGarrisonedSPInfo : OpenRA.Mods.Common.Traits.AttackGarrisonedInfo
	{
		// Accepted only while loading unmigrated legacy YAML; independent targeting is unconditional.
		public readonly bool PerPassengerTargeting = true;
		public override void RulesetLoaded(Ruleset rules, ActorInfo actor)
		{
			Log.Write("debug", $"{actor.Name}: AttackGarrisonedSP is deprecated; use AttackGarrisoned.");
			base.RulesetLoaded(rules, actor);
		}
	}
}
