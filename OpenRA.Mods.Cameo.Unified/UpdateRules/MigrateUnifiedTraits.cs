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
using System.Text.Json;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	/// <summary>
	/// Trait-unification migration rule (SPEC_2026-10-05_trait_unification section 6/7):
	/// rewrites yaml references from old suffixed type names to the unified plain name,
	/// driven by tools/audit/trait_aliases.json. Only entries whose state is "active"
	/// are rewritten; "pending" and "retired" entries are left alone so each family can
	/// be flipped independently. All edits are MiniYAML tree transformations: renamed
	/// keys keep their '-' removal prefix and '@instance' suffix in place (ordering and
	/// comments are preserved by the node builder), projectile/warhead renames touch only
	/// the Projectile: / Warhead@x: VALUES of weapon definitions, and any collision with
	/// an existing same-named sibling is reported as a manual step instead of silently
	/// merging two stateful instances.
	/// </summary>
	public class MigrateUnifiedTraits : UpdateRule
	{
		public const string RegistryRelativePath = "tools/audit/trait_aliases.json";

		/// <summary>One entry of trait_aliases.json.</summary>
		public sealed class Alias
		{
			public string Old;
			public string New;
			public string Kind;
			public string State;
			public string Phase;
			public string Decision;
		}

		readonly List<Alias> aliases = [];
		readonly string registryNote;

		public override string Name => "Migrate suffixed trait/projectile/warhead implementations to unified plain names.";

		public override string Description =>
			"Reads tools/audit/trait_aliases.json and rewrites every yaml use of each state=active " +
			"old name to its unified plain name: trait keys (including -Trait@instance cancellations), " +
			"Projectile: / Warhead@x: values on weapon definitions, and trait keys under map actor " +
			"definitions and placed-actor overrides. Entries with state pending or retired are not " +
			"touched. Collisions against an already-present same-named sibling are reported as manual " +
			"steps and the node is left unchanged. Registry: " + (registryNote ?? "unresolved");

		/// <summary>Used by UpdatePath/ObjectCreator: loads the registry from the default location.</summary>
		public MigrateUnifiedTraits()
		{
			var path = DefaultRegistryPath();
			if (path == null)
			{
				registryNote = "trait_aliases.json not found";
				return;
			}

			registryNote = path;
			aliases = LoadRegistry(path);
		}

		/// <summary>Used by tests and by --update-unified-traits with an explicit registry.</summary>
		public MigrateUnifiedTraits(IReadOnlyList<Alias> aliases, string registrySource = null)
		{
			this.aliases = aliases.ToList();
			registryNote = registrySource ?? "in-memory";
		}

		/// <summary>Total registered aliases regardless of state.</summary>
		public int AliasCount => aliases.Count;

		/// <summary>Entries that may be rewritten right now: state=active and a non-empty new name.</summary>
		public IReadOnlyList<Alias> ActiveAliases =>
			aliases.Where(a => a.State == "active" && !string.IsNullOrEmpty(a.New)).ToList();

		public IReadOnlyDictionary<string, string> ActiveRenames(string kind) =>
			ActiveAliases
				.Where(a => a.Kind == kind)
				.GroupBy(a => a.Old)
				.ToDictionary(g => g.Key, g => g.First().New);

		/// <summary>Renames matching child keys of a node, preserving '-' and '@instance'.</summary>
		public static IEnumerable<string> RenameMatchingChildren(
			MiniYamlNodeBuilder parent, IReadOnlyDictionary<string, string> renames)
		{
			foreach (var child in parent.Value.Nodes.ToList())
			{
				var key = child.Key;
				if (string.IsNullOrEmpty(key))
					continue;

				var baseName = key.TrimStart('-').Split('@')[0];
				if (!renames.TryGetValue(baseName, out var newName))
					continue;

				// Effective key after rename: same removal prefix and instance suffix.
				var at = key.IndexOf('@');
				var target = (key.StartsWith('-') ? "-" : "") + newName + (at >= 0 ? key[at..] : "");

				// Spec section 6: never silently merge instances. A collision is another sibling
				// occupying the SAME destination instance - same base name and same @suffix, with
				// either sign (a positive New@a would be silently overridden; a -New@a would
				// cancel a node the author wrote). Different @suffixes are different instances
				// and are allowed.
				var mySuffix = at >= 0 ? key[at..] : "";
				var collision = parent.Value.Nodes.FirstOrDefault(s =>
				{
					if (ReferenceEquals(s, child) || s.Key == null)
						return false;
					var stripped = s.Key.TrimStart('-');
					if (!stripped.StartsWith(newName))
						return false;
					var sSuffix = stripped.Length > newName.Length && stripped[newName.Length] == '@'
						? stripped[newName.Length..]
						: stripped.Length == newName.Length ? "" : null;
					return sSuffix == mySuffix;
				});
				if (collision != null)
				{
					yield return $"{parent.Key}: '{key}' wants to become '{target}' but sibling " +
						$"'{collision.Key}' already exists - map the instance keys by hand.";
					continue;
				}

				child.RenameKey(newName);
			}
		}

		/// <summary>Rewrites Projectile:/Warhead@x: values of a weapon definition.</summary>
		public static IEnumerable<string> RenameWeaponValues(
			MiniYamlNodeBuilder weaponNode,
			IReadOnlyDictionary<string, string> projectileRenames,
			IReadOnlyDictionary<string, string> warheadRenames)
		{
			foreach (var child in weaponNode.Value.Nodes)
			{
				var key = child.Key;
				if (string.IsNullOrEmpty(key) || key.StartsWith('-'))
					continue;

				var value = (child.Value?.Value ?? "").Trim();
				if (value.Length == 0)
					continue;

				var baseName = key.Split('@')[0];
				if (baseName == "Projectile" && projectileRenames.TryGetValue(value, out var newProjectile))
					child.ReplaceValue(newProjectile);
				else if (baseName == "Warhead" && warheadRenames.TryGetValue(value, out var newWarhead))
					child.ReplaceValue(newWarhead);
			}

			yield break;
		}

		public override IEnumerable<string> BeforeUpdate(ModData modData)
		{
			if (aliases.Count == 0)
				yield return $"alias registry empty or missing ({registryNote}) - no renames will be performed";

			var byState = aliases.GroupBy(a => a.State).ToDictionary(g => g.Key, g => g.Count());
			yield return $"trait_aliases.json ({registryNote}): " +
				$"{byState.GetValueOrDefault("active")} active, {byState.GetValueOrDefault("pending")} pending, " +
				$"{byState.GetValueOrDefault("retired")} retired aliases";
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			return RenameMatchingChildren(actorNode, ActiveRenames("trait"));
		}

		public override IEnumerable<string> UpdateMapActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			return RenameMatchingChildren(actorNode, ActiveRenames("trait"));
		}

		public override IEnumerable<string> UpdateWeaponNode(ModData modData, MiniYamlNodeBuilder weaponNode)
		{
			return RenameWeaponValues(weaponNode, ActiveRenames("projectile"), ActiveRenames("warhead"));
		}

		/// <summary>Locates trait_aliases.json relative to the running utility.</summary>
		public static string DefaultRegistryPath()
		{
			var candidates = new List<string>();

			// utility.cmd runs with the engine directory as cwd and MOD_SEARCH_PATHS pointing
			// at the repo's mods folder, so the repo root is one level up from either.
			var searchPaths = Environment.GetEnvironmentVariable("MOD_SEARCH_PATHS");
			if (!string.IsNullOrEmpty(searchPaths))
				foreach (var p in searchPaths.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
					candidates.Add(Path.Combine(p, "..", RegistryRelativePath));

			candidates.Add(Path.Combine("..", RegistryRelativePath));
			candidates.Add(RegistryRelativePath);
			return candidates.FirstOrDefault(File.Exists);
		}

		/// <summary>Parses trait_aliases.json. Unknown fields are ignored; missing required keys throw.</summary>
		public static List<Alias> LoadRegistry(string path)
		{
			using var doc = JsonDocument.Parse(File.ReadAllText(path));
			var list = new List<Alias>();
			foreach (var el in doc.RootElement.GetProperty("aliases").EnumerateArray())
			{
				list.Add(new Alias
				{
					Old = el.GetProperty("old").GetString(),
					New = el.TryGetProperty("new", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : null,
					Kind = el.GetProperty("kind").GetString(),
					State = el.GetProperty("state").GetString(),
					Phase = el.TryGetProperty("phase", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null,
					Decision = el.TryGetProperty("decision", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null,
				});
			}

			return list;
		}
	}
}
