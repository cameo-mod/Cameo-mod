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
using OpenRA.Graphics;
using OpenRA.Mods.Cameo.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Periodically spawns subtle visual sparkle effects on resource cells in the viewport. Purely cosmetic.")]
	public class ResourceSparkleEffectInfo : TraitInfo, Requires<IResourceLayerInfo>
	{
		public class SparkleConfig
		{
			[FieldLoader.Require]
			[Desc("Image (sequence outer key) to play.")]
			public readonly string Image = null;

			[FieldLoader.Require]
			[Desc("Sequences inside Image to randomly choose between.")]
			public readonly string[] Sequences = null;

			[Desc("Palette to render the sparkle with.")]
			public readonly string Palette = "effect";

			[Desc("Minimum delay between spawn cycles, in ticks.")]
			public readonly int MinInterval = 75;

			[Desc("Maximum delay between spawn cycles, in ticks.")]
			public readonly int MaxInterval = 125;

			[Desc("Sparkles spawned per cycle (each lands on a different random visible cell).")]
			public readonly int SparklesPerInterval = 1;

			[Desc("Minimum cell density required, as a percentage (0-100) of the type's MaxDensity. " +
				"0 means any density qualifies; 50 means only the upper half of the growth range.")]
			public readonly int MinDensityFraction = 0;

			[Desc("Recolor the (white) sparkle to this color. Leave white for no tint.")]
			public readonly Color TintColor = Color.White;
		}

		[FieldLoader.LoadUsing(nameof(LoadResourceTypes))]
		public readonly Dictionary<string, SparkleConfig> ResourceTypes = null;

		protected static object LoadResourceTypes(MiniYaml yaml)
		{
			var ret = new Dictionary<string, SparkleConfig>();
			var resources = yaml.NodeWithKeyOrDefault("ResourceTypes");
			if (resources != null)
				foreach (var r in resources.Value.Nodes)
				{
					var cfg = new SparkleConfig();
					FieldLoader.Load(cfg, r.Value);
					ret[r.Key] = cfg;
				}

			return ret;
		}

		public override object Create(ActorInitializer init) { return new ResourceSparkleEffect(init.Self, this); }
	}

	public class ResourceSparkleEffect : ITick, IWorldLoaded
	{
		sealed class TypeState
		{
			public string Type;
			public ResourceSparkleEffectInfo.SparkleConfig Config;
			public byte MinDensity;
			public int Countdown;
		}

		readonly ResourceSparkleEffectInfo info;
		readonly World world;
		readonly List<TypeState> states = new();
		IResourceLayer resourceLayer;
		WorldRenderer worldRenderer;

		public ResourceSparkleEffect(Actor self, ResourceSparkleEffectInfo info)
		{
			this.info = info;
			world = self.World;
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			worldRenderer = wr;
			resourceLayer = w.WorldActor.TraitOrDefault<IResourceLayer>();
			if (resourceLayer == null)
				return;

			foreach (var kv in info.ResourceTypes)
			{
				var max = resourceLayer.GetMaxDensity(kv.Key);
				if (max == 0)
					continue;

				var fraction = Math.Clamp(kv.Value.MinDensityFraction, 0, 100);
				var minDensity = (byte)Math.Max(1, max * fraction / 100);

				states.Add(new TypeState
				{
					Type = kv.Key,
					Config = kv.Value,
					MinDensity = minDensity,
					Countdown = NextDelay(kv.Value),
				});
			}
		}

		static int NextDelay(ResourceSparkleEffectInfo.SparkleConfig cfg)
		{
			var lo = Math.Max(1, cfg.MinInterval);
			var hi = Math.Max(lo, cfg.MaxInterval);
			return Game.CosmeticRandom.Next(lo, hi + 1);
		}

		void ITick.Tick(Actor self)
		{
			if (resourceLayer == null || worldRenderer == null || states.Count == 0)
				return;

			foreach (var state in states)
			{
				if (--state.Countdown > 0)
					continue;

				state.Countdown = NextDelay(state.Config);
				SpawnCycle(state);
			}
		}

		void SpawnCycle(TypeState state)
		{
			var candidates = new List<CPos>();
			foreach (var uv in worldRenderer.Viewport.VisibleCellsInsideBounds.CandidateMapCoords)
			{
				if (world.ShroudObscures(uv))
					continue;

				var cell = uv.ToCPos(world.Map);
				var content = resourceLayer.GetResource(cell);
				if (content.Type != state.Type)
					continue;

				if (content.Density < state.MinDensity)
					continue;

				candidates.Add(cell);
			}

			if (candidates.Count == 0)
				return;

			var count = Math.Min(state.Config.SparklesPerInterval, candidates.Count);
			var cfg = state.Config;
			for (var i = 0; i < count; i++)
			{
				var cell = candidates[Game.CosmeticRandom.Next(candidates.Count)];
				var pos = world.Map.CenterOfCell(cell);
				var seq = cfg.Sequences[Game.CosmeticRandom.Next(cfg.Sequences.Length)];
				world.AddFrameEndTask(w => w.Add(new TintedSpriteEffect(pos, w, cfg.Image, seq, cfg.Palette, cfg.TintColor)));
			}
		}
	}
}
