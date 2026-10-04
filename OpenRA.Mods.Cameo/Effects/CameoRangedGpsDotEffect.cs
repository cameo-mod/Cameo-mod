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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Effects
{
	/// <summary>
	/// Cameo shadow of Mods.AS RangedGpsDotEffect. Branches mirror the engine's
	/// ShouldRender one-for-one; the provider-range check (engine
	/// RangedGpsDotEffect.cs:87) is widened so providers owned by an ALLY of the
	/// viewer also count — allied radars share their GPS discs (RADAR-ALLY).
	/// Render-side only: the Visible flag is consumed exclusively by
	/// RenderAboveShroud for the local render player; nothing synced reads it.
	/// </summary>
	sealed class CameoRangedGpsDotEffect : IEffect, IEffectAboveShroud
	{
		readonly Actor actor;
		readonly CameoRangedGpsDot trait;
		readonly Animation anim;

		readonly PlayerDictionary<RangedDotState> dotStates;
		readonly IDefaultVisibility visibility;
		readonly IVisibilityModifier[] visibilityModifiers;

		sealed class RangedDotState
		{
			public readonly RangedGpsWatcher Watcher;
			public readonly FrozenActor FrozenActor;
			public bool Visible;
			public bool AlliedLogged;
			public RangedDotState(Actor a, RangedGpsWatcher watcher, FrozenActorLayer frozenLayer)
			{
				Watcher = watcher;
				if (frozenLayer != null)
					FrozenActor = frozenLayer.FromID(a.ActorID);
			}
		}

		public CameoRangedGpsDotEffect(Actor actor, CameoRangedGpsDot trait)
		{
			this.actor = actor;
			this.trait = trait;
			anim = new Animation(actor.World, trait.Info.Image);
			anim.PlayRepeating(trait.Info.Sequence);

			visibility = actor.Trait<IDefaultVisibility>();
			visibilityModifiers = actor.TraitsImplementing<IVisibilityModifier>().ToArray();

			dotStates = new PlayerDictionary<RangedDotState>(actor.World,
				p => new RangedDotState(actor, p.PlayerActor.Trait<RangedGpsWatcher>(), p.FrozenActorLayer));
		}

		/// <summary>
		/// The RADAR-ALLY decision, kept pure for unit tests: a provider counts for
		/// the viewer when it is alive and owned by the viewer OR by an ally of the
		/// viewer (the stance engine code uses for shared vision, e.g.
		/// RangedGpsDotEffect's own allied-actor check and GpsWatcher's ally grant).
		/// </summary>
		internal static bool ProviderCountsForViewer(bool sameOwner, bool allied, bool providerDead)
		{
			return !providerDead && (sameOwner || allied);
		}

		/// <summary>
		/// The full gate as a pure conjunction so tests can veto each branch
		/// independently; the effect calls it with the live values below.
		/// </summary>
		internal static bool ShouldRenderDot(bool traitDisabled, bool anyWatcherGranted,
			bool frozenPortraitVisible, bool actorLooksAllied, bool visibilityModifierDenies,
			bool shroudBlocks, bool providerInRange, bool alreadyVisible)
		{
			return !traitDisabled && anyWatcherGranted && !frozenPortraitVisible && !actorLooksAllied
				&& !visibilityModifierDenies && !shroudBlocks && providerInRange && !alreadyVisible;
		}

		bool ShouldRender(RangedDotState state, Player toPlayer)
		{
			// Hide the indicator if the owner trait is disabled
			var traitDisabled = trait.IsTraitDisabled;

			// Hide the indicator if no watchers are available (own or allied —
			// RangedGpsWatcher.GrantedAllies already tracks allied providers)
			var anyWatcherGranted = state.Watcher.Granted || state.Watcher.GrantedAllies;

			// Hide the indicator if a frozen actor portrait is visible
			var frozenPortraitVisible = state.FrozenActor != null && state.FrozenActor.HasRenderables;

			// Hide the indicator if the unit appears to be owned by an allied player
			var actorLooksAllied = actor.EffectiveOwner != null && actor.EffectiveOwner.Owner != null &&
				toPlayer.IsAlliedWith(actor.EffectiveOwner.Owner);

			// Hide indicator if the actor wouldn't otherwise be visible if there wasn't fog
			var visibilityModifierDenies = visibilityModifiers.Any(m => !m.IsVisible(actor, toPlayer));

			// Hide the indicator behind shroud
			var shroudBlocks = !trait.Info.VisibleInShroud && !toPlayer.Shroud.IsExplored(actor.CenterPosition);

			// Hide the indicator if it is not in range of an owned OR ALLIED provider (RADAR-ALLY fix)
			var providerInRange = trait.Providers.Exists(p => ProviderCountsForViewer(
				p.Owner == toPlayer, toPlayer.IsAlliedWith(p.Owner), p.IsDead));

			// Hide the indicator if the unit is already visible without fog
			var alreadyVisible = visibility.IsVisible(actor, toPlayer);

			return ShouldRenderDot(traitDisabled, anyWatcherGranted, frozenPortraitVisible,
				actorLooksAllied, visibilityModifierDenies, shroudBlocks, providerInRange, alreadyVisible);
		}

		void IEffect.Tick(World world)
		{
			// Visible is consumed only by RenderAboveShroud, and only ever for the local render player,
			// so recompute the (expensive) visibility test for that one player instead of for every
			// player each tick. Cosmetic, non-synced state - safe to compute per-client.
			var renderPlayer = world.RenderPlayer;
			if (renderPlayer == null)
				return;

			var state = dotStates[renderPlayer];
			state.Visible = ShouldRender(state, renderPlayer);

			// RADAR-ALLY diagnostic: a dot that becomes visible with NO live own
			// provider but a live ALLIED one in range proves the shared-disc fix.
			// Logged once per actor per render player; render-side only.
			if (state.Visible && !state.AlliedLogged
				&& !trait.Providers.Exists(p => !p.IsDead && p.Owner == renderPlayer)
				&& trait.Providers.Exists(p => !p.IsDead && renderPlayer.IsAlliedWith(p.Owner)))
			{
				state.AlliedLogged = true;
				Log.Write("debug", $"radar_ally: dot on {actor.Info.Name} visible to {renderPlayer.InternalName} via allied provider");
			}
		}

		IEnumerable<IRenderable> IEffect.Render(WorldRenderer wr)
		{
			return SpriteRenderable.None;
		}

		IEnumerable<IRenderable> IEffectAboveShroud.RenderAboveShroud(WorldRenderer wr)
		{
			if (actor.World.RenderPlayer == null || !dotStates[actor.World.RenderPlayer].Visible)
				return SpriteRenderable.None;

			var effectiveOwner = actor.EffectiveOwner != null && actor.EffectiveOwner.Owner != null ?
				actor.EffectiveOwner.Owner : actor.Owner;

			var palette = wr.Palette(trait.Info.IndicatorPalettePrefix + effectiveOwner.InternalName);
			return anim.Render(actor.CenterPosition, palette);
		}
	}
}
