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
using OpenRA.GameRules;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// The tier-1 offline-fitted engagement priors (tools/ai/fit_engagement_coefficients.py), free of world state
	/// so tests can drive it with an inline MiniYaml string. Every correction is a permille multiplier; 1000 = neutral.
	/// Missing keys are neutral — the file only ever nudges, it never switches the predictor (BotCombatPredictor).
	/// </summary>
	public sealed class EngagementPriors
	{
		public const int Neutral = 1000;

		readonly Dictionary<string, int> factors = new(StringComparer.Ordinal);

		public string StatFingerprint { get; private set; }
		public int StaticDefenceFactorPermille { get; private set; } = Neutral;
		public int FactorCount => factors.Count;

		/// <summary>Delivery×armour correction in permille; the <c>*|armor</c> row is the armour-wide fallback.</summary>
		public int FactorPermille(string delivery, string armor)
		{
			armor ??= "-";
			return factors.TryGetValue(delivery + "|" + armor, out var v) ? v
				: factors.TryGetValue("*|" + armor, out v) ? v
				: Neutral;
		}

		public static EngagementPriors Parse(IEnumerable<MiniYamlNode> nodes)
		{
			var priors = new EngagementPriors();
			var root = nodes.FirstOrDefault(n => n.Key == "EngagementPriors")
				?? nodes.FirstOrDefault(n => n.Key == "BotEngagementPriors");
			if (root == null)
				return priors;

			foreach (var node in root.Value.Nodes)
			{
				if (node.Key == "StatFingerprint")
					priors.StatFingerprint = node.Value.Value;
				else if (node.Key == "StaticDefenceFactorPermille")
				{
					if (int.TryParse(node.Value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sdf))
						priors.StaticDefenceFactorPermille = sdf;
				}
				else if (node.Key.StartsWith("Factor@", StringComparison.Ordinal)
					&& int.TryParse(node.Value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pct))
					priors.factors[node.Key["Factor@".Length..]] = pct;
				else if (node.Key.StartsWith("DeliveryArmour@", StringComparison.Ordinal)
					&& int.TryParse(node.Value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dpct))
				{
					// EMBER's native fitter key DeliveryArmour@<tag>__x__<armour> — the same axis verbatim.
					var cell = node.Key["DeliveryArmour@".Length..];
					var sep = cell.IndexOf("__x__", StringComparison.Ordinal);
					if (sep > 0)
						priors.factors[cell[..sep] + "|" + cell[(sep + 5)..]] = dpct;
				}
			}

			return priors;
		}
	}

	/// <summary>The pure verdict half of the veto — no World, no Actor, so tests drive every branch directly.</summary>
	public static class CombatVetoMath
	{
		public static int SurvivingPermille(double fraction) => (int)Math.Round(Math.Clamp(fraction, 0, 1) * 1000);

		/// <summary>The predicted trade on the engagement log's own scale (-1000..1000, thousandths of value exchanged).</summary>
		public static int PredictedTradeMilli(int ownValue, int enemyValue, double ownSurviving, double enemySurviving) =>
			EngagementScore.PredictedTrade(ownValue, enemyValue, SurvivingPermille(ownSurviving), SurvivingPermille(enemySurviving));

		/// <summary>Attack veto: something to fight is seen AND the predicted trade is worse than the threshold.</summary>
		public static bool VetoAttackVerdict(int predictedTradeMilli, int thresholdMilli, int seenFighterCount) =>
			seenFighterCount > 0 && predictedTradeMilli < thresholdMilli;

		/// <summary>
		/// Retreat veto: the squad can still hurt them AND the fastest seen fighter runs at least as fast as our
		/// slowest runner (scaled by the margin) — turning tail trades worse than standing. A squad that cannot
		/// shoot is never told to stand (it would die for nothing).
		/// </summary>
		public static bool VetoRetreatVerdict(int ownMinSpeed, int enemyMaxSpeed, bool ownCanFight, int marginPercent) =>
			ownCanFight && enemyMaxSpeed * Math.Max(0, marginPercent) / 100 >= ownMinSpeed;

		/// <summary>
		/// FNV-1a-64 over the canonical stat string of every armed actor — the fingerprint tier-1 stamps into
		/// engagement_priors.yaml so a rebalance silently discounts the file (DESIGN 19.13 "Rebalance").
		/// Canonical per actor (AttackBaseInfo only, name-sorted):
		/// "name;cost;hp;armor|" then per weapon "delivery,dptMilli,range|" then "armor=vs," sorted.
		/// </summary>
		public static string StatFingerprint(Ruleset rules)
		{
			var sb = new System.Text.StringBuilder();
			foreach (var actor in rules.Actors.Values.Where(a => a.HasTraitInfo<AttackBaseInfo>()).OrderBy(a => a.Name, StringComparer.Ordinal))
			{
				var p = BotUnitProfiles.Get(rules, actor);
				sb.Append(p.Name).Append(';').Append(p.Cost).Append(';').Append(p.Hp).Append(';').Append(p.Armor ?? "-").Append('|');
				foreach (var w in p.Weapons)
				{
					sb.Append(w.Delivery ?? "-").Append(',').Append((int)Math.Round(w.DamagePerTick * 1000)).Append(',').Append(w.Range.Length).Append('|');
					foreach (var kv in w.Versus.OrderBy(kv => kv.Key, StringComparer.Ordinal))
						sb.Append(kv.Key).Append('=').Append(kv.Value).Append(',');
				}
			}

			const ulong offset = 14695981039346656037UL;
			const ulong prime = 1099511628211UL;
			var hash = offset;
			foreach (var c in sb.ToString())
			{
				hash ^= c;
				hash *= prime;
			}

			return hash.ToString("x16", CultureInfo.InvariantCulture);
		}
	}

	[TraitLocation(SystemActors.Player)]
	[Desc("Tier-2 combat veto (DESIGN 19.13, AI_ARCHITECTURE 12.31): consults the shared BotCombatPredictor where a",
		"squad commits an attack or a retreat — blocks an attack whose predicted trade is below MinPredictedTradeMilli",
		"(seen forces incl. seen static defences, tier-1 priors applied when the fitted file is present) and a retreat",
		"that cannot outrun the threat. Learns nothing in-match; issues no orders — the squad manager owns the",
		"decision, the consult and the mission-card emission. No provider (classic, switch off) = bit-identical.")]
	public class CombatVetoBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Attack veto fires when the predicted trade (thousandths of value exchanged, -1000..1000) is worse than this.")]
		public readonly int MinPredictedTradeMilli = -350;

		[Desc("Seen enemy static defences within this many cells of the commit target join the predicted enemy force.")]
		public readonly int DefenceScanCells = 14;

		[Desc("Retreat veto margin (percent): the fastest seen fighter scaled by this reaching our slowest speed means 'cannot outrun'. 100 = any pursuer at least as fast.")]
		public readonly int OutrunMarginPercent = 100;

		[Desc("Mod-relative path of the tier-1 fitted priors; missing file or stat-fingerprint mismatch = neutral priors.")]
		public readonly string PriorsFile = "ai/learned/engagement_priors.yaml";

		public override object Create(ActorInitializer init) { return new CombatVetoBotModule(init.Self, this); }
	}

	public class CombatVetoBotModule : ConditionalTrait<CombatVetoBotModuleInfo>, IBotCombatVeto, IBotTick
	{
		readonly World world;
		readonly OpenRA.Player player;
		EngagementPriors priors = new();
		IBotFoggedEnemyProvider[] fogProviders = [];
		bool loaded;

		public CombatVetoBotModule(Actor self, CombatVetoBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		void IBotTick.BotTick(IBot bot)
		{
			EnsureLoaded();
		}

		// Frozen at match start (DESIGN 19.13): the file loads once, the stat fingerprint is checked once,
		// a mismatch discounts the whole file to neutral — a rebalanced ruleset never inherits stale priors.
		void EnsureLoaded()
		{
			if (loaded)
				return;

			loaded = true;
			fogProviders = player.PlayerActor.TraitsImplementing<IBotFoggedEnemyProvider>().ToArray();

			var fs = Game.ModData.DefaultFileSystem;
			if (!fs.Exists(Info.PriorsFile))
			{
				Log.Write("debug", $"AI {player.InternalName}: COMBAT_VETO priors: none ({Info.PriorsFile} missing), neutral");
				return;
			}

			EngagementPriors parsed;
			using (var stream = fs.Open(Info.PriorsFile))
				parsed = EngagementPriors.Parse(MiniYaml.FromStream(stream, Info.PriorsFile));

			var fingerprint = CombatVetoMath.StatFingerprint(world.Map.Rules);
			if (parsed.StatFingerprint != null && parsed.StatFingerprint != fingerprint)
			{
				Log.Write("debug", $"AI {player.InternalName}: COMBAT_VETO priors: discounted (fingerprint {parsed.StatFingerprint} != {fingerprint})");
				return;
			}

			priors = parsed;
			Log.Write("debug", $"AI {player.InternalName}: COMBAT_VETO priors: {priors.FactorCount} factors, static-defence {priors.StaticDefenceFactorPermille}‰");
		}

		double Factor(BotUnitProfile attacker, BotWeaponProfile weapon, BotUnitProfile target)
		{
			var f = priors.FactorPermille(weapon.Delivery, target.Armor);
			if (attacker.IsBuilding)
				f = (int)(f * (long)priors.StaticDefenceFactorPermille / 1000);

			return f / 1000.0;
		}

		static List<(BotUnitProfile Unit, int Count)> Force(IEnumerable<Actor> actors, Ruleset rules, Func<Actor, bool> filter) =>
			actors.Where(filter).GroupBy(a => a.Info).Select(g => (BotUnitProfiles.Get(rules, g.Key), g.Count())).ToList();

		static int ForceValue(IReadOnlyList<(BotUnitProfile Unit, int Count)> force) => force.Sum(f => f.Unit.Cost * f.Count);

		bool Fogged => SquadManagerBotModuleCA.FoggedScansActive(IsTraitDisabled, fogProviders);

		// Seen static defences near the commit target: they are the fights squads actually lose walking in.
		// Same fog standard as the squad manager's own scans (IsNotHiddenUnit == CanBeViewedByPlayer).
		List<Actor> SeenDefencesNear(WPos pos, IReadOnlyList<Actor> alreadySeen)
		{
			var seen = new HashSet<Actor>(alreadySeen);
			var fogged = Fogged;
			return world.FindActorsInCircle(pos, WDist.FromCells(Info.DefenceScanCells))
				.Where(a => !a.IsDead && a.IsInWorld
					&& a.Owner != null && player.RelationshipWith(a.Owner) == PlayerRelationship.Enemy
					&& a.Info.HasTraitInfo<BuildingInfo>() && a.Info.HasTraitInfo<AttackBaseInfo>()
					&& !seen.Contains(a)
					&& (!fogged || a.CanBeViewedByPlayer(player)))
				.ToList();
		}

		bool IBotCombatVeto.TryVetoAttack(IReadOnlyList<Actor> ownUnits, IReadOnlyList<Actor> seenEnemies, WPos targetPos, out CombatVetoVerdict verdict)
		{
			EnsureLoaded();
			verdict = null;

			var rules = world.Map.Rules;
			var foes = seenEnemies.Concat(SeenDefencesNear(targetPos, seenEnemies)).ToList();
			var own = Force(ownUnits, rules, a => !a.IsDead);
			var enemy = Force(foes, rules, a => a.Info.HasTraitInfo<AttackBaseInfo>());

			var prediction = BotCombatPredictor.Predict(own, enemy, Factor);
			var ownValue = ForceValue(own);
			var enemyValue = ForceValue(enemy);
			var trade = CombatVetoMath.PredictedTradeMilli(ownValue, enemyValue, prediction.OwnSurvivingFraction, prediction.EnemySurvivingFraction);

			if (!CombatVetoMath.VetoAttackVerdict(trade, Info.MinPredictedTradeMilli, enemy.Count))
				return false;

			verdict = new CombatVetoVerdict
			{
				Reason = BotMissionReasons.BelowThreshold,
				OwnValue = ownValue,
				Detail = $"trade={trade};ratio={prediction.Ratio.ToString("F2", CultureInfo.InvariantCulture)};ownv={ownValue};foev={enemyValue};defences={foes.Count - seenEnemies.Count}"
			};

			return true;
		}

		bool IBotCombatVeto.TryVetoRetreat(IReadOnlyList<Actor> ownUnits, IReadOnlyList<Actor> seenEnemies, out CombatVetoVerdict verdict)
		{
			EnsureLoaded();
			verdict = null;

			var rules = world.Map.Rules;
			var own = Force(ownUnits, rules, a => !a.IsDead);
			var enemy = Force(seenEnemies, rules, a => a.Info.HasTraitInfo<AttackBaseInfo>());

			// The tail is what gets caught: our slowest runner vs their fastest fighter.
			var speeds = own.Where(o => o.Unit.Speed > 0).Select(o => o.Unit.Speed).ToList();
			var ownMin = speeds.Count > 0 ? speeds.Min() : 0;
			var foeMax = enemy.Count > 0 ? enemy.Max(e => e.Unit.Speed) : 0;
			var ownCanFight = own.Any(o => o.Unit.Weapons.Length > 0);
			var ownValue = ForceValue(own);

			if (!CombatVetoMath.VetoRetreatVerdict(ownMin, foeMax, ownCanFight, Info.OutrunMarginPercent))
				return false;

			verdict = new CombatVetoVerdict
			{
				Reason = BotMissionReasons.CantOutrun,
				OwnValue = ownValue,
				Detail = $"own_min_speed={ownMin};foe_max_speed={foeMax};ownv={ownValue};margin={Info.OutrunMarginPercent}"
			};

			return true;
		}
	}
}
