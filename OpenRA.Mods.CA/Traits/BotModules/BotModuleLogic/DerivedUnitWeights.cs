#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// AI_ARCHITECTURE.md §2.8b item 3 (UW-1): the derived `UnitsToBuild` table. The
	/// personality's RoleMix (§12.5) sets a target share per combat role; a candidate's
	/// weight is that share scaled by its Versus-weighted strength relative to the other
	/// candidates that share its primary role:
	///
	///   share[r]    = RoleShares(mix, RoleMixUnnamedTotalPct, ...)[r] (PROD-IDLE_PLAN_05): the
	///                 named roles keep their relative weights inside 100 - U, the unnamed
	///                 buildable roles split U evenly; unnamed transport and air_transport get
	///                 no share and their rows keep the yaml weight verbatim
	///   strength[u] = max over weapons of (DamagePerTick × mean(Versus values) / 100),
	///                 floored at 1 so a weaponless unit still counts toward the role mean
	///   w[u]        = max(1, round(share[primaryRole(u)] × strength[u] / mean strength
	///                 over the candidates whose primary role is primaryRole(u)))
	///
	/// The candidate set is exactly the yaml `UnitsToBuild` keys: the rows stay the
	/// membership gate, and a unit with no primary combat role keeps its yaml weight
	/// verbatim — the deliberate hand rows of §2.8b survive as overrides. A unit counts
	/// toward exactly one role, its PRIMARY (IBotUnitRoles.PrimaryRoleOf), mirroring
	/// ChooseRoleDeficit's composition accounting. The mix's own domain is
	/// BotUnitRole.CombatRoles; anything else a mix names can never be served and is
	/// ignored. Pure own-side stats — no enemy is enumerated (DESIGN §19.5).
	/// </summary>
	public sealed class DerivedUnitWeights
	{
		// The derived table is a pure function of (yamlWeights, mix, unnamedTotalPct, roles,
		// ruleset); every input but the enabled squad manager is fixed at rules load,
		// and a personality switch swaps the manager — hence the mix dictionary — so
		// (mix, unnamedTotalPct) is the whole cache key. One table serves every queue
		// category: UnitsToBuild is not split per category.
		IReadOnlyDictionary<string, int> cachedMix;
		int cachedUnnamedPct = int.MinValue;
		Dictionary<string, int> cachedTable;

		/// <summary>
		/// PROD-IDLE_PLAN_05, the ONE share function used by both consumers (the deficit chooser
		/// `ChooseRoleDeficit` and `Derive`): percent share per combat role.
		///
		///   S = sum of the positive mix values over the named combat roles (keys outside
		///       BotUnitRole.CombatRoles are ignored, as before); S = 0 or no mix: empty result.
		///   N = combat roles the mix does not name, minus transport and air_transport, that have
		///       at least one member for which `inGate` is true (the UnitsToBuild membership gate).
		///   unnamed role in N : U / |N|
		///   named role        : (100 - U) x mix[r] / S when N is non-empty and U &gt; 0,
		///                       otherwise mix[r] unscaled (nowhere to put the reserve).
		///   unnamed transport / air_transport: absent (no deficit target, yaml weight verbatim).
		///   A mix that names transport or air_transport serves it as any named role.
		/// U is clamped to 0..100; a negative U (line not written) means no unnamed share.
		/// </summary>
		public static Dictionary<string, double> RoleShares(
			IReadOnlyDictionary<string, int> mix,
			int unnamedTotalPct,
			IBotUnitRoles roles,
			Func<string, bool> inGate)
		{
			var shares = new Dictionary<string, double>(StringComparer.Ordinal);
			if (mix == null || mix.Count == 0 || roles == null)
				return shares;

			double sum = 0;
			foreach (var kv in mix)
				if (BotUnitRole.CombatRoles.Contains(kv.Key))
					sum += Math.Max(0, kv.Value);

			if (sum <= 0)
				return shares;

			var u = Math.Clamp(unnamedTotalPct, 0, 100);
			var unnamed = new List<string>();
			if (u > 0 && inGate != null)
			{
				foreach (var role in BotUnitRole.PrimaryRoleOrder)
				{
					if (mix.ContainsKey(role) || role == BotUnitRole.Transport || role == BotUnitRole.AirTransport)
						continue;

					if (roles.RoleMembers.TryGetValue(role, out var members) && members.Any(inGate))
						unnamed.Add(role);
				}
			}

			var scaled = unnamed.Count > 0;
			foreach (var kv in mix)
				if (BotUnitRole.CombatRoles.Contains(kv.Key))
					shares[kv.Key] = scaled ? (100 - u) * (double)Math.Max(0, kv.Value) / sum : Math.Max(0, kv.Value);

			foreach (var role in unnamed)
				shares[role] = (double)u / unnamed.Count;

			return shares;
		}

		/// <summary>
		/// The UnitsToBuild table for one queue pick. An applicable composition wins over
		/// everything (§1.4 compositions stay the personality flavour); flag off, no
		/// RoleMix, or no roles provider each return the yaml table unchanged — the
		/// pre-derivation path, byte-identical.
		/// </summary>
		public Dictionary<string, int> Select(
			bool useDerived,
			Dictionary<string, int> compositionWeights,
			Dictionary<string, int> yamlWeights,
			IReadOnlyDictionary<string, int> mix,
			int unnamedTotalPct,
			IBotUnitRoles roles,
			Func<string, double> strengthOf)
		{
			if (compositionWeights != null)
				return compositionWeights;

			if (!useDerived || yamlWeights == null || mix == null || mix.Count == 0 ||
				roles == null || strengthOf == null)
				return yamlWeights;

			if (cachedTable == null || !ReferenceEquals(cachedMix, mix) || cachedUnnamedPct != unnamedTotalPct)
			{
				cachedTable = Derive(yamlWeights, mix, unnamedTotalPct, roles, strengthOf);
				cachedMix = mix;
				cachedUnnamedPct = unnamedTotalPct;
			}

			return cachedTable;
		}

		/// <summary>
		/// The derivation itself: the result carries exactly `yamlWeights`' keys — roled
		/// rows rewritten by the share × relative-strength formula, every other row verbatim.
		/// </summary>
		public static Dictionary<string, int> Derive(
			IReadOnlyDictionary<string, int> yamlWeights,
			IReadOnlyDictionary<string, int> mix,
			int unnamedTotalPct,
			IBotUnitRoles roles,
			Func<string, double> strengthOf)
		{
			var result = new Dictionary<string, int>(yamlWeights.Count, StringComparer.Ordinal);
			foreach (var kv in yamlWeights)
				result[kv.Key] = kv.Value;

			// S = 0 (no usable mix): nothing to derive, the yaml rows stay verbatim.
			var shares = RoleShares(mix, unnamedTotalPct, roles, yamlWeights.ContainsKey);
			if (shares.Count == 0)
				return result;

			// Bucket the candidates by the one role they count toward. Roles with no
			// candidate member (a mix key the roster cannot serve) are simply absent.
			var byRole = new Dictionary<string, List<string>>(StringComparer.Ordinal);
			foreach (var name in yamlWeights.Keys)
			{
				var role = roles.PrimaryRoleOf(name);
				if (role == null || !BotUnitRole.CombatRoles.Contains(role))
					continue;

				if (!byRole.TryGetValue(role, out var members))
					byRole[role] = members = new List<string>();
				members.Add(name);
			}

			foreach (var (role, members) in byRole)
			{
				// An unnamed transport / air_transport has no share: its rows keep the yaml weight.
				if (!shares.TryGetValue(role, out var share))
					continue;

				var strength = members.ToDictionary(m => m, strengthOf, StringComparer.Ordinal);

				// strength is floored at 1, so the mean is never 0.
				var mean = strength.Values.Average();
				foreach (var m in members)
					result[m] = Math.Max(1, (int)Math.Round(share * strength[m] / mean, MidpointRounding.AwayFromZero));
			}

			return result;
		}

		/// <summary>
		/// The strength proxy: the best weapon's damage per tick scaled by the mean of its
		/// Versus entries — a weapon with no Versus overrides reads 100, the DamageWarhead
		/// default. A weaponless profile floors at 1 so it still counts toward the role mean.
		/// </summary>
		public static double Strength(BotUnitProfile profile, bool useEffective = false)
		{
			var best = 0.0;
			if (profile != null)
				foreach (var w in profile.Weapons)
				{
					var versus = w.Versus.Count == 0 ? 100.0 : w.Versus.Values.Average();
					best = Math.Max(best, useEffective && w.EffectiveDamagePerTick > 0
						? w.EffectiveDamagePerTick
						: w.DamagePerTick * versus / 100.0);
				}

			return Math.Max(1.0, best);
		}
	}
}
