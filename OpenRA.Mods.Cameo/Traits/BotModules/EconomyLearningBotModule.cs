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
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>Read-only parser for the reviewed output of tools/ai/fit_economy_learning.py.</summary>
	public sealed class BotEconomyLearning
	{
		const int Schema = 1;
		readonly Dictionary<string, int> expansion = new(StringComparer.Ordinal);
		readonly Dictionary<string, int> harvesters = new(StringComparer.Ordinal);
		readonly Dictionary<string, int> production = new(StringComparer.Ordinal);
		readonly Dictionary<string, int> creditFloat = new(StringComparer.Ordinal);
		readonly Dictionary<string, int> expansionEvidence = new(StringComparer.Ordinal);
		readonly Dictionary<string, int> harvesterEvidence = new(StringComparer.Ordinal);
		readonly Dictionary<string, int> productionEvidence = new(StringComparer.Ordinal);
		readonly Dictionary<string, int> creditFloatEvidence = new(StringComparer.Ordinal);

		public static BotEconomyLearning Parse(IEnumerable<MiniYamlNode> nodes)
		{
			var result = new BotEconomyLearning();
			var root = nodes.FirstOrDefault(n => n.Key == "BotEconomyLearning");
			if (root == null)
				return result;
			var schema = root.Value.Nodes.FirstOrDefault(n => n.Key == "Schema");
			if (schema == null || !int.TryParse(schema.Value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var version) || version != Schema)
				return result;

			foreach (var node in root.Value.Nodes)
			{
				if (!int.TryParse(node.Value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
					continue;
				Add(node.Key, "ExpansionCashDivisor@", result.expansion, value);
				Add(node.Key, "HarvesterLimit@", result.harvesters, value);
				Add(node.Key, "ProductionCashThreshold@", result.production, value);
				Add(node.Key, "CreditFloat@", result.creditFloat, value);
				Add(node.Key, "ExpansionEvidence@", result.expansionEvidence, value);
				Add(node.Key, "HarvesterEvidence@", result.harvesterEvidence, value);
				Add(node.Key, "ProductionEvidence@", result.productionEvidence, value);
				Add(node.Key, "CreditFloatEvidence@", result.creditFloatEvidence, value);
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

		static bool ScopeIsSafe(string scope)
		{
			if (scope == "any")
				return true;
			var halves = scope.Split("__vs__", StringSplitOptions.None);
			if (halves.Length is < 1 or > 2 || halves.Any(string.IsNullOrEmpty))
				return false;
			return halves.All(part => part.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-' or '.'));
		}

		public bool TryExpansionCashDivisor(string own, string enemy, out int value) => TryLookup(expansion, expansionEvidence, 100, own, enemy, out value);
		public bool TryHarvesterLimit(string own, string enemy, out int value) => TryLookup(harvesters, harvesterEvidence, 120, own, enemy, out value);
		public bool TryProductionCashThreshold(string own, string enemy, out int value) => TryLookup(production, productionEvidence, 120, own, enemy, out value);
		public bool TryCreditFloat(string own, string enemy, out int value) => TryLookup(creditFloat, creditFloatEvidence, 150, own, enemy, out value);

		public int ExpansionCashDivisor(string own, string enemy, int fallback) => TryExpansionCashDivisor(own, enemy, out var value) ? value : fallback;
		public int HarvesterLimit(string own, string enemy, int fallback) => TryHarvesterLimit(own, enemy, out var value) ? value : fallback;
		public int ProductionCashThreshold(string own, string enemy, int fallback) => TryProductionCashThreshold(own, enemy, out var value) ? value : fallback;
		public int CreditFloat(string own, string enemy, int fallback) => TryCreditFloat(own, enemy, out var value) ? value : fallback;

		static bool TryLookup(Dictionary<string, int> source, Dictionary<string, int> evidence, int minimum, string own, string enemy, out int value)
		{
			value = 0;
			var exact = string.IsNullOrEmpty(enemy) ? null : own + "__vs__" + enemy;
			if (exact != null && HasEvidence(evidence, exact, minimum) && source.TryGetValue(exact, out value))
				return true;
			if (!string.IsNullOrEmpty(own) && HasEvidence(evidence, own, minimum) && source.TryGetValue(own, out value))
				return true;
			var family = own.IndexOf('_') is var separator && separator > 0 ? "family_" + own[..separator] : null;
			if (family != null && HasEvidence(evidence, family, minimum) && source.TryGetValue(family, out value))
				return true;
			return HasEvidence(evidence, "any", minimum) && source.TryGetValue("any", out value);
		}

		static bool HasEvidence(Dictionary<string, int> evidence, string scope, int minimum) => evidence.TryGetValue(scope, out var samples) && samples >= minimum;
	}

	[TraitLocation(SystemActors.Player)]
	[Desc("Reads frozen offline-fitted economy thresholds. Missing, invalid, or sparse scope rows retain authored values.",
		"BW_learn_econ mounts this only for generic bots; classic has no provider.")]
	public class EconomyLearningBotModuleInfo : ConditionalTraitInfo
	{
		public readonly string LearnedFile = "ai/learned/economy_learning.yaml";
		public readonly int MinimumExpansionCashDivisor = 1000;
		public readonly int MaximumExpansionCashDivisor = 50000;
		public readonly int MinimumHarvesterLimit = 1;
		public readonly int MaximumHarvesterLimit = 64;
		public readonly int MinimumProductionCashThreshold = 0;
		public readonly int MaximumProductionCashThreshold = 50000;
		public readonly int MinimumCreditFloat = 0;
		public readonly int MaximumCreditFloat = 50000;
		public override object Create(ActorInitializer init) => new EconomyLearningBotModule(init.Self, this);
	}

	public class EconomyLearningBotModule : ConditionalTrait<EconomyLearningBotModuleInfo>, IBotTick, IBotEconomyLearning
	{
		readonly World world;
		readonly OpenRA.Player player;
		BotEconomyLearning learned = new();
		string enemyFaction = "";
		bool loaded;

		public EconomyLearningBotModule(Actor self, EconomyLearningBotModuleInfo info)
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
			// The existing public-faction helper deliberately returns empty for Random/hidden factions.
			// Empty selects faction/global priors rather than inventing an identity-bearing key.
			enemyFaction = PlanBanditBotModule.EnemyFactionOf(world, player);
			var fs = Game.ModData.DefaultFileSystem;
			if (!fs.Exists(Info.LearnedFile))
				return;
			try
			{
				using var stream = fs.Open(Info.LearnedFile);
				learned = BotEconomyLearning.Parse(MiniYaml.FromStream(stream, Info.LearnedFile));
			}
			catch (Exception)
			{
				learned = new BotEconomyLearning();
				Log.Write("debug", "Economy learning ignored invalid artifact.");
			}
		}

		string OwnFaction => player.Faction?.InternalName ?? "";
		int IBotEconomyLearning.ExpansionCashDivisor(int fallback) => Value(learned.TryExpansionCashDivisor(OwnFaction, enemyFaction, out var value), value, Info.MinimumExpansionCashDivisor, Info.MaximumExpansionCashDivisor, fallback);
		int IBotEconomyLearning.HarvesterLimit(int fallback) => Value(learned.TryHarvesterLimit(OwnFaction, enemyFaction, out var value), value, Info.MinimumHarvesterLimit, Info.MaximumHarvesterLimit, fallback);
		int IBotEconomyLearning.ProductionCashThreshold(int fallback) => Value(learned.TryProductionCashThreshold(OwnFaction, enemyFaction, out var value), value, Info.MinimumProductionCashThreshold, Info.MaximumProductionCashThreshold, fallback);
		int IBotEconomyLearning.CreditFloat(int fallback) => Value(learned.TryCreditFloat(OwnFaction, enemyFaction, out var value), value, Info.MinimumCreditFloat, Info.MaximumCreditFloat, fallback);

		int Value(bool found, int value, int minimum, int maximum, int fallback) => IsTraitDisabled || !loaded || !found ? fallback : Math.Clamp(value, minimum, maximum);
	}
}
