#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// Allows modifying previews e.g. with WithColoredOverlayCA
	public interface IActorPreviewRenderModifierInfo : ITraitInfoInterface
	{
		IActorPreviewRenderModifier GetPreviewRenderModifier(WorldRenderer wr, ActorInfo actorInfo, TypeDictionary inits, Color previewColor);
	}

	public interface IActorPreviewRenderModifier
	{
		IEnumerable<IRenderable> ModifyPreviewRender(WorldRenderer wr, IEnumerable<IRenderable> renderables, Rectangle bounds);
		void Tick();
	}

	[RequireExplicitImplementation]
	public interface ISmokeParticleInfo
	{
		string Image { get; }
		string[] Sequences { get; }
		string Palette { get; }

		int[] Duration { get; }

		WDist[] Speed { get; }

		WDist[] Gravity { get; }

		WeaponInfo Weapon { get; }

		int TurnRate { get; }

		int RandomRate { get; }
	}

	public interface INotifyActivate { void Launching(Actor self); }

	[RequireExplicitImplementation]
	public interface IPointDefense
	{
		bool Destroy(WPos position, Player attacker, string type);
	}

	public interface IBotCAInfo : ITraitInfoInterface { string Name { get; } }
	public interface IBotCA
	{
		bool IsEnemyUnit(Actor a);
	}

	[RequireExplicitImplementation]
	public interface INotifyChronosphere { void Teleporting(WPos from, WPos to); }

	public interface ILoadsOverlayPlayerPalettes { void LoadOverlayPlayerPalettes(WorldRenderer wr, string playerName, Color playerColor, bool replaceExisting); }

	public interface INotifyPrismCharging { void Charging(Actor self, in Target target); }

	[RequireExplicitImplementation]
	public interface INotifyEnterTeleporter { void Charging(Actor self, Actor teleporter); }
	public interface INotifyExitTeleporter { void Arrived(Actor self); }

	[RequireExplicitImplementation]
	public interface IBotAircraftBuilder { bool CanBuildMoreOfAircraft(ActorInfo actorInfo); }

	[RequireExplicitImplementation]
	public interface IPortableChronoModifier { int GetCooldownModifier(); int GetRangeModifier(); int GetExtraCharges(); }

	[RequireExplicitImplementation]
	public interface INotifyFallDown { void OnLanded(Actor self); }

	[RequireExplicitImplementation]
	public interface IRemoveInfector { void RemoveInfector(Actor self, bool kill, AttackInfo e = null); }

	[RequireExplicitImplementation]
	public interface INotifyAttachedTo { void Attached(Actor self, Actor attachedActor, Attachable attachable); void Detached(Actor self, Actor detachedActor, Attachable attachable); }

	// U2 (service-contract §5, Sol dispositions 0CB8EF18/6919D6E4): the local routing
	// fate of a single order inside the bot's own FIFO. IssuedToWorld means the bot
	// released the order to world.IssueOrder — it is NOT engine acceptance, queue
	// insertion or production completion. LocallyQueued/Deferred are routing notes
	// only; RefusedLocally and IssuedToWorld are terminal for that order instance.
	public enum BotOrderOutcome { LocallyQueued, Deferred, RefusedLocally, IssuedToWorld }

	/// <summary>
	/// Record-only sink for a tracked order's local routing disposition. Called
	/// synchronously on the bot's own thread wherever the event happens — inside
	/// QueueOrderTracked for LocallyQueued, inside the drain for the rest. Must
	/// never issue orders or mutate world state.
	/// </summary>
	[RequireExplicitImplementation]
	public interface IBotOrderDisposition { void OrderDisposition(Order order, BotOrderOutcome outcome); }

	/// <summary>
	/// Implemented by a bot that can report the local routing fate of a single
	/// order. QueueOrderTracked binds the observer to the exact Order instance
	/// BEFORE the FIFO can evict anything; grouped orders are refused (the gate
	/// may rebuild them into a different instance, breaking identity). Returns
	/// false where tracking is impossible — callers fall back to plain
	/// QueueOrder and keep the intent honestly pending instead of claiming an
	/// observable disposition.
	/// </summary>
	[RequireExplicitImplementation]
	public interface IBotOrderTracking
	{
		bool QueueOrderTracked(Order order, IBotOrderDisposition observer);
	}
}
