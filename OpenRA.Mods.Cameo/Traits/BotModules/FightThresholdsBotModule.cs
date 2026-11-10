#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors.
 * This file is part of OpenRA, which is free software. It is made available
 * under the terms of the GNU General Public License, version 3 or later.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>Parser and read-only consumer for tools/ai/fit_fight_learning.py output.</summary>
	public sealed class BotFightThresholds
	{
		const int Schema = 1;
		const int MinimumEvidence = 150;
		readonly Dictionary<string, int> retreat = new(StringComparer.Ordinal);
		readonly Dictionary<string, int> engage = new(StringComparer.Ordinal);
		readonly Dictionary<string, int> evidence = new(StringComparer.Ordinal);

		public static BotFightThresholds Parse(IEnumerable<MiniYamlNode> nodes)
		{
			var result = new BotFightThresholds();
			var root = nodes.FirstOrDefault(n => n.Key == "BotFightLearning");
			if (root == null)
				return result;
			var schema = root.Value.Nodes.FirstOrDefault(n => n.Key == "Schema");
			if (schema == null || !int.TryParse(schema.Value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var version) || version != Schema)
				return result;

			foreach (var node in root.Value.Nodes)
			{
				if (!int.TryParse(node.Value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
					continue;

				Add(node.Key, "Evidence@", result.evidence, value);
				// TIER-01 D1: the learned retreat value is a multiplier in percent (100 = 1.00). An absolute RetreatRatioPct@ row of
				// the former format is not read, so such a file falls back to the tier value instead of being misread.
				Add(node.Key, "RetreatMultPct@", result.retreat, value);
				Add(node.Key, "EngageMarginPct@", result.engage, value);
			}

			return result;
		}

		static void Add(string key, string prefix, Dictionary<string, int> destination, int value)
		{
			if (!key.StartsWith(prefix, StringComparison.Ordinal))
				return;

			var scope = key[prefix.Length..];
			if (ScopeIsSafe(scope))
				destination[scope] = value;
		}

		// This is a defensive runtime boundary. The closed learned-artifact audit validates
		// factions against rules vocabulary; here we also reject arbitrary text and never
		// let a malformed artifact become a player-derived key.
		static bool ScopeIsSafe(string scope)
		{
			if (scope == "any")
				return true;

			var halves = scope.Split("__vs__", StringSplitOptions.None);
			if (halves.Length is < 1 or > 2 || halves.Any(string.IsNullOrEmpty))
				return false;

			return halves.All(part => part.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-' or '.'));
		}

		public bool TryRetreatMultPct(string own, string enemy, out int value) => TryLookup(retreat, own, enemy, out value);
		public bool TryEngageMarginPct(string own, string enemy, out int value) => TryLookup(engage, own, enemy, out value);

		bool TryLookup(Dictionary<string, int> source, string own, string enemy, out int value)
		{
			value = 0;
			var exact = string.IsNullOrEmpty(enemy) ? null : own + "__vs__" + enemy;
			if (exact != null && HasEvidence(exact) && source.TryGetValue(exact, out value))
				return true;
			if (!string.IsNullOrEmpty(own) && HasEvidence(own) && source.TryGetValue(own, out value))
				return true;

			var family = own.IndexOf('_') is var separator && separator > 0 ? "family_" + own[..separator] : null;
			if (family != null && HasEvidence(family) && source.TryGetValue(family, out value))
				return true;
			return HasEvidence("any") && source.TryGetValue("any", out value);
		}

		bool HasEvidence(string scope) => evidence.TryGetValue(scope, out var samples) && samples >= MinimumEvidence;
	}

	[TraitLocation(SystemActors.Player)]
	[Desc("Reads frozen offline-fitted fight thresholds. Missing or sparse scope rows return existing thresholds.",
		"Only mounted for generic bots by the BR_learn_fight increment; classic remains unchanged.")]
	public class FightThresholdsBotModuleInfo : ConditionalTraitInfo
	{
		public readonly string LearnedFile = "ai/learned/fight_learning.yaml";
		[Desc("TIER-01 D1: the effective retreat percent (base x learned multiplier / 100) is clamped to this range. 0 = never retreat.")]
		public readonly int MinimumRetreatRatioPct = 0;
		public readonly int MaximumRetreatRatioPct = 100;
		[Desc("TIER-01 D1: the learned RetreatMultPct row (percent of the base, 100 = 1.00) is clamped to this range first.")]
		public readonly int MinimumRetreatMultPct = 0;
		public readonly int MaximumRetreatMultPct = 200;
		public readonly int MinimumEngageMarginPct = 100;
		public readonly int MaximumEngageMarginPct = 300;
		public override object Create(ActorInitializer init) => new FightThresholdsBotModule(init.Self, this);
	}

	public class FightThresholdsBotModule : ConditionalTrait<FightThresholdsBotModuleInfo>, IBotTick, IBotFightThresholds
	{
		BotFightThresholds thresholds = new();
		bool loaded;

		public FightThresholdsBotModule(Actor self, FightThresholdsBotModuleInfo info)
			: base(info) { }

		void IBotTick.BotTick(IBot bot)
		{
			if (loaded)
				return;
			loaded = true;
			thresholds = Load(Game.ModData.DefaultFileSystem, Info.LearnedFile);
		}

		// M9: the learned file lives nested inside the cameo package — the bare mod-relative path can
		// never hit the top-level-only file index, so Exists/Open (and the parser diagnostic source)
		// go through the one resolution point. A package-qualified override passes through unchanged.
		internal static BotFightThresholds Load(IReadOnlyFileSystem fs, string file)
		{
			var resolved = LearnedFilePath.Resolve(file);
			if (!fs.Exists(resolved))
				return new BotFightThresholds();
			try
			{
				using var stream = fs.Open(resolved);
				return BotFightThresholds.Parse(MiniYaml.FromStream(stream, resolved));
			}
			catch (Exception)
			{
				Log.Write("debug", "Fight learning ignored invalid artifact.");
				return new BotFightThresholds();
			}
		}

		// TIER-01 D1: a learned row scales the caller's base (the squad manager's tier value with the in-match delta, or CombatVeto's
		// own engage bar). No row returns the base unchanged, which is the same as a row of 100.
		int IBotFightThresholds.RetreatRatioPct(string own, string enemy, int fallback)
		{
			if (IsTraitDisabled || !loaded || !thresholds.TryRetreatMultPct(own, enemy, out var value))
				return fallback;
			return EffectiveRetreatPct(fallback, value, Info);
		}

		/// <summary>TIER-01 D1: base x clamp(mult, 0..200) / 100, clamped to the retreat range (0..100).</summary>
		internal static int EffectiveRetreatPct(int basePct, int multPct, FightThresholdsBotModuleInfo info) =>
			Math.Clamp((int)((long)basePct * Math.Clamp(multPct, info.MinimumRetreatMultPct, info.MaximumRetreatMultPct) / 100),
				info.MinimumRetreatRatioPct, info.MaximumRetreatRatioPct);

		int IBotFightThresholds.EngageMarginPct(string own, string enemy, int fallback)
		{
			if (IsTraitDisabled || !loaded || !thresholds.TryEngageMarginPct(own, enemy, out var value))
				return fallback;
			return Math.Clamp(value, Info.MinimumEngageMarginPct, Info.MaximumEngageMarginPct);
		}
	}
}
