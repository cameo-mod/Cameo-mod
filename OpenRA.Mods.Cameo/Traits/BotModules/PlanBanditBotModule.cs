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
using OpenRA.Mods.CA;
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

		/// <summary>The PersonalityArms the personality controller still offers on this ruleset (a map may narrow it).</summary>
		[FieldLoader.Ignore]
		public string[] OfferedPersonalityArms = [];

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

			// A map may narrow the controller to fewer personalities (the Raid gate pins personality-rush), so the bandit
			// draws only among the arms the controller still offers. This runs at rules load even with the module off,
			// so a narrowed map must not fail here (INC d: the Raid gate map stopped loading). Fail loud only when NO
			// arm matches: then every arm is a misconfiguration (Nova review 2026-10-03, same spirit as PinnedPersonalities).
			var controller = ai.TraitInfos<BotPersonalityControllerInfo>().FirstOrDefault();
			OfferedPersonalityArms = controller == null ? PersonalityArms
				: PersonalityArms.Where(arm => controller.Conditions.Any(c => BotPersonalityController.PersonalityName(c, controller.PersonalityPrefix) == arm)).ToArray();
			if (controller != null && PersonalityArms.Length > 0 && OfferedPersonalityArms.Length == 0)
				throw new YamlException($"PlanBanditBotModule: no PersonalityArms entry ({string.Join(", ", PersonalityArms)}) has a matching personality-* condition.");
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

		PlanBanditSnapshot snapshot;

		/// <summary>The frozen draw for the situation/engagement logs; null while the trait is disabled
		/// (rule 5, orders 2026-10-03 — every public reader self-guards, callers filter IsTraitEnabled anyway).</summary>
		public PlanBanditSnapshot Snapshot => IsTraitDisabled ? null : snapshot;

		public PlanBanditBotModule(Actor self, PlanBanditBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		/// <summary>The personality arm the bandit pinned for this match, or null when the personality bandit did not run.
		/// Host-side only (AR-2): read in MasterAiBotModule's BotTick, which carries it to the world as a synced
		/// SetBotPersonality order — a synced-context read of this LocalRandom draw would desync clients.</summary>
		public string PinnedPersonalityArm
		{
			get
			{
				if (IsTraitDisabled)
					return null;

				Resolve();
				return snapshot is { PersonalityPinned: true } ? snapshot.PersonalityArm : null;
			}
		}

		/// <summary>The chosen plan overlay for a knob in thousandths; 1000 when the plan bandit did not run,
		/// the arm omits the knob, or the trait is disabled.</summary>
		public int PlanOverlayMilli(string knob)
		{
			if (IsTraitDisabled)
				return BuildOrderKnob.Neutral;

			Resolve();
			if (snapshot == null || snapshot.PlanArm.Length == 0
				|| !Info.PlanArms.TryGetValue(snapshot.PlanArm, out var overlay)
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
			this.snapshot = snapshot;

			var ownFaction = player.Faction?.InternalName ?? "";
			var enemyFaction = EnemyFactionOf(world, player);
			var scope = ownFaction.Length > 0 && enemyFaction.Length > 0 ? $"{ownFaction}__vs__{enemyFaction}" : ownFaction;
			snapshot.Scope = scope;
			snapshot.ArmedModules = ArmedModules();
			Log.Write("debug", $"AI {player.InternalName}: plan-bandit resolve diag: tick={world.WorldTick} players={world.Players.Count()} lobby={world.LobbyInfo?.Clients.Count ?? -1} me={world.LobbyInfo?.Clients.FirstOrDefault(c => c.Index == player.ClientIndex)?.Index ?? -1} enemy='{enemyFaction}'");

			LoadLearned();

			if (Info.OfferedPersonalityArms.Length > 0)
			{
				var arm = Choose("Personality", ownFaction, enemyFaction, Info.OfferedPersonalityArms);
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
				yield return BotRng.For(player).NextFloat();
		}

		// IObservesVariables: the engine hands the full granted-condition map at create and after every grant/revoke.
		void ConditionsChanged(Actor self, IReadOnlyDictionary<string, int> variables)
		{
			conditionCounts = variables;
		}

		public override IEnumerable<VariableObserver> GetVariableObservers()
		{
			foreach (var observer in base.GetVariableObservers())
				yield return observer;

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

		// Player-level information only (the lobby-visible faction of the main target, else the most common
		// publicly-known enemy faction), never actors — BotFactionView.PublicFactionOf throughout, so a
		// Random/hidden pick contributes "" and the scope pools to the generic levels. The same rule
		// BuildOrderKnobsBotModule uses for its opening scope, kept verbatim so both modules see the same matchup.
		internal static string EnemyFactionOf(World world, OpenRA.Player player)
		{
			var main = player.PlayerActor.TraitsImplementing<IBotMainTargetProvider>()
				.Select(p => p.MainTarget).FirstOrDefault(t => t != null);
			if (main != null)
				return BotFactionView.PublicFactionOf(main);

			// The bandit resolves inside the Player ctor (PlayerActor creation), before w.SetPlayers ran —
			// world.Players is still empty there. Enemy refs ARE populated by then, so early resolves
			// fall back to the map-level path. A Random pick yields "" — the generic levels still apply.
			if (!world.Players.Any())
				return LobbyEnemyFaction(world, player);

			return DominantFaction(world.Players
				.Where(p => p != player && !p.NonCombatant && !p.Spectating && player.RelationshipWith(p) == PlayerRelationship.Enemy)
				.Select(BotFactionView.PublicFactionOf));
		}

		// Enemy faction from map data alone (used only while world.Players is unpopulated — the bandit resolves
		// inside the Player ctor, before SetPlayers/SetupPlayerMasks): the owning PlayerReference.Enemies names
		// the enemy player refs; each ref's faction name is mapped through PublicFactionName the same way the
		// live path maps DisplayFaction — a Random pick is "" (honest about what was publicly known at draw
		// time). NonCombatant refs (Creeps) are skipped the same way the live-players path skips them.
		static string LobbyEnemyFaction(World world, OpenRA.Player player)
		{
			var pr = player.PlayerReference;
			if (pr == null || pr.Enemies.Length == 0)
				return "";

			var mapPlayers = new MapPlayers(world.Map.PlayerDefinitions).Players;
			return DominantFaction(pr.Enemies
				.Select(name => mapPlayers.TryGetValue(name, out var ep) && !ep.NonCombatant
					? BotFactionView.PublicFactionName(world, ep.Faction) : null));
		}

		/// <summary>The most common known faction among the candidates (count desc, ordinal tie-break);
		/// null/empty candidates — unknown or Random-hid factions — are skipped. "" when none remain.</summary>
		internal static string DominantFaction(IEnumerable<string> names) =>
			names.Where(f => !string.IsNullOrEmpty(f))
				.GroupBy(f => f)
				.OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
				.Select(g => g.Key).FirstOrDefault() ?? "";
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
