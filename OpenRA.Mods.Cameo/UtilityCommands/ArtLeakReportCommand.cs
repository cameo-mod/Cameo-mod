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
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.UtilityCommands
{
	// "Safety map" for per-bundle art gating. For every actor it gathers the images it needs (own sprite,
	// faction-specific art, trait Image overrides, and its weapons' projectile/warhead images), maps each
	// image to the sequence "bundle" (file) that defines it, and flags any image whose bundle differs from
	// the actor's own bundle and is not in the always-loaded floor. Those are the references that would crash
	// if the actor's bundle were loaded without the bundle the image lives in.
	sealed class ArtLeakReportCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--art-leak-report";

		static readonly string[] DefaultFloor = { "misc", "shared_effects", "decorations", "funpark", "civilian", "campaign" };

		bool IUtilityCommand.ValidateArguments(string[] args) => true;

		[Desc("[--floor a,b,c]",
			"List cross-bundle art references: actors whose sprites/weapons point at images defined in a " +
			"different bundle than the actor's own (and not in the shared floor). These must be fixed before " +
			"per-bundle gating, or they crash when the owning bundle isn't loaded.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var modData = Game.ModData = utility.ModData;
			var fs = modData.DefaultFileSystem;
			var rules = modData.DefaultRules;

			var floorNames = DefaultFloor.ToHashSet(StringComparer.OrdinalIgnoreCase);
			for (var i = 1; i < args.Length - 1; i++)
				if (args[i] == "--floor")
					floorNames = args[i + 1].Split(',').Select(s => s.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

			// image key (lowercased) -> set of bundles (sequence files, short name) that define it.
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

			var weaponsByName = new Dictionary<string, WeaponInfo>(StringComparer.OrdinalIgnoreCase);
			foreach (var w in rules.Weapons)
				weaponsByName[w.Key] = w.Value;

			var factions = rules.Actors[SystemActors.World].TraitInfos<FactionInfo>().Select(f => f.InternalName).ToArray();

			// bundle pair "X -> Y" -> set of "image (via)" facts, plus an example actor.
			var leaks = new Dictionary<string, SortedSet<string>>(StringComparer.OrdinalIgnoreCase);
			var missing = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
			var actorsScanned = 0;
			var actorsWithLeaks = new HashSet<string>();

			foreach (var actorInfo in rules.Actors)
			{
				RenderSpritesInfo renderInfo;
				try { renderInfo = actorInfo.Value.TraitInfoOrDefault<RenderSpritesInfo>(); }
				catch { continue; }
				if (renderInfo == null)
					continue;

				actorsScanned++;
				var ownImage = renderInfo.GetImage(actorInfo.Value, null).ToLowerInvariant();
				var ownBundle = BundleOf(ownImage, imageBundles, floorNames);

				// image -> how it's referenced (for reporting).
				var referenced = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				void Add(string img, string via)
				{
					if (string.IsNullOrEmpty(img))
						return;
					img = img.ToLowerInvariant();
					referenced.TryAdd(img, via);
				}

				Add(ownImage, "self");
				foreach (var faction in factions)
					Add(renderInfo.GetImage(actorInfo.Value, faction).ToLowerInvariant(), "faction-image");

				TraitInfo[] traitInfos;
				try { traitInfos = actorInfo.Value.TraitInfos<TraitInfo>().ToArray(); }
				catch { traitInfos = Array.Empty<TraitInfo>(); }

				foreach (var traitInfo in traitInfos)
				{
					var traitName = traitInfo.GetType().Name;
					CollectSequenceImages(traitInfo, img => Add(img, $"trait:{traitName}"));

					foreach (var weaponName in GetWeaponNames(traitInfo))
					{
						if (!weaponsByName.TryGetValue(weaponName, out var weapon))
							continue;

						if (weapon.Projectile != null)
							CollectSequenceImages(weapon.Projectile, img => Add(img, $"weapon:{weaponName}"));

						if (weapon.Warheads != null)
							foreach (var warhead in weapon.Warheads)
								CollectSequenceImages(warhead, img => Add(img, $"weapon:{weaponName}"));
					}
				}

				foreach (var (img, via) in referenced)
				{
					if (!imageBundles.TryGetValue(img, out var bundles))
					{
						missing.Add($"{img}  (referenced by {actorInfo.Value.Name} via {via})");
						continue;
					}

					// Available if defined in the floor or in the actor's own bundle.
					var available = bundles.Any(b => floorNames.Contains(b) || b.Equals(ownBundle, StringComparison.OrdinalIgnoreCase));
					if (available)
						continue;

					var imageBundle = string.Join("/", bundles.OrderBy(b => b));
					var pair = $"{ownBundle} -> {imageBundle}";
					leaks.GetOrAdd(pair, _ => new SortedSet<string>(StringComparer.OrdinalIgnoreCase))
						.Add($"{img}  (e.g. {actorInfo.Value.Name} via {via})");
					actorsWithLeaks.Add(actorInfo.Value.Name);
				}
			}

			Console.WriteLine($"Actors scanned (with sprites): {actorsScanned}");
			Console.WriteLine($"Actors with cross-bundle references: {actorsWithLeaks.Count}");
			Console.WriteLine($"Distinct bundle dependency pairs: {leaks.Count}");
			Console.WriteLine();

			Console.WriteLine("CROSS-BUNDLE REFERENCES (own-bundle -> needed-bundle): image (example actor via field)");
			Console.WriteLine(new string('-', 70));
			foreach (var (pair, imgs) in leaks.OrderBy(kv => kv.Key))
			{
				Console.WriteLine($"{pair}   [{imgs.Count}]");
				foreach (var img in imgs)
					Console.WriteLine($"    {img}");
			}

			if (missing.Count > 0)
			{
				Console.WriteLine();
				Console.WriteLine($"IMAGES REFERENCED BUT DEFINED IN NO ACTIVE BUNDLE ({missing.Count}):");
				foreach (var m in missing)
					Console.WriteLine($"    {m}");
			}
		}

		static string BundleOf(string image, Dictionary<string, HashSet<string>> imageBundles, HashSet<string> floorNames)
		{
			if (!imageBundles.TryGetValue(image, out var bundles))
				return "?";

			// Prefer a non-floor bundle as the "owning" bundle (the floor is shared by everyone).
			return bundles.FirstOrDefault(b => !floorNames.Contains(b)) ?? bundles.First();
		}

		// Adds the image named by each [SequenceReference(ImageReference=...)] field on info.
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

		// Weapon names referenced by [WeaponReference] fields on info.
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
	}
}
