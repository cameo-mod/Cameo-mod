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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// One parsed MiniYAML node: key / value / children plus source provenance.
	/// Mirror of the <c>Node</c> dataclass in <c>tools/audit/miniyaml.py</c> —
	/// deliberately independent of the engine's <c>MiniYamlNode</c>: the balance
	/// fixtures were produced by the Python parser's exact semantics, so the bot
	/// model mirrors THAT implementation, not <c>MiniYaml.Merge</c>.
	/// </summary>
	public sealed class MiniYamlMirrorNode
	{
		public MiniYamlMirrorNode(string key, string value, List<MiniYamlMirrorNode> children = null, string file = "", int line = 0)
		{
			Key = key;
			Value = value ?? "";
			Children = children ?? new List<MiniYamlMirrorNode>();
			File = file ?? "";
			Line = line;
		}

		/// <summary>The yaml key verbatim, including any <c>@suffix</c>.</summary>
		public string Key { get; }

		/// <summary>The text after the first <c>:</c>, stripped; empty when unset.</summary>
		public string Value { get; internal set; }

		public List<MiniYamlMirrorNode> Children { get; internal set; }
		public string File { get; internal set; }
		public int Line { get; internal set; }

		/// <summary>First child whose key equals <paramref name="key"/> exactly (@suffix included), else null.</summary>
		public MiniYamlMirrorNode Child(string key)
		{
			return Children.FirstOrDefault(c => c.Key == key);
		}

		/// <summary>All children whose key is <paramref name="baseKey"/> or <c>baseKey@anything</c>.</summary>
		public List<MiniYamlMirrorNode> ChildrenNamed(string baseKey)
		{
			return Children
				.Where(c => c.Key == baseKey || c.Key.StartsWith(baseKey + "@", StringComparison.Ordinal))
				.ToList();
		}

		/// <summary>Walk child keys; return the value at the end (empty value -&gt; null).</summary>
		public string Get(params string[] path)
		{
			var node = this;
			foreach (var p in path)
			{
				node = node.Child(p);
				if (node == null)
					return null;
			}

			return string.IsNullOrEmpty(node.Value) ? null : node.Value;
		}

		public MiniYamlMirrorNode DeepCopy()
		{
			return new MiniYamlMirrorNode(Key, Value, Children.Select(c => c.DeepCopy()).ToList(), File, Line);
		}

		public override string ToString() => $"{Key}: {Value} ({File}:{Line})";
	}

	/// <summary>
	/// Ordered key → node map mirroring the insertion-order dict the Python merge builds.
	/// Key equality is ordinal; assigning an existing key keeps its position.
	/// </summary>
	public sealed class MiniYamlMirrorNodeMap : IReadOnlyDictionary<string, MiniYamlMirrorNode>
	{
		readonly Dictionary<string, MiniYamlMirrorNode> byKey = new(StringComparer.Ordinal);
		readonly List<KeyValuePair<string, MiniYamlMirrorNode>> entries = new();

		public MiniYamlMirrorNode this[string key] => byKey[key];

		public IEnumerable<string> Keys => entries.Select(e => e.Key);
		public IEnumerable<MiniYamlMirrorNode> Values => entries.Select(e => e.Value);
		public int Count => byKey.Count;

		public bool ContainsKey(string key) => byKey.ContainsKey(key);

		public bool TryGetValue(string key, out MiniYamlMirrorNode value) => byKey.TryGetValue(key, out value);

		public IEnumerator<KeyValuePair<string, MiniYamlMirrorNode>> GetEnumerator() => entries.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		internal void Set(string key, MiniYamlMirrorNode node)
		{
			var existing = entries.FindIndex(e => e.Key == key);
			if (existing >= 0)
				entries[existing] = new KeyValuePair<string, MiniYamlMirrorNode>(key, node);
			else
				entries.Add(new KeyValuePair<string, MiniYamlMirrorNode>(key, node));
			byKey[key] = node;
		}

		internal bool Remove(string key)
		{
			if (!byKey.Remove(key))
				return false;
			entries.RemoveAt(entries.FindIndex(e => e.Key == key));
			return true;
		}
	}

	/// <summary>
	/// Result of <see cref="MiniYamlMirror.LoadManifest"/>: ordered absolute file
	/// lists per manifest section plus every manifest file that was read.
	/// </summary>
	public sealed class MiniYamlMirrorManifest
	{
		public List<string> Rules { get; } = new();
		public List<string> Weapons { get; } = new();
		public List<string> Sequences { get; } = new();
		public List<string> Fluent { get; } = new();
		public List<string> Sources { get; } = new();
	}

	/// <summary>
	/// Shared MiniYAML loader/merger/resolver mirroring <c>tools/audit/miniyaml.py</c>.
	/// Implements the subset of OpenRA MiniYAML semantics the balance model needs:
	/// tab/space indentation, <c>#</c> comments (<c>\#</c> escapes), <c>key: value</c>
	/// pairs, <c>^Template</c> keys, <c>Trait@Suffix</c> instance keys, <c>-Key</c>
	/// removals, multi-file merging in manifest order, and <c>Inherits</c> /
	/// <c>Inherits@X</c> resolution with cycle-guard taint.
	/// </summary>
	public static class MiniYamlMirror
	{
		/// <summary>Manifest <c>package|path</c> prefixes mapped to repo-relative directories.</summary>
		public static readonly IReadOnlyDictionary<string, string> PackagePrefixes =
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["cameo"] = "mods/cameo",
				["ContentPacks"] = "mods/cameo/ContentPacks",
				["common"] = "engine/mods/common",
			};

		static readonly Regex CommentTail = new(@"(?<!\\)#.*$", RegexOptions.Compiled);

		/// <summary>Strip a trailing comment (<c>#</c> not preceded by a backslash), unescape <c>\#</c>, rstrip.</summary>
		public static string StripComment(string text)
		{
			var stripped = CommentTail.Replace(text ?? "", "");
			return stripped.Replace("\\#", "#").TrimEnd();
		}

		/// <summary>Parse one MiniYAML file into a list of top-level nodes.</summary>
		public static List<MiniYamlMirrorNode> LoadFile(string path)
		{
			// utf-8-sig: BOM tolerant; errors="replace" is StreamReader's default (no throw on invalid).
			return LoadText(File.ReadAllText(path), path);
		}

		/// <summary>Parse MiniYAML text into top-level nodes, tagging each with its source and line.</summary>
		public static List<MiniYamlMirrorNode> LoadText(string text, string source = "<memory>")
		{
			var root = new List<MiniYamlMirrorNode>();
			var stack = new List<(int Indent, MiniYamlMirrorNode Node)> { (-1, null) };
			var normalized = (text ?? "").TrimStart('\uFEFF').Replace("\r\n", "\n").Replace('\r', '\n');
			var lines = normalized.Split('\n');
			for (var lineno = 0; lineno < lines.Length; lineno++)
			{
				var strippedFull = StripComment(lines[lineno]);
				if (strippedFull.Trim().Length == 0)
					continue;

				// Engine-faithful depth: '\t' = +1 level, every 4 spaces = +1 level
				// (MiniYaml.cs ~L239-256). Only the leading "\t " prefix counts.
				var indent = 0;
				var sp = 0;
				var prefixLen = strippedFull.Length - strippedFull.TrimStart('\t', ' ').Length;
				for (var i = 0; i < prefixLen; i++)
				{
					if (strippedFull[i] == '\t')
						indent++;
					else
					{
						sp++;
						if (sp >= 4)
						{
							sp = 0;
							indent++;
						}
					}
				}

				var body = strippedFull.Trim();
				var colon = body.IndexOf(':');
				var key = colon < 0 ? body.Trim() : body.Substring(0, colon).Trim();
				var val = colon < 0 ? "" : body.Substring(colon + 1).Trim();
				var node = new MiniYamlMirrorNode(key, val, null, source, lineno + 1);
				while (stack[stack.Count - 1].Indent >= indent)
					stack.RemoveAt(stack.Count - 1);
				var parent = stack[stack.Count - 1].Node;
				(parent?.Children ?? root).Add(node);
				stack.Add((indent, node));
			}

			return root;
		}

		/// <summary>
		/// Merge <paramref name="overrideNodes"/> into <paramref name="baseNodes"/> in place:
		/// same-key nodes merge recursively (override value wins when non-empty),
		/// <c>-Key</c> removes, new keys append (deep-copied so source trees are never aliased).
		/// </summary>
		internal static void MergeInto(List<MiniYamlMirrorNode> baseNodes, Dictionary<string, MiniYamlMirrorNode> index,
			IReadOnlyList<MiniYamlMirrorNode> overrideNodes)
		{
			foreach (var onode in overrideNodes)
			{
				if (onode.Key.StartsWith("-", StringComparison.Ordinal))
				{
					var target = onode.Key.Substring(1);
					if (baseNodes.Any(n => n.Key == target))
					{
						baseNodes.RemoveAll(n => n.Key == target);
						index.Remove(target);
					}

					continue;
				}

				if (index.TryGetValue(onode.Key, out var existing))
				{
					if (!string.IsNullOrEmpty(onode.Value))
						existing.Value = onode.Value;
					var childIndex = new Dictionary<string, MiniYamlMirrorNode>(StringComparer.Ordinal);
					foreach (var c in existing.Children)
						childIndex[c.Key] = c;
					MergeInto(existing.Children, childIndex, onode.Children);
					existing.File = onode.File;
					existing.Line = onode.Line;
				}
				else
				{
					var copy = onode.DeepCopy();
					baseNodes.Add(copy);
					index[copy.Key] = copy;
				}
			}
		}

		/// <summary>Functional merge wrapper: deep-copies <paramref name="baseNodes"/>, merges, returns the result.</summary>
		public static List<MiniYamlMirrorNode> MergeChildren(
			IReadOnlyList<MiniYamlMirrorNode> baseNodes, IReadOnlyList<MiniYamlMirrorNode> overrideNodes)
		{
			var result = baseNodes.Select(n => n.DeepCopy()).ToList();
			var index = new Dictionary<string, MiniYamlMirrorNode>(StringComparer.Ordinal);
			foreach (var n in result)
				index[n.Key] = n;
			MergeInto(result, index, overrideNodes);
			return result;
		}

		/// <summary>
		/// Merge top-level nodes of every document in order, in the manifest's file order:
		/// later files merge into earlier ones. Mirror of <c>Ruleset._merge_files</c>.
		/// </summary>
		public static MiniYamlMirrorNodeMap MergeDocuments(IEnumerable<IReadOnlyList<MiniYamlMirrorNode>> documents)
		{
			var merged = new MiniYamlMirrorNodeMap();
			foreach (var doc in documents)
			{
				foreach (var top in doc)
				{
					if (top.Key.StartsWith("-", StringComparison.Ordinal))
					{
						merged.Remove(top.Key.Substring(1));
						continue;
					}

					if (merged.TryGetValue(top.Key, out var prev))
					{
						prev.Children = MergeChildren(prev.Children, top.Children);
						if (!string.IsNullOrEmpty(top.Value))
							prev.Value = top.Value;
					}
					else
						merged.Set(top.Key, top.DeepCopy());
				}
			}

			return merged;
		}

		/// <summary>Merge already-parsed document lists (each document = one file's top-level nodes).</summary>
		public static MiniYamlMirrorNodeMap MergeDocuments(IEnumerable<List<MiniYamlMirrorNode>> documents)
		{
			return MergeDocuments(documents.Cast<IReadOnlyList<MiniYamlMirrorNode>>());
		}

		/// <summary>
		/// Resolve a <c>package|relative/path</c> manifest reference to an absolute filesystem path.
		/// A non-qualified reference resolves under <c>mods/&lt;modId&gt;/</c>. Unknown packages raise
		/// <see cref="KeyNotFoundException"/> for the default mod and fall back to
		/// <c>mods/&lt;pkg&gt;/</c> or the mod's own directory for foreign checkouts.
		/// </summary>
		public static string ResolveReference(string repoRoot, string reference, string modId = "cameo")
		{
			var pipe = reference.IndexOf('|');
			if (pipe >= 0)
			{
				var pkg = reference.Substring(0, pipe);
				var rel = reference.Substring(pipe + 1);
				if (PackagePrefixes.TryGetValue(pkg, out var baseDir))
					return Path.GetFullPath(Path.Combine(repoRoot, baseDir, rel));

				// An unknown prefix in a FOREIGN mod is normal — every mod names its own
				// packages. Prefer a real mods/<pkg>/ directory, fall back to the mod's own.
				if (!string.Equals(modId, "cameo", StringComparison.Ordinal))
				{
					var byPkg = Path.GetFullPath(Path.Combine(repoRoot, "mods", pkg, rel));
					if (File.Exists(byPkg))
						return byPkg;
					return Path.GetFullPath(Path.Combine(repoRoot, "mods", modId, rel));
				}

				throw new KeyNotFoundException($"unknown package prefix '{pkg}' in '{reference}'");
			}

			return Path.GetFullPath(Path.Combine(repoRoot, "mods", modId, reference));
		}

		/// <summary>Read <c>mods/&lt;modId&gt;/mod.yaml</c> plus every <c>Include:</c>'d manifest, in order.</summary>
		public static MiniYamlMirrorManifest LoadManifest(string repoRoot, string modId = "cameo")
		{
			var manifest = new MiniYamlMirrorManifest();
			var seenIncludes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var modYaml = Path.GetFullPath(Path.Combine(repoRoot, "mods", modId, "mod.yaml"));
			manifest.Sources.Add(modYaml);
			AbsorbManifest(manifest, LoadFile(modYaml), Path.GetDirectoryName(modYaml), repoRoot, modId, seenIncludes);
			return manifest;
		}

		static void AbsorbManifest(MiniYamlMirrorManifest manifest, IReadOnlyList<MiniYamlMirrorNode> doc,
			string baseDir, string repoRoot, string modId, HashSet<string> seenIncludes)
		{
			foreach (var top in doc)
			{
				if (top.Key == "Include" && !string.IsNullOrEmpty(top.Value))
				{
					var inc = top.Value.Contains('|')
						? ResolveReference(repoRoot, top.Value, modId)
						: Path.GetFullPath(Path.Combine(baseDir, top.Value));
					if (!seenIncludes.Add(inc))
						continue;
					if (File.Exists(inc))
					{
						manifest.Sources.Add(inc);
						AbsorbManifest(manifest, LoadFile(inc), Path.GetDirectoryName(inc), repoRoot, modId, seenIncludes);
					}

					continue;
				}

				List<string> target = top.Key switch
				{
					"Rules" => manifest.Rules,
					"Weapons" => manifest.Weapons,
					"Sequences" => manifest.Sequences,
					"FluentMessages" => manifest.Fluent,
					_ => null,
				};
				if (target == null)
					continue;
				foreach (var entry in top.Children)
				{
					var reference = string.IsNullOrEmpty(entry.Value) ? entry.Key : $"{entry.Key}:{entry.Value}";
					string path;
					try
					{
						path = ResolveReference(repoRoot, reference, modId);
					}
					catch (KeyNotFoundException)
					{
						continue;
					}

					if (File.Exists(path))
						target.Add(path);
				}
			}
		}

		/// <summary>Walk up from <paramref name="start"/> to the directory containing <c>mods/cameo/mod.yaml</c>.</summary>
		public static string FindRepoRoot(string start = null)
		{
			var dir = new DirectoryInfo(start ?? AppContext.BaseDirectory);
			while (dir != null)
			{
				if (File.Exists(Path.Combine(dir.FullName, "mods", "cameo", "mod.yaml")))
					return dir.FullName;
				dir = dir.Parent;
			}

			return null;
		}
	}

	/// <summary>
	/// Merged view of the rules/weapons/sequences documents with an inheritance
	/// resolver and case-insensitive lookup — mirror of <c>miniyaml.Ruleset</c>.
	/// </summary>
	public sealed class MiniYamlMirrorRuleset
	{
		static readonly IReadOnlyList<MiniYamlMirrorNode> EmptyDocument = Array.Empty<MiniYamlMirrorNode>();

		readonly Dictionary<string, MiniYamlMirrorNode> resolveCache = new(StringComparer.Ordinal);
		readonly Dictionary<string, string> actorCi;
		readonly Dictionary<string, string> weaponCi;
		readonly Dictionary<string, string> sequenceCi;
		int cycleEvents;

		/// <summary>Build from already-parsed documents (each = one file's top-level nodes).</summary>
		public MiniYamlMirrorRuleset(
			IEnumerable<IReadOnlyList<MiniYamlMirrorNode>> rulesDocuments,
			IEnumerable<IReadOnlyList<MiniYamlMirrorNode>> weaponsDocuments,
			IEnumerable<IReadOnlyList<MiniYamlMirrorNode>> sequenceDocuments = null)
		{
			Actors = MiniYamlMirror.MergeDocuments(rulesDocuments ?? Enumerable.Empty<IReadOnlyList<MiniYamlMirrorNode>>());
			Weapons = MiniYamlMirror.MergeDocuments(weaponsDocuments ?? Enumerable.Empty<IReadOnlyList<MiniYamlMirrorNode>>());
			Sequences = MiniYamlMirror.MergeDocuments(sequenceDocuments ?? Enumerable.Empty<IReadOnlyList<MiniYamlMirrorNode>>());
			actorCi = CaseInsensitiveKeys(Actors);
			weaponCi = CaseInsensitiveKeys(Weapons);
			sequenceCi = CaseInsensitiveKeys(Sequences);
		}

		public MiniYamlMirrorNodeMap Actors { get; }
		public MiniYamlMirrorNodeMap Weapons { get; }
		public MiniYamlMirrorNodeMap Sequences { get; }

		/// <summary>Alias kept for parity with the Python caller surface.</summary>
		public MiniYamlMirrorNodeMap SequenceImages => Sequences;

		/// <summary>Count of inheritance cycles caught during resolution (tainted results are never cached).</summary>
		public int CycleEvents => cycleEvents;

		static Dictionary<string, string> CaseInsensitiveKeys(MiniYamlMirrorNodeMap map)
		{
			// Python `{k.lower(): k for k in map}` — later duplicates overwrite, so last wins.
			var result = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (var key in map.Keys)
				result[key.ToLowerInvariant()] = key;
			return result;
		}

		/// <summary>Parse and merge the manifest-listed files of a repository checkout.</summary>
		public static MiniYamlMirrorRuleset FromRepo(string repoRoot, string modId = "cameo")
		{
			var manifest = MiniYamlMirror.LoadManifest(repoRoot, modId);
			return FromManifest(manifest);
		}

		/// <summary>Parse and merge the file lists a <see cref="MiniYamlMirrorManifest"/> produced.</summary>
		public static MiniYamlMirrorRuleset FromManifest(MiniYamlMirrorManifest manifest)
		{
			return new MiniYamlMirrorRuleset(
				manifest.Rules.Select(MiniYamlMirror.LoadFile),
				manifest.Weapons.Select(MiniYamlMirror.LoadFile),
				manifest.Sequences.Select(MiniYamlMirror.LoadFile));
		}

		/// <summary>Build from texts opened through a virtual file system (one entry per manifest ref).</summary>
		public static MiniYamlMirrorRuleset FromTexts(
			IEnumerable<(string Name, string Text)> rulesDocuments,
			IEnumerable<(string Name, string Text)> weaponsDocuments,
			IEnumerable<(string Name, string Text)> sequenceDocuments = null)
		{
			return new MiniYamlMirrorRuleset(
				rulesDocuments.Select(d => MiniYamlMirror.LoadText(d.Text, d.Name)),
				weaponsDocuments.Select(d => MiniYamlMirror.LoadText(d.Text, d.Name)),
				sequenceDocuments?.Select(d => MiniYamlMirror.LoadText(d.Text, d.Name)));
		}

		/// <summary>Exact-key actor lookup, then case-insensitive fallback.</summary>
		public MiniYamlMirrorNode Actor(string name)
		{
			if (name != null && Actors.TryGetValue(name, out var node))
				return node;
			var lowered = name?.ToLowerInvariant() ?? "";
			return actorCi.TryGetValue(lowered, out var canonical) ? Actors[canonical] : null;
		}

		/// <summary>Exact-key weapon lookup, then case-insensitive fallback.</summary>
		public MiniYamlMirrorNode Weapon(string name)
		{
			if (name != null && Weapons.TryGetValue(name, out var node))
				return node;
			var lowered = name?.ToLowerInvariant() ?? "";
			return weaponCi.TryGetValue(lowered, out var canonical) ? Weapons[canonical] : null;
		}

		/// <summary>Exact-key sequence lookup, then case-insensitive fallback.</summary>
		public MiniYamlMirrorNode SequenceImage(string name)
		{
			if (name != null && Sequences.TryGetValue(name, out var node))
				return node;
			var lowered = name?.ToLowerInvariant() ?? "";
			return sequenceCi.TryGetValue(lowered, out var canonical) ? Sequences[canonical] : null;
		}

		/// <summary>[(inherit key, target), ...] in document order.</summary>
		public IReadOnlyList<(string Key, string Target)> InheritsOf(MiniYamlMirrorNode node)
		{
			var output = new List<(string, string)>();
			foreach (var c in node.Children)
				if (c.Key == "Inherits" || c.Key.StartsWith("Inherits@", StringComparison.Ordinal))
					output.Add((c.Key, c.Value));
			return output;
		}

		/// <summary>Fully resolve inheritance for an actor.</summary>
		public MiniYamlMirrorNode Resolve(string name)
		{
			return ResolveGeneric(name, Actor, new List<string>(), "");
		}

		/// <summary>Fully resolve inheritance for a weapon.</summary>
		public MiniYamlMirrorNode ResolveWeapon(string name)
		{
			return ResolveGeneric(name, Weapon, new List<string>(), "w:");
		}

		MiniYamlMirrorNode ResolveGeneric(string name, Func<string, MiniYamlMirrorNode> lookup, List<string> stack, string cachePrefix)
		{
			var cacheKey = cachePrefix + (name ?? "").ToLowerInvariant();
			if (resolveCache.TryGetValue(cacheKey, out var cached))
				return cached;
			var node = lookup(name);
			if (node == null)
				return null;
			if (stack.Any(s => string.Equals(s, name, StringComparison.OrdinalIgnoreCase)))
			{
				// Cycle guard fired: everything computed above this point is TAINTED
				// (missing this subtree) and must not be cached — otherwise the partial
				// result poisons later resolutions of unrelated actors.
				cycleEvents++;
				return null;
			}

			var before = cycleEvents;
			var acc = new List<MiniYamlMirrorNode>();
			var index = new Dictionary<string, MiniYamlMirrorNode>(StringComparer.Ordinal);
			stack.Add(name);
			try
			{
				foreach (var child in node.Children)
				{
					if (child.Key == "Inherits" || child.Key.StartsWith("Inherits@", StringComparison.Ordinal))
					{
						var parent = ResolveGeneric(child.Value, lookup, stack, cachePrefix);
						if (parent != null)
							MiniYamlMirror.MergeInto(acc, index, parent.Children);
						continue;
					}

					MiniYamlMirror.MergeInto(acc, index, new[] { child });
				}
			}
			finally
			{
				stack.RemoveAt(stack.Count - 1);
			}

			var resolved = new MiniYamlMirrorNode(node.Key, node.Value, acc, node.File, node.Line);
			if (cycleEvents == before)
				resolveCache[cacheKey] = resolved;
			return resolved;
		}

		/// <summary>Depth of the actor's Inherits chain (0 when absent or cyclic).</summary>
		public int InheritDepth(string name) => InheritDepth(name, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

		int InheritDepth(string name, HashSet<string> seen)
		{
			var node = Actor(name);
			if (node == null || seen.Contains(name))
				return 0;
			var next = new HashSet<string>(seen, StringComparer.OrdinalIgnoreCase) { name };
			var depth = 0;
			foreach (var (_, target) in InheritsOf(node))
				depth = Math.Max(depth, 1 + InheritDepth(target, next));
			return depth;
		}
	}
}
