#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Dev
{
	/// <summary>
	/// ABLATE-01 DEV-only value: `&lt;id&gt;[,&lt;id&gt;...][@team&lt;k&gt;]` (ABLATE_01_PLAN_02 section 2). Every id must be a registry id
	/// (the constants of the partial <see cref="AblationIds"/>); an unknown id is a parse error, so a DEV start fails closed
	/// instead of silently running the full bundle. Without `@team&lt;k&gt;` the ids are off for every player; with it, only
	/// for players on lobby team k. The `needs` closure of the registry yaml is applied by the tooling that writes the
	/// value (ABLATE-01 runner), not here. This type and its option strings exist only in the DEV assembly.
	/// </summary>
	public sealed class AblationSpec
	{
		public const string OptionId = "cameo-ablate";
		public const string ArgumentName = "Cameo.DevAblate";
		public const string EnvironmentName = "CAMEO_DEV_ABLATE";

		public static readonly AblationSpec Empty = new(new HashSet<string>(StringComparer.Ordinal), null);

		AblationSpec(HashSet<string> ids, int? team)
		{
			Ids = ids;
			Team = team;
		}

		public IReadOnlyCollection<string> Ids { get; }

		/// <summary>The lobby team (1 or more) the ids are limited to, or null for every player.</summary>
		public int? Team { get; }

		/// <summary>Registry ids: the public string constants of every part of the partial <see cref="AblationIds"/>.</summary>
		public static IReadOnlyCollection<string> RegistryIds() => typeof(AblationIds)
			.GetFields(BindingFlags.Public | BindingFlags.Static)
			.Where(f => f.IsLiteral && f.FieldType == typeof(string))
			.Select(f => (string)f.GetRawConstantValue())
			.ToArray();

		public static bool TryParse(string value, IReadOnlyCollection<string> known, out AblationSpec spec, out string error)
		{
			spec = Empty;
			error = null;
			if (string.IsNullOrWhiteSpace(value))
				return true;

			var text = value.Trim();
			int? team = null;
			var at = text.LastIndexOf('@');
			if (at >= 0)
			{
				var suffix = text[(at + 1)..];
				if (!suffix.StartsWith("team", StringComparison.Ordinal)
					|| !int.TryParse(suffix[4..], NumberStyles.None, CultureInfo.InvariantCulture, out var k) || k < 1)
				{
					error = "bad team suffix '" + suffix + "' (expected team<k> with k >= 1)";
					return false;
				}

				team = k;
				text = text[..at];
			}

			var ids = new HashSet<string>(StringComparer.Ordinal);
			foreach (var raw in text.Split(','))
			{
				var id = raw.Trim();
				if (id.Length == 0)
				{
					error = "empty id in the list";
					return false;
				}

				if (!known.Contains(id))
				{
					error = "unknown ablation id '" + id + "'";
					return false;
				}

				ids.Add(id);
			}

			spec = new AblationSpec(ids, team);
			return true;
		}

		/// <summary>True when <paramref name="id"/> is ablated for a player on lobby team <paramref name="ownerTeam"/> (0 = no team).</summary>
		public bool IsOff(string id, int ownerTeam) => Ids.Contains(id) && (Team == null || Team == ownerTeam);
	}
}
