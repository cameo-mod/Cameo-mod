#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// LEARN-ALL-02: the generic release READ seam of the learned knob table. A consumer (squad manager, unit builder, fog memory ...) asks for the decision value
	/// of one named knob in the bot's current match-up and gets the four-axis result (G x F_own x E_eff x S_obs)^(1/4) in per-mille (1000 = neutral).
	/// Implemented on the player actor by a mod that owns the accepted snapshot (Cameo: LearnAllBotModule). No provider, the switch off, the knob ablated or a knob the
	/// snapshot does not hold = false, and the consumer uses its own prior. It never writes and never learns: training lives in the DEV build only.
	/// </summary>
	public interface IBotLearnedKnobs
	{
		bool TryResolvePermille(string knob, out int permille);
	}
}
