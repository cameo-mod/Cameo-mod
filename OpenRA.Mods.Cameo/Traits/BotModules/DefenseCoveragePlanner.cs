#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	[Desc("Places defences so the whole base is covered: each one goes where its weapon range covers the most base cells",
		"no defence of the same role (anti-air or ground) covers yet, preferring the outer edge and the side of the enemy.",
		"Advises the shared base builder through IBotDefensePlacementAdvisor; switched off, the base builder is untouched.")]
	public class DefenseCoveragePlannerInfo : ConditionalTraitInfo
	{
		[Desc("Advise the base builder. False = inert (the base builder places defences as it always did).")]
		public readonly bool Enabled = false;

		[Desc("Candidate cells scored per placement, sampled by a fixed stride (no random draw).")]
		public readonly int MaxCandidates = 300;

		[Desc("Score per percent of how far out (0-100) from the base centre the cell lies.")]
		public readonly int EdgeWeight = 30;

		[Desc("Score per percent (-100..100) of how well the cell lies toward the enemy.")]
		public readonly int ThreatWeight = 40;

		[Desc("Search annulus (cells) when no base builder is found.")]
		public readonly int MinimumRadius = 5;

		[Desc("Search annulus (cells) when no base builder is found.")]
		public readonly int MaximumRadius = 20;

		public override object Create(ActorInitializer init) { return new DefenseCoveragePlanner(init.Self, this); }
	}

	public class DefenseCoveragePlanner : ConditionalTrait<DefenseCoveragePlannerInfo>, IBotDefensePlacementAdvisor, INotifyActorDisposing
	{
		// Coverage dominates (maintainer: "the whole base should always be covered"): one newly covered base cell (1000)
		// outweighs the full edge + enemy-side bonus (EdgeWeight 30 + ThreatWeight 40, x100 = 7000) only from 8 cells on,
		// so the bonus decides between spots of nearly equal coverage and pulls them outward and toward the enemy.
		const int CoverageScore = 1000;

		readonly World world;
		readonly OpenRA.Player player;

		// Own buildings, kept from the world's add/remove events: no enumeration of the world's actors.
		readonly HashSet<Actor> buildings = new();
		BaseBuilderBotModuleCA[] baseBuilders;

		public DefenseCoveragePlanner(Actor self, DefenseCoveragePlannerInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			world.ActorAdded += Added;
			world.ActorRemoved += Removed;
		}

		void Added(Actor a)
		{
			if (a.Owner == player && a.Info.HasTraitInfo<BuildingInfo>())
				buildings.Add(a);
		}

		void Removed(Actor a) { buildings.Remove(a); }

		void INotifyActorDisposing.Disposing(Actor self)
		{
			world.ActorAdded -= Added;
			world.ActorRemoved -= Removed;
		}

		bool IBotDefensePlacementAdvisor.IsActive => !IsTraitDisabled && Info.Enabled;

		// --- world-free helpers (tested) ---

		/// <summary>The longest weapon range, in whole cells (rounded up), of a defence's armaments; 0 with no weapon.</summary>
		public static int MaxRangeCells(ActorInfo info)
		{
			var max = 0;
			foreach (var armament in info.TraitInfos<ArmamentInfo>())
				if (armament.WeaponInfo != null && armament.WeaponInfo.Range.Length > max)
					max = armament.WeaponInfo.Range.Length;

			return (max + 1023) / 1024;
		}

		/// <summary>True when the cell lies within the range of any of the defences (centre, range in cells).</summary>
		public static bool Covered(CPos cell, IEnumerable<(CPos Center, int Range)> defences)
		{
			foreach (var d in defences)
				if ((cell - d.Center).LengthSquared <= d.Range * d.Range)
					return true;

			return false;
		}

		/// <summary>How many of the still-uncovered base cells a defence at <paramref name="at"/> would newly cover.</summary>
		public static int NewlyCovered(CPos at, int range, IReadOnlyCollection<CPos> uncovered)
		{
			var n = 0;
			foreach (var c in uncovered)
				if ((c - at).LengthSquared <= range * range)
					n++;

			return n;
		}

		/// <summary>How far out the cell lies, 0 (at the centre) to 100 (at the maximum radius or beyond), in percent.</summary>
		public static int Edgeness(CPos cell, CPos center, int maxRadius)
		{
			if (maxRadius <= 0)
				return 0;

			var d = Math.Sqrt((cell - center).LengthSquared);
			return (int)Math.Min(100, d * 100 / maxRadius);
		}

		/// <summary>Cosine, in percent (-100..100), between (cell - center) and (threat - center); 0 when either is degenerate.</summary>
		public static int ThreatAlignment(CPos cell, CPos center, CPos threat)
		{
			var a = cell - center;
			var b = threat - center;
			var la = Math.Sqrt(a.LengthSquared);
			var lb = Math.Sqrt(b.LengthSquared);
			if (la < 1e-6 || lb < 1e-6)
				return 0;

			return (int)Math.Round((a.X * b.X + a.Y * b.Y) * 100 / (la * lb));
		}

		public static int Score(int newlyCovered, int edgeness, int threatAlignment, int edgeWeight, int threatWeight)
		{
			return newlyCovered * CoverageScore + edgeWeight * edgeness + threatWeight * threatAlignment;
		}

		// --- placement ---

		CPos? IBotDefensePlacementAdvisor.ChooseDefenseCell(ActorInfo defense, bool antiAir, CPos baseCenter, Func<CPos, bool> canPlace)
		{
			var range = MaxRangeCells(defense);
			if (range <= 0)
				return null;

			baseBuilders ??= player.PlayerActor.TraitsImplementing<BaseBuilderBotModuleCA>().ToArray();
			var builder = baseBuilders.FirstOrDefault(b => b.IsTraitEnabled());
			var antiAirTypes = builder?.Info.AntiAirTypes;
			var minRadius = builder?.Info.MinimumDefenseRadius ?? Info.MinimumRadius;
			var maxRadius = builder?.Info.MaximumDefenseRadius ?? Info.MaximumRadius;

			// Our own buildings and, of those, the defences of this role (buildings under construction are in the world already).
			var baseCells = new HashSet<CPos>();
			var sameRole = new List<(CPos Center, int Range)>();
			foreach (var b in buildings)
			{
				if (b.IsDead || !b.IsInWorld || b.Owner != player) // captured since it was added
					continue;

				var bi = b.Info.TraitInfoOrDefault<BuildingInfo>();
				foreach (var c in bi.Tiles(b.Location))
					baseCells.Add(c);

				if (b.Info.HasTraitInfo<AttackBaseInfo>())
				{
					var isAa = antiAirTypes != null && antiAirTypes.Contains(b.Info.Name);
					var r = MaxRangeCells(b.Info);
					if (isAa == antiAir && r > 0)
						sameRole.Add((b.Location, r));
				}
			}

			var uncovered = baseCells.Where(c => !Covered(c, sameRole)).ToArray();

			var cells = world.Map.FindTilesInAnnulus(baseCenter, minRadius, maxRadius).ToArray();
			var stride = Math.Max(1, (cells.Length + Info.MaxCandidates - 1) / Math.Max(1, Info.MaxCandidates));

			var threat = ThreatCell();
			CPos? best = null;
			var bestScore = int.MinValue;
			int bestNew = 0, bestEdge = 0, bestThreat = 0;
			for (var i = 0; i < cells.Length; i += stride)
			{
				var cell = cells[i];
				if (!canPlace(cell))
					continue;

				var n = NewlyCovered(cell, range, uncovered);
				var edge = Edgeness(cell, baseCenter, maxRadius);
				var t = ThreatAlignment(cell, baseCenter, threat);
				var score = Score(n, edge, t, Info.EdgeWeight, Info.ThreatWeight);
				if (score > bestScore)
				{
					bestScore = score;
					best = cell;
					bestNew = n;
					bestEdge = edge;
					bestThreat = t;
				}
			}

			if (best == null)
				return null;

			Log.Write("debug", $"AI ({player.ClientIndex}): DEFENSE PLACE {defense.Name} at {best.Value}: covers {bestNew} new base cells " +
				$"(role {(antiAir ? "AA" : "ground")}), edge {bestEdge}, threat {bestThreat}" +
				(bestNew == 0 ? " [no new coverage: redundant edge/threat cell]" : "") + $" at tick {world.WorldTick}");
			return best;
		}

		// Fog-honest direction of the enemy: the centroid of the defences we REMEMBER of the main target (else of any
		// enemy), else the map centre. Nothing here enumerates the world's actors.
		CPos ThreatCell()
		{
			var main = player.PlayerActor.TraitsImplementing<IBotMainTargetProvider>().Select(p => p.MainTarget).FirstOrDefault(t => t != null);
			var remembered = player.PlayerActor.TraitsImplementing<IBotRememberedDefenceProvider>()
				.SelectMany(p => p.RememberedDefences()).ToArray();
			var pool = main != null ? remembered.Where(d => d.Enemy == main).ToArray() : remembered;
			if (pool.Length == 0)
				pool = remembered;

			if (pool.Length > 0)
				return new CPos((int)pool.Average(d => d.Cell.X), (int)pool.Average(d => d.Cell.Y));

			var size = world.Map.MapSize;
			return new CPos(size.Width / 2, size.Height / 2);
		}
	}
}
