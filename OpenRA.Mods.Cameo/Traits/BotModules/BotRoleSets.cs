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
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	[Desc("What this actor is FOR, as far as the bots are concerned (AI_ARCHITECTURE.md §2.8).",
		"Declared on the actor in its own ContentPack, so that no central ai.yaml list has to name it.",
		"BotRoleSets turns roles into the bot modules' actor lists.")]
	public class BotRolesInfo : TraitInfo<BotRoles>
	{
		[FieldLoader.Require]
		[Desc("Role names, e.g. harvester, guerrilla, highvaluetarget.")]
		public readonly FrozenSet<string> Roles = FrozenSet<string>.Empty;
	}

	public class BotRoles { }

	[TraitLocation(SystemActors.Player)]
	[Desc("Fills bot-module actor lists from ROLES instead of central id lists, so a faction's AI is",
		"plug and play: an unloaded ContentPack contributes no actors and therefore no ids (AI_ARCHITECTURE.md §2.8,",
		"ruled 2026-09-27). A role's members are the actors with a matching BotRoles entry plus, if a derivation",
		"is configured, the actors whose traits match it. Runs once at rules load, identically on every client.",
		"A role is only APPLIED when listed in Apply; otherwise it writes what it would add to bot-roles.log.")]
	public class BotRoleSetsInfo : TraitInfo<BotRoleSets>, IRulesetLoaded
	{
		[Desc("Role -> trait types an actor must ALL have to get the role by derivation.",
			"Type names without the Info suffix; base classes count (a subclass of Harvester is a Harvester).")]
		public readonly Dictionary<string, string[]> DeriveHas = [];

		[Desc("Role -> trait types that block the derived role.")]
		public readonly Dictionary<string, string[]> DeriveNot = [];

		[Desc("Role -> actors that never get the role by DERIVATION. An explicit BotRoles entry still applies.")]
		public readonly Dictionary<string, string[]> Exclude = [];

		[Desc("Only actors with Buildable get a derived role.")]
		public readonly bool DeriveOnlyBuildable = true;

		[Desc("Role -> the module list fields it fills, as TraitType.Field. Every instance of that trait on this actor is filled.")]
		public readonly Dictionary<string, string[]> Targets = [];

		[Desc("Roles whose members are ADDED to their Targets. Any other role only reports to bot-roles.log.")]
		public readonly FrozenSet<string> Apply = FrozenSet<string>.Empty;

		void IRulesetLoaded<ActorInfo>.RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			var actors = rules.Actors.Values
				.Where(a => !a.Name.StartsWith('^'))
				.Select(a => new RoleCandidate(
					a.Name,
					TraitTypeNames(a),
					a.TraitInfoOrDefault<BotRolesInfo>()?.Roles ?? FrozenSet<string>.Empty,
					a.HasTraitInfo<BuildableInfo>()));

			var members = ResolveMembers(actors, DeriveHas, DeriveNot, Exclude, DeriveOnlyBuildable);

			Log.AddChannel("bot-roles", "bot-roles.log");
			foreach (var (role, targets) in Targets)
			{
				var roleMembers = members.GetValueOrDefault(role) ?? [];
				foreach (var target in targets)
				{
					var dot = target.LastIndexOf('.');
					if (dot <= 0)
						throw new YamlException($"BotRoleSets on {info.Name}: target `{target}` must be TraitType.Field");

					var traitType = target[..dot];
					var fieldName = target[(dot + 1)..];
					var traitInfos = info.TraitInfos<TraitInfo>().Where(t => t.GetType().Name == traitType + "Info").ToList();
					if (traitInfos.Count == 0)
						throw new YamlException($"BotRoleSets on {info.Name}: role `{role}` targets `{traitType}`, which this actor does not have");

					foreach (var ti in traitInfos)
					{
						var field = ti.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance)
							?? throw new YamlException($"BotRoleSets: `{traitType}` has no public field `{fieldName}`");
						var current = (field.GetValue(ti) as IEnumerable<string>)?.ToHashSet()
							?? throw new YamlException($"BotRoleSets: `{target}` is not a set of actor names");

						var added = roleMembers.Where(m => !current.Contains(m)).OrderBy(m => m, StringComparer.Ordinal).ToList();
						var writtenOnly = current.Where(c => !roleMembers.Contains(c)).OrderBy(c => c, StringComparer.Ordinal).ToList();
						var applied = Apply.Contains(role);
						Log.Write("bot-roles",
							$"{info.Name} {target} role `{role}`: {roleMembers.Count} members, {current.Count} written; " +
							$"{(applied ? "ADDED" : "would add")} {added.Count}: {string.Join(", ", added)}; " +
							$"written but not in role {writtenOnly.Count}: {string.Join(", ", writtenOnly)}");

						if (applied && added.Count > 0)
							field.SetValue(ti, Union(field.FieldType, current, added));
					}
				}
			}
		}

		// Trait type names of every trait on the actor, including base classes, without the Info suffix.
		static HashSet<string> TraitTypeNames(ActorInfo a)
		{
			var names = new HashSet<string>();
			foreach (var ti in a.TraitInfos<TraitInfo>())
				for (var t = ti.GetType(); t != null && t != typeof(TraitInfo) && t != typeof(object); t = t.BaseType)
					names.Add(t.Name.EndsWith("Info", StringComparison.Ordinal) ? t.Name[..^4] : t.Name);

			return names;
		}

		public readonly record struct RoleCandidate(string Name, ISet<string> TraitTypes, IReadOnlySet<string> ExplicitRoles, bool Buildable);

		// Pure: role -> member actor names. Explicit BotRoles always count; derivations need every DeriveHas type,
		// none of the DeriveNot types, and (optionally) Buildable, and are blocked by Exclude.
		public static Dictionary<string, HashSet<string>> ResolveMembers(
			IEnumerable<RoleCandidate> actors,
			IReadOnlyDictionary<string, string[]> deriveHas,
			IReadOnlyDictionary<string, string[]> deriveNot,
			IReadOnlyDictionary<string, string[]> exclude,
			bool deriveOnlyBuildable)
		{
			var members = new Dictionary<string, HashSet<string>>();
			HashSet<string> Of(string role)
			{
				if (!members.TryGetValue(role, out var set))
					members[role] = set = [];
				return set;
			}

			foreach (var a in actors)
			{
				foreach (var role in a.ExplicitRoles)
					Of(role).Add(a.Name);

				if (deriveOnlyBuildable && !a.Buildable)
					continue;

				foreach (var (role, has) in deriveHas)
				{
					if (!has.All(a.TraitTypes.Contains))
						continue;
					if (deriveNot.TryGetValue(role, out var not) && not.Any(a.TraitTypes.Contains))
						continue;
					if (exclude.TryGetValue(role, out var ex) && ex.Contains(a.Name))
						continue;
					Of(role).Add(a.Name);
				}
			}

			return members;
		}

		// A new set of the field's own type: the lists are HashSet in some modules and FrozenSet in others,
		// and a FrozenSet cannot be added to in place.
		public static object Union(Type fieldType, IEnumerable<string> current, IEnumerable<string> added)
		{
			var all = current.Concat(added);
			if (fieldType == typeof(FrozenSet<string>))
				return all.ToFrozenSet();
			if (fieldType == typeof(HashSet<string>))
				return all.ToHashSet();
			if (fieldType == typeof(ImmutableHashSet<string>))
				return all.ToImmutableHashSet();
			throw new YamlException($"BotRoleSets: cannot fill a field of type {fieldType.Name}");
		}
	}

	public class BotRoleSets { }
}
