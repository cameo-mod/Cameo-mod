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
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Mods.AS.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;
using static OpenRA.GameInformation;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Manages AI squads.")]
	public class SquadManagerBotModuleCAInfo : ConditionalTraitInfo
	{
		[Desc("Actor types that are valid for naval squads.")]
		public readonly HashSet<string> NavalUnitsTypes = new HashSet<string>();

		[Desc("Actor types that are excluded from ground attacks.")]
		public readonly HashSet<string> AirUnitsTypes = new HashSet<string>();

		[Desc("Actor types that should generally be excluded from attack squads.")]
		public readonly HashSet<string> ExcludeFromSquadsTypes = new HashSet<string>();

		[Desc("Actor types that are considered construction yards (base builders).")]
		public readonly HashSet<string> ConstructionYardTypes = new HashSet<string>();

		[Desc("Enemy building types around which to scan for targets for naval squads.")]
		public readonly HashSet<string> NavalProductionTypes = new HashSet<string>();

		/*
		[Desc("Own actor types that are prioritized when defending.")]
		public readonly HashSet<string> ProtectionTypes = new HashSet<string>();
		*/

		[Desc("Minimum number of units AI must have before attacking.")]
		public readonly int SquadSize = 8;

		[Desc("Random number of up to this many units is added to squad size when creating an attack squad.")]
		public readonly int SquadSizeRandomBonus = 30;

		[Desc("Maximum number of units AI can have idle.")]
		public readonly int MaxIdleUnits = 36;

		[ActorReference]
		[Desc("Units that form a guerrilla squad.")]
		public readonly HashSet<string> GuerrillaTypes = new();

		[Desc("Percent chance (0-100) that a pass of new GuerrillaTypes units joins the guerrilla squad. " +
			"Cameo: the engine this came from compared the other way round (join chance = 100 - value); flipped 2026-09-27.")]
		public readonly int JoinGuerrilla = 50;

		[Desc("Max number of units AI has in guerrilla squad")]
		public readonly int MaxGuerrillaSize = 10;

		[Desc("Units that form harasser squads — high-value-target raids that launch once a",
			"quorum gathers (upstream CA harasser port; empty = off). Shares the guerrilla",
			"hit/run-adjacent routing exemption but fights with ordinary attack states.")]
		public readonly HashSet<string> HarasserTypes = new HashSet<string>();

		[Desc("Harasser squads wait for at least this many units before launching.")]
		public readonly int HarassMinLaunchSize = 3;

		[Desc("Distinct route candidates a harasser squad requests — the flanking breadth.",
			"It then picks randomly from the last (least direct) routes.")]
		public readonly int HarassRouteCount = 12;

		[Desc("Delay (in ticks) between giving out orders to units.")]
		public readonly int AssignRolesInterval = 50;

		[Desc("Delay (in ticks) between issuing a protection order.")]
		public readonly int ProtectInterval = 50;

		[Desc("Delay (in ticks) between updating squads.")]
		public readonly int AttackForceInterval = 75;

		[Desc("Minimum delay (in ticks) between creating squads.")]
		public readonly int MinimumAttackForceDelay = 0;

		[Desc("Radius in cells around the base that should be scanned for units to be protected.")]
		public readonly int ProtectUnitScanRadius = 15;

		[Desc("Maximum distance in cells from center of the base when checking for MCV deployment location.",
			"Only applies if RestrictMCVDeploymentFallbackToBase is enabled and there's at least one construction yard.")]
		public readonly int MaxBaseRadius = 20;

		[Desc("Radius in cells that squads should scan for enemies around their position while idle.")]
		public readonly int IdleScanRadius = 10;

		[Desc("Radius in cells that squads should scan for danger around their position to make flee decisions.")]
		public readonly int DangerScanRadius = 10;

		[Desc("Radius in cells that attack squads should scan for enemies around their position when trying to attack.")]
		public readonly int AttackScanRadius = 12;

		[Desc("Radius in cells that protecting squads should scan for enemies around their position.")]
		public readonly int ProtectionScanRadius = 8;

		[Desc("Radius in cells that naval squads should scan for targets.")]
		public readonly int NavalScanRadius = 8;

		[Desc("Enemy target types to never target.")]
		public readonly BitSet<TargetableType> IgnoredEnemyTargetTypes = default(BitSet<TargetableType>);

		// CA additions
		[Desc("Minimum value of units AI must have before attacking.")]
		public readonly int SquadValue = 0;

		[Desc("Random number of up to this value units is added to squad valuee when creating an attack squad.")]
		public readonly int SquadValueRandomBonus = 0;

		[Desc("Maximum random bonus added to squad value at the start of the match.")]
		public readonly int SquadValueMaxEarlyBonus = 0;

		[Desc("Minimum random bonus added to squad value at the end of the ramp.")]
		public readonly int SquadValueMinLateBonus = 0;

		[Desc("Maximum random bonus added to squad value at the end of the ramp.")]
		public readonly int SquadValueMaxLateBonus = 0;

		[Desc("Percent change for ground squads to attack a random priority target rather than the closest enemy.")]
		public readonly int HighValueTargetPriority = 0;

		[Desc("CA-3 (AI_ARCHITECTURE.md 12.5): target army composition by role, percent of own mobile combat units.",
			"Production fills the largest deficit against this mix; absent or empty keeps the proportional pick.",
			"Classic carries no mix on purpose — verbatim upstream behaviour.")]
		public readonly Dictionary<string, int> RoleMix = null;

		[Desc("Minimum target share for every role a buildable member exists for, when RoleMix is set.",
			"Explicit mix entries win over the floor; roles the mix omits still get produced at this share.")]
		public readonly int RoleMixRoleFloorPct = 5;

		[Desc("6f: Rush squads gather at the own building nearest the target before committing, so the wave arrives together.")]
		public readonly bool StageBeforeAssault = false;

		[Desc("Percent of squad units that must reach the staging point before the assault proceeds.")]
		public readonly int StageAssemblePercent = 60;

		[Desc("Cells around the staging point within which a unit counts as assembled.")]
		public readonly int StageRadiusCells = 8;

		[Desc("Ticks a staging squad waits before committing regardless of assembly.")]
		public readonly int StageTimeoutTicks = 750;

		[Desc("Extra units to treat as heal/repair support squads, beyond the derived set. " +
			"Derived at rules load: every armament must carry a negative-damage, ally-valid " +
			"warhead for the carrier to count as support — no central ids.")]
		public readonly HashSet<string> SupportUnitTypes = [];

		[Desc("Cells a support squad may trail behind its assault squad before catching up.")]
		public readonly int SupportFollowRangeCells = 6;

		[Desc("Prefer actors owned by the bot's main target player when picking a proactive attack target. Falls back to the nearest enemy when that player has no valid candidates.")]
		public readonly bool PreferMainTarget = false;
		[Desc("Allow published master-AI missions to defer or focus newly formed attack forces.")]
		public readonly bool UseMissions = true;
		[Desc("Maximum number of ticks a Defend mission may hold an otherwise ready attack force.")]
		public readonly int MissionDefendHoldTicks = 1500;

		[Desc("6g (CN A3): rules-derived BotTargetTags each squad type prefers when choosing targets (artillery, harvester, production, superweapon).")]
		public readonly HashSet<string> AssaultPriorityTags = [];
		public readonly HashSet<string> RushPriorityTags = [];
		public readonly HashSet<string> ArtilleryPriorityTags = [];
		public readonly HashSet<string> AirPriorityTags = [];
		public readonly HashSet<string> NavalPriorityTags = [];
		public readonly HashSet<string> GuerrillaPriorityTags = [];
		public readonly HashSet<string> HarassPriorityTags = [];
		public readonly HashSet<string> ProtectionPriorityTags = [];

		[Desc("Actor tags the support squads prefer to target.")]
		public readonly HashSet<string> SupportPriorityTags = [];

		[Desc("Pre-commit risk gate (AI_FRANSBOT_RESEARCH.md 6c): a proactive ground squad only commits to a target when its unit value beats the remembered enemy threat at that region by this percent margin. Negative disables the gate.")]
		public readonly int AttackRiskMargin = 25;

		[Desc("Actor types to prioritise based on HighValueTargetPriority.")]
		public readonly HashSet<string> HighValueTargetTypes = new HashSet<string>();

		[Desc("Percent change for air squads (that can attack aircraft) to prioritise enemy aircraft.")]
		public readonly int AirToAirPriority = 85;

		[Desc("Limit target types for specific air unit squads.")]
		public readonly Dictionary<string, BitSet<TargetableType>> AirSquadTargetTypes = null;

		[Desc("Enemy building types around which to scan for targets for naval squads.")]
		public readonly HashSet<string> StaticAntiAirTypes = new HashSet<string>();

		[Desc("Air threats to prioritise above all others.")]
		public readonly HashSet<string> BigAirThreats = new HashSet<string>();

		[Desc("Locomotor used by pathfinding leader for squads")]
		public readonly HashSet<string> SuggestedGroundLeaderLocomotor = new();

		[Desc("Locomotor used by pathfinding leader for squads")]
		public readonly HashSet<string> SuggestedNavyLeaderLocomotor = new();

		[Desc("Percent chance that a regular assault squad will take an indirect (flanking) route instead of the most direct path. 0 to disable.")]
		public readonly int IndirectRouteChance = 0;

		[Desc("Ask region-memory routers (IBotRouteThreatRouter) for waypoints that skirt remembered enemy threat (AI_FRANSBOT_RESEARCH.md 6e). Squads fall back to normal routing when no router answers.")]
		public readonly bool UseRiskRouting = true;

		[Desc("Ground units whose maximum weapon range reaches this many cells split into artillery squads that hang back behind assault squads (AI_FRANSBOT_RESEARCH.md 6f). Negative disables artillery squads.")]
		public readonly int ArtilleryMinRangeCells = 10;

		[Desc("Cells an artillery squad trails its parent assault squad, measured away from the parent's target.")]
		public readonly int ArtilleryHangBackCells = 8;

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (DangerScanRadius <= 0)
				throw new YamlException("DangerScanRadius must be greater than zero.");

			if (SquadValueMaxEarlyBonus > SquadValueMaxLateBonus)
				throw new YamlException("SquadValueMaxEarlyBonus cannot be greater than SquadValueMaxLateBonus.");

			if (SquadValueMinLateBonus > SquadValueMaxLateBonus)
				throw new YamlException("SquadValueMinLateBonus cannot be greater than SquadValueMaxLateBonus.");

			if (SquadValueRandomBonus != 0 &&
				(SquadValueMaxEarlyBonus != 0 || SquadValueMinLateBonus != 0 || SquadValueMaxLateBonus != 0))
				throw new YamlException("SquadValueRandomBonus cannot be combined with squad value ramp bonuses.");

			// Derive support units from weapon metadata: an actor is support only when
			// EVERY armament it carries heals (negative-damage, ally-valid warhead).
			// Requiring all armaments excludes hybrids that also fight — the RA2 IFVs,
			// Tesla Trooper, WC2 knights/paladins and the SCV each carry a heal weapon
			// alongside damage weapons and must not be pulled out of combat squads.
			foreach (var actor in rules.Actors.Values)
			{
				// Support must be mobile to follow a squad — a heal-armament building
				// (repair aura/depot) is not a squad member.
				if (actor.Name.StartsWith('^') ||
					(!actor.HasTraitInfo<MobileInfo>() && !actor.HasTraitInfo<AircraftInfo>()))
					continue;

				var armaments = actor.TraitInfos<ArmamentInfo>()
					.Where(a => !string.IsNullOrEmpty(a.Weapon))
					.ToList();

				if (armaments.Count == 0)
					continue;

				// An unresolvable weapon cannot be proven to heal, so it disqualifies.
				if (armaments.All(a =>
					rules.Weapons.TryGetValue(a.Weapon.ToLowerInvariant(), out var weapon) &&
					weapon.Warheads.Any(w => w is DamageWarhead dw && dw.Damage < 0 &&
						dw.ValidRelationships.HasRelationship(PlayerRelationship.Ally))))
				{
					SupportUnitTypes.Add(actor.Name);
				}
			}
		}

		public override object Create(ActorInitializer init) { return new SquadManagerBotModuleCA(init.Self, this); }
	}

	public class SquadManagerBotModuleCA : ConditionalTrait<SquadManagerBotModuleCAInfo>, IBotEnabled, IBotTick, IBotRespondToAttack, IBotPositionsUpdated, IGameSaveTraitData, INotifyActorDisposing, IBotMissionAssignmentProvider
	{
		const float SquadValueRampDurationTicks = 20f * 60f * 25f; // Assumes the default 25 ticks per second.

		public CPos GetRandomBaseCenter()
		{
			var randomConstructionYard = constructionYardBuildings.Actors.RandomOrDefault(World.LocalRandom);

			return randomConstructionYard?.Location ?? initialBaseCenter;
		}

		public readonly World World;
		public readonly Player Player;

		public readonly Predicate<Actor> unitCannotBeOrdered;
		readonly List<UnitWposWrapper> unitsHangingAroundTheBase = new();

		// Units that the bot already knows about. Any unit not on this list needs to be given a role.
		readonly List<Actor> activeUnits = new();

		public List<SquadCA> Squads = new();
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> constructionYardBuildings;

		IBot bot;
		IBotPositionsUpdated[] notifyPositionsUpdated;
		IBotNotifyIdleBaseUnits[] notifyIdleBaseUnits;
		IBotAircraftBuilder[] aircraftBuilders;
		IBotMainTargetProvider[] mainTargetProviders;
		IBotRegionThreatProvider[] threatProviders;
		IBotFoggedEnemyProvider[] fogProviders;
		IBotRouteThreatRouter[] routeRouters;
		IBotMissionProvider[] missionProviders;

		CPos initialBaseCenter;
		Actor airStrikeTarget;
		public CPos[] airStrikeGrid;

		int rushTicks;
		int assignRolesTicks;
		int attackForceTicks;
		int protectionForceTicks;
		int minAttackForceDelayTicks;
		BotMission heldDefendMission;
		int defendMissionHeldSince = -1;
		readonly HashSet<int> defendMissionExhaustedRegions = [];
		public BotMissionAssignment LastMissionAssignment { get; private set; }

		int protectOwnTicks;
		Actor protectOwnFrom;

		int desiredAttackForceValue;
		int desiredAttackForceSize;
		readonly Dictionary<string, int> cachedUnitValues = new();

		// Loss telemetry (situation log): the role, cost and position each unit held at the last
		// role pass. Squads drop dead units at several sites, so a death is detected here, on the
		// next pass, from this snapshot rather than at any one removal site.
		readonly Dictionary<Actor, (string Role, int Cost, CPos Location)> lastKnownRoles = new();
		readonly Dictionary<string, int> lossesByRole = new();
		readonly Dictionary<string, int> awayLossesByRole = new();

		/// <summary>Cumulative cost of units lost, by the role they held: a squad type or "idle".</summary>
		public IReadOnlyDictionary<string, int> LossesByRole => lossesByRole;

		/// <summary>The part of <see cref="LossesByRole"/> lost outside MaxBaseRadius of the base centre.</summary>
		public IReadOnlyDictionary<string, int> AwayLossesByRole => awayLossesByRole;

		BotLimits botLimits;
		int initialAttackDelay;

		// H1 attention consumer: null when no IBotActionBudget producer is on the
		// player (then squads act unconditionally, as before). squadCursor rotates
		// the per-round pass order so a spent budget staggers rather than starves.
		IBotActionBudget actionBudget;
		int squadCursor;

		public SquadManagerBotModuleCA(Actor self, SquadManagerBotModuleCAInfo info)
			: base(info)
		{
			World = self.World;
			Player = self.Owner;

			unitCannotBeOrdered = a => a == null || a.Owner != Player || a.IsDead || !a.IsInWorld || a.CurrentActivity is Enter;
			constructionYardBuildings = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(World, info.ConstructionYardTypes, Player);
		}

		IReadOnlyDictionary<string, HashSet<string>> targetTagMap;

		// Lazily built per ruleset: actor name -> rules-derived BotTargetTags.
		internal IReadOnlyDictionary<string, HashSet<string>> TargetTags =>
			targetTagMap ??= BotTargetTags.BuildTagMap(World.Map.Rules);

		internal HashSet<string> PriorityTagsFor(SquadCAType type)
		{
			return type switch
			{
				SquadCAType.Air => Info.AirPriorityTags,
				SquadCAType.Artillery => Info.ArtilleryPriorityTags,
				SquadCAType.Naval => Info.NavalPriorityTags,
				SquadCAType.Rush => Info.RushPriorityTags,
				SquadCAType.Guerrilla => Info.GuerrillaPriorityTags,
				SquadCAType.Harass => Info.HarassPriorityTags,
				SquadCAType.Protection => Info.ProtectionPriorityTags,
				SquadCAType.Support => Info.SupportPriorityTags,
				_ => Info.AssaultPriorityTags,
			};
		}

		internal HashSet<string> TagsOf(Actor a)
		{
			return a != null && TargetTags.TryGetValue(a.Info.Name, out var tags) ? tags : null;
		}

		internal HashSet<string> TagsOf(ActorInfo info)
		{
			return info != null && TargetTags.TryGetValue(info.Name, out var tags) ? tags : null;
		}

		internal List<T> PreferSquadTargets<T>(List<T> candidates, SquadCA owner, Func<T, HashSet<string>> tagsOf)
		{
			return owner == null ? candidates : BotTargetTags.PreferTagged(candidates, owner.PriorityTags, tagsOf);
		}

		bool IsValidEnemyUnit(Actor a)
		{
			if (a == null || a.IsDead || Player.RelationshipWith(a.Owner) != PlayerRelationship.Enemy || a.Info.HasTraitInfo<HuskInfo>() || a.Info.HasTraitInfo<CarrierSlaveInfo>())
				return false;

			var targetTypes = a.GetEnabledTargetTypes();
			return !targetTypes.IsEmpty && !targetTypes.Overlaps(Info.IgnoredEnemyTargetTypes);
		}

		// Use for proactive targeting.
		public bool IsPreferredEnemyUnit(Actor a)
		{
			return IsValidEnemyUnit(a) && !a.Info.HasTraitInfo<AircraftInfo>();
		}

		public bool IsPreferredEnemyBuilding(Actor a)
		{
			return IsValidEnemyUnit(a) && a.Info.HasTraitInfo<BuildingInfo>();
		}

		public bool IsPreferredEnemyAircraft(Actor a)
		{
			return IsValidEnemyUnit(a) && a.Info.HasTraitInfo<AircraftInfo>() && a.Info.HasTraitInfo<AttackBaseInfo>();
		}

		public bool IsHighValueTarget(Actor a)
		{
			return IsValidEnemyUnit(a) && Info.HighValueTargetTypes.Contains(a.Info.Name);
		}

		public bool IsAirSquadTargetType(Actor a, SquadCA owner)
		{
			if (a == null || a.IsDead)
				return false;

			var airSquadUnitType = owner.Units[0].Actor.Info.Name;
			if (owner.SquadManager.Info.AirSquadTargetTypes.ContainsKey(airSquadUnitType))
			{
				var targetTypes = a.GetEnabledTargetTypes();

				if (targetTypes.IsEmpty || !targetTypes.Overlaps(owner.SquadManager.Info.AirSquadTargetTypes[airSquadUnitType]))
					return false;
			}

			return true;
		}

		public bool IsNotHiddenUnit(Actor a)
		{
			return a.CanBeViewedByPlayer(Player);
		}

		// 6d fogged observation: when a provider reports fogged scans, squads only
		// pick targets they can see (or remember via FrozenActorLayer). Without a
		// provider the legacy omniscient scans run unchanged — same degradation
		// rule as the risk gate.
		internal bool FoggedScans => FoggedScansActive(IsTraitDisabled, fogProviders);

		// 6e risk routing: ask region-memory routers for waypoints that skirt
		// remembered threat. Returns null (caller keeps direct routing) when
		// disabled, no router answers, or the router has no useful detour.
		internal List<CPos> RouteAroundThreat(Actor leader, CPos target, int maxWaypoints = 4)
		{
			if (!Info.UseRiskRouting || IsTraitDisabled || routeRouters == null)
				return null;

			foreach (var router in routeRouters)
			{
				var route = router.RouteAroundThreat(leader, target, maxWaypoints);
				if (route != null && route.Count > 0)
					return route;
			}

			return null;
		}

		public static bool FoggedScansActive(bool traitDisabled, IBotFoggedEnemyProvider[] providers)
		{
			return !traitDisabled && providers != null && providers.Any(p => p.FoggedObservation);
		}

		// 6f: rules-derived artillery classification — a mobile ground unit whose
		// weapons reach ArtilleryMinRangeCells. No actor ids, so every faction's
		// artillery qualifies automatically (CN's tag-derivation rule).
		internal bool IsArtilleryUnit(Actor a)
		{
			if (Info.ArtilleryMinRangeCells < 0 || a == null
				|| a.Info.HasTraitInfo<AircraftInfo>() || a.Info.HasTraitInfo<BuildingInfo>())
				return false;

			return MaximumEnabledRange(a) >= WDist.FromCells(Info.ArtilleryMinRangeCells);
		}

		// Longest range over the actor's enabled attack traits. Never TraitOrDefault<AttackBase>:
		// 76 mobile ground actors carry two or more (e.g. AttackFrontal + AttackFollow on
		// ts_nod_attackbuggy), and TraitOrDefault throws on the second one.
		internal static WDist MaximumEnabledRange(Actor a)
		{
			var range = WDist.Zero;
			foreach (var attack in a.TraitsImplementing<AttackBase>())
			{
				if (attack.IsTraitDisabled)
					continue;

				var r = attack.GetMaximumRange();
				if (r > range)
					range = r;
			}

			return range;
		}

		// The assault squad an artillery squad trails: nearest living Rush squad.
		internal SquadCA FindAttachableAssault(SquadCA artillery)
		{
			if (!artillery.IsValid)
				return null;

			var from = artillery.Units[0].Actor.CenterPosition;
			SquadCA best = null;
			var bestDistance = long.MaxValue;
			foreach (var squad in Squads)
			{
				if (squad == artillery || !squad.IsValid || squad.Type != SquadCAType.Rush)
					continue;

				var distance = (squad.CenterPosition - from).HorizontalLengthSquared;
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = squad;
				}
			}

			return best;
		}

		// Point hangBackLength behind the parent's position, away from the target.
		public static WPos HangBackAnchor(WPos parentPos, WPos targetPos, int hangBackLength)
		{
			var offset = parentPos - targetPos;
			var distance = offset.HorizontalLength;
			if (distance <= 0)
				return parentPos;

			return parentPos + new WVec(
				(int)((long)offset.X * hangBackLength / distance),
				(int)((long)offset.Y * hangBackLength / distance),
				0);
		}

		// IsPreferredEnemyUnit restricted to what the bot can currently observe.
		internal bool IsPreferredObservedEnemyUnit(Actor a)
		{
			return IsPreferredEnemyUnit(a) && (!FoggedScans || IsNotHiddenUnit(a));
		}

		// Fogged fallback target: an enemy building the engine's FrozenActorLayer
		// remembers under shroud. The layer invalidates the record when the cell
		// is re-observed empty, so a stale frozen target drops out on its own.
		internal FrozenActor FindFrozenEnemyTarget(WPos from, int attackerValue, SquadCA owner = null, Player targetPlayer = null)
		{
			var layer = Player.FrozenActorLayer;
			if (layer == null)
				return null;

			var map = World.Map;
			// Mirrors Target.IsValidFor's FrozenActor predicate: only rendered
			// ghosts count — hidden (revealed-empty) and invalid records drop out.
			var candidates = layer.FrozenActorsInRegion(map.AllCells)
				.Where(fa => fa.IsValid && fa.Visible && !fa.Hidden && fa.Owner != null
					&& Player.RelationshipWith(fa.Owner) == PlayerRelationship.Enemy
					&& (targetPlayer == null || fa.Owner == targetPlayer)
					&& !fa.TargetTypes.IsEmpty && !fa.TargetTypes.Overlaps(Info.IgnoredEnemyTargetTypes))
				.ToList();

			var mainTarget = EffectiveMainTarget();
			candidates = PreferOwned(candidates, mainTarget == null ? null : fa => fa.Owner == mainTarget);
			candidates = PreferSquadTargets(candidates, owner, fa => TagsOf(fa.Info));

			if (attackerValue >= 0)
				candidates.RemoveAll(fa => !PassesRiskGate(map.CellContaining(fa.CenterPosition), attackerValue));

			FrozenActor closest = null;
			var closestDistance = long.MaxValue;
			foreach (var fa in candidates)
			{
				var delta = fa.CenterPosition - from;
				var distance = (long)delta.LengthSquared;
				if (distance < closestDistance)
				{
					closestDistance = distance;
					closest = fa;
				}
			}

			return closest;
		}

		public bool IsValidAllyUnit(Actor a)
		{
			if (a == null || a.IsDead || Player.RelationshipWith(a.Owner) != PlayerRelationship.Ally || a.Info.HasTraitInfo<HuskInfo>() || a.Info.HasTraitInfo<CarrierSlaveInfo>())
				return false;

			return true;
		}

		public CPos[] AirstrikeGrid(Actor self)
		{
			var map = self.World.Map;
			var dangerRadius = Info.DangerScanRadius;

			var columnCount = (map.MapSize.Width + dangerRadius - 1) / dangerRadius;
			var rowCount = (map.MapSize.Height + dangerRadius - 1) / dangerRadius;

			var checkIndices = Exts.MakeArray(columnCount * rowCount, i => new MPos((i % columnCount) * dangerRadius + dangerRadius / 2, (i / columnCount) * dangerRadius + dangerRadius / 2).ToCPos(map));

			return checkIndices;
		}

		protected override void Created(Actor self)
		{
			notifyPositionsUpdated = self.Owner.PlayerActor.TraitsImplementing<IBotPositionsUpdated>().ToArray();
			notifyIdleBaseUnits = self.Owner.PlayerActor.TraitsImplementing<IBotNotifyIdleBaseUnits>().ToArray();
			aircraftBuilders = self.Owner.PlayerActor.TraitsImplementing<IBotAircraftBuilder>().ToArray();
			mainTargetProviders = self.Owner.PlayerActor.TraitsImplementing<IBotMainTargetProvider>().ToArray();
			threatProviders = self.Owner.PlayerActor.TraitsImplementing<IBotRegionThreatProvider>().ToArray();
			fogProviders = self.Owner.PlayerActor.TraitsImplementing<IBotFoggedEnemyProvider>().ToArray();
			routeRouters = self.Owner.PlayerActor.TraitsImplementing<IBotRouteThreatRouter>().ToArray();
			missionProviders = self.Owner.PlayerActor.TraitsImplementing<IBotMissionProvider>().ToArray();
			airStrikeGrid = AirstrikeGrid(self);
		}

		protected override void TraitEnabled(Actor self)
		{
			botLimits = self.Owner.PlayerActor.TraitsImplementing<BotLimits>().FirstEnabledTraitOrDefault();
			actionBudget = self.Owner.PlayerActor.TraitsImplementing<IBotActionBudget>().FirstEnabledTraitOrDefault();

			if (botLimits != null)
				initialAttackDelay = botLimits.Info.InitialAttackDelay;

			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			assignRolesTicks = World.LocalRandom.Next(0, Info.AssignRolesInterval);
			attackForceTicks = World.LocalRandom.Next(0, Info.AttackForceInterval);
			protectionForceTicks = World.LocalRandom.Next(0, Info.ProtectInterval);
			minAttackForceDelayTicks = World.LocalRandom.Next(0, Info.MinimumAttackForceDelay) +
				RemainingInitialAttackDelay(initialAttackDelay, World.WorldTick);

			// Without this the desired force stays 0/0 and the very first
			// `idleUnits >= desired` check passes unconditionally — an empty Rush
			// squad on the first tick the module runs (and after every load).
			SetNextDesiredAttackForce();
		}

		protected override void TraitDisabled(Actor self)
		{
			heldDefendMission = null;
			defendMissionHeldSince = -1;
			defendMissionExhaustedRegions.Clear();
			foreach (var squad in Squads)
				DismissSquad(squad);

			Squads.Clear();
			activeUnits.Clear();
			unitsHangingAroundTheBase.Clear();

			// The next personality's manager takes these units over; counting their deaths here too
			// when this one is re-enabled would book them twice.
			lastKnownRoles.Clear();
			foreach (var n in notifyIdleBaseUnits)
				n.UpdatedIdleBaseUnits(unitsHangingAroundTheBase);
		}

		public static int RemainingInitialAttackDelay(int initialAttackDelay, int worldTick)
		{
			return Math.Max(0, initialAttackDelay - worldTick);
		}

		void IBotEnabled.BotEnabled(IBot bot)
		{
			this.bot = bot;
		}

		void IBotTick.BotTick(IBot bot)
		{
			AssignRolesToIdleUnits(bot);
		}

		internal Actor FindClosestEnemy(Actor sourceActor, SquadCA owner = null)
		{
			var units = World.Actors.Where(IsPreferredEnemyUnit).ToList();
			var mainTarget = EffectiveMainTarget();
			units = PreferOwned(units, mainTarget == null ? null : a => a.Owner == mainTarget);
			units = PreferSquadTargets(units, owner, TagsOf);
			var visible = units.Where(IsNotHiddenUnit).ToList();

			// Fogged scans never fall back to actors the bot cannot see; remembered
			// enemy buildings are offered separately as FrozenActor targets.
			if (FoggedScans)
				return visible.ClosestToIgnoringPath(sourceActor.CenterPosition);

			return visible.ClosestToIgnoringPath(sourceActor.CenterPosition) ?? units.Where(IsPreferredEnemyBuilding).ClosestToIgnoringPath(sourceActor.CenterPosition) ?? units.ClosestToIgnoringPath(sourceActor.CenterPosition);
		}

		// 6c pre-commit risk gate: as FindClosestEnemy, but candidates whose region's
		// remembered enemy threat exceeds attackerValue by more than AttackRiskMargin
		// are skipped — an unknown region (threat 0) never blocks. When every
		// candidate fails the gate the squad holds instead of suiciding.
		internal Actor FindClosestEnemy(Actor sourceActor, int attackerValue, SquadCA owner = null)
		{
			var units = World.Actors.Where(IsPreferredEnemyUnit).ToList();
			var mainTarget = EffectiveMainTarget();
			units = PreferOwned(units, mainTarget == null ? null : a => a.Owner == mainTarget);
			units = PreferSquadTargets(units, owner, TagsOf);
			units.RemoveAll(u => !PassesRiskGate(u.Location, attackerValue));
			var visible = units.Where(IsNotHiddenUnit).ToList();
			if (FoggedScans)
				return visible.ClosestToIgnoringPath(sourceActor.CenterPosition);

			return visible.ClosestToIgnoringPath(sourceActor.CenterPosition) ?? units.Where(IsPreferredEnemyBuilding).ClosestToIgnoringPath(sourceActor.CenterPosition) ?? units.ClosestToIgnoringPath(sourceActor.CenterPosition);
		}

		// Fogged scans require a currently visible actor; mission consumers add remembered
		// FrozenActor targets separately. With FoggedScans disabled, the fallback may
		// select an unseen actor, inheriting the existing omniscient behavior of that
		// mode rather than introducing a mission-layer cheat.
		internal Actor FindClosestEnemy(CPos location, int attackerValue, Player targetPlayer, SquadCA owner = null)
		{
			if (targetPlayer == null)
				return null;

			var units = World.Actors
				.Where(a => a.Owner == targetPlayer && IsPreferredEnemyUnit(a))
				.ToList();
			units = PreferSquadTargets(units, owner, TagsOf);
			units.RemoveAll(u => !PassesRiskGate(u.Location, attackerValue));
			var visible = units.Where(IsNotHiddenUnit).ToList();
			var targetPosition = World.Map.CenterOfCell(location);
			if (FoggedScans)
				return visible.ClosestToIgnoringPath(targetPosition);

			return visible.ClosestToIgnoringPath(targetPosition) ??
				units.Where(IsPreferredEnemyBuilding).ClosestToIgnoringPath(targetPosition) ??
				units.ClosestToIgnoringPath(targetPosition);
		}

		internal Actor FindHighValueTarget(WPos pos)
		{
			var units = World.Actors.Where(IsHighValueTarget).ToList();
			if (FoggedScans)
				units = units.Where(IsNotHiddenUnit).ToList();

			var mainTarget = EffectiveMainTarget();
			units = PreferOwned(units, mainTarget == null ? null : a => a.Owner == mainTarget);
			return units.RandomOrDefault(World.LocalRandom);
		}

		internal Actor FindHighValueTarget(WPos pos, int attackerValue)
		{
			var units = World.Actors.Where(IsHighValueTarget).ToList();
			if (FoggedScans)
				units = units.Where(IsNotHiddenUnit).ToList();

			var mainTarget = EffectiveMainTarget();
			units = PreferOwned(units, mainTarget == null ? null : a => a.Owner == mainTarget);
			units.RemoveAll(u => !PassesRiskGate(u.Location, attackerValue));
			return units.RandomOrDefault(World.LocalRandom);
		}

		internal int SquadValueOf(SquadCA squad)
		{
			var value = 0;
			foreach (var u in squad.Units)
			{
				if (!cachedUnitValues.TryGetValue(u.Actor.Info.Name, out var unitCost))
				{
					unitCost = u.Actor.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;
					cachedUnitValues[u.Actor.Info.Name] = unitCost;
				}

				value += unitCost;
			}

			return value;
		}

		internal bool PassesRiskGate(Actor target, int attackerValue)
		{
			return PassesRiskGate(target.Location, attackerValue);
		}

		internal bool PassesRiskGate(CPos cell, int attackerValue)
		{
			var threat = threatProviders?.Sum(p => p.RememberedEnemyThreatAt(cell)) ?? 0;
			var pass = PassesRiskGate(attackerValue, threat, Info.AttackRiskMargin);
			if (!pass)
				AIUtils.BotDebug("AI ({0}): risk gate held a {1}-value squad off {2} (remembered threat {3}, margin {4}%)",
					Player.ClientIndex, attackerValue, cell, threat, Info.AttackRiskMargin);

			return pass;
		}

		public static bool PassesRiskGate(int attackerValue, int threat, int marginPercent)
		{
			if (marginPercent < 0 || threat <= 0)
				return true;

			return (long)attackerValue * 100 >= (long)threat * (100 + marginPercent);
		}

		internal Actor FindClosestEnemy(Actor sourceActor, WDist radius, SquadCA owner = null)
		{
			var candidates = World.FindActorsInCircle(sourceActor.CenterPosition, radius)
				.Where(a => IsPreferredEnemyUnit(a) && IsNotHiddenUnit(a)).ToList();
			candidates = PreferSquadTargets(candidates, owner, TagsOf);
			return candidates.ClosestToIgnoringPath(sourceActor);
		}

		Player EffectiveMainTarget()
		{
			if (!Info.PreferMainTarget || mainTargetProviders == null)
				return null;

			return mainTargetProviders
				.Select(p => p.MainTarget)
				.FirstOrDefault(target => target != null && target.WinState == WinState.Undefined);
		}

		public static List<T> PreferOwned<T>(List<T> candidates, Func<T, bool> ownedByMainTarget)
		{
			if (ownedByMainTarget == null)
				return candidates;

			var preferred = candidates.Where(ownedByMainTarget).ToList();
			return preferred.Count > 0 ? preferred : candidates;
		}

		public static BotMission BestAffordableMission(IEnumerable<IBotMissionProvider> providers, int idleForceValue,
			Func<BotMission, bool> exclude = null)
		{
			if (providers == null)
				return null;

			foreach (var provider in providers)
				foreach (var mission in provider?.Missions ?? Array.Empty<BotMission>())
					if (mission != null && mission.RequiredValue <= idleForceValue && (exclude == null || !exclude(mission)))
						return mission;

			return null;
		}

		void MissionTaken(BotMission mission)
		{
			foreach (var provider in missionProviders ?? Array.Empty<IBotMissionProvider>())
				if ((provider.Missions ?? Array.Empty<BotMission>()).Any(candidate => ReferenceEquals(candidate, mission)))
				{
					provider.MissionTaken(mission);
					return;
				}
		}

		void CleanSquads()
		{
			Squads.RemoveAll(s => !s.IsValid);
			foreach (var s in Squads)
			{
				s.Units.RemoveAll(u => unitCannotBeOrdered(u.Actor));

				if (s.Type == SquadCAType.Air)
				{
					s.NewUnits.RemoveWhere(unitCannotBeOrdered);
					s.RearmingUnits.RemoveWhere(unitCannotBeOrdered);
					s.WaitingUnits.RemoveWhere(unitCannotBeOrdered);
				}
			}
		}

		// HACK: Use of this function requires that there is one squad of this type.
		SquadCA GetSquadOfType(SquadCAType type)
		{
			return Squads.FirstOrDefault(s => s.Type == type);
		}

		IEnumerable<SquadCA> GetSquadsOfType(SquadCAType type)
		{
			return Squads.Where(s => s.Type == type);
		}

		SquadCA RegisterNewSquad(IBot bot, SquadCAType type, Actor target = null)
		{
			var ret = new SquadCA(bot, this, type, target);
			ret.PriorityTags = PriorityTagsFor(type);
			Squads.Add(ret);
			return ret;
		}

		public void DismissSquad(SquadCA squad)
		{
			unitsHangingAroundTheBase.AddRange(squad.Units);

			squad.Units.Clear();
		}

		// Squads kick stuck/blocked units out of their Units list; without a route
		// back those actors stay in activeUnits but in no squad and no pool — the
		// manager never touches them again. Return them to the idle pool so
		// FindNewUnits can reclassify them.
		internal void ReturnToIdlePool(Actor actor)
		{
			if (actor == null || unitCannotBeOrdered(actor))
				return;

			unitsHangingAroundTheBase.Add(new UnitWposWrapper(actor));
		}

		int UnitValue(Actor actor)
		{
			if (!cachedUnitValues.TryGetValue(actor.Info.Name, out var cost))
			{
				cost = actor.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;
				cachedUnitValues[actor.Info.Name] = cost;
			}

			return cost;
		}

		void TrackRoleLosses()
		{
			var maxRadiusSquared = Info.MaxBaseRadius * Info.MaxBaseRadius;
			foreach (var (actor, known) in lastKnownRoles)
			{
				if (!actor.IsDead)
					continue;

				lossesByRole[known.Role] = lossesByRole.GetValueOrDefault(known.Role) + known.Cost;
				if ((known.Location - initialBaseCenter).LengthSquared > maxRadiusSquared)
					awayLossesByRole[known.Role] = awayLossesByRole.GetValueOrDefault(known.Role) + known.Cost;
			}

			lastKnownRoles.Clear();
			foreach (var squad in Squads)
			{
				var role = squad.Type.ToString().ToLowerInvariant();
				foreach (var unit in squad.Units)
					if (unit.Actor != null && !unit.Actor.IsDead && unit.Actor.IsInWorld)
						lastKnownRoles[unit.Actor] = (role, UnitValue(unit.Actor), unit.Actor.Location);
			}

			foreach (var unit in unitsHangingAroundTheBase)
				if (unit.Actor != null && !unit.Actor.IsDead && unit.Actor.IsInWorld)
					lastKnownRoles.TryAdd(unit.Actor, ("idle", UnitValue(unit.Actor), unit.Actor.Location));
		}

		void AssignRolesToIdleUnits(IBot bot)
		{
			// Telemetry only: a snapshot every role pass is fresh enough for a death's position.
			if (World.WorldTick % Math.Max(1, Info.AssignRolesInterval) == 0)
				TrackRoleLosses();

			CleanSquads();

			activeUnits.RemoveAll(unitCannotBeOrdered);
			unitsHangingAroundTheBase.RemoveAll(u => unitCannotBeOrdered(u.Actor));
			foreach (var n in notifyIdleBaseUnits)
				n.UpdatedIdleBaseUnits(unitsHangingAroundTheBase);

			if (--attackForceTicks <= 0)
			{
				attackForceTicks = Info.AttackForceInterval;
				foreach (var s in Squads)
					s.Units.RemoveAll(u => unitCannotBeOrdered(u.Actor));

				// H1: squads consult the attention budget before acting. The cursor
				// rotates the pass order so a spent budget staggers squads instead of
				// starving the tail of the list.
				if (Squads.Count > 0)
				{
					var start = squadCursor % Squads.Count;
					for (var i = 0; i < Squads.Count; i++)
					{
						var index = (start + i) % Squads.Count;
						var s = Squads[index];

						// The Units pruning above can empty a squad after CleanSquads ran —
						// a corpse must not burn an attention slot on a no-op Update.
						if (!s.IsValid)
							continue;
						if (actionBudget != null && !actionBudget.TryConsumeAttention(s))
							continue;

						s.Update();
						squadCursor = index + 1;
					}
				}
			}

			if (--assignRolesTicks <= 0)
			{
				assignRolesTicks = Info.AssignRolesInterval;
				unitsHangingAroundTheBase.RemoveAll(u => unitCannotBeOrdered(u.Actor));
				activeUnits.RemoveAll(unitCannotBeOrdered);
				FindNewUnits(bot);
			}

			if (--minAttackForceDelayTicks <= 0)
			{
				minAttackForceDelayTicks = Info.MinimumAttackForceDelay;
				unitsHangingAroundTheBase.RemoveAll(u => unitCannotBeOrdered(u.Actor));
				CreateAttackForce(bot);
			}

			if (--protectOwnTicks <= 0 && protectOwnFrom != null)
				ProtectOwn(protectOwnFrom);
		}

		public void SetAirStrikeTarget(Actor target)
		{
			airStrikeTarget = target;
		}

		public Actor PopAirStrikeTarget()
		{
			var target = airStrikeTarget;
			airStrikeTarget = null;
			return target;
		}

		void FindNewUnits(IBot bot)
		{
			var newUnits = World.ActorsHavingTrait<IPositionable>()
				.Where(a => a.Owner == Player &&
					!Info.ExcludeFromSquadsTypes.Contains(a.Info.Name) &&
					!activeUnits.Contains(a) && a.IsInWorld);

			var guerrillaForce = GetSquadOfType(SquadCAType.Guerrilla);
			// JoinGuerrilla gates creation too: 0 means this personality never forms
			// guerrilla squads, not "the first unit always joins". The size cap is
			// evaluated per actor — a single pass may add a whole production wave.
			var guerrillaRoll = World.LocalRandom.Next(100) < Info.JoinGuerrilla;

			foreach (var a in newUnits)
			{
				if (Info.GuerrillaTypes.Contains(a.Info.Name) && guerrillaRoll &&
					(guerrillaForce == null || guerrillaForce.Units.Count < Info.MaxGuerrillaSize))
				{
					guerrillaForce ??= RegisterNewSquad(bot, SquadCAType.Guerrilla);

					guerrillaForce.Units.Add(new UnitWposWrapper(a));
					AIUtils.BotDebug("AI ({0}): Added {1} to squad {2}", Player.ClientIndex, a, guerrillaForce.Type);
				}
				else if (Info.AirUnitsTypes.Contains(a.Info.Name))
				{
					var airSquads = Squads.Where(s => s.Type == SquadCAType.Air);
					var matchingAirSquadFound = false;

					foreach (var airSquad in airSquads)
					{
						if (airSquad.Units.Any(u => u.Actor.Info.Name == a.Info.Name))
						{
							airSquad.Units.Add(new UnitWposWrapper(a));
							airSquad.NewUnits.Add(a);
							matchingAirSquadFound = true;
							break;
						}
					}

					if (!matchingAirSquadFound)
					{
						var newAirSquad = RegisterNewSquad(bot, SquadCAType.Air);
						newAirSquad.Units.Add(new UnitWposWrapper(a));
						newAirSquad.NewUnits.Add(a);
					}
				}
				else if (Info.NavalUnitsTypes.Contains(a.Info.Name))
				{
					var navalSquads = Squads.Where(s => s.Type == SquadCAType.Naval);
					var matchingNavalSquadFound = false;

					foreach (var navalSquad in navalSquads)
					{
						if (navalSquad.Units.Any(u => u.Actor.Info.Name == a.Info.Name))
						{
							navalSquad.Units.Add(new UnitWposWrapper(a));
							matchingNavalSquadFound = true;
							break;
						}
					}

					if (!matchingNavalSquadFound)
					{
						var newNavalSquad = RegisterNewSquad(bot, SquadCAType.Naval);
						newNavalSquad.Units.Add(new UnitWposWrapper(a));
					}
				}
				else if (Info.HarasserTypes.Contains(a.Info.Name))
				{
					var harasserSquads = Squads.Where(s => s.Type == SquadCAType.Harass);
					var matchingHarasserSquadFound = false;

					foreach (var harasserSquad in harasserSquads)
					{
						if (harasserSquad.Units.Any(u => u.Actor.Info.Name == a.Info.Name))
						{
							harasserSquad.Units.Add(new UnitWposWrapper(a));
							matchingHarasserSquadFound = true;
							break;
						}
					}

					if (!matchingHarasserSquadFound)
					{
						var newHarasserSquad = RegisterNewSquad(bot, SquadCAType.Harass);
						newHarasserSquad.Units.Add(new UnitWposWrapper(a));
					}
				}
				else if (Info.SupportUnitTypes.Contains(a.Info.Name))
				{
					var supportSquad = Squads.FirstOrDefault(s => s.Type == SquadCAType.Support);
					if (supportSquad == null)
					{
						supportSquad = RegisterNewSquad(bot, SquadCAType.Support);
						AIUtils.BotDebug("AI ({0}): Created support squad {1}", Player.ClientIndex, supportSquad.Type);
					}

					supportSquad.Units.Add(new UnitWposWrapper(a));
				}
				else
					unitsHangingAroundTheBase.Add(new UnitWposWrapper(a));

				activeUnits.Add(a);
			}

			// Notifying here rather than inside the loop, should be fine and saves a bunch of notification calls
			foreach (var n in notifyIdleBaseUnits)
				n.UpdatedIdleBaseUnits(unitsHangingAroundTheBase);
		}

		void CreateAttackForce(IBot bot)
		{
			// Create an attack force when we have enough units around our base.
			// (don't bother leaving any behind for defense)
			var idleUnitsValue = 0;

			if (Info.SquadValue > 0)
			{
				foreach (var a in unitsHangingAroundTheBase)
				{
					if (!cachedUnitValues.TryGetValue(a.Actor.Info.Name, out var unitCost))
					{
						unitCost = a.Actor.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;
						cachedUnitValues[a.Actor.Info.Name] = unitCost;
					}

					idleUnitsValue += unitCost;
				}
			}

			if (unitsHangingAroundTheBase.Count >= Info.MaxIdleUnits || (idleUnitsValue >= desiredAttackForceValue && unitsHangingAroundTheBase.Count >= desiredAttackForceSize))
			{
				BotMission mission = null;
				Actor missionTarget = null;
				FrozenActor missionFrozenTarget = null;
				if (Info.UseMissions && missionProviders?.Length > 0)
				{
					// A Defend whose hold window already lapsed this cycle is excluded at
					// selection time so it can't shadow a Raid sitting behind it in the
					// published order (Defend has RequiredValue 0 and would always win).
					mission = SelectMission();
					while (mission?.Type == BotMissionType.Defend)
					{
						if (heldDefendMission == null)
						{
							heldDefendMission = mission;
							defendMissionHeldSince = World.WorldTick;
						}
						else
						{
							// A different region re-published as Defend must not inherit the
							// previous region's elapsed hold (it could exhaust instantly).
							if (heldDefendMission.RegionIndex != mission.RegionIndex)
								defendMissionHeldSince = World.WorldTick;
							heldDefendMission = mission;
						}

						var heldTicks = World.WorldTick - defendMissionHeldSince;
						if (heldTicks <= Math.Max(0, Info.MissionDefendHoldTicks))
						{
							AIUtils.BotDebug("AI ({0}): holding {1} idle units for Defend mission in region {2} ({3}/{4} ticks)",
								Player.ClientIndex, unitsHangingAroundTheBase.Count, mission.RegionIndex, heldTicks, Info.MissionDefendHoldTicks);
							return;
						}

						AIUtils.BotDebug("AI ({0}): releasing Defend mission in region {1} after {2} ticks",
							Player.ClientIndex, mission.RegionIndex, heldTicks);
						defendMissionExhaustedRegions.Add(mission.RegionIndex);
						heldDefendMission = null;
						defendMissionHeldSince = -1;
						mission = SelectMission();
					}

					// Null is "no mission published this tick", not "a non-Defend won" —
					// clearing here would re-arm an exhausted region between publishes.
					if (mission != null && mission.Type != BotMissionType.Defend)
					{
						heldDefendMission = null;
						defendMissionHeldSince = -1;
						defendMissionExhaustedRegions.Clear();
					}

					if (mission?.Type == BotMissionType.Raid)
					{
						missionTarget = FindClosestEnemy(mission.Location, idleUnitsValue, mission.TargetPlayer);
						if (missionTarget == null && FoggedScans)
							missionFrozenTarget = FindFrozenEnemyTarget(
								World.Map.CenterOfCell(mission.Location), idleUnitsValue, null, mission.TargetPlayer);
					}
				}

				BotMission SelectMission()
				{
					return BestAffordableMission(missionProviders, idleUnitsValue,
						m => m.Type == BotMissionType.Defend
							&& defendMissionExhaustedRegions.Contains(m.RegionIndex));
				}

				var attackForce = RegisterNewSquad(bot, SquadCAType.Rush);

				// 6f: long-range units peel off into an artillery squad that trails
				// the assault and bombards its target, instead of charging with it.
				var artilleryUnits = unitsHangingAroundTheBase.Where(u => IsArtilleryUnit(u.Actor)).ToList();
				attackForce.Units.AddRange(unitsHangingAroundTheBase.Where(u => !IsArtilleryUnit(u.Actor)));
				if (missionTarget != null)
					attackForce.Target = Target.FromActor(missionTarget);
				else if (missionFrozenTarget != null)
					attackForce.Target = Target.FromFrozenActor(missionFrozenTarget);

				if (artilleryUnits.Count > 0)
				{
					var artillerySquad = RegisterNewSquad(bot, SquadCAType.Artillery);
					artillerySquad.Units.AddRange(artilleryUnits);
					artillerySquad.Parent = attackForce.IsValid ? attackForce : null;
					AIUtils.BotDebug("AI ({0}): Added {1} units to squad {2} (escorts {3})", Player.ClientIndex, artilleryUnits.Count, artillerySquad.Type, artillerySquad.Parent);
				}

				// Orphaned artillery squads (e.g. after a load) re-attach to the new assault.
				foreach (var squad in Squads.Where(s => s.Type == SquadCAType.Artillery && (s.Parent == null || !s.Parent.IsValid)))
					squad.Parent = attackForce.IsValid ? attackForce : squad.Parent;

				// 6f: support squads trail the newest assault, healing/repairing in its wake.
				foreach (var squad in Squads.Where(s => s.Type == SquadCAType.Support && (s.Parent == null || !s.Parent.IsValid)))
					squad.Parent = attackForce.IsValid ? attackForce : squad.Parent;

				AIUtils.BotDebug("AI ({0}): Added {1} units to squad {2}", Player.ClientIndex, attackForce.Units.Count, attackForce.Type);
				unitsHangingAroundTheBase.Clear();
				foreach (var n in notifyIdleBaseUnits)
					n.UpdatedIdleBaseUnits(unitsHangingAroundTheBase);

				SetNextDesiredAttackForce();
				if (mission?.Type == BotMissionType.Raid && (missionTarget != null || missionFrozenTarget != null))
				{
					LastMissionAssignment = new BotMissionAssignment
					{
						Type = mission.Type,
						RegionIndex = mission.RegionIndex,
						Frozen = missionFrozenTarget != null
					};
					MissionTaken(mission);
				}
				heldDefendMission = null;
				defendMissionHeldSince = -1;
			}
		}

		void SetNextDesiredAttackForce()
		{
			desiredAttackForceSize = Info.SquadSize + World.LocalRandom.Next(Info.SquadSizeRandomBonus);
			desiredAttackForceValue = 0;

			if (Info.SquadValue > 0)
			{
				if (Info.SquadValueMaxEarlyBonus == 0 &&
					Info.SquadValueMinLateBonus == 0 &&
					Info.SquadValueMaxLateBonus == 0)
					desiredAttackForceValue = Info.SquadValue + World.LocalRandom.Next(Info.SquadValueRandomBonus);
				else
				{
					desiredAttackForceValue = Info.SquadValue;
					// Add a random bonus between a min and max that scale over the first 20 minutes.
					// Min scales from 0 to SquadValueMinLateBonus; max scales from SquadValueMaxEarlyBonus to SquadValueMaxLateBonus.
					var t = Math.Min(1f, World.WorldTick / SquadValueRampDurationTicks);
					var minBonus = (int)(Info.SquadValueMinLateBonus * t);
					var maxBonus = (int)(Info.SquadValueMaxEarlyBonus + (Info.SquadValueMaxLateBonus - Info.SquadValueMaxEarlyBonus) * t);

					if (maxBonus <= minBonus)
						desiredAttackForceValue += minBonus;
					else
						desiredAttackForceValue += World.LocalRandom.Next(minBonus, maxBonus);
				}
			}
		}

		void ProtectOwn(Actor attacker)
		{
			protectOwnFrom = null;
			protectOwnTicks = Info.ProtectInterval;

			// Fogged scans only chase an attacker the bot can actually see; an
			// unseen attacker still updates the defence center above, and the
			// protection squad's own radius scan picks up anything it can see.
			var protectTarget = FoggedScans && !IsNotHiddenUnit(attacker) ? null : attacker;

			var protectSq = GetSquadOfType(SquadCAType.Protection);
			if (protectSq == null)
				protectSq = RegisterNewSquad(bot, SquadCAType.Protection, protectTarget);

			if (!protectSq.IsValid)
			{
				// Draft from the idle pool only. activeUnits contains both squad members
				// and hanging units, so a world scan would draft units already assigned
				// to Rush/Guerrilla/etc. — dual membership and competing orders.
				var draftable = unitsHangingAroundTheBase
					.Where(u => !Info.ExcludeFromSquadsTypes.Contains(u.Actor.Info.Name) && u.Actor.Info.HasTraitInfo<AttackBaseInfo>()
						&& !u.Actor.Info.HasTraitInfo<BuildingInfo>() && !u.Actor.Info.HasTraitInfo<HarvesterInfo>() && !u.Actor.Info.HasTraitInfo<AircraftInfo>())
					.ToList();

				foreach (var u in draftable)
				{
					protectSq.Units.Add(u);
					unitsHangingAroundTheBase.Remove(u);
				}
			}

			if (protectSq.IsValid && !protectSq.IsTargetValid && protectTarget != null)
				protectSq.TargetActor = protectTarget;
		}

		void IBotPositionsUpdated.UpdatedBaseCenter(CPos newLocation)
		{
			initialBaseCenter = newLocation;
		}

		void IBotPositionsUpdated.UpdatedDefenseCenter(CPos newLocation) { }

		void IBotRespondToAttack.RespondToAttack(IBot bot, Actor self, AttackInfo e)
		{
			if (!IsPreferredEnemyUnit(e.Attacker))
				return;

			// Protected priority assets, MCVs, harvesters and buildings
			// TODO: Use *CommonNames, instead of hard-coding trait(info)s.
			if (self.Info.HasTraitInfo<HarvesterInfo>() || self.Info.HasTraitInfo<BuildingInfo>() || self.Info.HasTraitInfo<BaseBuildingInfo>())
			{
				foreach (var n in notifyPositionsUpdated)
					n.UpdatedDefenseCenter(e.Attacker.Location);

				ProtectOwn(e.Attacker);
			}
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled)
				return null;

			return new List<MiniYamlNode>()
			{
				new("Squads", "", Squads.ConvertAll(s => new MiniYamlNode("Squad", s.Serialize()))),
				new("InitialBaseCenter", FieldSaver.FormatValue(initialBaseCenter)),
				new("UnitsHangingAroundTheBase", FieldSaver.FormatValue(unitsHangingAroundTheBase
					.Where(u => !unitCannotBeOrdered(u.Actor))
					.Select(u => u.Actor.ActorID)
					.ToArray())),
				new("ActiveUnits", FieldSaver.FormatValue(activeUnits
					.Where(a => !unitCannotBeOrdered(a))
					.Select(a => a.ActorID)
					.ToArray())),
				new("RushTicks", FieldSaver.FormatValue(rushTicks)),
				new("AssignRolesTicks", FieldSaver.FormatValue(assignRolesTicks)),
				new("protectionForceTicks", FieldSaver.FormatValue(protectionForceTicks)),
				new("AttackForceTicks", FieldSaver.FormatValue(attackForceTicks)),
				new("MinAttackForceDelayTicks", FieldSaver.FormatValue(minAttackForceDelayTicks)),
			};
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, MiniYaml data)
		{
			if (self.World.IsReplay)
				return;

			defendMissionExhaustedRegions.Clear();

			var nodes = data.ToDictionary();

			if (nodes.TryGetValue("InitialBaseCenter", out var initialBaseCenterNode))
				initialBaseCenter = FieldLoader.GetValue<CPos>("InitialBaseCenter", initialBaseCenterNode.Value);

			if (nodes.TryGetValue("UnitsHangingAroundTheBase", out var unitsHangingAroundTheBaseNode))
			{
				unitsHangingAroundTheBase.Clear();
				foreach (var a in FieldLoader.GetValue<uint[]>("UnitsHangingAroundTheBase", unitsHangingAroundTheBaseNode.Value)
					.Select(a => self.World.GetActorById(a)).Where(a => a != null))
				{
					unitsHangingAroundTheBase.Add(new UnitWposWrapper(a));
				}
			}

			if (nodes.TryGetValue("ActiveUnits", out var activeUnitsNode))
			{
				activeUnits.Clear();
				activeUnits.AddRange(FieldLoader.GetValue<uint[]>("ActiveUnits", activeUnitsNode.Value)
					.Select(a => self.World.GetActorById(a)).Where(a => a != null));
			}

			if (nodes.TryGetValue("RushTicks", out var rushTicksNode))
				rushTicks = FieldLoader.GetValue<int>("RushTicks", rushTicksNode.Value);

			if (nodes.TryGetValue("AssignRolesTicks", out var assignRolesTicksNode))
				assignRolesTicks = FieldLoader.GetValue<int>("AssignRolesTicks", assignRolesTicksNode.Value);

			if (nodes.TryGetValue("protectionForceTicks", out var protectionForceTicksNode))
				protectionForceTicks = FieldLoader.GetValue<int>("protectionForceTicks", protectionForceTicksNode.Value);

			if (nodes.TryGetValue("AttackForceTicks", out var attackForceTicksNode))
				attackForceTicks = FieldLoader.GetValue<int>("AttackForceTicks", attackForceTicksNode.Value);

			if (nodes.TryGetValue("MinAttackForceDelayTicks", out var minAttackForceDelayTicksNode))
				minAttackForceDelayTicks = FieldLoader.GetValue<int>("MinAttackForceDelayTicks", minAttackForceDelayTicksNode.Value);

			if (nodes.TryGetValue("Squads", out var squadsNode))
			{
				Squads.Clear();
				foreach (var n in squadsNode.Nodes)
					Squads.Add(SquadCA.Deserialize(bot, this, n.Value));
			}

			// Reconcile stranded units: an actor restored into activeUnits that belongs to
			// no squad and no idle pool would never be managed again — park it in the pool.
			var inSquads = Squads.SelectMany(s => s.Units.Select(u => u.Actor)).ToHashSet();
			var inPool = unitsHangingAroundTheBase.Select(u => u.Actor).ToHashSet();
			foreach (var a in activeUnits.Where(a => !inSquads.Contains(a) && !inPool.Contains(a) && !unitCannotBeOrdered(a)))
				unitsHangingAroundTheBase.Add(new UnitWposWrapper(a));
		}

		public bool CanBuildMoreOfAircraft(ActorInfo actorInfo)
		{
			foreach (var aircraftBuilder in aircraftBuilders)
			{
				if (!aircraftBuilder.IsTraitEnabled())
					continue;

				if (aircraftBuilder.CanBuildMoreOfAircraft(actorInfo))
					return true;
			}

			return false;
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			constructionYardBuildings.Dispose();
		}
	}
}
