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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.UtilityCommands
{
	// Classifies every actor by which factions can build/own it, using the engine's fully-resolved
	// prerequisite graph (ProvidesPrerequisite incl. inherited templates) plus a tech-tree closure
	// seeded from each faction's StartingUnits (deploy/transform chain). Output is machine-readable:
	//   ACTORNAME<TAB>fac1,fac2     (the factions that can build/own it)
	//   ACTORNAME<TAB>*SHARED       (all real factions)
	//   ACTORNAME<TAB>*NONE         (no faction can build it - husk/civilian/support)
	// Used to drive the per-faction ContentPacks migration of bare-named theme rules files.
	sealed class FactionBuildableReportCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--faction-report";

		bool IUtilityCommand.ValidateArguments(string[] args) => true;

		[Desc("[onlyPrefix]",
			"Report, per actor, which factions can build/own it (engine prerequisite closure seeded from " +
			"StartingUnits). Optional arg filters output to actor keys starting with the given prefix.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var modData = Game.ModData = utility.ModData;
			var rules = modData.DefaultRules;
			var onlyPrefix = args.Length > 1 ? args[1] : null;

			var byKey = new Dictionary<string, ActorInfo>(StringComparer.OrdinalIgnoreCase);
			foreach (var ai in rules.Actors)
				byKey[ai.Key] = ai.Value;

			var world = rules.Actors[SystemActors.World];
			var factions = world.TraitInfos<FactionInfo>()
				.Where(f => !f.RandomFactionMembers.Any() && !string.IsNullOrEmpty(f.InternalName))
				.Select(f => f.InternalName)
				.ToArray();

			// token (lower) -> list of (providerActor, restrictedFactions(lower) or null=all)
			// token (lower) -> list of (providerActor, restrictedFactions(lower) or null=all). Index ALL
			// ITechTreePrerequisite providers (ProvidesPrerequisite, ProvidesTechPrerequisite, lobby/support
			// power grants, ...) so "no provider" genuinely means an unsatisfiable token (e.g. the bot-only
			// 'defensebot' sentinel on ^BaseBuilding), not just a provider we failed to read.
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

			// transforms target per actor (MCV -> conyard etc.)
			string TransformTarget(ActorInfo ai)
			{
				try
				{
					var t = ai.TraitInfoOrDefault<TransformsInfo>();
					return t?.IntoActor;
				}
				catch { return null; }
			}

			// Faction-variant suffix -> faction. A key "foo.allies" is the allies variant of foo and is
			// faction-locked even when its generic prerequisites pass for everyone (the real lock is the
			// faction-specific producer). Derive the tag->faction map from each faction's StartingUnits
			// BaseActor (e.g. ramcv.japan -> modjapan), so it generalises across themes without hardcoding.
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

			// starting actors per faction (seed of the owned set)
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

			// global tokens: anything granted by the Player/World actors (lobby options, tech-level, default
			// player prereqs) is treated as always available, so units gated only by such tokens still classify.
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

			// closure per faction
			var ownedByFaction = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
			foreach (var f in factions)
				ownedByFaction[f] = ComputeOwned(f, rules, byKey, providers, seedByFaction[f], globalTokens, TransformTarget, LockedFaction);

			// water locomotors (move on Water but not on land) -> used to classify naval units
			var waterLocos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var li in world.TraitInfos<LocomotorInfo>())
				if (li.TerrainSpeeds != null && li.TerrainSpeeds.ContainsKey("Water") && !li.TerrainSpeeds.ContainsKey("Clear"))
					waterLocos.Add(li.Name);

			string TypeOf(ActorInfo ai)
			{
				try
				{
					if (ai.HasTraitInfo<BuildingInfo>()) return "buildings";
					if (ai.HasTraitInfo<AircraftInfo>()) return "aircraft";
					var mob = ai.TraitInfoOrDefault<MobileInfo>();
					if (mob != null)
					{
						if (!string.IsNullOrEmpty(mob.Locomotor) && waterLocos.Contains(mob.Locomotor)) return "naval";
						if (ai.HasTraitInfo<WithInfantryBodyInfo>()) return "infantry";
						return "vehicles";
					}
				}
				catch { }
				return "upgrades";
			}

			// classify and emit:  ACTOR<TAB>factions<TAB>type
			Console.Error.WriteLine($"# factions: {string.Join(", ", factions)}");
			foreach (var ai in rules.Actors.OrderBy(a => a.Key, StringComparer.OrdinalIgnoreCase))
			{
				if (ai.Key.StartsWith(ActorInfo.AbstractActorPrefix) || ai.Key == "World" || ai.Key == "Player")
					continue;
				if (onlyPrefix != null && !ai.Key.StartsWith(onlyPrefix, StringComparison.OrdinalIgnoreCase))
					continue;

				var owners = factions.Where(f => ownedByFaction[f].Contains(ai.Key)).ToArray();
				string val;
				if (owners.Length == 0) val = "*NONE";
				else if (owners.Length == factions.Length) val = "*SHARED";
				else val = string.Join(",", owners);
				Console.WriteLine($"{ai.Key}\t{val}\t{TypeOf(ai.Value)}");
			}
		}

		static HashSet<string> ComputeOwned(string faction, Ruleset rules, Dictionary<string, ActorInfo> byKey,
			Dictionary<string, List<(string Actor, HashSet<string> Factions)>> providers,
			HashSet<string> seed, HashSet<string> globalTokens, Func<ActorInfo, string> transformTarget,
			Func<string, string> lockedFaction)
		{
			var owned = new HashSet<string>(seed, StringComparer.OrdinalIgnoreCase);

			bool TryOwn(string key)
			{
				var lk = lockedFaction(key);
				if (lk != null && lk != faction)
					return false;
				return owned.Add(key);
			}

			// expand transform chains of the seed (MCV -> conyard)
			ExpandTransforms(owned, byKey, transformTarget, TryOwn);

			bool TokenAvailable(string token)
			{
				token = token.ToLowerInvariant();
				if (globalTokens.Contains(token))
					return true;
				if (!providers.TryGetValue(token, out var list))
					return false;  // no provider anywhere => unsatisfiable (e.g. bot-only 'defensebot' sentinel)
				foreach (var (actor, facs) in list)
					if (owned.Contains(actor) && (facs == null || facs.Contains(faction)))
						return true;
				return false;
			}

			var changed = true;
			var guard = 0;
			while (changed && guard++ < 100)
			{
				changed = false;
				foreach (var ai in rules.Actors)
				{
					if (ai.Key.StartsWith(ActorInfo.AbstractActorPrefix) || owned.Contains(ai.Key))
						continue;

					BuildableInfo b;
					try { b = ai.Value.TraitInfoOrDefault<BuildableInfo>(); }
					catch { continue; }
					if (b == null)
						continue;

					// Faction variants are locked to their faction regardless of generic prerequisites.
					var lk = lockedFaction(ai.Key);
					if (lk != null && lk != faction)
						continue;

					var ok = true;
					foreach (var raw in b.Prerequisites)
					{
						var p = raw.ToLowerInvariant();
						var negative = false;
						while (p.Length > 0 && (p[0] == '~' || p[0] == '!'))
						{
							if (p[0] == '!') negative = true;
							p = p[1..];
						}

						// Ignore negative prerequisites: they encode mutually-exclusive upgrade/doctrine
						// choices, not faction membership (a unit and its upgraded form share a faction).
						if (negative || p.Length == 0)
							continue;

						if (!TokenAvailable(p))
						{
							ok = false;
							break;
						}
					}

					if (ok)
					{
						// Owning a buildable actor also grants its deploy target (e.g. a building's "making"
						// form -> finished building). This is safe now that unprovided tokens are unsatisfiable:
						// a faction's MCV is no longer buildable-by-all, so its conyard can't leak cross-faction.
						owned.Add(ai.Key);
						var tgt = transformTarget(ai.Value);
						if (!string.IsNullOrEmpty(tgt))
							TryOwn(tgt);
						changed = true;
					}
				}
			}

			return owned;
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
