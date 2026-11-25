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
	// TECHCHAIN-PAYGO-01 (Opus ruling A, 2026-11-25; designer rule "tech chain never blocked"). Test A6 of the staged planner requires the bank to cover
	// a start's full cost. While units drain the bank, a tech-chain building can then wait for the whole game, although the engine pays production
	// as it goes. With the switch bb_techchain_paygo on, a TechChainTypes pick from stage S3 (the tech-centre opening role) or S5 (core once) is
	// admitted with any bank above 0, while no other tech-chain item is in flight. Every other pick keeps the full-cost test. Pure, no world.
	public static class TechChainPaygo
	{
		/// <summary>The DEV pipeline_a6_hold row is written at most once per building name per this many ticks.</summary>
		public const int HoldLogIntervalTicks = 750;

		/// <summary>The stages whose tech-chain picks may start below their full cost.</summary>
		public static bool StageWaives(BaseBuildStage stage) => stage == BaseBuildStage.S3Opening || stage == BaseBuildStage.S5CoreOnce;

		/// <summary>True = test A6 is waived for this tech-chain start: an eligible stage, a bank above 0 and no tech-chain item in flight.
		/// The caller has already checked that the switch is armed and that the start is a TechChainTypes building.</summary>
		public static bool Waives(BaseBuildStage stage, int bank, int techChainInFlight) =>
			bank > 0 && techChainInFlight <= 0 && StageWaives(stage);

		/// <summary>The hold row for a name is due when none was written yet (lastLoggedTick &lt; 0) or the interval has passed.</summary>
		public static bool HoldLogDue(int lastLoggedTick, int now) => lastLoggedTick < 0 || now - lastLoggedTick >= HoldLogIntervalTicks;
	}
}
