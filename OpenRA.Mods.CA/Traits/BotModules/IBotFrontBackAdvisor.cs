#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// The placement classes of the front/back rule (DESIGN §19.15, BP-SPEC §2). One placement owner per class:
	/// refineries keep the refinery law, defences keep <see cref="IBotDefensePlacementAdvisor"/>, crawl keeps the
	/// expansion-target aim; the advisor only places Radar, Production and Valuable and labels the rest for logging.
	/// </summary>
	public enum FrontBackClass
	{
		Building = 0,
		Refinery = 1,
		Radar = 2,
		Defence = 3,
		Production = 4,
		Valuable = 5,
		Crawl = 6,
	}

	/// <summary>
	/// The advisor's answer to one class placement. <see cref="Cell"/> null with <see cref="Hold"/> false leaves the
	/// caller's own fallback; <see cref="Hold"/> true means no legal cell exists AND the caller must not use its own
	/// fallback (a radar on a front with no defence line waits — never goes forward).
	/// </summary>
	public readonly struct FrontBackPick
	{
		public readonly CPos? Cell;
		public readonly bool Hold;
		public readonly int FrontId;
		public readonly int FrontBackScore;
		public readonly int NewCoverageCells;
		public readonly int OverlapCells;
		public readonly int SetbackCells;

		public FrontBackPick(CPos? cell, bool hold, int frontId, int frontBackScore, int newCoverageCells, int overlapCells, int setbackCells)
		{
			Cell = cell;
			Hold = hold;
			FrontId = frontId;
			FrontBackScore = frontBackScore;
			NewCoverageCells = newCoverageCells;
			OverlapCells = overlapCells;
			SetbackCells = setbackCells;
		}

		public static FrontBackPick None => new(null, false, -1, 0, 0, 0, 0);
	}

	/// <summary>
	/// Lets a planner own the front/back placement classes (radar per front behind the defence line, production at
	/// the front except air-only producers, tech/superweapon/passive-income buildings in the back). With no ACTIVE
	/// advisor the base builder keeps today's placement, draw for draw. Advisors live in OpenRA.Mods.Cameo and must
	/// not be referenced by name here.
	/// </summary>
	public interface IBotFrontBackAdvisor
	{
		/// <summary>Whether this advisor is enabled; an inactive advisor is never asked.</summary>
		bool IsActive { get; }

		/// <summary>
		/// The class <paramref name="info"/> belongs to (rules-derived; <see cref="FrontBackClass.Building"/> is the
		/// residue). The caller decides which classes the advisor places — it only answers for
		/// Radar/Production/Valuable; the other labels exist for the placement log.
		/// </summary>
		FrontBackClass Classify(ActorInfo info);

		/// <summary>
		/// Pick one cell for the class out of <paramref name="candidates"/>, which contains ONLY cells that already
		/// passed the caller's placement checks (CanPlaceBuilding, IsCloseEnoughToBase, gap). See <see cref="FrontBackPick"/>.
		/// </summary>
		FrontBackPick ChooseCell(FrontBackClass cls, ActorInfo building, CPos baseCenter, IReadOnlyList<CPos> candidates);

		/// <summary>Enabled radar providers wanted: one per defended front plus justified extras (0 with no line).</summary>
		int WantedRadarProviders { get; }

		/// <summary>Excess power the base builder should hold so owned/planned radars are never blinded.</summary>
		int RadarPowerMargin { get; }

		/// <summary>The radar building to queue, preferring one whose production queues do not collide with owned producers.</summary>
		ActorInfo PreferredRadarProvider(IReadOnlyList<ActorInfo> candidates);

		// --- diagnostics for the situation log ---
		int FrontCount { get; }
		int FrontsWithoutRadar { get; }
		int RadarUnionCells { get; }
		int RadarApproachCells { get; }
	}
}
