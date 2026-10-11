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
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Cameo.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>The last refresh's contact picture, for the situation log. Rebuilt every refresh.</summary>
	public sealed class RadarContactsSnapshot
	{
		public int Tick, Providers;
		public BotRadarContact[] Contacts = Array.Empty<BotRadarContact>();
	}

	[TraitLocation(SystemActors.Player)]
	[Desc("Radar contacts (RADAR-A): the bot-side equivalent of a human watching the ranged-GPS dots.",
		"Each refresh enumerates the own/allied RangedGpsWatcher provider lists (player-actor trait reads,",
		"never a world scan) and keeps the actors that pass the SAME predicate the render effect applies",
		"for a human viewer — CameoRangedGpsDotEffect.ShouldRenderDot branch-for-branch. Contacts carry",
		"only cell, tick, drift velocity, the icon class and the owner colour a human would see; no type,",
		"no health, no ActorID. A PURE PROVIDER: publishes IBotRadarContacts and the situation-log",
		"snapshot, issues no orders, and phase A has no consumers. `genericbot && radar_contacts`: off on",
		"master until the increment A/B arms it (switch group AI_radar_contacts).")]
	public class RadarContactsBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Ticks between contact refreshes — the cadence at which a human could write the radar down.")]
		public readonly int ContactsIntervalTicks = 25;

		[Desc("World ticks a contact's last sighting survives for velocity after the actor leaves every",
			"disc (a dot blinking out and back in keeps its drift; a long-gone one re-enters fresh).")]
		public readonly int TrackGraceTicks = 250;

		public override object Create(ActorInitializer init) { return new RadarContactsBotModule(init.Self, this); }
	}

	public class RadarContactsBotModule : ConditionalTrait<RadarContactsBotModuleInfo>, IBotTick, IBotRadarContacts
	{
		// Internal velocity memory keyed by ActorID — the key never leaves this type;
		// contacts expose drift only, no identity.
		internal sealed class Track
		{
			public CPos Cell;
			public int Tick;
			public int VXPerKilotick, VYPerKilotick;
		}

		readonly World world;
		readonly OpenRA.Player player;
		readonly Dictionary<uint, Track> tracks = new();
		readonly List<BotRadarContact> contacts = new();
		IReadOnlyList<BotRadarContact> published = Array.Empty<BotRadarContact>();
		int nextRefreshTick;

		public RadarContactsSnapshot Snapshot { get; private set; }

		public RadarContactsBotModule(Actor self, RadarContactsBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		IReadOnlyList<BotRadarContact> IBotRadarContacts.Contacts => published;

		// Pure, for tests: fixed-point drift in cells x1000 per tick (the situation
		// log's vx_per_kilotick convention).
		internal static int VelocityPerKilotick(int delta, int ticks)
		{
			return ticks > 0 ? delta * 1000 / ticks : 0;
		}

		// Pure, for tests: fold a sighting into the track — first sighting anchors
		// position with zero drift, later ones update drift from the delta. Same
		// tick updates position but keeps the older velocity (no dt to divide by).
		internal static void UpdateTrack(Track track, CPos cell, int tick)
		{
			if (tick == track.Tick)
			{
				track.Cell = cell;
				return;
			}

			track.VXPerKilotick = VelocityPerKilotick(cell.X - track.Cell.X, tick - track.Tick);
			track.VYPerKilotick = VelocityPerKilotick(cell.Y - track.Cell.Y, tick - track.Tick);
			track.Cell = cell;
			track.Tick = tick;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (IsTraitDisabled)
				return;

			var tick = world.WorldTick;
			if (tick < nextRefreshTick)
				return;

			nextRefreshTick = tick + Math.Max(1, Info.ContactsIntervalTicks);
			Refresh(tick);
		}

		void Refresh(int tick)
		{
			// Age out tracks whose dot has been dark too long (blinking keeps the
			// drift across TrackGraceTicks; a re-entering contact starts fresh).
			List<uint> stale = null;
			foreach (var (id, t) in tracks)
				if (tick - t.Tick > Info.TrackGraceTicks)
					(stale ??= new List<uint>()).Add(id);
			if (stale != null)
				foreach (var id in stale)
					tracks.Remove(id);

			contacts.Clear();
			var watcher = player.PlayerActor?.TraitOrDefault<RangedGpsWatcher>();
			var anyWatcherGranted = watcher != null && (watcher.Granted || watcher.GrantedAllies);
			var providerCount = 0;

			if (anyWatcherGranted)
			{
				var seen = new HashSet<uint>();
				foreach (var p in world.Players)
				{
					if (p != player && !player.IsAlliedWith(p))
						continue;

					var allied = p.PlayerActor?.TraitOrDefault<RangedGpsWatcher>();
					if (allied == null)
						continue;

					foreach (var provider in allied.Providers)
					{
						// Only cameo providers expose their proximity list; engine
						// providers (none in cameo rules) cannot be enumerated and
						// are skipped rather than scanned for.
						if (provider is not CameoRangedGpsProvider cp)
							continue;

						var self = cp.Self;
						if (self.IsDead || !self.IsInWorld || cp.IsTraitDisabled)
							continue;

						providerCount++;
						foreach (var a in cp.ActorsInRange)
							if (a.IsInWorld && !a.IsDead && seen.Add(a.ActorID))
								AddContact(a, tick);
					}
				}
			}

			contacts.Sort(static (x, y) =>
			{
				var c = x.Cell.X.CompareTo(y.Cell.X);
				if (c != 0) return c;
				c = x.Cell.Y.CompareTo(y.Cell.Y);
				if (c != 0) return c;
				c = string.CompareOrdinal(x.Owner?.InternalName, y.Owner?.InternalName);
				if (c != 0) return c;
				return string.CompareOrdinal(x.Class, y.Class);
			});

			published = contacts.ToArray();
			Snapshot = new RadarContactsSnapshot { Tick = tick, Providers = providerCount, Contacts = (BotRadarContact[])published };
		}

		void AddContact(Actor a, int tick)
		{
			var dot = a.TraitOrDefault<CameoRangedGpsDot>();
			if (dot == null)
				return;

			var frozen = player.FrozenActorLayer?.FromID(a.ActorID);
			var effectiveOwner = a.EffectiveOwner != null && a.EffectiveOwner.Owner != null ? a.EffectiveOwner.Owner : a.Owner;

			// The renderer's own conjunction, branch-for-branch — one predicate on
			// purpose so the contacts can never drift from what the human sees.
			var visible = CameoRangedGpsDotEffect.ShouldRenderDot(
				traitDisabled: dot.IsTraitDisabled,
				anyWatcherGranted: true,
				frozenPortraitVisible: frozen != null && frozen.HasRenderables,
				actorLooksAllied: a.EffectiveOwner != null && a.EffectiveOwner.Owner != null
					&& player.IsAlliedWith(a.EffectiveOwner.Owner),
				visibilityModifierDenies: a.TraitsImplementing<IVisibilityModifier>().Any(m => !m.IsVisible(a, player)),
				shroudBlocks: !dot.Info.VisibleInShroud && !player.Shroud.IsExplored(a.CenterPosition),
				providerInRange: dot.Providers.Exists(p => CameoRangedGpsDotEffect.ProviderCountsForViewer(
					p.Owner == player, player.IsAlliedWith(p.Owner), p.IsDead)),
				alreadyVisible: a.TraitOrDefault<IDefaultVisibility>()?.IsVisible(a, player) ?? false);
			if (!visible)
				return;

			var cell = a.Location;
			if (!tracks.TryGetValue(a.ActorID, out var track))
				tracks[a.ActorID] = track = new Track { Cell = cell, Tick = tick };
			else
				UpdateTrack(track, cell, tick);

			contacts.Add(new BotRadarContact
			{
				Cell = cell,
				Tick = tick,
				VXPerKilotick = track.VXPerKilotick,
				VYPerKilotick = track.VYPerKilotick,
				Class = dot.Info.Sequence,
				Owner = effectiveOwner
			});
		}
	}
}
