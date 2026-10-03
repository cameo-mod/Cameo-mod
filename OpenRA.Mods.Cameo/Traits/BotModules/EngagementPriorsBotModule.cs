#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Globalization;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModuleLogic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	[TraitLocation(SystemActors.Player)]
	[Desc("The combat veto's tier-1 prior source (AI_ARCHITECTURE §12.31): reads the same committed",
		"ai/learned/arsenal_priors.yaml the unit builder's BotLearnedPriors serves, and maps the fitted",
		"trade percents onto IBotEngagementPriors corrections (percent x 10 = thousandths). Frozen at match",
		"start; a missing file or an unknown unit means 1000 — the pure BotCombatPredictor numbers.",
		"genericbot && combatveto only — classic never sees the provider.")]
	public class EngagementPriorsBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Mod-relative path of the priors file written by tools/ai/fit_arsenal_priors.py --write.")]
		public readonly string PriorsFile = "ai/learned/arsenal_priors.yaml";

		[Desc("Lowest correction (thousandths) a fitted prior may give an attacker's damage.")]
		public readonly int MinCorrectionMilli = 500;

		[Desc("Highest correction (thousandths) a fitted prior may give an attacker's damage.")]
		public readonly int MaxCorrectionMilli = 2000;

		public override object Create(ActorInitializer init) { return new EngagementPriorsBotModule(init.Self, this); }
	}

	public class EngagementPriorsBotModule : ConditionalTrait<EngagementPriorsBotModuleInfo>, IBotTick, IBotEngagementPriors
	{
		readonly World world;
		readonly OpenRA.Player player;
		ArsenalPriors priors = new();
		bool loaded;

		public EngagementPriorsBotModule(Actor self, EngagementPriorsBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (loaded)
				return;

			loaded = true;
			var fs = Game.ModData.DefaultFileSystem;
			if (!fs.Exists(Info.PriorsFile))
				return;

			using (var stream = fs.Open(Info.PriorsFile))
				priors = ArsenalPriors.Parse(MiniYaml.FromStream(stream, Info.PriorsFile));

			Log.Write("debug", $"AI {player.InternalName}: COMBAT VETO priors: {priors.PairCount} pairs, {priors.TypeCount} types");
		}

		int IBotEngagementPriors.CorrectionMilli(BotUnitProfile attacker, BotUnitProfile target)
		{
			if (IsTraitDisabled || !loaded)
				return 1000;

			// The fitted file is per (faction pair, own unit type) — the target profile is unused at this
			// granularity; a finer attacker x target table lands with a later fitter without an API change.
			var pct = priors.TradePercent(player.Faction?.InternalName, EnemyFaction(), attacker.Name);
			return Math.Clamp(pct * 10, Info.MinCorrectionMilli, Info.MaxCorrectionMilli);
		}

		// Same enemy-faction resolution as BotLearnedPriors: the main target's if a provider names one,
		// else the most common faction among enemy PLAYERS (never actors).
		string EnemyFaction()
		{
			var main = player.PlayerActor.TraitsImplementing<IBotMainTargetProvider>()
				.Select(p => p.MainTarget).FirstOrDefault(t => t != null);
			if (main != null)
				return main.Faction?.InternalName;

			return world.Players
				.Where(p => p != player && !p.NonCombatant && !p.Spectating && player.RelationshipWith(p) == PlayerRelationship.Enemy)
				.GroupBy(p => p.Faction?.InternalName).Where(g => g.Key != null)
				.OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
				.Select(g => g.Key).FirstOrDefault();
		}
	}
}
