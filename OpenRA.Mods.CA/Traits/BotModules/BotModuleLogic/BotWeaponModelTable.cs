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
using System.Runtime.CompilerServices;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// Per-Ruleset effective-weapon-model table. Builds the MiniYaml mirror
	/// (manifest rules + weapons) lazily, measures the corpus (armor census,
	/// shield share, median weapon range) and memoises one BotWeaponModel per
	/// weapon name. The classic predictor path never touches this — the table
	/// is only constructed when a caller opts into the effective model.
	/// </summary>
	public sealed class BotWeaponModelTable
	{
		static readonly ConditionalWeakTable<Ruleset, Holder> Tables = new();

		sealed class Holder
		{
			public BotWeaponModelTable Table;
		}

		readonly MiniYamlMirrorRuleset rs;
		readonly Dictionary<string, BotWeaponModel> cache = new(StringComparer.OrdinalIgnoreCase);

		public BotTargetModel Target { get; }

		/// <summary>False when the manifest could not be located (packaged install,
		/// exotic host). Compute returns null for everything; callers fall back to
		/// classic numbers.</summary>
		public bool Available => rs != null;

		BotWeaponModelTable(MiniYamlMirrorRuleset ruleset)
		{
			rs = ruleset;
			if (rs != null)
				Target = new BotTargetModel(rs);
		}

		public static readonly BotWeaponModelTable Unavailable = new(null);

		/// <summary>Build a table straight from the manifest file lists
		/// (test path — no engine Ruleset required).</summary>
		public static BotWeaponModelTable FromFiles(IEnumerable<string> weaponsYamlPaths, IEnumerable<string> rulesYamlPaths)
		{
			return new BotWeaponModelTable(new MiniYamlMirrorRuleset(
				rulesYamlPaths.Select(MiniYamlMirror.LoadFile),
				weaponsYamlPaths.Select(MiniYamlMirror.LoadFile)));
		}

		/// <summary>The table for one engine Ruleset, built lazily on first request.
		/// Returns BotWeaponModelTable.Unavailable when the repo manifest cannot be
		/// located — flag-on consumers must then fall back to classic numbers.</summary>
		public static BotWeaponModelTable Get(Ruleset rules)
		{
			if (rules == null)
				return Unavailable;
			return Tables.GetValue(rules, _ => new Holder { Table = BuildForCurrentMod() }).Table;
		}

		static BotWeaponModelTable BuildForCurrentMod()
		{
			try
			{
				var modId = "cameo";
				try
				{
					var id = Game.ModData?.Manifest?.Id;
					if (!string.IsNullOrEmpty(id))
						modId = id;
				}
				catch
				{
					// Game.ModData may be null in exotic hosts; default to cameo.
				}

				var root = FindRepoRoot(modId);
				if (root == null)
					return Unavailable;
				return new BotWeaponModelTable(MiniYamlMirrorRuleset.FromRepo(root, modId));
			}
			catch (Exception e)
			{
				Log.Write("debug", $"BotWeaponModelTable: manifest build failed: {e.Message}");
				return Unavailable;
			}
		}

		/// <summary>Walk up from the executable / working directory for
		/// mods/&lt;modId&gt;/mod.yaml — the dev-worktree layout the mirror needs.</summary>
		static string FindRepoRoot(string modId)
		{
			var bases = new[]
			{
				AppDomain.CurrentDomain.BaseDirectory,
				Directory.GetCurrentDirectory()
			};

			foreach (var start in bases)
			{
				if (string.IsNullOrEmpty(start))
					continue;
				var dir = new DirectoryInfo(start);
				for (var i = 0; i < 12 && dir != null; i++, dir = dir.Parent)
					if (File.Exists(Path.Combine(dir.FullName, "mods", modId, "mod.yaml")))
						return dir.FullName;
			}

			return null;
		}

		/// <summary>The per-weapon model record — same numbers the Python pipeline
		/// writes into docs/balance/derived. Null when the weapon is unresolvable
		/// or the table is unavailable.</summary>
		public BotWeaponModel Compute(string weaponName)
		{
			if (rs == null || weaponName == null)
				return null;
			if (cache.TryGetValue(weaponName, out var cached))
				return cached;

			BotWeaponModel model;
			try
			{
				model = BotWeaponModel.Compute(rs, Target, Target.MedianWeaponRange, weaponName);
			}
			catch (Exception e)
			{
				// A weapon the model rejects (the Python pipeline would have raised and
				// dropped it too) degrades to "unmodelled": the predictor's classic path
				// must never die inside a bot tick on one malformed yaml field.
				model = new BotWeaponModel
				{
					Weapon = weaponName,
					IsModelled = false,
					ModelLimitations = new[] { $"model_error:{e.GetType().Name}" },
				};
			}

			cache[weaponName] = model;
			return model;
		}

		/// <summary>The actor's charge-up record (extract_stats.charge_up), or null.</summary>
		public BotChargeUp ChargeUpFor(string actorName)
		{
			if (rs == null || actorName == null)
				return null;
			var resolved = rs.Resolve(actorName);
			return resolved == null ? null : BotChargeUp.For(resolved);
		}
	}
}
