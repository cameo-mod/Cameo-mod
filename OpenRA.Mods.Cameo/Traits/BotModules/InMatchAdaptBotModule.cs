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

using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>The pure bounded rule — no World, no Actor, so tests drive every branch directly.</summary>
	public static class InMatchAdaptMath
	{
		/// <summary>
		/// Bias in percentage points added to the retreat/commit bar. Negative running score (losing
		/// engagements) produces a positive delta — the bar tightens, squads disengage earlier and only
		/// commit at higher advantage. Winning form loosens it toward today's value. Hard-bounded.
		/// </summary>
		public static int RetreatRatioDelta(int runningTotalMilli, int gainPermille, int maxDeltaPct)
		{
			var bound = System.Math.Abs(maxDeltaPct);
			var delta = (int)(-(long)runningTotalMilli * gainPermille / 1000);
			return System.Math.Clamp(delta, -bound, bound);
		}
	}

	[TraitLocation(SystemActors.Player)]
	[Desc("EL-1 bounded in-match adaptation (DESIGN 19.13, AI_ARCHITECTURE 12.32): reads the running total_milli",
		"of this match's own closed non-skirmish engagements (seen side only) and exposes one bounded bias on the",
		"commit/retreat bar — losing form tightens it, winning form loosens it. Resets every match because the",
		"tally does; gains are tuned knobs; issues no orders and enumerates no actors.")]
	public class InMatchAdaptBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("RetreatRatioPct bias points per 1000 milli of running engagement score (sign-flipped: losing tightens).")]
		public readonly int GainPermille = 15;

		[Desc("Hard bound on the bias, in percentage points of RetreatRatioPct.")]
		public readonly int MaxDeltaPct = 20;

		[Desc("Recompute cadence in ticks — matches the engagement posture interval.")]
		public readonly int IntervalTicks = 250;

		public override object Create(ActorInitializer init) { return new InMatchAdaptBotModule(init.Self, this); }
	}

	public class InMatchAdaptBotModule : ConditionalTrait<InMatchAdaptBotModuleInfo>, IBotInMatchAdaptation, IBotTick
	{
		readonly OpenRA.Player player;
		EngagementLogBotModule log;
		int deltaPct;

		public InMatchAdaptBotModule(Actor self, InMatchAdaptBotModuleInfo info)
			: base(info)
		{
			player = self.Owner;
		}

		public int RetreatRatioDeltaPct => IsTraitDisabled ? 0 : deltaPct;

		void IBotTick.BotTick(IBot bot)
		{
			if (IsTraitDisabled || player.World.WorldTick % Info.IntervalTicks != 0)
				return;

			log ??= player.PlayerActor.TraitOrDefault<EngagementLogBotModule>();
			deltaPct = InMatchAdaptMath.RetreatRatioDelta(log?.RunningTotalMilli ?? 0, Info.GainPermille, Info.MaxDeltaPct);
		}
	}
}
