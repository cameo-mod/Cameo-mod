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
				Add(node.Key, "RetreatRatioPct@", result.retreat, value);
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

		public bool TryRetreatRatioPct(string own, string enemy, out int value) => TryLookup(retreat, own, enemy, out value);
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
		"Only mounted for generic bots by the BV_learn_fight increment; classic remains unchanged.")]
	public class FightThresholdsBotModuleInfo : ConditionalTraitInfo
	{
		public readonly string LearnedFile = "ai/learned/fight_learning.yaml";
		public readonly int MinimumRetreatRatioPct = 1;
		public readonly int MaximumRetreatRatioPct = 100;
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
			var fs = Game.ModData.DefaultFileSystem;
			if (!fs.Exists(Info.LearnedFile))
				return;
			try
			{
				using var stream = fs.Open(Info.LearnedFile);
				thresholds = BotFightThresholds.Parse(MiniYaml.FromStream(stream, Info.LearnedFile));
			}
			catch (Exception)
			{
				thresholds = new BotFightThresholds();
				Log.Write("debug", "Fight learning ignored invalid artifact.");
			}
		}

		int IBotFightThresholds.RetreatRatioPct(string own, string enemy, int fallback)
		{
			if (IsTraitDisabled || !loaded || !thresholds.TryRetreatRatioPct(own, enemy, out var value))
				return fallback;
			return Math.Clamp(value, Info.MinimumRetreatRatioPct, Info.MaximumRetreatRatioPct);
		}

		int IBotFightThresholds.EngageMarginPct(string own, string enemy, int fallback)
		{
			if (IsTraitDisabled || !loaded || !thresholds.TryEngageMarginPct(own, enemy, out var value))
				return fallback;
			return Math.Clamp(value, Info.MinimumEngageMarginPct, Info.MaximumEngageMarginPct);
		}
	}
}
