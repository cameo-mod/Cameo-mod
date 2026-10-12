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
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	public enum SquadCAType { Guerrilla, Air, Rush, Protection, Naval, Artillery, Support, Harass, FireSupport, Fighter, Gunship, Bomber, Stealth }

	public class SquadCA
	{
		public List<UnitWposWrapper> Units = new();

		// lists used for air squads to determine what members should be doing
		public HashSet<Actor> NewUnits = new HashSet<Actor>();
		public HashSet<Actor> WaitingUnits = new HashSet<Actor>();
		public HashSet<Actor> RearmingUnits = new HashSet<Actor>();

		public SquadCAType Type;

		internal IBot Bot;
		internal World World;
		internal SquadManagerBotModuleCA SquadManager;
		internal MersenneTwister Random;

		internal Target Target;
		internal StateMachineCA FuzzyStateMachine;

		// 6f: artillery squads attach to an assault squad and bombard what it can
		// see. Runtime-only — not serialized; the state reattaches after load.
		internal SquadCA Parent;
		// Default-off observation bookkeeping; registration/intent is not progress.
		internal bool M13ObservedProgress;

		// M13 save/restore: the wave identity allocated at RegisterNewSquad (0 = none),
		// the dispatch-intent bookkeeping (tick + wave snapshot, -1 tick = no intent),
		// the protection-squad membership flag, and a deserialization-corrupt marker.
		internal long M13WaveId;
		internal int M13IntentTick = -1;
		internal long M13IntentWave;
		internal bool M13Response;
		internal bool M13Corrupt;

		// 6g (CN A3): rules-derived BotTargetTags this squad prefers when picking
		// targets (e.g. air raiders prefer artillery). Empty = no preference.
		internal HashSet<string> PriorityTags = [];

		// Objective-shape deploy cooldown (12.7a, merged assault fan): the target cell and tick of this
		// squad's last objective deployment arm/commit/abort, so the squad is not looped back onto the
		// prongs forever. Per squad (no provider needed - the objective shape is gated by
		// FormationMovement); it dies with the squad.
		internal CPos DeployCooldownCell;
		internal int DeployCooldownTick = int.MinValue / 2;

		// AR-S residual (2026-10-04): the last (order, quantized target) a dedup call site queued
		// per member — survives state changes, so even a new state repeating the same order is
		// suppressed. Armed by UseSquadOrderDedup; empty while unarmed (bit-identical stream).
		internal readonly Dictionary<Actor, SquadOrderKey> OrderMemory = new();

		// AR-S: the armed-path flee-episode home latch. Retreat(flee: true) re-rolls
		// HomeLocation every squad tick — a fresh random building each pass cancels
		// the in-flight leg and wanders the squad forever. The latch holds the pick
		// until the whole squad is idle (the episode's end); a new leg then re-rolls.
		internal CPos? FleeHomeCell;

		SquadDesireController<Actor> desireController;

		// internal CPos BaseLocation;

		public SquadCA(IBot bot, SquadManagerBotModuleCA squadManager, SquadCAType type)
			: this(bot, squadManager, type, null) { }

		public SquadCA(IBot bot, SquadManagerBotModuleCA squadManager, SquadCAType type, Actor target)
		{
			Bot = bot;
			SquadManager = squadManager;
			World = bot.Player.PlayerActor.World;
			Random = BotRng.For(bot, nameof(SquadCA));
			Type = type;
			Target = Target.FromActor(target);
			FuzzyStateMachine = new StateMachineCA();

			switch (type)
			{
				case SquadCAType.Guerrilla:
					FuzzyStateMachine.ChangeState(this, new GroundUnitsIdleStateCA(), true);
					break;
				case SquadCAType.Harass:
					FuzzyStateMachine.ChangeState(this, new HarasserUnitsIdleStateCA(), true);
					break;
				case SquadCAType.Rush:
					FuzzyStateMachine.ChangeState(this, new GroundUnitsIdleStateCA(), true);
					break;
				case SquadCAType.Air:
					FuzzyStateMachine.ChangeState(this, new AirIdleStateCA(), true);
					break;
				case SquadCAType.Fighter:
					FuzzyStateMachine.ChangeState(this, new FighterIdleStateCA(), true);
					break;
				case SquadCAType.Gunship:
					FuzzyStateMachine.ChangeState(this, new GunshipCASStateCA(), true);
					break;
				case SquadCAType.Bomber:
					FuzzyStateMachine.ChangeState(this, new BomberIdleStateCA(), true);
					break;
				case SquadCAType.Protection:
					FuzzyStateMachine.ChangeState(this, new UnitsForProtectionIdleState(), true);
					break;
				case SquadCAType.Naval:
					FuzzyStateMachine.ChangeState(this, new NavyUnitsIdleState(), true);
					break;
				case SquadCAType.Artillery:
					FuzzyStateMachine.ChangeState(this, new ArtilleryUnitsIdleStateCA(), true);
					break;
				case SquadCAType.Support:
					FuzzyStateMachine.ChangeState(this, new SupportUnitsIdleStateCA(), true);
					break;
				case SquadCAType.FireSupport:
					FuzzyStateMachine.ChangeState(this, new FireSupportUnitsIdleStateCA(), true);
					break;
				case SquadCAType.Stealth:
					FuzzyStateMachine.ChangeState(this, new StealthUnitsIdleStateCA(), true);
					break;
			}
		}

		// The production factory remains world-backed; offline consumer tests replace only
		// observation and the order sink, while Update/provider lookup/controller stay real.
		internal virtual ISquadDesireExecution<Actor> CreateDesireExecution() => new LiveSquadDesireExecution(this);

		public void Update()
		{
			if (IsValid)
			{
				var desire = SquadManager.Info.UseSquadDesire && SquadDesireOrders.Supports(Type)
					? SquadManager.SquadDesire : null;
				// LC6: the target the state is about to act on must still be
				// observable — an Actor that went hidden while committed is
				// stale consumption and the canary logs it. FrozenActor memory
				// targets are legal by design and skip the check.
				if (desire == null && Target.Type == TargetType.Actor)
					SquadManager.CanaryObserved(Target.Actor, "squad-update-target");

				if (desire != null)
					(desireController ??= new SquadDesireController<Actor>()).Tick(CreateDesireExecution(), desire);
				else
					FuzzyStateMachine.Update(this);

				if (SquadManager.Info.UseSquadOrderDedup)
				{
					if (OrderMemory.Count > 0)
						foreach (var stale in OrderMemory.Keys.Where(a => !Units.Any(u => u.Actor == a)).ToList())
							OrderMemory.Remove(stale);

					if (FleeHomeCell != null && Units.All(u => u.Actor.IsIdle))
						FleeHomeCell = null;
				}
			}
		}

		public bool IsValid => Units.Count > 0;

		/// <summary>
		/// Deduped order issue (UseSquadOrderDedup): true when queuing <paramref name="key"/> to
		/// <paramref name="member"/> is a real change — different order/target, first order, or a
		/// completed mobile order on an idle member. Terminal orders (Stop/ReturnToBase/Scatter)
		/// pass <paramref name="terminal"/> = true and never repeat to the same effect.
		/// </summary>
		internal bool OrderChanged(Actor member, SquadOrderKey key, bool terminal = false)
		{
			return SquadOrderDedup.Changed(OrderMemory, SquadManager.Info.UseSquadOrderDedup, member, key, terminal);
		}

		public Actor TargetActor
		{
			get => Target.Actor;
			set => Target = Target.FromActor(value);
		}

		public bool IsTargetValid => IsValid && Target.IsValidFor(Units[0].Actor);

		public bool IsTargetVisible => Target.Actor == null || Target.Actor.CanBeViewedByPlayer(Bot.Player);

		public WPos CenterPosition { get { return Units[0].Actor.CenterPosition; } }

		public MiniYaml Serialize()
		{
			var nodes = new List<MiniYamlNode>()
			{
				new("Type", FieldSaver.FormatValue(Type)),
				new("Units", FieldSaver.FormatValue(Units.Where(a => !SquadManager.unitCannotBeOrdered(a.Actor)).Select(a => a.Actor.ActorID).ToArray())),
			};
			if (Target.Type == TargetType.Actor)
				nodes.Add(new MiniYamlNode("Target", FieldSaver.FormatValue(Target.Actor.ActorID)));

			if (M13WaveId > 0)
				nodes.Add(new MiniYamlNode("M13Wave", FieldSaver.FormatValue(M13WaveId)));
			if (M13ObservedProgress)
				nodes.Add(new MiniYamlNode("M13Observed", FieldSaver.FormatValue(true)));
			if (M13IntentTick >= 0)
			{
				nodes.Add(new MiniYamlNode("M13IntentTick", FieldSaver.FormatValue(M13IntentTick)));
				nodes.Add(new MiniYamlNode("M13IntentWave", FieldSaver.FormatValue(M13IntentWave)));
			}

			if (M13Response)
				nodes.Add(new MiniYamlNode("M13Response", FieldSaver.FormatValue(true)));

			return new MiniYaml("", nodes);
		}

		public static SquadCA Deserialize(IBot bot, SquadManagerBotModuleCA squadManager, MiniYaml yaml)
		{
			var type = SquadCAType.Rush;
			Actor targetActor = null;

			var typeNode = yaml.NodeWithKeyOrDefault("Type");
			if (typeNode != null)
				type = FieldLoader.GetValue<SquadCAType>("Type", typeNode.Value.Value);

			var targetNode = yaml.NodeWithKeyOrDefault("Target");
			if (targetNode != null)
				targetActor = squadManager.World.GetActorById(FieldLoader.GetValue<uint>("Target", targetNode.Value.Value));

			var squad = new SquadCA(bot, squadManager, type, targetActor);

			// RegisterNewSquad assigns this — Deserialize bypassed it, losing the
			// per-type target preferences (PreferTagged) for every loaded squad.
			squad.PriorityTags = squadManager.PriorityTagsFor(type);

			var unitsNode = yaml.NodeWithKeyOrDefault("Units");
			if (unitsNode != null)
			{
				// GetActorById can return null for actors removed between save
				// versions; a null wrapper survives Count>0 IsValid checks and
				// then NREs on the first .Actor dereference.
				foreach (var a in FieldLoader.GetValue<uint[]>("Units", unitsNode.Value.Value)
					.Select(a => squadManager.World.GetActorById(a))
					.Where(a => a != null))
				{
					squad.Units.Add(new UnitWposWrapper(a));
				}
			}

			// M13 nodes absent in legacy saves parse as defaults; malformed values mark
			// the squad corrupt so the manager adapter leaves restored evidence UNKNOWN.
			try
			{
				var waveNode = yaml.NodeWithKeyOrDefault("M13Wave");
				if (waveNode != null)
					squad.M13WaveId = FieldLoader.GetValue<long>("M13Wave", waveNode.Value.Value);
				var observedNode = yaml.NodeWithKeyOrDefault("M13Observed");
				if (observedNode != null)
					squad.M13ObservedProgress = FieldLoader.GetValue<bool>("M13Observed", observedNode.Value.Value);
				var intentTickNode = yaml.NodeWithKeyOrDefault("M13IntentTick");
				if (intentTickNode != null)
					squad.M13IntentTick = FieldLoader.GetValue<int>("M13IntentTick", intentTickNode.Value.Value);
				var intentWaveNode = yaml.NodeWithKeyOrDefault("M13IntentWave");
				if (intentWaveNode != null)
					squad.M13IntentWave = FieldLoader.GetValue<long>("M13IntentWave", intentWaveNode.Value.Value);
				var responseNode = yaml.NodeWithKeyOrDefault("M13Response");
				if (responseNode != null)
					squad.M13Response = FieldLoader.GetValue<bool>("M13Response", responseNode.Value.Value);
			}
			catch (InvalidCastException) { squad.M13Corrupt = true; }
			catch (FormatException) { squad.M13Corrupt = true; }
			catch (OverflowException) { squad.M13Corrupt = true; }
			catch (YamlException) { squad.M13Corrupt = true; }

			return squad;
		}
	}
}
