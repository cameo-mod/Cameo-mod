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
using System.Linq;
using OpenRA.Mods.Cameo.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	// Lazy sprite loading (memory reduction): loads only the sprite art the in-play factions and preplaced map
	// actors need, instead of every faction's art. Attach to the World actor. Requires the manifest to set
	// DeferSpriteLoading, which makes the engine skip the eager full sprite load and invoke this once, right after
	// the World is built (so lobby factions — including a resolved Random pick — and preplaced map actors are known
	// but nothing has been resolved or rendered yet). See FactionArtClosure for the resolver and --gating-preview
	// for an offline preview of what a given faction set would load.
	[Desc("On the World actor: loads only the sprite bundles the in-play factions and preplaced map actors need. ",
		"Requires the manifest DeferSpriteLoading flag; without it the engine loads everything eagerly as before.")]
	public class LazySpriteGateInfo : TraitInfo
	{
		[Desc("Sequence bundles (theme file short-names) always loaded regardless of the factions in play — the ",
			"shared floor of effects/decorations/civilian/campaign art any faction or map can reference.")]
		public readonly string[] Floor = { "misc", "shared_effects", "decorations", "funpark", "civilian", "campaign" };

		public override object Create(ActorInitializer init) => new LazySpriteGate(this);
	}

	public class LazySpriteGate : ISpriteLoadGate
	{
		readonly LazySpriteGateInfo info;

		public LazySpriteGate(LazySpriteGateInfo info)
		{
			this.info = info;
		}

		void ISpriteLoadGate.LoadGatedSprites(World world)
		{
			var modData = Game.ModData;
			var rules = world.Map.Rules;

			// The floor bundles: prefer the manifest's EagerSpriteBundles (the same list the engine loads eagerly
			// before this gate runs) so the two stay in sync; fall back to the trait's own list if unset.
			var floorList = modData.Manifest.EagerSpriteBundles.Length > 0 ? modData.Manifest.EagerSpriteBundles.AsEnumerable() : info.Floor;
			var floor = floorList.ToHashSet(StringComparer.OrdinalIgnoreCase);

			// Resolved in-play factions. Random is already resolved to a concrete faction on each player by now, so
			// this is the exact set of factions the match will field (no need to preload every Random member).
			// Skip spectators (the shared "Everyone" observer carries a resolved Random faction that fields nothing).
			var factions = world.Players
				.Where(p => !p.Spectating && p.Faction != null && !string.IsNullOrEmpty(p.Faction.InternalName))
				.Select(p => p.Faction.InternalName)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();

			// Preplaced/neutral map actors (capturable tech, campaign/shellmap units) — these aren't reachable via
			// the build-prerequisite graph from a faction's starting units, so fold them in as extra seed actors.
			var mapActors = world.Map.ActorDefinitions
				.Select(a => a.Value.Value)
				.Where(t => !string.IsNullOrEmpty(t))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();

			var result = FactionArtClosure.ResolveByStartingUnits(modData, rules, factions, floor, mapActors);

			// Load whole needed theme bundles + the floor, not just the precise image closure: loading a bundle's
			// siblings is cheap (they share the theme's sheets) and robust to any intra-theme reference the static
			// closure misses. Map-inline images (defined in a map's own sequences, not in any manifest bundle) are
			// always loaded too, since the gate can't know which faction/map would reference them.
			var imageBundles = FactionArtClosure.BuildImageBundles(modData);
			var loadBundles = new HashSet<string>(result.Bundles, StringComparer.OrdinalIgnoreCase);
			loadBundles.UnionWith(floor);

			var toLoad = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var kv in imageBundles)
				if (kv.Value.Any(loadBundles.Contains))
					toLoad.Add(kv.Key);

			foreach (var image in world.Map.Sequences.Images)
				if (!imageBundles.ContainsKey(image))
					toLoad.Add(image);

			world.Map.Sequences.LoadImages(toLoad);
		}
	}
}
