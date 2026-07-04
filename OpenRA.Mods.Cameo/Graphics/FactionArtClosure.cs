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
using System.IO;
using System.Linq;
using System.Reflection;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Graphics
{
	// Shared resolver for lazy sprite loading (theme-file gating): given a set of factions, work out which
	// sequence "themes" (bundles / sequence files) their art needs, so only those are loaded. Composes the
	// same logic proven by the --faction-report (full per-faction roster via prerequisite closure) and
	// --art-leak-report (per-actor image closure + image->bundle map) utility commands. Used by the offline
	// --gating-preview command and by the in-game gate that filters the sprite load. Metadata/reflection only
	// (no sprite decode), so it is cheap to run at match start.
	public static class FactionArtClosure
	{
		public sealed class Result
		{
			// Bundles (theme short-names) that must be loaded for the requested factions (excludes the floor).
			public HashSet<string> Bundles = new(StringComparer.OrdinalIgnoreCase);

			// Lowercased image keys referenced by the requested factions' rosters.
			public HashSet<string> Images = new(StringComparer.OrdinalIgnoreCase);

			// Images referenced but whose only defining bundle is neither loaded nor in the floor: a real leak
			// that would fail to render once gating is on. Should be empty (shared effects live in the floor).
			public SortedSet<string> Leaks = new(StringComparer.OrdinalIgnoreCase);

			// Roster (actor keys) per requested faction, for reporting.
			public Dictionary<string, HashSet<string>> RosterByFaction = new(StringComparer.OrdinalIgnoreCase);
		}

		// image key (lower) -> set of bundles (sequence file short-names) that define it. Voxels excluded
		// (model definitions, separate load path).
		public static Dictionary<string, HashSet<string>> BuildImageBundles(ModData modData)
		{
			var fs = modData.DefaultFileSystem;
			var imageBundles = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
			foreach (var path in modData.Manifest.Sequences)
			{
				var bundle = Path.GetFileNameWithoutExtension(path.Contains('|') ? path[(path.IndexOf('|') + 1)..] : path);
				if (bundle.Contains("voxel", StringComparison.OrdinalIgnoreCase))
					continue;

				using var stream = fs.Open(path);
				foreach (var node in MiniYaml.FromStream(stream, path))
				{
					if (node.Key.StartsWith(ActorInfo.AbstractActorPrefix))
						continue;

					imageBundles.GetOrAdd(node.Key.ToLowerInvariant(), _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase)).Add(bundle);
				}
			}

			return imageBundles;
		}

		// Real (non-random-container) faction internal names.
		public static string[] Factions(Ruleset rules)
		{
			return rules.Actors[SystemActors.World].TraitInfos<FactionInfo>()
				.Where(f => !f.RandomFactionMembers.Any() && !string.IsNullOrEmpty(f.InternalName))
				.Select(f => f.InternalName)
				.ToArray();
		}

		// Expand any requested "Random"-style faction (one with RandomFactionMembers, e.g. Random / RandomTournament
		// / Randomcnc) to the union of its concrete members, and normalise every name to its canonical
		// FactionInfo.InternalName casing. A Random lobby slot is not resolved to a concrete faction until the World
		// is created (after the sprite load), so the gate must preload every member's art. Members can themselves be
		// random containers, so this resolves transitively. Names with no matching FactionInfo are passed through
		// verbatim so the caller's validation can still report them.
		public static string[] ExpandRandomFactions(Ruleset rules, IEnumerable<string> requested)
		{
			var byName = new Dictionary<string, FactionInfo>(StringComparer.OrdinalIgnoreCase);
			foreach (var f in rules.Actors[SystemActors.World].TraitInfos<FactionInfo>())
				if (!string.IsNullOrEmpty(f.InternalName))
					byName.TryAdd(f.InternalName, f);

			var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var queue = new Queue<string>(requested);
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var guard = 0;
			while (queue.Count > 0 && guard++ < 1000)
			{
				var name = queue.Dequeue();
				if (!byName.TryGetValue(name, out var fi))
				{
					result.Add(name);
					continue;
				}

				if (fi.RandomFactionMembers.Count > 0)
				{
					foreach (var m in fi.RandomFactionMembers)
						if (seen.Add(m))
							queue.Enqueue(m);
				}
				else
					result.Add(fi.InternalName);
			}

			return result.ToArray();
		}

		// All faction names including Random-style containers (for input validation, which must accept "Random").
		public static string[] AllFactionNames(Ruleset rules)
		{
			return rules.Actors[SystemActors.World].TraitInfos<FactionInfo>()
				.Where(f => !string.IsNullOrEmpty(f.InternalName))
				.Select(f => f.InternalName)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();
		}

		// Full potential roster (actor keys) each faction can build/own, via the engine prerequisite graph
		// seeded from StartingUnits. This is the "could ever build this match" set, not the currently-unlocked
		// set, because art must be preloaded (it can't hitch-load at runtime).
		public static Dictionary<string, HashSet<string>> ComputeRosters(Ruleset rules)
		{
			var byKey = new Dictionary<string, ActorInfo>(StringComparer.OrdinalIgnoreCase);
			foreach (var ai in rules.Actors)
				byKey[ai.Key] = ai.Value;

			var world = rules.Actors[SystemActors.World];
			var factions = Factions(rules);

			// token (lower) -> providers (actor, restricted factions or null=all).
			var providers = new Dictionary<string, List<(string Actor, HashSet<string> Factions)>>(StringComparer.OrdinalIgnoreCase);
			foreach (var ai in rules.Actors)
			{
				if (ai.Key.StartsWith(ActorInfo.AbstractActorPrefix))
					continue;

				List<ITechTreePrerequisiteInfo> pps;
				try { pps = ai.Value.TraitInfos<ITechTreePrerequisiteInfo>().ToList(); }
				catch { continue; }

				foreach (var pp in pps)
				{
					var facs = pp is ProvidesPrerequisiteInfo ppi && ppi.Factions.Count > 0
						? ppi.Factions.Select(f => f.ToLowerInvariant()).ToHashSet()
						: null;
					foreach (var raw in pp.Prerequisites(ai.Value))
						providers.GetOrAdd(raw.ToLowerInvariant(), _ => new List<(string, HashSet<string>)>()).Add((ai.Key, facs));
				}
			}

			string TransformTarget(ActorInfo ai)
			{
				try { return ai.TraitInfoOrDefault<TransformsInfo>()?.IntoActor; }
				catch { return null; }
			}

			// faction-variant suffix (e.g. ".japan") -> faction, from each faction's StartingUnits BaseActor.
			var startUnits = world.TraitInfos<StartingUnitsInfo>().ToList();
			var factionBySuffix = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var su in startUnits)
			{
				if (string.IsNullOrEmpty(su.BaseActor) || !su.BaseActor.Contains('.'))
					continue;

				var suffix = su.BaseActor[(su.BaseActor.LastIndexOf('.') + 1)..];
				foreach (var f in su.Factions)
					if (factions.Contains(f))
						factionBySuffix[suffix] = f;
			}

			string LockedFaction(string key)
			{
				var dot = key.LastIndexOf('.');
				if (dot < 0)
					return null;

				return factionBySuffix.TryGetValue(key[(dot + 1)..], out var f) ? f : null;
			}

			var seedByFaction = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
			foreach (var f in factions)
			{
				var seed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				void SeedAdd(string a)
				{
					if (string.IsNullOrEmpty(a))
						return;

					var lk = LockedFaction(a);
					if (lk == null || lk == f)
						seed.Add(a);
				}

				foreach (var su in startUnits)
				{
					if (su.Factions.Count > 0 && !su.Factions.Contains(f))
						continue;

					SeedAdd(su.BaseActor);
					foreach (var a in su.SupportActors) SeedAdd(a);
					foreach (var a in su.SupportBuildings) SeedAdd(a);
					foreach (var a in su.SupportProxyActors) SeedAdd(a);
				}

				seedByFaction[f] = seed;
			}

			var globalTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var sa in new[] { SystemActors.Player, SystemActors.World })
			{
				try
				{
					var actor = rules.Actors[sa];
					foreach (var pp in actor.TraitInfos<ITechTreePrerequisiteInfo>())
					{
						// Skip default-off lobby toggles. Their tokens are only granted at runtime when the
						// checkbox is enabled, but the static Prerequisites() harvest returns them unconditionally.
						// Treating a default-off token (e.g. `wip-content`, required by every ^Conyard) as always
						// available makes every faction's conyard buildable, collapsing all factions into one
						// roster. Runtime resolution should instead honour LobbyInfo.GlobalSettings.
						if (pp is LobbyPrerequisiteCheckboxInfo cb && !cb.Enabled)
							continue;

						foreach (var token in pp.Prerequisites(actor))
							globalTokens.Add(token.ToLowerInvariant());
					}
				}
				catch { }
			}

			var rosters = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
			foreach (var f in factions)
				rosters[f] = ComputeOwned(f, rules, byKey, providers, seedByFaction[f], globalTokens, TransformTarget, LockedFaction);

			return rosters;
		}

		static HashSet<string> ComputeOwned(string faction, Ruleset rules, Dictionary<string, ActorInfo> byKey,
			Dictionary<string, List<(string Actor, HashSet<string> Factions)>> providers,
			HashSet<string> seed, HashSet<string> globalTokens, Func<ActorInfo, string> transformTarget,
			Func<string, string> lockedFaction)
		{
			var owned = new HashSet<string>(seed, StringComparer.OrdinalIgnoreCase);

			bool TryOwn(string key)
			{
				var lk = lockedFaction(key);
				if (lk != null && lk != faction)
					return false;

				return owned.Add(key);
			}

			ExpandTransforms(owned, byKey, transformTarget, TryOwn);

			bool TokenAvailable(string token)
			{
				token = token.ToLowerInvariant();
				if (globalTokens.Contains(token))
					return true;

				if (!providers.TryGetValue(token, out var list))
					return false;

				foreach (var (actor, facs) in list)
					if (owned.Contains(actor) && (facs == null || facs.Contains(faction)))
						return true;

				return false;
			}

			var changed = true;
			var guard = 0;
			while (changed && guard++ < 100)
			{
				changed = false;
				foreach (var ai in rules.Actors)
				{
					if (ai.Key.StartsWith(ActorInfo.AbstractActorPrefix) || owned.Contains(ai.Key))
						continue;

					BuildableInfo b;
					try { b = ai.Value.TraitInfoOrDefault<BuildableInfo>(); }
					catch { continue; }
					if (b == null)
						continue;

					var lk = lockedFaction(ai.Key);
					if (lk != null && lk != faction)
						continue;

					var ok = true;
					foreach (var raw in b.Prerequisites)
					{
						var p = raw.ToLowerInvariant();
						var negative = false;
						while (p.Length > 0 && (p[0] == '~' || p[0] == '!'))
						{
							if (p[0] == '!') negative = true;
							p = p[1..];
						}

						if (negative || p.Length == 0)
							continue;

						if (!TokenAvailable(p))
						{
							ok = false;
							break;
						}
					}

					if (ok)
					{
						owned.Add(ai.Key);
						var tgt = transformTarget(ai.Value);
						if (!string.IsNullOrEmpty(tgt))
							TryOwn(tgt);
						changed = true;
					}
				}
			}

			return owned;
		}

		static void ExpandTransforms(HashSet<string> owned, Dictionary<string, ActorInfo> byKey,
			Func<ActorInfo, string> transformTarget, Func<string, bool> tryOwn)
		{
			var changed = true;
			while (changed)
			{
				changed = false;
				foreach (var key in owned.ToArray())
				{
					if (!byKey.TryGetValue(key, out var ai))
						continue;

					var tgt = transformTarget(ai);
					if (!string.IsNullOrEmpty(tgt) && tryOwn(tgt))
						changed = true;
				}
			}
		}

		// Images an actor needs: own sprite, faction-specific art, trait [SequenceReference] images, and its
		// weapons' projectile/warhead [SequenceReference] images.
		public static void CollectActorImages(ActorInfo actor, string[] factions,
			Dictionary<string, WeaponInfo> weaponsByName, Action<string> add)
		{
			RenderSpritesInfo renderInfo;
			try { renderInfo = actor.TraitInfoOrDefault<RenderSpritesInfo>(); }
			catch { return; }
			if (renderInfo == null)
				return;

			void Add(string img)
			{
				if (!string.IsNullOrEmpty(img))
					add(img.ToLowerInvariant());
			}

			Add(renderInfo.GetImage(actor, null));
			foreach (var faction in factions)
				Add(renderInfo.GetImage(actor, faction));

			TraitInfo[] traitInfos;
			try { traitInfos = actor.TraitInfos<TraitInfo>().ToArray(); }
			catch { traitInfos = Array.Empty<TraitInfo>(); }

			foreach (var traitInfo in traitInfos)
			{
				CollectSequenceImages(traitInfo, Add);

				foreach (var weaponName in GetWeaponNames(traitInfo))
				{
					if (!weaponsByName.TryGetValue(weaponName, out var weapon))
						continue;

					if (weapon.Projectile != null)
						CollectSequenceImages(weapon.Projectile, Add);

					if (weapon.Warheads != null)
						foreach (var warhead in weapon.Warheads)
							CollectSequenceImages(warhead, Add);
				}
			}
		}

		static void CollectSequenceImages(object info, Action<string> add)
		{
			var fields = info.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
			foreach (var field in fields)
			{
				var sr = field.GetCustomAttribute<SequenceReferenceAttribute>(true);
				if (sr == null || string.IsNullOrEmpty(sr.ImageReference))
					continue;

				var imageField = fields.FirstOrDefault(f => f.Name == sr.ImageReference);
				if (imageField?.GetValue(info) is string image)
					add(image);
			}
		}

		static IEnumerable<string> GetWeaponNames(object info)
		{
			foreach (var field in info.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				if (field.GetCustomAttribute<WeaponReferenceAttribute>(true) == null)
					continue;

				var value = field.GetValue(info);
				if (value is string s)
				{
					if (!string.IsNullOrEmpty(s))
						yield return s;
				}
				else if (value is IEnumerable<string> list)
				{
					foreach (var w in list)
						if (!string.IsNullOrEmpty(w))
							yield return w;
				}
			}
		}

		// Actor keys an actor references via [ActorReference] trait fields — husks it leaves (LeavesHusk), actors it
		// spawns when sold (SpawnActorsOnSell), capture-transform targets (TransformOnCapture), support-power drop
		// payloads, etc. These predictably appear in-play but are NOT reachable through the build-prerequisite graph
		// from the referencing actor, so their art must be folded in explicitly or the spawned actor renders blank.
		public static IEnumerable<string> GetReferencedActors(ActorInfo actor)
		{
			TraitInfo[] traitInfos;
			try { traitInfos = actor.TraitInfos<TraitInfo>().ToArray(); }
			catch { yield break; }

			foreach (var traitInfo in traitInfos)
			{
				foreach (var field in traitInfo.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
				{
					if (field.GetCustomAttribute<ActorReferenceAttribute>(true) == null)
						continue;

					var value = field.GetValue(traitInfo);
					if (value is string s)
					{
						if (!string.IsNullOrEmpty(s))
							yield return s;
					}
					else if (value is IEnumerable<string> list)
					{
						foreach (var a in list)
							if (!string.IsNullOrEmpty(a))
								yield return a;
					}
				}
			}
		}

		// Grow an actor set by the actors it references (transitively, bounded) so husks/spawned/capture-transform
		// targets have their art loaded too. Only actors that actually exist in the ruleset are added.
		public static void ExpandReferencedActors(HashSet<string> actors, IReadOnlyDictionary<string, ActorInfo> byKey)
		{
			var changed = true;
			var guard = 0;
			while (changed && guard++ < 100)
			{
				changed = false;
				foreach (var key in actors.ToArray())
				{
					if (!byKey.TryGetValue(key, out var ai))
						continue;

					foreach (var reff in GetReferencedActors(ai))
						if (byKey.ContainsKey(reff) && actors.Add(reff))
							changed = true;
				}
			}
		}

		// Resolve the bundles/images to load for a set of factions, given the always-loaded floor bundles.
		// extraActors lets callers fold in preplaced/map actors (shellmap, campaign) whose factions aren't in
		// the lobby set.
		public static Result Resolve(ModData modData, Ruleset rules, IEnumerable<string> requestedFactions,
			HashSet<string> floor, IEnumerable<string> extraActors = null, bool excludeUniversal = false)
		{
			var imageBundles = BuildImageBundles(modData);
			var rosters = ComputeRosters(rules);
			var allFactions = Factions(rules);

			// Expand Random-style slots to their member union and normalise casing (a Random slot's concrete pick
			// isn't known until the World is built, after sprite load, so preload every member).
			requestedFactions = ExpandRandomFactions(rules, requestedFactions);

			var byKey = new Dictionary<string, ActorInfo>(StringComparer.OrdinalIgnoreCase);
			foreach (var ai in rules.Actors)
				byKey[ai.Key] = ai.Value;

			var weaponsByName = new Dictionary<string, WeaponInfo>(StringComparer.OrdinalIgnoreCase);
			foreach (var w in rules.Weapons)
				weaponsByName[w.Key] = w.Value;

			// Actors every faction can build ("universal"): a static-closure artifact (prereqs COULD be met by
			// anyone), not art the in-match factions distinctively need. Excluding them stops one faction's
			// roster from dragging in every theme.
			var universal = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (excludeUniversal && rosters.Count > 0)
			{
				universal.UnionWith(rosters.Values.First());
				foreach (var roster in rosters.Values.Skip(1))
					universal.IntersectWith(roster);
			}

			var result = new Result();

			var actorKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var faction in requestedFactions)
			{
				if (rosters.TryGetValue(faction, out var roster))
				{
					result.RosterByFaction[faction] = roster;
					actorKeys.UnionWith(roster);
				}
			}

			actorKeys.ExceptWith(universal);

			if (extraActors != null)
				actorKeys.UnionWith(extraActors);

			// Fold in husks / sold-spawned / capture-transform target actors (not build-reachable from the roster).
			ExpandReferencedActors(actorKeys, byKey);

			foreach (var key in actorKeys)
			{
				if (!rules.Actors.TryGetValue(key, out var actor))
					continue;

				CollectActorImages(actor, allFactions, weaponsByName, img => result.Images.Add(img));
			}

			MapImagesToBundles(imageBundles, floor, result);
			return result;
		}

		// SEED-UNIT basis (robust alternative to the prerequisite closure): a faction needs the theme bundle(s)
		// that define its StartingUnits' art. A faction's MCV / starting combat units unambiguously belong to
		// its own theme, so this sidesteps the closure's generic-token bridges (wip-content etc.) that make every
		// faction reach every tree. Whole theme bundles load, so identifying any seed image per theme suffices;
		// transforms are expanded so the MCV's ConYard (a definitive theme building) is included.
		public static Result ResolveByStartingUnits(ModData modData, Ruleset rules,
			IEnumerable<string> requestedFactions, HashSet<string> floor, IEnumerable<string> extraActors = null)
		{
			var imageBundles = BuildImageBundles(modData);
			var world = rules.Actors[SystemActors.World];
			var startUnits = world.TraitInfos<StartingUnitsInfo>().ToList();

			// Expand Random-style slots to their member union and normalise casing. Normalisation also fixes the
			// case-sensitive StartingUnits.Factions match below (that set uses an ordinal comparer, so a mis-cased
			// name like "GDI" would silently match no StartingUnits and seed nothing).
			var factions = ExpandRandomFactions(rules, requestedFactions);

			var byKey = new Dictionary<string, ActorInfo>(StringComparer.OrdinalIgnoreCase);
			foreach (var ai in rules.Actors)
				byKey[ai.Key] = ai.Value;

			var weaponsByName = new Dictionary<string, WeaponInfo>(StringComparer.OrdinalIgnoreCase);
			foreach (var w in rules.Weapons)
				weaponsByName[w.Key] = w.Value;

			var result = new Result();

			foreach (var faction in factions)
			{
				var seed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				void SeedAdd(string a)
				{
					if (!string.IsNullOrEmpty(a))
						seed.Add(a);
				}

				foreach (var su in startUnits)
				{
					if (su.Factions.Count > 0 && !su.Factions.Any(x => string.Equals(x, faction, StringComparison.OrdinalIgnoreCase)))
						continue;

					SeedAdd(su.BaseActor);
					foreach (var a in su.SupportActors) SeedAdd(a);
					foreach (var a in su.SupportBuildings) SeedAdd(a);
					foreach (var a in su.SupportProxyActors) SeedAdd(a);
				}

				// Expand transforms (MCV -> ConYard, etc.) so the base building's theme art is included.
				var changed = true;
				while (changed)
				{
					changed = false;
					foreach (var key in seed.ToArray())
					{
						if (!byKey.TryGetValue(key, out var ai))
							continue;

						string tgt;
						try { tgt = ai.TraitInfoOrDefault<TransformsInfo>()?.IntoActor; }
						catch { tgt = null; }

						if (!string.IsNullOrEmpty(tgt) && seed.Add(tgt))
							changed = true;
					}
				}

				// Fold in husks / sold-spawned / capture-transform target actors (not build-reachable from the seed).
				ExpandReferencedActors(seed, byKey);

				result.RosterByFaction[faction] = seed;
				foreach (var key in seed)
					if (rules.Actors.TryGetValue(key, out var actor))
						CollectActorImages(actor, new[] { faction }, weaponsByName, img => result.Images.Add(img));
			}

			if (extraActors != null)
			{
				var extra = new HashSet<string>(extraActors, StringComparer.OrdinalIgnoreCase);
				ExpandReferencedActors(extra, byKey);
				foreach (var key in extra)
					if (rules.Actors.TryGetValue(key, out var actor))
						CollectActorImages(actor, factions, weaponsByName, img => result.Images.Add(img));
			}

			MapImagesToBundles(imageBundles, floor, result);
			return result;
		}

		// Map the collected images to the bundles that must load + flag leaks (images whose only defining bundle
		// is neither loaded nor in the floor).
		static void MapImagesToBundles(Dictionary<string, HashSet<string>> imageBundles, HashSet<string> floor, Result result)
		{
			foreach (var img in result.Images)
			{
				if (!imageBundles.TryGetValue(img, out var bundles))
					continue;

				// Covered if the floor defines it; otherwise every defining bundle must load.
				if (bundles.Any(floor.Contains))
					continue;

				foreach (var b in bundles)
					result.Bundles.Add(b);
			}

			var loadable = new HashSet<string>(result.Bundles, StringComparer.OrdinalIgnoreCase);
			loadable.UnionWith(floor);
			foreach (var img in result.Images)
			{
				if (!imageBundles.TryGetValue(img, out var bundles))
				{
					result.Leaks.Add($"{img}  (defined in no active bundle)");
					continue;
				}

				if (!bundles.Any(loadable.Contains))
					result.Leaks.Add($"{img}  (only in {string.Join("/", bundles.OrderBy(b => b))})");
			}
		}
	}
}
