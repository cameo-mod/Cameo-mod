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
		readonly Dictionary<string, int> retreat = new(StringComparer.Ordinal);
		readonly Dictionary<string, int> engage = new(StringComparer.Ordinal);

		public static BotFightThresholds Parse(IEnumerable<MiniYamlNode> nodes)
		{
			var result = new BotFightThresholds();
			var root = nodes.FirstOrDefault(n => n.Key == "BotFightLearning");
			if (root == null)
				return result;

			foreach (var node in root.Value.Nodes)
			{
				if (!int.TryParse(node.Value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
					continue;
				if (node.Key.StartsWith("RetreatRatioPct@", StringComparison.Ordinal))
					result.retreat[node.Key["RetreatRatioPct@".Length..]] = value;
				else if (node.Key.StartsWith("EngageMarginPct@", StringComparison.Ordinal))
					result.engage[node.Key["EngageMarginPct@".Length..]] = value;
			}

			return result;
		}

		public int RetreatRatioPct(string own, string enemy, int fallback) => Lookup(retreat, own, enemy, fallback);
		public int EngageMarginPct(string own, string enemy, int fallback) => Lookup(engage, own, enemy, fallback);

		static int Lookup(Dictionary<string, int> source, string own, string enemy, int fallback)
		{
			var exact = string.IsNullOrEmpty(enemy) ? null : own + "__vs__" + enemy;
			if (exact != null && source.TryGetValue(exact, out var value))
				return value;
			if (!string.IsNullOrEmpty(own) && source.TryGetValue(own, out value))
				return value;
			return source.TryGetValue("any", out value) ? value : fallback;
		}
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
		readonly OpenRA.Player player;
		BotFightThresholds thresholds = new();
		bool loaded;

		public FightThresholdsBotModule(Actor self, FightThresholdsBotModuleInfo info)
			: base(info) { player = self.Owner; }

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
			catch (Exception e)
			{
				thresholds = new BotFightThresholds();
				Log.Write("debug", $"AI {player.InternalName}: fight learning ignored ({e.Message})");
			}
		}

		int IBotFightThresholds.RetreatRatioPct(string own, string enemy, int fallback) =>
			IsTraitDisabled || !loaded ? fallback : Math.Clamp(thresholds.RetreatRatioPct(own, enemy, fallback), Info.MinimumRetreatRatioPct, Info.MaximumRetreatRatioPct);

		int IBotFightThresholds.EngageMarginPct(string own, string enemy, int fallback) =>
			IsTraitDisabled || !loaded ? fallback : Math.Clamp(thresholds.EngageMarginPct(own, enemy, fallback), Info.MinimumEngageMarginPct, Info.MaximumEngageMarginPct);
	}
}
