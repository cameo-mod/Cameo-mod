#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>The nine learned numbers of the harvester raid (RAID-HARV-01 design 02 section 6, delta 02 section 1).</summary>
	public enum RaidKey
	{
		LostRecon,
		MoveRefresh,
		ReqMargin,
		ReconRadius,
		IncidentLoss,
		IncidentWindow,
		IncidentDecay,
		WaypointSpacing,
		DefenceRadius,
	}

	/// <summary>
	/// One key's declaration: its stable id, the PRIOR, the clamp and the read quantum, all in milli-units (value x 1000, positive, never zero). The learner (the shared LA02 core, P3) keeps the full-scale estimate in this fixed
	/// point and moves it with <see cref="RaidHarvKeys.GainNumerator"/>/<see cref="RaidHarvKeys.GainDenominator"/> and <see cref="RaidHarvKeys.StepMaxPermille"/>; this file holds only the declaration, no step, no aggregate, no root.
	/// </summary>
	public readonly record struct RaidKeySpec(RaidKey Key, string Id, string Unit, int PriorMilli, int MinMilli, int MaxMilli, int QuantumMilli);

	/// <summary>
	/// The source of the raid numbers for one module instance, in whole units (ticks, cells or percent). P1 returns the quantized PRIOR; the learned implementation (the accepted snapshot of the shared core) is wired in P3.
	/// Normal play reads the last accepted snapshot; an official build never trains (DEV only).
	/// </summary>
	public interface IRaidKeyValues
	{
		int Read(RaidKey key);
	}

	/// <summary>The declared parameters of the nine keys, the read-side quantizer and the prior source. Integer only; no private learning arithmetic lives here.</summary>
	public static class RaidHarvKeys
	{
		/// <summary>Every key is stored as value x 1000 (positive fixed point), so a repeated small step is never lost to rounding; only the READ rounds.</summary>
		public const int Milli = 1000;

		/// <summary>The learner's gain on the relative error: 1/8.</summary>
		public const int GainNumerator = 1;
		public const int GainDenominator = 8;

		/// <summary>The cap of one game's step, permille of the CURRENT value (5 percent), not of the clamp span.</summary>
		public const int StepMaxPermille = 50;

		/// <summary>The clip of a signal's relative error, permille.</summary>
		public const int ErrorClipPermille = 300;

		/// <summary>The size of the categorical signals' error (up or down), permille.</summary>
		public const int CategoricalPermille = 200;

		/// <summary>The declared keys, in <see cref="RaidKey"/> order.</summary>
		public static readonly IReadOnlyList<RaidKeySpec> All = new[]
		{
			new RaidKeySpec(RaidKey.LostRecon, "raid.lost_recon", "ticks", 400 * Milli, 100 * Milli, 1200 * Milli, Milli),
			new RaidKeySpec(RaidKey.MoveRefresh, "raid.move_refresh", "ticks", 125 * Milli, 25 * Milli, 300 * Milli, Milli),
			new RaidKeySpec(RaidKey.ReqMargin, "raid.req_margin", "percent", 150 * Milli, 100 * Milli, 300 * Milli, Milli),
			new RaidKeySpec(RaidKey.ReconRadius, "raid.recon_radius", "cells", 8 * Milli, 3 * Milli, 16 * Milli, Milli),
			new RaidKeySpec(RaidKey.IncidentLoss, "raid.incident_loss", "percent", 25 * Milli, 10 * Milli, 60 * Milli, Milli),
			new RaidKeySpec(RaidKey.IncidentWindow, "raid.incident_window", "ticks", 300 * Milli, 100 * Milli, 900 * Milli, Milli),
			new RaidKeySpec(RaidKey.IncidentDecay, "raid.incident_decay", "ticks", 3000 * Milli, 600 * Milli, 9000 * Milli, Milli),
			new RaidKeySpec(RaidKey.WaypointSpacing, "raid.waypoint_spacing", "cells", 8 * Milli, 4 * Milli, 16 * Milli, Milli),
			new RaidKeySpec(RaidKey.DefenceRadius, "raid.defence_radius", "cells", 6 * Milli, 3 * Milli, 12 * Milli, Milli),
		};

		public static RaidKeySpec Spec(RaidKey key) => All[(int)key];

		/// <summary>A value in milli-units clamped to the key's clamp.</summary>
		public static int ClampMilli(RaidKey key, long milli)
		{
			var spec = Spec(key);
			return (int)Math.Clamp(milli, spec.MinMilli, spec.MaxMilli);
		}

		/// <summary>
		/// The read: clamp, then round to the nearest quantum (half up), in whole units. Sub-quantum progress of the fixed-point estimate is kept by the learner and only becomes visible here
		/// (a 6.6-cell radius reads 7), so a repeated small signal never rounds to zero.
		/// </summary>
		public static int Read(RaidKey key, long milli)
		{
			var spec = Spec(key);
			var clamped = ClampMilli(key, milli);
			var quantum = Math.Max(1, spec.QuantumMilli);
			var units = ((long)clamped + (quantum / 2)) / quantum;
			var min = (spec.MinMilli + quantum - 1) / quantum;
			var max = spec.MaxMilli / quantum;
			return (int)Math.Clamp(units, min, max);
		}
	}

	/// <summary>The P1 source of the raid numbers: the yaml PRIORS of one module instance, clamped and quantized. Nothing learns here.</summary>
	public sealed class RaidKeyPriors : IRaidKeyValues
	{
		readonly int[] values;

		/// <summary>The priors in whole units, in <see cref="RaidKey"/> order (a missing entry takes the declared prior).</summary>
		public RaidKeyPriors(IReadOnlyList<int> priorUnits)
		{
			values = new int[RaidHarvKeys.All.Count];
			for (var i = 0; i < values.Length; i++)
			{
				var spec = RaidHarvKeys.All[i];
				var units = priorUnits != null && i < priorUnits.Count ? priorUnits[i] : spec.PriorMilli / RaidHarvKeys.Milli;
				values[i] = RaidHarvKeys.Read(spec.Key, (long)units * RaidHarvKeys.Milli);
			}
		}

		/// <summary>The yaml priors of one module instance (the Info keys, in <see cref="RaidKey"/> order).</summary>
		public static RaidKeyPriors FromInfo(SquadManagerBotModuleCAInfo info) => new(new[]
		{
			info.RaidHarvLostReconTicks,
			info.RaidHarvMoveRefreshTicks,
			info.RaidHarvReqMarginPercent,
			info.RaidHarvReconRadiusCells,
			info.RaidHarvIncidentLossPercent,
			info.RaidHarvIncidentWindowTicks,
			info.RaidHarvIncidentDecayTicks,
			info.RaidHarvWaypointSpacingCells,
			info.RaidHarvDefenceRadiusCells,
		});

		public int Read(RaidKey key) => values[(int)key];
	}
}
