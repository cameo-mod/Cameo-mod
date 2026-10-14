#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class BotOverflowPolicyInfo : ConditionalTraitInfo
	{
		[Desc("PUSH-1 (bank overflow): cash+resources above which the armed base builder preempts the",
			"planner's front/link wants with a production building pick, converting bank to producer width.",
			"Resolved per-profile like BotLimits — absent or disabled profile runs the legacy ladder",
			"byte-identically (zero extra RNG draws). 0 or negative: never armed. Candidate tuning value,",
			"not a proven band cure — the sentinel evidence judges it.")]
		public readonly int OverflowCash = 0;

		public override object Create(ActorInitializer init) { return new BotOverflowPolicy(init.Self, this); }
	}

	public class BotOverflowPolicy : ConditionalTrait<BotOverflowPolicyInfo>
	{
		public BotOverflowPolicy(Actor self, BotOverflowPolicyInfo info) : base(info)
		{
		}
	}
}
