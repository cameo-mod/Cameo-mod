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
using System.Globalization;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
#if CAMEO_DEV_LOGGING
using OpenRA;
#endif

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("DEV-ONLY economy knob for the round-robin evaluation (STAGGER_EVAL_PLAN_03 option B). Inert in every build",
		"without CAMEO_DEV_LOGGING. In a DEV build the launch argument Cameo.DevCashBonus=Multi0:10,Multi1:0",
		"(player = slot id or player name, value = whole percent 0..100) gives that player extra starting cash of",
		"startingCash x pct / 100 once at world start, and tops up every resource deposit by value x pct / 100",
		"(integer division). A value of 0, or no argument, changes nothing. Every client of a game must be given",
		"the same argument or the game desyncs.")]
	public class DevCashBonusInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new DevCashBonus(init.Self);
	}

	public class DevCashBonus : INotifyResourceAccepted, IWorldLoaded
	{
		public const int MaxPercent = 100;

		readonly Actor self;
		int percent;

		public DevCashBonus(Actor self)
		{
			this.self = self;
		}

		/// <summary>
		/// The percent named for this player in <paramref name="spec"/> (<c>Multi0:10,Multi1:0</c>); 0 when the spec is empty or
		/// names nobody. Keys match the slot id or the player name, case-insensitive. A malformed entry or a value outside
		/// 0..100 throws: a mistyped DEV argument must be loud, not a silently unbalanced game.
		/// </summary>
		public static int ParsePercent(string spec, params string[] playerKeys)
		{
			if (string.IsNullOrWhiteSpace(spec))
				return 0;

			foreach (var entry in spec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				var i = entry.LastIndexOf(':');
				if (i <= 0)
					throw new FormatException($"Cameo.DevCashBonus entry '{entry}' is not Player:percent.");

				if (!int.TryParse(entry[(i + 1)..].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var pct) || pct > MaxPercent)
					throw new FormatException($"Cameo.DevCashBonus entry '{entry}' needs a whole percent 0..{MaxPercent}.");

				var key = entry[..i].Trim();
				foreach (var k in playerKeys)
					if (k != null && string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
						return pct;
			}

			return 0;
		}

		/// <summary>Integer top-up for one deposit: value x pct / 100 (long intermediate, rounds toward zero).</summary>
		public static int TopUp(int value, int pct) => value <= 0 || pct <= 0 ? 0 : (int)((long)value * pct / 100);

		void IWorldLoaded.WorldLoaded(World w, OpenRA.Graphics.WorldRenderer wr)
		{
#if CAMEO_DEV_LOGGING
			var owner = self.Owner;
			if (!owner.Playable || owner.NonCombatant)
				return;

			percent = ParsePercent(CameoDevArgs.Value("Cameo.DevCashBonus"), owner.InternalName, owner.PlayerName);

			var resources = self.TraitOrDefault<PlayerResources>();
			var before = resources?.Cash ?? 0;
			var extra = 0;

			// A loaded save already carries the bonus in its cash; only a new game (tick 0) gets it.
			if (resources != null && w.WorldTick == 0 && !w.IsLoadingGameSave)
			{
				extra = TopUp(before, percent);
				if (extra > 0)
					resources.GiveCash(extra);
			}

			Log.Write("perf", string.Format(CultureInfo.InvariantCulture,
				"DevCashBonus seat={0} player={1} pct={2} start_cash={3} start_extra={4} tick={5}",
				owner.InternalName, owner.PlayerName, percent, before, extra, w.WorldTick));
#endif
		}

		void INotifyResourceAccepted.OnResourceAccepted(Actor self, Actor refinery, string resourceType, int count, int value)
		{
#if CAMEO_DEV_LOGGING
			// The engine calls this only for traits whose actor is owned by the refinery's owner (Refinery.cs:81-87).
			if (percent <= 0)
				return;

			var extra = TopUp(value, percent);
			if (extra > 0)
				self.Trait<PlayerResources>().GiveCash(extra);
#endif
		}
	}
}
