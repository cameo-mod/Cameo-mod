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

using System;
using System.Collections.Generic;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// B4 OBSERVED-STYLE (DESIGN_OBSERVED_STYLE_02 section 1): the typed fog-only input of the style classifier.
	/// One remembered apparent enemy actor, projected at observation time.
	/// The field set is an exact allowlist, pinned by a reflection test: adding a field fails the test and forces a review.
	/// Never carried: EnemyProfile, Alive, WinState, a player or faction name, any Actor or Player reference, any trait query on an enemy,
	/// KnownRegions, order streams, unit-id order, bot cadence. ActorId is a tracker key only and is never logged.
	/// </summary>
	public struct FogStyleActor
	{
		/// <summary>The tracker key only; never logged, never a feature.</summary>
		public uint ActorId;

		/// <summary>The static ActorInfo name (ruleset data). Carried for the tracker and debugging; no feature reads it (pinned).</summary>
		public string TypeName;

		public CPos Cell;
		public int LastSeenTick;

		/// <summary>The static cost of the type (ruleset data).</summary>
		public int Value;

		public bool Building;
		public bool BaseBuilding;
		public bool Combat;
		public bool Defence;
		public bool Tech;
		public bool Production;
		public bool Refinery;
		public bool Harvester;
		public bool Mcv;
		public bool Aircraft;
		public bool ProducesAircraft;
		public bool AntiAir;
		public bool Infantry;
		public bool Vehicle;
		public bool Naval;
		public bool Cloaked;
	}

	/// <summary>
	/// Copies only the viewer-facing appearance stored at observation time.
	/// </summary>
	internal static class FogStyleAdapter
	{
		internal static int Copy(IReadOnlyCollection<ObservedActor> remembered, Span<FogStyleActor> buffer) => Copy(remembered, buffer, out _);

		/// <summary>The bounded copy (OBS-STYLE-PROVIDER-01, design 03 "bound work to 128 records per visit with stable recency and actor-ID ordering"):
		/// up to buffer.Length appearances. When more are remembered, the most recent are kept (newest LastSeenTick first, the lower actor id on a tie)
		/// and <paramref name="truncated"/> is set. The result never depends on the memory's enumeration order once truncated, and nothing is allocated.</summary>
		internal static int Copy(IReadOnlyCollection<ObservedActor> remembered, Span<FogStyleActor> buffer, out bool truncated)
		{
			truncated = false;
			if (remembered == null || buffer.Length == 0)
				return 0;

			var count = 0;
			var worst = -1;
			foreach (var observation in remembered)
			{
				if (observation.StyleAppearance is not FogStyleActor appearance)
					continue;

				if (count < buffer.Length)
				{
					buffer[count++] = appearance;
					continue;
				}

				truncated = true;
				if (worst < 0)
					worst = Worst(buffer);

				if (Newer(appearance, buffer[worst]))
				{
					buffer[worst] = appearance;
					worst = Worst(buffer);
				}
			}

			// The kept set in recency order: an insertion sort (at most 128 records, allocation-free; Span.Sort with a comparison allocates).
			if (truncated)
				for (var i = 1; i < count; i++)
				{
					var item = buffer[i];
					var j = i - 1;
					for (; j >= 0 && Newer(item, buffer[j]); j--)
						buffer[j + 1] = buffer[j];

					buffer[j + 1] = item;
				}

			return count;
		}

		/// <summary>The recency order: the later sighting first, the lower actor id on a tie.</summary>
		internal static bool Newer(in FogStyleActor a, in FogStyleActor b) =>
			a.LastSeenTick != b.LastSeenTick ? a.LastSeenTick > b.LastSeenTick : a.ActorId < b.ActorId;

		static int Worst(Span<FogStyleActor> buffer)
		{
			var worst = 0;
			for (var i = 1; i < buffer.Length; i++)
				if (Newer(buffer[worst], buffer[i]))
					worst = i;

			return worst;
		}

		internal static FogStyleActor Project(ObservedActor o) => new()
		{
			ActorId = o.ActorID,
			TypeName = o.Info?.Name,
			Cell = o.Location,
			LastSeenTick = o.LastSeenTick,
			Value = o.Value,
			Building = o.Building,
			BaseBuilding = o.BaseBuilding,
			Combat = o.Combat,
			Defence = o.Defence,
			Tech = o.Tech,
			Production = o.Production,
			Refinery = o.Refinery,
			Harvester = o.Harvester,
			Mcv = o.Mcv,
			Aircraft = o.Aircraft,
			ProducesAircraft = o.ProducesAircraft,
			AntiAir = o.AntiAir,
			Infantry = o.Infantry,
			Vehicle = o.Vehicle,
			Naval = o.Naval,
			Cloaked = o.Cloaked,
		};
	}
}
