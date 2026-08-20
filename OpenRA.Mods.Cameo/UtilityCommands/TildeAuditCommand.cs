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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.UtilityCommands
{
	// Audits Buildable.Prerequisites '~' (tilde) usage against the engine's real hide/show
	// semantics (TechTree.Watcher.HasPrerequisites/IsHidden): a bare '~token' hides an actor
	// from the production palette entirely until 'token' is met, while every other token
	// (plain, '!negated', or '~!negated') only ever gates whether it is *buildable* - it never
	// hides the icon. So a bare '~token' that isn't a genuine "you don't have the right
	// ConYard/faction" or "this lobby feature is off" gate is an over-hidden tech/rank/doctrine
	// gate: the actor should stay visible-but-greyed once its queue exists, not disappear until
	// individually unlocked. This command never edits YAML; it is a read-only report to drive a
	// human-reviewed follow-up edit (strip the '~' from CANDIDATE tokens, leave KEEP alone).
	//
	// Two modes:
	//   --tilde-audit tokens      (default) per-actor token classification (KEEP_*/CANDIDATE*).
	//   --tilde-audit visibility  per-faction HIDDEN/VISIBLE_LOCKED/BUILDABLE state, for
	//                             before/after diffing and cross-faction leak detection.
	sealed class TildeAuditCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--tilde-audit";

		bool IUtilityCommand.ValidateArguments(string[] args) => true;

		// Known lobby/global feature toggles - on/off switches, not tech-tree progression.
		// Extend as new ones are discovered; anything not on this list and not a ConYard/
		// faction-identity token is treated as a real progression gate (CANDIDATE).
		static readonly string[] KnownToggleTokens =
		{
			"techlevel.low", "techlevel.medium", "techlevel.high", "techlevel.superweapons",
			"globalnaval", "disabled", "disable", "botplayer", "hardbotplayer",
			"unbuildable", "wip-content", "not available", "wip",
		};

		// Queue-tab-defining structures not covered by ai.yaml's ConstructionYardTypes list
		// (Outpost2 has no MCV/ConYard concept; D2k has ConYard variants ai.yaml doesn't
		// enumerate) - confirmed by reading each actor's Prerequisites usage: these gate an
		// entire production queue tab's existence (the "War Factory" of their theme), not a
		// specific item inside an already-open queue, so hiding until built is correct.
		static readonly string[] KnownStructuralTokens =
		{
			"eden_factory_structure", "plymouth_structure_factory",
			"eden_factory_vehicle", "plymouth_factory_vehicle",
			"plymouth_factory_arachnid", "eden_factory_consumer",
			"construction_yard", "construction_yard.atreides", "upgrade_conyard",
			"tkmfactory", "tkmbarracks", "tkmairpad",
			// TiberianDawn: bare bi-faction producer buildings (GDI/Nod each have their own
			// actor under this name, per-faction-folder-split, never shared across the two -
			// verified via each folder's own definition). "fact" is the generic ConYard token
			// both FACT.GDI/FACT.NOD explicitly grant via ProvidesPrerequisite@buildingname,
			// so it's a safe TD-only (not faction-specific) anchor for shared items like walls.
			"weap", "afld", "pyle", "hand", "hpad.gdi", "hpad.nod", "fact",
			"eye", "tmpl", "hq.gdi", "hq.nod",
			// RedAlert: bare/per-faction producer buildings (Barracks/War Factory/Airfield/
			// Helipad-equivalent) - each verified to require only "~rafact.<faction>" (+ a
			// non-hiding companion), so they're transitively faction-exclusive and safe
			// queue-tab-defining anchors. "rafact" bare is RA1's version of TD's "fact" (granted
			// by the shared ^RAFACT template, explicitly withdrawn for Japan).
			"barr", "raweap", "raweapa", "raafld", "rahpad", "modraweapj",
			"tent", "modtentj", "rafact",
			// RA1-wide shared tokens (granted by multiple faction producers, e.g. all three
			// Barracks variants grant "rabarracks") - RA1-exclusive, same generic-shared-anchor
			// role as TD's "fact".
			"rabarracks", "ranavaltransport",
			// RedAlert2: per-faction producer buildings (War Factory/Barracks/Airforce Command
			// HQ) plus the Radar/Psychic-Sensor buildings that host the Upgrades queue tab -
			// each verified to require only "~<faction ConYard>" (+ non-hiding companions), so
			// they're transitively faction-exclusive and safe queue-tab-defining anchors.
			"ra2gaweap", "ra2naweap", "yrygweap",
			"ra2gapile", "ra2nahand", "yryabrck",
			"ra2gaairc", "ra2naradr", "ra2napsis",
			// TiberianSun: per-faction producer buildings (War Factory/Barracks/Helipad) -
			// each verified to require only its own faction's ConYard, so they're safe
			// queue-tab-defining anchors. GDI/Nod/Mutant(Forgotten)/CABAL are 4 distinct
			// factions here, not tiers of the same building despite the "2" suffix naming.
			"tsgtweap", "tsntweap", "tsgtweapmutant", "tsntweapcabal",
			"tsgtpile", "tsgtpile2", "tsnthand", "tsnthand2",
			"tsgthpad", "tsnthpad", "tsgthpadmutant", "tsnthpad2",
			// TS ConYards also grant this alternate per-faction token (same secondary-grant
			// role as TD's "fact" / RA1's "rafact"), each verified to have exactly one provider.
			"fact.tsgdi", "fact.tsnod",
			// StarCraft: per-race unit-producer buildings (Barracks/Factory/Starport/Gateway/
			// Spawning Pool) and buildings that explicitly host their own "Upgrades" queue
			// (Queen's Nest/Defiler Mound/Armory/Engineering Bay) - each verified to require
			// only its own race's ConYard, so they're safe queue-tab-defining anchors.
			"scbarracks", "scfactory", "scstarport", "scgateway", "scspawningpool",
			"scqueensnest", "scdefilermound", "scarmory", "scengineeringbay",
			// D2k: shared refinery-grant token (both refinery.ixian/.ordos inherit the same
			// ^abstract template granting it) - D2k-exclusive but faction-agnostic, same role
			// as TD's "fact".
			"spice_refinery", "d2k_barracks", "outpost.d2k", "d2k_construction_yard",
		};

		[Desc("[tokens|visibility]",
			"tokens (default): classify every '~'-prefixed Buildable.Prerequisites token as " +
			"KEEP_CONYARD/KEEP_TOGGLE (correctly hidden) or CANDIDATE(_FIRST) (likely an " +
			"over-hidden tech/rank gate that should lose its '~'). " +
			"visibility: per-faction HIDDEN/VISIBLE_LOCKED/BUILDABLE state per actor, for " +
			"before/after diffing and cross-faction leak checks.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var modData = Game.ModData = utility.ModData;
			var rules = modData.DefaultRules;
			var mode = args.Length > 1 ? args[1].ToLowerInvariant() : "tokens";

			if (mode == "visibility")
				RunVisibility(rules);
			else
				RunTokens(modData, rules);
		}

		static void RunTokens(ModData modData, Ruleset rules)
		{
			var conyardTokens = LoadConyardTokens();
			Console.Error.WriteLine($"# conyard/faction tokens loaded from ai.yaml: {conyardTokens.Count}");

			Console.WriteLine("actor\tqueue\ttoken\tkind\tclass\tnote");
			foreach (var ai in rules.Actors.OrderBy(a => a.Key, StringComparer.OrdinalIgnoreCase))
			{
				if (ai.Key.StartsWith(ActorInfo.AbstractActorPrefix))
					continue;

				List<BuildableInfo> buildables;
				try { buildables = ai.Value.TraitInfos<BuildableInfo>().ToList(); }
				catch { continue; }
				if (buildables.Count == 0)
					continue;

				foreach (var b in buildables)
				{
					var queue = string.Join(",", b.Queue);

					// Classify every bare '~token' (kind + KEEP class) first, in one pass, before
					// emitting anything - the safety note below depends on the OUTCOME for the
					// whole line, not just this one token.
					string ClassifyBare(string token)
					{
						if (conyardTokens.Contains(token))
							return "KEEP_CONYARD";
						if (KnownToggleTokens.Contains(token))
							return "KEEP_TOGGLE";
						if (KnownStructuralTokens.Contains(token))
							return "KEEP_STRUCTURAL";
						return "CANDIDATE";
					}

					var bareTildeClasses = b.Prerequisites
						.Where(p => p.StartsWith('~') && !p.StartsWith("~!"))
						.Select(p => ClassifyBare(p[1..].ToLowerInvariant()))
						.ToList();

					// KEEP_TOGGLE (lobby/global settings like techlevel.*) does NOT count as a
					// leak-preventing anchor: every faction in every theme can satisfy it equally,
					// so an actor whose ONLY other tilde is a toggle is exactly as exposed as one
					// with no keep token at all. Only KEEP_CONYARD/KEEP_STRUCTURAL (theme- or
					// faction-exclusive by construction) count. Caught live: IRON/JSHRINE/MSLO/PDOX
					// (`stek, ~techlevel.superweapons`) leaked into every other theme's palette
					// because ~techlevel.superweapons alone was wrongly treated as sufficient.
					var keepCount = bareTildeClasses.Count(c => c is "KEEP_CONYARD" or "KEEP_STRUCTURAL");

					// If NOTHING on this line is a real KEEP-classified anchor, stripping ANY '~'
					// from a candidate token - whether it's the only one or one of several - can
					// leave the actor with zero hide-gates, leaking it into every other faction/
					// theme sharing the same Queue tag. Flag every candidate on such a line so a
					// strip must ADD a companion KEEP token (or deliberately keep one candidate
					// tilde'd as the anchor), never just delete every '~'.
					var noKeepAnchor = keepCount == 0 && bareTildeClasses.Count > 0;

					var firstTildeSeen = false;
					foreach (var raw in b.Prerequisites)
					{
						string kind;
						var cls = "";
						var note = "";
						var body = raw;

						if (body.StartsWith('~'))
						{
							body = body[1..];
							if (body.StartsWith('!'))
							{
								kind = "TILDEBANG";
							}
							else
							{
								kind = "TILDE";
								var isFirst = !firstTildeSeen;
								firstTildeSeen = true;
								var token = body.ToLowerInvariant();
								cls = ClassifyBare(token);
								if (cls == "CANDIDATE")
								{
									cls = isFirst ? "CANDIDATE_FIRST" : "CANDIDATE";
									if (noKeepAnchor)
										note = "NO_KEEP_ANCHOR-do-not-strip-every-tilde-on-this-line-without-adding-a-KEEP-companion-or-keeping-one-as-anchor";
								}
							}
						}
						else
							kind = body.StartsWith('!') ? "BANG" : "PLAIN";

						Console.WriteLine($"{ai.Key}\t{queue}\t{raw}\t{kind}\t{cls}\t{note}");
					}
				}
			}
		}

		static HashSet<string> LoadConyardTokens()
		{
			var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			string path = null;

			var searchPaths = Environment.GetEnvironmentVariable("MOD_SEARCH_PATHS");
			if (!string.IsNullOrEmpty(searchPaths))
			{
				var candidate = Path.Combine(searchPaths.Split(',')[0], "cameo", "ai", "ai.yaml");
				if (File.Exists(candidate))
					path = candidate;
			}

			path ??= Path.Combine("..", "mods", "cameo", "ai", "ai.yaml");
			if (!File.Exists(path))
			{
				Console.Error.WriteLine($"# WARNING: could not locate ai.yaml at '{path}' - conyard allowlist is EMPTY");
				return tokens;
			}

			var fields = new[] { "ConstructionYardTypes:", "HarvesterTypes:", "RefineryTypes:", "NavalProductionTypes:" };
			foreach (var line in File.ReadAllLines(path))
			{
				var trimmed = line.Trim();
				var field = fields.FirstOrDefault(f => trimmed.StartsWith(f, StringComparison.OrdinalIgnoreCase));
				if (field == null)
					continue;

				var value = trimmed[field.Length..].Trim();
				foreach (var t in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
					tokens.Add(t.ToLowerInvariant());
			}

			return tokens;
		}

		// --- visibility mode: per-faction HIDDEN / VISIBLE_LOCKED / BUILDABLE snapshot ---
		// Reuses the same seed+provider+closure technique as --faction-report
		// (FactionBuildableReportCommand.cs) to determine, per faction, which tokens/actors
		// are reachable at all. "Visible" approximates the live TechTree by treating a bare
		// '~token' as satisfied once that token is reachable in the faction's full buildable
		// closure - a reasonable diagnostic approximation (queue-gating structures have simple
		// prerequisites themselves, so "reachable" and "already built" coincide in practice);
		// this is a snapshot for before/after diffing and leak detection, not a live UI sim.
		static void RunVisibility(Ruleset rules)
		{
			var byKey = new Dictionary<string, ActorInfo>(StringComparer.OrdinalIgnoreCase);
			foreach (var ai in rules.Actors)
				byKey[ai.Key] = ai.Value;

			var world = rules.Actors[SystemActors.World];
			var factions = world.TraitInfos<FactionInfo>()
				.Where(f => !f.RandomFactionMembers.Any() && !string.IsNullOrEmpty(f.InternalName))
				.Select(f => f.InternalName)
				.ToArray();

			var providers = new Dictionary<string, List<(string Actor, HashSet<string> Factions)>>(StringComparer.OrdinalIgnoreCase);
			foreach (var ai in rules.Actors)
			{
				if (ai.Key.StartsWith(ActorInfo.AbstractActorPrefix))
					continue;

				List<ITechTreePrerequisiteInfo> pps;
				try { pps = ai.Value.TraitInfos<ITechTreePrerequisiteInfo>().ToList(); }
				catch { continue; }

				foreach (var pp in pps)
				{
					var facs = pp is ProvidesPrerequisiteInfo ppi && ppi.Factions.Count > 0
						? ppi.Factions.Select(f => f.ToLowerInvariant()).ToHashSet()
						: null;
					foreach (var raw in pp.Prerequisites(ai.Value))
						providers.GetOrAdd(raw.ToLowerInvariant(), _ => new List<(string, HashSet<string>)>()).Add((ai.Key, facs));
				}
			}

			string TransformTarget(ActorInfo ai)
			{
				try { return ai.TraitInfoOrDefault<TransformsInfo>()?.IntoActor; }
				catch { return null; }
			}

			var startUnits = world.TraitInfos<StartingUnitsInfo>().ToList();
			var factionBySuffix = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var su in startUnits)
			{
				if (string.IsNullOrEmpty(su.BaseActor) || !su.BaseActor.Contains('.'))
					continue;
				var suffix = su.BaseActor[(su.BaseActor.LastIndexOf('.') + 1)..];
				foreach (var f in su.Factions)
					if (factions.Contains(f))
						factionBySuffix[suffix] = f;
			}

			string LockedFaction(string key)
			{
				var dot = key.LastIndexOf('.');
				if (dot < 0)
					return null;
				return factionBySuffix.TryGetValue(key[(dot + 1)..], out var f) ? f : null;
			}

			var seedByFaction = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
			foreach (var f in factions)
			{
				var seed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				void SeedAdd(string a)
				{
					if (string.IsNullOrEmpty(a))
						return;
					var lk = LockedFaction(a);
					if (lk == null || lk == f)
						seed.Add(a);
				}

				foreach (var su in startUnits)
				{
					if (su.Factions.Count > 0 && !su.Factions.Contains(f))
						continue;
					SeedAdd(su.BaseActor);
					foreach (var a in su.SupportActors) SeedAdd(a);
					foreach (var a in su.SupportBuildings) SeedAdd(a);
					foreach (var a in su.SupportProxyActors) SeedAdd(a);
				}

				seedByFaction[f] = seed;
			}

			var globalTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var sa in new[] { SystemActors.Player, SystemActors.World })
			{
				try
				{
					var actor = rules.Actors[sa];
					foreach (var pp in actor.TraitInfos<ITechTreePrerequisiteInfo>())
						foreach (var token in pp.Prerequisites(actor))
							globalTokens.Add(token.ToLowerInvariant());
				}
				catch { }
			}

			// Deliberately NOT the full fixpoint Buildable closure (that's what ComputeOwned /
			// --faction-report compute): a steady-state "everything this faction will ever
			// reach" closure makes every eventually-buildable actor look BUILDABLE regardless of
			// tilde, which erases exactly the Hidden-vs-Visible timing distinction this mode
			// exists to check. Instead this is a single, well-defined early-game snapshot -
			// "just deployed the MCV, built nothing else yet" - the moment a HIDDEN item most
			// needs to become VISIBLE_LOCKED instead.
			var seedOwnedByFaction = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
			foreach (var f in factions)
			{
				var seedOwned = new HashSet<string>(seedByFaction[f], StringComparer.OrdinalIgnoreCase);
				bool TrySeedOwn(string key)
				{
					var lk = LockedFaction(key);
					if (lk != null && lk != f)
						return false;
					return seedOwned.Add(key);
				}

				ExpandTransforms(seedOwned, byKey, TransformTarget, TrySeedOwn);
				seedOwnedByFaction[f] = seedOwned;
			}

			bool TokenAvailable(string token, HashSet<string> owned, string faction)
			{
				token = token.ToLowerInvariant();
				if (globalTokens.Contains(token))
					return true;
				if (!providers.TryGetValue(token, out var list))
					return false;
				foreach (var (actor, facs) in list)
					if (owned.Contains(actor) && (facs == null || facs.Contains(faction)))
						return true;
				return false;
			}

			bool IsHiddenFor(IEnumerable<string> prerequisites, HashSet<string> owned, string faction)
			{
				foreach (var raw in prerequisites)
				{
					if (!raw.StartsWith('~'))
						continue;
					var body = raw[1..];
					var negative = body.StartsWith('!');
					if (negative)
						body = body[1..];
					var available = TokenAvailable(body, owned, faction);
					if (negative ? available : !available)
						return true;
				}

				return false;
			}

			// Mirrors TechTree.Watcher.HasPrerequisites: strip all prefix chars, '!' inverts.
			bool HasPrerequisitesAt(IEnumerable<string> prerequisites, HashSet<string> owned, string faction)
			{
				foreach (var raw in prerequisites)
				{
					var p = raw;
					var negative = false;
					while (p.Length > 0 && (p[0] == '~' || p[0] == '!'))
					{
						if (p[0] == '!') negative = true;
						p = p[1..];
					}

					if (p.Length == 0)
						continue;

					var available = TokenAvailable(p, owned, faction);
					if (negative ? available : !available)
						return false;
				}

				return true;
			}

			Console.WriteLine("actor\tfaction\tstate");
			foreach (var ai in rules.Actors.OrderBy(a => a.Key, StringComparer.OrdinalIgnoreCase))
			{
				if (ai.Key.StartsWith(ActorInfo.AbstractActorPrefix) || ai.Key == "World" || ai.Key == "Player")
					continue;

				List<BuildableInfo> buildables;
				try { buildables = ai.Value.TraitInfos<BuildableInfo>().ToList(); }
				catch { continue; }
				if (buildables.Count == 0)
					continue;

				foreach (var f in factions)
				{
					var owned = seedOwnedByFaction[f];
					string state;
					if (buildables.Any(b => HasPrerequisitesAt(b.Prerequisites, owned, f)))
						state = "BUILDABLE";
					else if (buildables.Any(b => !IsHiddenFor(b.Prerequisites, owned, f)))
						state = "VISIBLE_LOCKED";
					else
						state = "HIDDEN";

					Console.WriteLine($"{ai.Key}\t{f}\t{state}");
				}
			}
		}

		static void ExpandTransforms(HashSet<string> owned, Dictionary<string, ActorInfo> byKey,
			Func<ActorInfo, string> transformTarget, Func<string, bool> tryOwn)
		{
			var changed = true;
			while (changed)
			{
				changed = false;
				foreach (var key in owned.ToArray())
				{
					if (!byKey.TryGetValue(key, out var ai))
						continue;
					var tgt = transformTarget(ai);
					if (!string.IsNullOrEmpty(tgt) && tryOwn(tgt))
						changed = true;
				}
			}
		}
	}
}
