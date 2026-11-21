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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// What the bot KNOWS about an area, from its own observations only (ENGDROP_IMPL_SPEC_02). Known is true only when every
	/// sampled cell across the target, its approach and its landing has a recent ACTUAL visibility stamp (a cell the bot saw,
	/// not a region centre and not a hidden-world read); missing, truncated or stale coverage is unknown, never "safe".
	/// The values are the remembered/observed threat inside the sampled area, at public ruleset cost.
	/// </summary>
	public readonly record struct AreaIntel(
		bool Known,
		int DefenceValue,
		int ArmyValue,
		int AntiAirValue,
		int MaxDefenceRangeCells,
		int OldestObservedTick,
		int Detectors)
	{
		/// <summary>Known coverage and zero observed or remembered defence, ground army and anti-air: the Trigger A bar.</summary>
		public bool Undefended => Known && DefenceValue == 0 && ArmyValue == 0 && AntiAirValue == 0;

		public int ThreatValue => DefenceValue + ArmyValue + AntiAirValue;
	}

	public interface IBotAreaIntelProvider
	{
		/// <summary>
		/// The bot's knowledge of the area around <paramref name="center"/> (radius in cells) plus the given approach and landing
		/// cells, judged at <paramref name="nowTick"/> with visibility stamps no older than <paramref name="freshTicks"/>.
		/// </summary>
		AreaIntel Assess(CPos center, int radiusCells, IReadOnlyList<CPos> approach, IReadOnlyList<CPos> landing, int nowTick, int freshTicks);
	}
}
