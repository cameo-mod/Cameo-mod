#region Copyright & License Information
/*
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
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	[TraitLocation(SystemActors.Player)]
	[Desc("OBS-STYLE-PROVIDER-01 (design 03 02f2f333): classifies the main target's observed style from what this bot has seen, as it appeared (a disguise as its",
		"disguise, a mirage as nothing). Observation only: no enemy personality, condition, queue, cash or order is read. Release and DEV both infer;",
		"off by default (switch obs_style_provider, ablation row obs_style_provider). 75-tick cadence staggered per seat; at most 128 records per visit.")]
	public sealed class ObservedStyleBotModuleInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) => new ObservedStyleBotModule(init.Self, this);
	}

	public sealed class ObservedStyleBotModule : ConditionalTrait<ObservedStyleBotModuleInfo>, IBotTick, IBotObservedMainTargetStyle, IBotObservedStyle
	{
		const int Interval = 75;
		const int MaxObservations = 128;
		readonly OpenRA.Player viewer;
		readonly World world;
		readonly FogStyleActor[] snapshot = new FogStyleActor[MaxObservations];
		readonly int[] features = new int[StyleFeatures.Count];
		sealed class EnemyState
		{
			public readonly StyleTracker Tracker = new();
			public readonly StyleClassifier Classifier = new(StyleTable.HandPrior);
			public int LastGeneration = -1;
		}

		readonly Dictionary<OpenRA.Player, EnemyState> states = new();
		readonly ObservedStyleQualification qualification = new();
		readonly List<CPos> yards = new(32);
		Dictionary<ITooltipInfo, ActorInfo> apparentTooltips;
		readonly Dictionary<ActorInfo, ActorInfo> publicImages = new();
		MasterAiBotModule master;
		int nextEnemy;
		OpenRA.Player target;
		int nextTick = -1;
		int generationTick = -1;
		AblationLatch ablate;

		public ObservedStyleBotModule(Actor actor, ObservedStyleBotModuleInfo info)
			: base(info)
		{
			viewer = actor.Owner;
			world = actor.World;
		}

		internal bool CollectAppearance => !IsTraitDisabled && !ablate.Off(viewer, AblationIds.ObservedStyleProvider);

		internal ActorInfo ApparentInfo(Actor actor, OpenRA.Player enemy)
		{
			var effective = actor.EffectiveOwner;
			if (effective?.Disguised != true)
				return actor.Owner == enemy ? PublicImage(actor.Info) : null;

			var disguise = actor.TraitOrDefault<Disguise>();
			var apparent = EffectiveAppearance(enemy, effective.Owner, disguise?.AsActor);
			return apparent != null ? PublicImage(apparent) : null;
		}

		internal static ActorInfo EffectiveAppearance(OpenRA.Player enemy, OpenRA.Player apparentOwner, ActorInfo apparentType) =>
			apparentOwner == enemy ? apparentType : null;

		/// <summary>The type an enemy viewer is shown for <paramref name="info"/>, or null when the type itself is hidden from enemies (a generic
		/// tooltip) or its sprite alias does not resolve. Resolved once per type: the rules never change during a game.</summary>
		ActorInfo PublicImage(ActorInfo info)
		{
			if (publicImages.TryGetValue(info, out var image))
				return image;

			image = ResolvePublicImage(info);
			publicImages.Add(info, image);
			return image;
		}

		ActorInfo ResolvePublicImage(ActorInfo info)
		{
			if (info.TraitInfoOrDefault<TooltipInfo>() is TooltipInfo tooltip
				&& tooltip.GenericVisibility.HasRelationship(PlayerRelationship.Enemy))
				return null;

			// Sprite aliases use the canonical apparent type. Unresolved aliases carry no type evidence.
			var image = info.TraitInfoOrDefault<RenderSpritesInfo>()?.Image;
			return string.IsNullOrEmpty(image) || image == info.Name
				? info : world.Map.Rules.Actors.GetValueOrDefault(image);
		}

		internal ActorInfo ApparentInfo(FrozenActor actor, OpenRA.Player enemy)
		{
			if (actor.TooltipOwner != enemy || actor.TooltipInfo == null)
				return null;

			if (apparentTooltips == null)
			{
				apparentTooltips = new();
				foreach (var template in world.Map.Rules.Actors.Values)
					foreach (var tooltip in template.TraitInfos<ITooltipInfo>())
						if (!apparentTooltips.TryAdd(tooltip, template))
							apparentTooltips[tooltip] = null;
			}

			return apparentTooltips.TryGetValue(actor.TooltipInfo, out var info) && info != null ? PublicImage(info) : null;
		}

		OpenRA.Player MainTarget() => ((IBotMainTargetProvider)master)?.MainTarget;

		bool IsEnemy(OpenRA.Player player) => player != null && !player.NonCombatant && !player.Spectating
			&& player.WinState == WinState.Undefined && viewer.RelationshipWith(player) == PlayerRelationship.Enemy;

		public string ObservedStyleOf(OpenRA.Player enemy)
		{
			if (!CollectAppearance || enemy != target || enemy != MainTarget() || !IsEnemy(enemy))
				return null;

			var label = qualification.QualifiedClass(world.WorldTick);
			return label >= 0 ? StyleReading.StyleNames[label] : null;
		}

		public StyleReading Reading(OpenRA.Player enemy) =>
			ObservedStyleOf(enemy) != null ? states[enemy].Classifier.Reading : StyleReading.Prior;

		void IBotTick.BotTick(IBot bot)
		{
			if (!CollectAppearance)
				return;

			master ??= viewer.PlayerActor.TraitOrDefault<MasterAiBotModule>();
			var now = world.WorldTick;
			if (nextTick < 0)
				nextTick = now + Math.Max(0, viewer.ClientIndex) % Interval;

			var current = MainTarget();
			if (!IsEnemy(current))
				current = null;

			if (current != target)
			{
				target = current;
				qualification.Reset();
				generationTick = -1;
			}

			if (target == null || now < nextTick || master?.Situation == null)
				return;

			nextTick = now + Interval;
			yards.Clear();
			foreach (var yard in master.Situation.OwnBuildingSnapshot ?? [])
				if (yard.Owner == viewer && yard.IsInWorld && !yard.IsDead && yard.Info.HasTraitInfo<BaseBuildingInfo>())
					yards.Add(yard.Location);

			var used = UpdateEnemy(target, now, MaxObservations, true);
			var players = world.Players;
			for (var checkedPlayers = 0; checkedPlayers < players.Length && used < MaxObservations; checkedPlayers++)
			{
				var other = players[nextEnemy++ % players.Length];
				if (other == target || !IsEnemy(other))
					continue;

				UpdateEnemy(other, now, MaxObservations - used, false);
				break;
			}
		}

		int UpdateEnemy(OpenRA.Player enemy, int now, int capacity, bool isTarget)
		{
			var count = FogStyleAdapter.Copy(master.Situation.Remembered(enemy), snapshot.AsSpan(0, capacity));
			if (count == 0)
				return 0;

			if (!states.TryGetValue(enemy, out var state))
				states.Add(enemy, state = new EnemyState());

			var newest = -1;
			for (var i = 0; i < count; i++)
				newest = Math.Max(newest, snapshot[i].LastSeenTick);

			if (newest <= state.LastGeneration)
				return count;

			state.LastGeneration = newest;
			var observations = snapshot.AsSpan(0, count);
			state.Tracker.Update(observations, System.Runtime.InteropServices.CollectionsMarshal.AsSpan(yards), now);
			StyleFeatures.Extract(observations, now, state.Tracker, features);
			state.Classifier.Update(features, observations, state.Tracker, now);
			if (!isTarget || newest <= generationTick)
				return count;

			generationTick = newest;
			var families = 0;
			var previousFamily = '\0';
			for (var i = 0; i < StyleFeatures.CoverageIndex; i++)
			{
				var family = StyleFeatures.Names[i][1];
				if (features[i] == StyleFeatures.Unknown || !StyleTable.HandPrior.Informative(i) || family == previousFamily)
					continue;
				previousFamily = family;
				families++;
			}

			var reading = state.Classifier.Reading;
			qualification.Observe(newest, now, families, reading.ConfidencePermille, reading.PosteriorPermille);
			return count;
		}
	}
}
