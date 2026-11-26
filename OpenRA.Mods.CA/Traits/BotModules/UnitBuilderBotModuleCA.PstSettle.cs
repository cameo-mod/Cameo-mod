#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-03 P6 (b), row pst_settle_authoritative (Opus ruling P6b, option a). The mod has no UpgradeManager: an upgrade is a queue item whose actor
	// provides prerequisites, and the owned state is the player's TechTree. Authoritative settle = the item's provided prerequisite is present in the OWN TechTree.
	// Off = OwnedPrerequisites() (the bot's own scan), byte for byte. Own player only: TechTree.HasPrerequisites reads Owner's actors (engine Player/TechTree.cs:66).
	public partial class UnitBuilderBotModuleCA
	{
		AblationLatch ablatePstSettleAuthoritative;

		bool PstSettleAuthoritativeOn => Info.PstSettleAuthoritative && !ablatePstSettleAuthoritative.Off(player, AblationIds.PstSettleAuthoritative);

		// The owned set Settle and UpgradePrerequisitesHeld read. On: only the names the pending items need, each asked of the own TechTree.
		ISet<string> SettleOwned()
		{
			if (!PstSettleAuthoritativeOn)
				return OwnedPrerequisites();

			var held = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (techTree == null)
				return held;

			foreach (var name in upgradeDone.PendingNames)
			{
				foreach (var raw in UpgradeGrantsOf(name).Concat(BuildablePrerequisites(name)))
				{
					var p = raw.Replace("~", "");
					if (p.Length == 0 || p.StartsWith('!') || held.Contains(p))
						continue;

					if (techTree.HasPrerequisites(new[] { p }))
						held.Add(p);
				}
			}

			return held;
		}

		IEnumerable<string> BuildablePrerequisites(string name) =>
			world.Map.Rules.Actors.TryGetValue(name, out var info)
				? info.TraitInfos<BuildableInfo>().SelectMany(bi => bi.Prerequisites)
				: Enumerable.Empty<string>();
	}
}
