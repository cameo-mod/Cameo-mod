#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>
	/// AR-S residual 2 (BL_protection_episode_guard, 2026-10-04): consecutive-eval hysteresis
	/// on the protection lure decision. The emitter attribution showed the surviving flap is
	/// engage-vs-lure inside the protection tick: the combat predictor's verdict flickers at
	/// the fog edge, so the squad alternates Move-to-rally and AttackMove-to-target every eval.
	/// The dedup key cannot suppress it — the targets differ.
	///
	/// The episode enters on <c>enterConfirm</c> consecutive losing evals and aborts on
	/// <c>abortConfirm</c> consecutive non-losing evals. A squad not in a lure engages the
	/// moment it sees a winnable enemy — engage entry is never confirmed, so real fights are
	/// not delayed; only aborting an in-flight retreat asks for confirmation.
	/// </summary>
	public struct ProtectionEpisode
	{
		public bool Luring;
		public int LossStreak;
		public int WinStreak;

		/// <summary>The pure rule. All branches pinned by ProtectionEpisodeGuardTest.</summary>
		public ProtectionEpisode Eval(bool wantsLure, int enterConfirm, int abortConfirm)
		{
			var loss = wantsLure ? LossStreak + 1 : 0;
			var win = wantsLure ? 0 : WinStreak + 1;
			var luring = Luring ? win < abortConfirm : loss >= enterConfirm;
			return new ProtectionEpisode { Luring = luring, LossStreak = loss, WinStreak = win };
		}
	}
}
