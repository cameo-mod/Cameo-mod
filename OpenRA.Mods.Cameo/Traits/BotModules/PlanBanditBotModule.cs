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
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>The resolved tier-3 choice of one match, for the situation log. Frozen once drawn.</summary>
	public sealed class PlanBanditSnapshot
	{
		public int Tick;
		public string Scope = "";
		public string PersonalityArm = "", PlanArm = "";
		public bool PersonalityPinned;

		/// <summary>Which watched decision-side module conditions were granted when the bandits resolved
		/// (combatveto, inmatchadapt, ...), "+"-joined and sorted; "none" when none were. Record-only: every armed
		/// decision module filters which engagements ever exist, so a fitter pass must be able to condition on the set.</summary>
		public string ArmedModules = "none";
	}

	[TraitLocation(SystemActors.Player)]
	[Desc("Tier-3 pooled bandits (fleet orders 2026-10-03): at match start, Thompson-sample one personality arm and one attack-plan arm",
		"over the engagement-log posteriors in LearnedFile, pooled any -> family -> faction -> matchup with a safety floor (arms whose",
		"evidence-backed LCB is below MinSafetyLcb cannot win). The personality arm pins BotPersonalityController for the match; the plan arm",
		"adds one bounded knob overlay inside BuildOrderKnobsBotModule. Both choices freeze at draw time, are deterministic per match seed,",
		"and are recorded on the situation snapshot for the offline fitter — along with which WatchConditions decision-side modules were",
		"armed, so the fitter can condition on survivorship filters. No orders, no actor access — record and steering only.")]
	public class PlanBanditBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Mod-relative path of the learned posteriors written by tools/ai/tune_plan_bandits.py --write.")]
		public readonly string LearnedFile = "ai/learned/plan_bandits.yaml";

		[Desc("Personality bandit arms: personality names (no prefix) that must match a BotPersonalityController condition.",
			"Empty = the personality bandit does not run and nothing is pinned.")]
		public readonly string[] PersonalityArms = { "rush", "turtle", "tech", "expansion", "steamroller", "guerrilla" };

		[FieldLoader.LoadUsing(nameof(LoadPlanArms))]
		[Desc("Attack-plan arms: plan name -> knob -> multiplier in thousandths (1000 = neutral). The chosen overlay multiplies the",
			"build-order knob vector next to preset x learned x jitter. Empty = the plan bandit does not run.")]
		public readonly Dictionary<string, Dictionary<string, int>> PlanArms = [];

		[Desc("Parent pseudo-observation cap for partial pooling: a child scope's stats are shrunk toward its parent at most this weight.")]
		public readonly int PriorCount = 8;

		[Desc("One-sided confidence z for the safety floor's lower confidence bound (1.64 ~ 95%).")]
		public readonly int LcbZ = 164;

		[Desc("An arm needs at least this many pooled observations before the safety floor may exclude it.")]
		public readonly int MinEvidence = 4;

		[Desc("Safety floor, in engagement milli: an evidenced arm whose LCB is below this cannot be chosen.")]
		public readonly int MinSafetyLcb = -250;

		[Desc("Conditions of decision-side modules that filter which engagements exist (e.g. combatveto, inmatchadapt).",
			"Granted ones are recorded on the bandit attribution as the armed set, so the fitter can condition on survivorship filters.")]
		public readonly string[] WatchConditions = [];

		static bool IsKnob(string name) => Array.IndexOf(BuildOrderKnob.All, name) >= 0;

		static object LoadPlanArms(MiniYaml yaml)
		{
			var result = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
			var node = yaml.NodeWithKeyOrDefault("PlanArms");
			if (node == null)
				return result;

			foreach (var plan in node.Value.Nodes)
			{
				var overlay = new Dictionary<string, int>(StringComparer.Ordinal);
				foreach (var entry in plan.Value.Nodes)
				{
					if (!IsKnob(entry.Key))
						throw new YamlException($"PlanBanditBotModule: PlanArms.{plan.Key} names unknown knob '{entry.Key}'.");

					overlay[entry.Key] = int.Parse(entry.Value.Value, CultureInfo.InvariantCulture);
				}

				result[plan.Key] = overlay;
			}

			return result;
		}

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (PriorCount < 0 || MinEvidence < 0 || LcbZ < 0)
				throw new YamlException("PlanBanditBotModule: PriorCount, MinEvidence and LcbZ must not be negative.");

			foreach (var arm in PersonalityArms)
				if (arm.IndexOf('@') >= 0 || arm.IndexOf("__", StringComparison.Ordinal) >= 0)
					throw new YamlException($"PlanBanditBotModule: PersonalityArms entry '{arm}' must be a bare personality name.");
		}

		public override object Create(ActorInitializer init) { return new PlanBanditBotModule(init.Self, this); }
	}

	public class PlanBanditBotModule : ConditionalTrait<PlanBanditBotModuleInfo>, IObservesVariables
	{
		readonly World world;
		readonly OpenRA.Player player;

		bool resolved;
		PlanBanditLearned learned = new();
		IReadOnlyDictionary<string, int> conditionCounts = new Dictionary<string, int>(0);

		public PlanBanditSnapshot Snapshot { get; private set; }

		public PlanBanditBotModule(Actor self, PlanBanditBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		/// <summary>The personality arm the bandit pinned for this match, or null when the personality bandit did not run.
		/// Read by BotPersonalityController at enable time; resolves lazily so trait ordering cannot race it.</summary>
		public string PinnedPersonalityArm
		{
			get
			{
				Resolve();
				return Snapshot is { PersonalityPinned: true } ? Snapshot.PersonalityArm : null;
			}
		}

		/// <summary>The chosen plan overlay for a knob in thousandths; 1000 when the plan bandit did not run or the arm omits the knob.</summary>
		public int PlanOverlayMilli(string knob)
		{
			Resolve();
			if (Snapshot == null || Snapshot.PlanArm.Length == 0
				|| !Info.PlanArms.TryGetValue(Snapshot.PlanArm, out var overlay)
				|| !overlay.TryGetValue(knob, out var milli))
				return BuildOrderKnob.Neutral;

			return milli;
		}

		/// <summary>Both bandit choices, drawn once from the host's random in a fixed order
		/// (personality first, then plan) and frozen for the match.</summary>
		void Resolve()
		{
			if (resolved)
				return;

			resolved = true;
			var snapshot = new PlanBanditSnapshot { Tick = world.WorldTick };
			Snapshot = snapshot;

			var ownFaction = player.Faction?.InternalName ?? "";
			var enemyFaction = EnemyFactionOf(world, player);
			var scope = ownFaction.Length > 0 && enemyFaction.Length > 0 ? $"{ownFaction}__vs__{enemyFaction}" : ownFaction;
			snapshot.Scope = scope;
			snapshot.ArmedModules = ArmedModules();

			LoadLearned();

			if (Info.PersonalityArms.Length > 0)
			{
				var arm = Choose("Personality", ownFaction, enemyFaction, Info.PersonalityArms);
				if (arm != null)
				{
					snapshot.PersonalityArm = arm;
					snapshot.PersonalityPinned = true;
					Log.Write("debug", $"AI {player.InternalName}: plan-bandit personality arm '{arm}' (scope {scope})");
				}
			}

			if (Info.PlanArms.Count > 0)
			{
				var arm = Choose("Plan", ownFaction, enemyFaction, Info.PlanArms.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray());
				if (arm != null)
				{
					snapshot.PlanArm = arm;
					Log.Write("debug", $"AI {player.InternalName}: plan-bandit plan arm '{arm}' (scope {scope})");
				}
			}
		}

		void LoadLearned()
		{
			var fs = Game.ModData.DefaultFileSystem;
			if (!fs.Exists(Info.LearnedFile))
			{
				Log.Write("debug", $"AI {player.InternalName}: plan-bandit learned: {Info.LearnedFile} missing, priors only");
				return;
			}

			using (var stream = fs.Open(Info.LearnedFile))
				learned = PlanBanditLearned.Parse(MiniYaml.FromStream(stream, Info.LearnedFile));

			Log.Write("debug", $"AI {player.InternalName}: plan-bandit learned: {learned.ScopeCount} scopes");
		}

		string Choose(string bandit, string ownFaction, string enemyFaction, string[] arms)
		{
			var pooled = new Dictionary<string, PlanBanditArmStats>(StringComparer.Ordinal);
			var evidence = new Dictionary<string, long>(StringComparer.Ordinal);
			foreach (var arm in arms)
			{
				pooled[arm] = learned.Pooled(bandit, arm, ownFaction, enemyFaction, Info.PriorCount);
				evidence[arm] = learned.EvidenceN(bandit, arm, ownFaction, enemyFaction);
			}

			return PlanBanditMath.ChooseArm(pooled, evidence, Info.LcbZ / 100.0, Info.MinEvidence, Info.MinSafetyLcb, Uniforms());
		}

		IEnumerable<double> Uniforms()
		{
			while (true)
				yield return world.LocalRandom.NextFloat();
		}

		// IObservesVariables: the engine hands the full granted-condition map at create and after every grant/revoke.
		void ConditionsChanged(Actor self, IReadOnlyDictionary<string, int> variables)
		{
			conditionCounts = variables;
		}

		public IEnumerable<VariableObserver> GetVariableObservers()
		{
			if (Info.WatchConditions.Length > 0)
				yield return new VariableObserver(ConditionsChanged, Info.WatchConditions);
		}

		string ArmedModules()
		{
			var armed = Info.WatchConditions
				.Where(c => conditionCounts.TryGetValue(c, out var n) && n > 0)
				.OrderBy(c => c, StringComparer.Ordinal);
			return string.Join("+", armed) is { Length: > 0 } s ? s : "none";
		}

		// Player-level information only (the lobby faction of the main target, else the most common enemy faction), never actors.
		// The same rule BuildOrderKnobsBotModule uses for its opening scope, kept verbatim so both modules see the same matchup.
		internal static string EnemyFactionOf(World world, OpenRA.Player player)
		{
			var main = player.PlayerActor.TraitsImplementing<IBotMainTargetProvider>()
				.Select(p => p.MainTarget).FirstOrDefault(t => t != null);
			if (main != null)
				return main.Faction?.InternalName ?? "";

			return world.Players
				.Where(p => p != player && !p.NonCombatant && !p.Spectating && player.RelationshipWith(p) == PlayerRelationship.Enemy)
				.GroupBy(p => p.Faction?.InternalName).Where(g => g.Key != null)
				.OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
				.Select(g => g.Key).FirstOrDefault() ?? "";
		}
	}

	/// <summary>
	/// The offline-fitted tier-3 posteriors (tools/ai/tune_plan_bandits.py), free of world state so tests can drive it with
	/// an inline MiniYaml string. Layout: a BotPlanBandits root with `Personality@&lt;scope&gt;` and `Plan@&lt;scope&gt;` nodes;
	/// scope = `any`, `family_&lt;f&gt;`, `&lt;faction&gt;`, or `&lt;faction&gt;__vs__&lt;enemy&gt;` (the enemy may itself be
	/// family_-scoped). Children are `&lt;arm&gt;: n mean m2` — n observations, sample mean and sum of squared deviations of
	/// the EL `total_milli` reward.
	/// </summary>
	public sealed class PlanBanditLearned
	{
		const string VersusSeparator = "__vs__";

		readonly Dictionary<(string Bandit, string Scope), Dictionary<string, PlanBanditArmStats>> scopes = new();

		public int ScopeCount => scopes.Count;

		public static PlanBanditLearned Parse(IEnumerable<MiniYamlNode> nodes)
		{
			var learned = new PlanBanditLearned();
			var root = nodes.FirstOrDefault(n => n.Key == "BotPlanBandits");
			if (root == null)
				return learned;

			foreach (var node in root.Value.Nodes)
			{
				string bandit = null, scope = null;
				foreach (var prefix in new[] { "Personality", "Plan" })
				{
					if (node.Key.StartsWith(prefix + "@", StringComparison.Ordinal))
					{
						bandit = prefix;
						scope = node.Key[(prefix.Length + 1)..];
						break;
					}
				}

				if (bandit == null || scope.Length == 0)
					continue;

				var arms = new Dictionary<string, PlanBanditArmStats>(StringComparer.Ordinal);
				foreach (var child in node.Value.Nodes)
				{
					var parts = child.Value.Value?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
					if (parts is { Length: 3 }
						&& long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
						&& double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var mean)
						&& double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var m2)
						&& n >= 0)
						arms[child.Key] = new PlanBanditArmStats { N = n, Mean = mean, M2 = Math.Max(0, m2) };
				}

				learned.scopes[(bandit, scope)] = arms;
			}

			return learned;
		}

		/// <summary>Stats of one scope for an arm; zero when the scope does not list it.</summary>
		public PlanBanditArmStats At(string bandit, string arm, string scope)
		{
			return scopes.TryGetValue((bandit, scope), out var arms) && arms.TryGetValue(arm, out var stats)
				? stats : default;
		}

		/// <summary>
		/// The pooled posterior stats of an arm for a matchup: the most specific scope that exists, shrunk toward its
		/// parent chain (matchup -> faction -> family -> any), each parent capped at priorCount pseudo-observations.
		/// Missing levels contribute nothing; with no data anywhere the neutral prior applies at draw time.
		/// </summary>
		public PlanBanditArmStats Pooled(string bandit, string arm, string ownFaction, string enemyFaction, int priorCount)
		{
			var family = "family_" + BuildOrderKnobsEval.FamilyOf(ownFaction);
			var matchup = ownFaction.Length > 0 && enemyFaction.Length > 0 ? ownFaction + VersusSeparator + enemyFaction : null;

			var pooled = matchup != null ? At(bandit, arm, matchup) : default;
			pooled = PlanBanditMath.Pool(pooled, At(bandit, arm, ownFaction), priorCount);
			pooled = PlanBanditMath.Pool(pooled, At(bandit, arm, family), priorCount);
			pooled = PlanBanditMath.Pool(pooled, At(bandit, arm, "any"), priorCount);
			return pooled;
		}

		/// <summary>
		/// How many times this arm was actually played at this matchup (matchup scope first, else the
		/// own-faction scope) — the n the safety floor binds on. The fitter rolls one observation into every
		/// level of the chain, so the pooled n over-counts real plays by up to one per level; the floor binds
		/// on own-scope evidence only, so a sparse arm can never be floor-blocked by roll-up inflation.
		/// </summary>
		public long EvidenceN(string bandit, string arm, string ownFaction, string enemyFaction)
		{
			var matchup = ownFaction.Length > 0 && enemyFaction.Length > 0 ? ownFaction + VersusSeparator + enemyFaction : null;
			var own = matchup != null ? At(bandit, arm, matchup).N : 0;
			if (own == 0)
				own = At(bandit, arm, ownFaction).N;
			return own;
		}
	}
}
