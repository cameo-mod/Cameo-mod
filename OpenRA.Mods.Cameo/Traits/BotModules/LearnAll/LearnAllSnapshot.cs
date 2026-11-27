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
using System.Globalization;
using System.Linq;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// LEARN-ALL-02 P1b: the accepted snapshot (mods/cameo/ai/learned/learn_all.yaml, schema learn_all_v1). The release reads it once at match start and never
	/// writes it; the DEV fitter (tools/ai/fit_learn_all.py, P1c) is the only writer. Integers only, deterministic order. Layout:
	/// <code>
	/// BotLearnAll:
	///   Version: 1
	///   Applied:                       (DEV bookkeeping: one child per applied game|personality|knob key; the release ignores it)
	///     g1|rush|k:
	///   Knob@rush__wave_role.frontline:   (personality__knob; personality "any" is the global table)
	///     Global: 1041
	///     Own@gdi: 1041
	///     Enemy@nod: 1041
	///     Style@boom: 1041
	/// </code>
	/// A missing file, a foreign root, another version or a damaged value yields an empty table (every consumer neutral); an unreadable estimate is dropped, never zero.
	/// </summary>
	public static class LearnAllSnapshot
	{
		public const string Root = "BotLearnAll";
		public const int Version = 1;
		const string KnobPrefix = "Knob@";
		const string Separator = "__";

		public static LearnTable Parse(IEnumerable<MiniYamlNode> nodes)
		{
			var table = new LearnTable();
			var root = nodes?.FirstOrDefault(n => n.Key == Root);
			if (root == null)
				return table;

			var version = root.Value.Nodes.FirstOrDefault(n => n.Key == "Version");
			if (version == null || !int.TryParse(version.Value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) || v != Version)
				return table;

			foreach (var node in root.Value.Nodes)
			{
				if (node.Key == "Applied")
				{
					foreach (var applied in node.Value.Nodes)
						table.MarkApplied(applied.Key);
				}
				else if (node.Key.StartsWith(KnobPrefix, StringComparison.Ordinal))
				{
					var key = node.Key[KnobPrefix.Length..];
					var split = key.IndexOf(Separator, StringComparison.Ordinal);
					if (split <= 0 || split + Separator.Length >= key.Length)
						continue;

					var axes = table.Install(key[..split], key[(split + Separator.Length)..]);
					foreach (var child in node.Value.Nodes)
					{
						if (!int.TryParse(child.Value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value <= 0)
							continue;

						value = LearnAllMath.ClampStore(value);
						if (child.Key == "Global")
							axes.Global = value;
						else if (child.Key.StartsWith("Own@", StringComparison.Ordinal))
							axes.OwnFaction[child.Key["Own@".Length..]] = value;
						else if (child.Key.StartsWith("Enemy@", StringComparison.Ordinal))
							axes.EnemyFaction[child.Key["Enemy@".Length..]] = value;
						else if (child.Key.StartsWith("Style@", StringComparison.Ordinal))
							axes.Style[child.Key["Style@".Length..]] = value;
					}
				}
			}

			return table;
		}

		public static LearnTable ParseText(string text, string name = "learn_all") =>
			Parse(string.IsNullOrWhiteSpace(text) ? null : MiniYaml.FromString(text, name));
	}
}
