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
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Controls AI unit production.")]
	public class UnitBuilderBotModuleCAInfo : ConditionalTraitInfo
	{
		// TODO: Investigate whether this might the (or at least one) reason why bots occasionally get into a state of doing nothing.
		// Reason: If this is less than SquadSize, the bot might get stuck between not producing more units due to this,
		// but also not creating squads since there aren't enough idle units.
		[Desc("Only produce units as long as there are less than this amount of units idling inside the base.")]
		public readonly int IdleBaseUnitsMaximum = 12;

		[Desc("Production queues AI uses for producing units.")]
		public readonly string[] UnitQueues = { "VehicleSQ", "InfantrySQ", "AircraftSQ", "ShipSQ", "VehicleMQ", "InfantryMQ", "AircraftMQ", "ShipMQ" };

		[Desc("Basic combat units that may be produced from the starting-cash surplus while the opening refinery is unfinished.")]
		public readonly HashSet<string> OpeningDefenseUnitTypes = new HashSet<string>();

		[Desc("What units to the AI should build.", "What relative share of the total army must be this type of unit.")]
		public readonly Dictionary<string, int> UnitsToBuild = null;

		[Desc("What units should the AI have a maximum limit to train.")]
		public readonly Dictionary<string, int> UnitLimits = null;

		[Desc("When should the AI start train specific units.")]
		public readonly Dictionary<string, int> UnitDelays = null;

		[Desc("Minimum duration between building a specific unit.")]
		public readonly Dictionary<string, int> UnitIntervals = null;

		[Desc("S1 (FINDINGS_2026-10-12 §3): live capturer units (engineers and kin) at most this percent",
			"of live armed infantry — the engineer:combat-infantry ratio. 0 disables. BotLimits",
			".CapturerInfantryLimitPct overrides per difficulty; the switch gate keeps it off classic.")]
		public readonly int CapturerInfantryLimitPct = 0;

		[Desc("S1: live capturers below this floor are always admitted — the capture capability",
			"survives even before the infantry army exists.")]
		public readonly int CapturerMinimumUnits = 2;

		[Desc("S1b (ENGINEER-CAP-10PCT, maintainer 2026-10-12): most extra capturers admitted on top",
			"of the ratio cap while KNOWN uncaptured capturable targets remain (+1 per fog-fair target",
			"the engineer owner reports through IBotCapturerTargetAllowance). 0 = no allowance.")]
		public readonly int CapturerAllowanceMax = 4;

		[Desc("S1b: fallback for BotLimits.EngineerCompletionIntervalTicks on a ruleset without the",
			"difficulty line — minimum ticks between capturer completions. 0 = off.")]
		public readonly int EngineerCompletionIntervalTicks = 0;

		[Desc("How often should the unit builder check to build more units")]
		public readonly int UnitBuilderInterval = 0;

		[Desc("Only queue construction of a new unit when above this requirement.")]
		public readonly int ProductionMinCashRequirement = 2000;

		[Desc("Only queue construction of a new unit when above this requirement.",
			"BotLimits.MaximiseProductionCashRequirement overrides this per difficulty (DESIGN §19.1).")]
		public readonly int MaximiseProductionCashRequirement = 10000;

		[Desc("M13 tempo (BV_m13_liveness): non-zero arms the bank-saturation override on this instance.",
			"The cash bar itself is the tier's BotLimits.BankSaturationCash; SwitchCondition must exclude",
			"classic on this shared @generic instance. 0 = off, bit-identical.")]
		public readonly int BankSaturationCash = 0;

		[Desc("Ticks between samples of the enemy army for adaptive counter-production (BotLimits.AdaptiveCounterWeight).")]
		public readonly int AdaptiveObservationInterval = 250;

		[Desc("Maximum number of aircraft AI can build.",
			"If MaintainAirSuperiority is true this only applies to units not listed in AirToAirUnits.")]
		public readonly int MaxAircraft = 4;

		[Desc("If true, will always attempt to match the number of enemy air threats.")]
		public readonly bool MaintainAirSuperiority = false;

		[ConsumedConditionReference]
		[Desc("Increment switches on this shared instance (CountQueuedAircraft, ObservedAirThreats) take effect only while this condition is true",
			"(e.g. genericbot), so a shared module can be A/B-switched without changing the classic reference. Null = switches apply to every owner.")]
		public readonly BooleanExpression SwitchCondition = null;

		[Desc("CA F2p2 (71754fc06): aircraft already queued for production count towards the aircraft limits,",
			"and allied air-to-air aircraft count as part of the current air-to-air count instead of lowering the limit.",
			"Default false keeps the previous behaviour.")]
		public readonly bool CountQueuedAircraft = false;

		[Desc("CA F2p2 / DESIGN 19.5: count enemy air threats (for MaintainAirSuperiority) from the observed enemy composition instead of",
			"scanning every actor in the world. Bots without a fog-observing composition provider (classic) keep the world count.",
			"Default false keeps the previous behaviour.")]
		public readonly bool ObservedAirThreats = false;

		[Desc("If MaintainAirSuperiority is true and this is non-zero,",
			"sets an upper limit for the number of air superiority aircraft.")]
		public readonly int MaxAirSuperiority = 0;

		[Desc("List of actor types to be used for air superiority.")]
		public readonly HashSet<string> AirToAirUnits = new HashSet<string>();

		[Desc("List of actor types to measure against for air superiority.")]
		public readonly HashSet<string> AirThreatUnits = new HashSet<string>();

		[Desc("If true, the bot will use compositions defined in the UnitCompositionsBotModule to determine what units to build.",
			"If false, the bot will ignore compositions and just use UnitsToBuild.")]
		public readonly bool UseCompositions = false;

		[Desc("AI_ARCHITECTURE.md §2.8b item 3 (UW-1): derive the UnitsToBuild weights from the enabled",
			"personality's RoleMix and each candidate's Versus-weighted strength instead of using the",
			"yaml shares. The yaml rows stay the membership gate; rows for units with no combat role",
			"pass through as overrides, and an active composition still wins. Off until the next",
			"increment A/B (switch group I_derived_unit_weights). Inert on classic: it carries no",
			"RoleMix and no IBotUnitRoles provider.")]
		public readonly bool UseDerivedUnitWeights = false;

		[Desc("BM_live_combat_model: the derived-weights strength term uses the balance",
			"pipeline's effective-damage model instead of the classic main-warhead DPS.",
			"False = classic numbers, bit-identical.")]
		public readonly bool UseEffectiveDamageModel = false;

		[Desc("Minimum ticks before selecting a new composition.")]
		public readonly int MinCompositionSelectInterval = 750;

		[Desc("Maximum ticks before selecting a new composition.")]
		public readonly int MaxCompositionSelectInterval = 7500;

		[Desc("TF-2 (turtle regression increment 2): percentage of spendable cash held for mobile",
			"production while the offensive pool needs replenishment — optional buildings must leave",
			"it unspent. 0 = off; essential items (yard/refinery/power/first factory) are exempt.")]
		public readonly int ReserveMobileProductionShare = 0;

		[Desc("TF-2: alive offensive-role units below which the pool counts as needing replenishment.",
			"The reserve holds only while the pool is under this floor.")]
		public readonly int ReserveArmyFloor = 8;

		[Desc("U1 (production service contract §3): feasible-candidate/fair-producer service —",
			"capable producer matched before slot, admissible candidates before the pick,",
			"bounded per-candidate retry (a rejected name leaves the set alone instead of",
			"excluding every limited name), per-category rotation over stable queue keys,",
			"and ephemeral in-pass slot/cost reservations. Gated by SwitchCondition;",
			"default off keeps the legacy path byte-identical.")]
		public readonly bool FairProducerService = false;

		[Desc("P4 (combat demand): while alive armed infantry sit below this floor, an",
			"infantry-capable queue that can legally produce one prefers it — highest combat",
			"value per cost, name tiebreak — before the normal pick. Engineers/capturers and",
			"support actors never count toward the floor (armed Infantry target-type only).",
			"0 = off. Requires FairProducerService.")]
		public readonly int CombatInfantryFloor = 0;

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (MinCompositionSelectInterval < 0)
				throw new YamlException("MinCompositionSelectInterval must not be negative.");

			if (MaxCompositionSelectInterval < 0)
				throw new YamlException("MaxCompositionSelectInterval must not be negative.");

			if (MinCompositionSelectInterval != 0 && MaxCompositionSelectInterval != 0 &&
				MaxCompositionSelectInterval < MinCompositionSelectInterval)
				throw new YamlException("MaxCompositionSelectInterval cannot be less than MinCompositionSelectInterval.");
		}

		public override object Create(ActorInitializer init) { return new UnitBuilderBotModuleCA(init.Self, this); }
	}

	public class UnitBuilderBotModuleCA : ConditionalTrait<UnitBuilderBotModuleCAInfo>, IBotTick, IBotNotifyIdleBaseUnits, IBotRequestUnitProduction, IGameSaveTraitData, IBotAircraftBuilder, INotifyActorDisposing, IBotUnitProductionGateInfo, IBotMobileSpendReserve
	{
		public const int FeedbackTime = 30; // ticks; = a bit over 1s. must be >= netlag.

		// S1b (maintainer 2026-10-12): the flat capturer-ratio ceiling — a difficulty line may only
		// go LOWER, never higher. Clamped inside CapturerCapAdmits so the invariant is testable.
		public const int CapturerLimitPctCeiling = 10;

		readonly World world;
		readonly Player player;

		// SwitchCondition result; true when no condition is configured.
		bool switchesActive;
		bool CountQueued => Info.CountQueuedAircraft && switchesActive;
		bool ObservedThreats => Info.ObservedAirThreats && switchesActive;

		UnitComposition activeComposition;
		int activeCompositionProducedValue;
		int activeCompositionSelectedTick;
		int nextCompositionSelectTick;

		// Record-only observability for the AI match log (schema 2): the id of the
		// currently selected composition, and a transition notification keyed by tick.
		public string ActiveCompositionId => activeComposition?.Id ?? string.Empty;
		public event Action<int, string> ActiveCompositionChanged;
		readonly Dictionary<string, int> compositionLastUsedTickById = new Dictionary<string, int>();

		readonly List<string> queuedBuildRequests = new List<string>();
		ActorIndex.OwnerAndNames unitsToBuild;
		readonly Dictionary<string, int> activeUnitIntervals = new Dictionary<string, int>();

		UnitCompositionsBotModule compositionsModule;
		List<UnitComposition> possibleActiveCompositions;
		TechTree techTree;

		IBotRequestPauseUnitProduction[] requestPause;
		IBotUnitProductionObserver[] unitObservers;
		int idleUnitCount;
		int currentQueueIndex = 0;
		PlayerResources playerResources;
		BotLimits botLimits;
		BaseBuilderBotModuleCA baseBuilder;

		// TF-2: the mobile-production reservation ledger (world-free) and the offensive-pool
		// replenish check — recomputed at the module's own cadence, so the standing share can
		// flap off the moment the pool is full while in-flight items stay tracked per sweep.
		readonly UnitSpendReservation mobileReserve = new();
		int nextPoolCheckTick;
		bool poolNeedsReplenish;

		static readonly HashSet<string> OffensiveRoles = new(StringComparer.Ordinal)
		{
			BotUnitRole.Frontline, BotUnitRole.Skirmisher, BotUnitRole.AntiInfantry,
			BotUnitRole.AntiArmour, BotUnitRole.Artillery, BotUnitRole.AntiAir,
			BotUnitRole.Gunship, BotUnitRole.Fighter, BotUnitRole.Bomber
		};

		int ticks;
		int openingDefenseTicks;
		int unitDelayModifier = 100;
		int unitIntervalModifier = 100;
		bool firstTick = true;

		readonly AdaptiveCounterProduction counters;
		IBotEnemyCompositionProvider compositionProvider;
		IBotPersonalityLeadProvider[] leadProviders;
		IBotUnitRoles unitRoles;

		// Cameo (§12.23, PP-1): lazy — resolved on first BuildUnit; null keeps the upstream one-queue fill.
		IBotProductionWidth productionWidth;
		IBotScaleTargets[] scaleTargets;
		readonly DerivedUnitWeights derivedUnitWeights = new DerivedUnitWeights();

		// U1 (service-contract §3): the switch-gated fair-service arm and its per-category
		// rotation cursor over stable queue keys ("actorID|queueType") — a visit resumes
		// AFTER the last examined producer instead of always re-walking the head.
		bool FairService => switchesActive && Info.FairProducerService;
		readonly Dictionary<string, string> serviceCursorByCategory = new(StringComparer.Ordinal);

		int CounterWeight => botLimits?.Info.AdaptiveCounterWeight ?? 0;

		int MaximiseProductionCash => botLimits != null && botLimits.Info.MaximiseProductionCashRequirement >= 0
			? botLimits.Info.MaximiseProductionCashRequirement : Info.MaximiseProductionCashRequirement;

		// S1 (FINDINGS_2026-10-12 §3): engineer:combat-infantry ratio cap — OFF for classic
		// (switchesActive is only ever true under genericbot), so a bot's share of the
		// unit mix can never stack capturers past the difficulty-scaled portion of its
		// infantry. Difficulty ladder wins over the module default, like every BotLimits override.
		int CapturerLimitPct
		{
			get
			{
				if (!switchesActive)
					return 0;
				if (botLimits != null && botLimits.Info.CapturerInfantryLimitPct >= 0)
					return botLimits.Info.CapturerInfantryLimitPct;
				return Info.CapturerInfantryLimitPct;
			}
		}

		// S1 pure seam: cap = floor(armed infantry * pct / 100) + the capturable-target allowance,
		// floored at capturerMinimumUnits so capture capability survives an empty infantry count.
		// S1b tightens it (ENGINEER-CAP-10PCT): pct never exceeds CapturerLimitPctCeiling (a
		// difficulty may only go lower), at most one capturer sits in production, and completions
		// keep a minimum interval while no known target justifies the allowance. Self-correcting:
		// over-cap engineers die back or succeed before more queue.
		public static bool CapturerCapAdmits(int capturerLimitPct, int capturerUnits, int armedInfantryUnits,
			int capturerMinimumUnits, int capturerAllowanceMax, int knownCapturableTargets,
			int queuedCapturers, int ticksSinceLastCompletion, int completionIntervalTicks)
		{
			var pct = Math.Min(CapturerLimitPctCeiling, Math.Max(0, capturerLimitPct));
			if (pct <= 0)
				return true;

			// One capturer in production at a time: a queued or building one blocks the next.
			if (queuedCapturers >= 1)
				return false;

			var allowance = Math.Min(Math.Max(0, capturerAllowanceMax), Math.Max(0, knownCapturableTargets));

			// Minimum interval between completions — skipped while free targets remain.
			if (allowance <= 0 && completionIntervalTicks > 0 && ticksSinceLastCompletion >= 0 &&
				ticksSinceLastCompletion < completionIntervalTicks)
				return false;

			var cap = Math.Max(capturerMinimumUnits, (int)Math.Floor(armedInfantryUnits * (long)pct / 100.0)) + allowance;
			return capturerUnits < cap;
		}

		// F1 (lead ruling 2026-10-12): the cap is armed only above 0 — hoisted ahead of
		// the world scan so an off cap (classic, where CapturerLimitPct reads 0) never
		// pays the army walk. CapturerCapAdmits keeps its own <=0 admit as the invariant.
		public static bool CapturerCapActive(int capturerLimitPct) => capturerLimitPct > 0;

		// "Combat infantry" for the ratio: an actor the enemy would target as Infantry
		// (the Infantry targetable type, the same marker squads use) that actually carries
		// a weapon. Capture bodies, unarmed medics/mechanics and slaved units never count.
		static readonly BitSet<TargetableType> InfantryTargetTypes = new("Infantry");

		static bool IsArmedInfantry(ActorInfo info) =>
			info.GetAllTargetTypes().Overlaps(InfantryTargetTypes) &&
			!info.HasTraitInfo<CapturesInfo>() &&
			!info.HasTraitInfo<CarrierSlaveInfo>() &&
			info.TraitInfos<ArmamentInfo>().Any(arm => arm.WeaponInfo != null);

		// Counts are world-state per tick; ShouldBuild may see several capturer
		// candidates in one queue pass, so scan once and cache.
		int capturerCountsTick = -1, capturerUnitsCached, armedInfantryCached;
		int capturerUnitsLastSample, lastCapturerCompletionTick = -1;

		void CapturerArmyCounts(out int capturerUnits, out int armedInfantryUnits)
		{
			if (capturerCountsTick == world.WorldTick)
			{
				capturerUnits = capturerUnitsCached;
				armedInfantryUnits = armedInfantryCached;
				return;
			}

			capturerCountsTick = world.WorldTick;
			capturerUnitsCached = 0;
			armedInfantryCached = 0;
			foreach (var actor in world.Actors.Where(actor => actor.Owner == player && !actor.IsDead && actor.IsInWorld))
			{
				var info = actor.Info;
				if (info.HasTraitInfo<CapturesInfo>())
					capturerUnitsCached++;
				else if (IsArmedInfantry(info))
					armedInfantryCached++;
			}

			// S1b: a rising capturer count is a completion (or any gain): stamp it so the pacing
			// interval measures from the last capturer that actually finished.
			if (capturerUnitsCached > capturerUnitsLastSample)
				lastCapturerCompletionTick = capturerCountsTick;
			capturerUnitsLastSample = capturerUnitsCached;

			capturerUnits = capturerUnitsCached;
			armedInfantryUnits = armedInfantryCached;
		}

		// S1b: KNOWN uncaptured capturable targets, reported by the engineer owner (none = 0).
		// Resolved lazily — the provider's own conditions settle after Created; per-tick cached.
		IBotCapturerTargetAllowance capturerAllowanceProvider;
		int capturerAllowanceTick = -1, capturerAllowanceCached;

		int CapturerAllowance()
		{
			if (capturerAllowanceTick == world.WorldTick)
				return capturerAllowanceCached;

			capturerAllowanceTick = world.WorldTick;
			capturerAllowanceProvider ??= player.PlayerActor
				.TraitsImplementing<IBotCapturerTargetAllowance>().FirstEnabledTraitOrDefault();
			return capturerAllowanceCached = capturerAllowanceProvider?.CapturerTargetAllowance ?? 0;
		}

		// S1b: capturers sitting in our own production queues — one in production at a time.
		int QueuedCapturers()
		{
			var count = 0;
			foreach (var group in OpenRA.Mods.Common.AIUtils.FindQueuesByCategory(player))
				foreach (var q in group)
					foreach (var item in q.AllQueued())
						if (world.Map.Rules.Actors.TryGetValue(item.Item, out var queued) && queued.HasTraitInfo<CapturesInfo>())
							count++;

			return count;
		}

		// S1b: the difficulty-line pacing interval (BotLimits value; 0 = off) and the ticks since
		// the last detected capturer completion (-1 until the first one finishes).
		int EngineerCompletionInterval =>
			botLimits != null && botLimits.Info.EngineerCompletionIntervalTicks >= 0
				? botLimits.Info.EngineerCompletionIntervalTicks : Info.EngineerCompletionIntervalTicks;

		int TicksSinceLastCapturerCompletion() =>
			lastCapturerCompletionTick < 0 ? -1 : world.WorldTick - lastCapturerCompletionTick;

		// BV_m13_liveness arm: the module field is only a flag, the cash bar is the tier's
		// BotLimits value. Classic shares this instance but never carries genericbot, so
		// SwitchCondition keeps the whole override inert there.
		int BankSaturationThreshold => switchesActive && Info.BankSaturationCash > 0 && botLimits != null
			? Math.Max(0, botLimits.Info.BankSaturationCash) : 0;

		public UnitBuilderBotModuleCA(Actor self, UnitBuilderBotModuleCAInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			counters = new AdaptiveCounterProduction(world, player);
			switchesActive = info.SwitchCondition == null;
		}

		public override IEnumerable<VariableObserver> GetVariableObservers()
		{
			foreach (var observer in base.GetVariableObservers())
				yield return observer;

			if (Info.SwitchCondition != null)
				yield return new VariableObserver(SwitchConditionChanged, Info.SwitchCondition.Variables);
		}

		void SwitchConditionChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			switchesActive = Info.SwitchCondition.Evaluate(conditions);
		}

		protected override void Created(Actor self)
		{
			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query player traits from self, which refers
			// for bot modules always to the Player actor.
			requestPause = self.TraitsImplementing<IBotRequestPauseUnitProduction>().ToArray();
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			techTree = self.Owner.PlayerActor.TraitOrDefault<TechTree>();
			compositionProvider = self.TraitsImplementing<IBotEnemyCompositionProvider>().FirstOrDefault();
			leadProviders = self.TraitsImplementing<IBotPersonalityLeadProvider>().ToArray();
			compositionsModule = Info.UseCompositions ? self.World.WorldActor.TraitOrDefault<UnitCompositionsBotModule>() : null;

			var referencedUnitTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (Info.UnitsToBuild != null)
				referencedUnitTypes.UnionWith(Info.UnitsToBuild.Keys);

			if (compositionsModule != null && compositionsModule.UnitCompositions.Count != 0)
			{
				foreach (var composition in compositionsModule.UnitCompositions)
					if (composition?.UnitsToBuild != null)
						referencedUnitTypes.UnionWith(composition.UnitsToBuild.Keys);

				possibleActiveCompositions = compositionsModule.UnitCompositions
					.Where(c => c != null && !c.IsBaseline &&
						(c.EnabledChance == 100 || BotRng.For(player, nameof(UnitBuilderBotModuleCA)).Next(100) < c.EnabledChance))
					.ToList();

				nextCompositionSelectTick = GetNextCompositionSelectTick();
			}

			unitsToBuild = new ActorIndex.OwnerAndNames(world, referencedUnitTypes, player);
		}

		protected override void TraitEnabled(Actor self)
		{
			RefreshDifficultyTraits();
		}

		void RefreshDifficultyTraits()
		{
			botLimits = player.PlayerActor.TraitsImplementing<BotLimits>().FirstEnabledTraitOrDefault();
			baseBuilder = player.PlayerActor.TraitsImplementing<BaseBuilderBotModuleCA>().FirstEnabledTraitOrDefault();
			unitDelayModifier = botLimits?.Info.UnitDelayModifier ?? 100;
			unitIntervalModifier = botLimits?.Info.UnitIntervalModifier ?? 100;
		}

		void IBotNotifyIdleBaseUnits.UpdatedIdleBaseUnits(List<UnitWposWrapper> idleUnits)
		{
			idleUnitCount = idleUnits.Count;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (firstTick)
			{
				RefreshDifficultyTraits();
				firstTick = false;
			}

			// TF-2: keep the reservation live even while unit production is paused — the floor
			// must decay the moment its conditions stop holding, not linger until the next build.
			UpdateMobileReserve();

			if (CounterWeight > 0)
				counters.Observe(Info.AdaptiveObservationInterval, compositionProvider);

			// Decrement any active unit intervals, removing any that reach zero
			foreach (KeyValuePair<string, int> i in activeUnitIntervals.ToList())
			{
				activeUnitIntervals[i.Key]--;
				if (activeUnitIntervals[i.Key] <= 0)
					activeUnitIntervals.Remove(i.Key);
			}

			var baseBuilderPause = requestPause.FirstOrDefault(rp => ReferenceEquals(rp, baseBuilder));
			if (requestPause.Any(rp => !ReferenceEquals(rp, baseBuilderPause) && rp.IsTraitEnabled() && rp.PauseUnitProduction))
				return;

			if (baseBuilderPause != null && baseBuilderPause.PauseUnitProduction)
			{
				if (++openingDefenseTicks % (FeedbackTime + Info.UnitBuilderInterval) == 0)
					TryBuildOpeningDefense(bot);

				return;
			}

			openingDefenseTicks = 0;

			ticks++;

			if (ticks % (FeedbackTime + Info.UnitBuilderInterval) == 0)
			{
				UpdateComposition();

				// U1 (Sol F1): one ephemeral reservation batch shared by the external
				// request scan and every category visit this pass — no call site can
				// oversubscribe a slot, cash or headroom another site already claimed.
				// Discarded when the pass ends; nothing durable. Flag off never allocates.
				var batch = FairService ? new ProducerServiceBatch(playerResources.GetCashAndResources()) : null;

				if (FairService && queuedBuildRequests.Count > 0)
				{
					// U1 (service-contract §4): every pending external request is examined
					// once per visit in request order — an emitted or permanently-invalid
					// request leaves the list, a temporarily blocked one survives for the
					// next visit. An impossible head request cannot hide a legal later one,
					// and a deferred order is never duplicated.
					foreach (var buildRequest in queuedBuildRequests.ToList())
						if (BuildUnit(bot, buildRequest, batch) || PermanentlyInvalidBuildRequest(buildRequest))
							queuedBuildRequests.Remove(buildRequest);
				}
				else
				{
					var buildRequest = queuedBuildRequests.FirstOrDefault();
					if (buildRequest != null)
					{
						BuildUnit(bot, buildRequest);
						queuedBuildRequests.Remove(buildRequest);
					}
				}

				// §12.14 PL-1: a trailing Steamroller lead relaxes both cash floors
				// toward spending sooner — at target (or leads off) the multiplier
				// is 1 and the floors are unchanged.
				var cashLean = BotPersonalityLeads.Lean(leadProviders, "steamroller");

				// Don't produce if we don't have enough cash
				if (playerResources.Cash + playerResources.Resources < BotPersonalityLeads.Scaled(Info.ProductionMinCashRequirement, cashLean))
					return;

				for (var i = 0; i < Info.UnitQueues.Length; i++)
				{
					if (++currentQueueIndex >= Info.UnitQueues.Length)
						currentQueueIndex = 0;

					if (AIUtils.FindQueues(player, Info.UnitQueues[currentQueueIndex]).Any())
					{
						// BV_m13_liveness: re-evaluated per category with the same cash accounting —
						// while the bank stays at/above the tier bar the MaximiseProductionCash break is skipped.
						var saturated = ProductionSaturationCA.Saturated(
							playerResources.Cash + playerResources.Resources, BankSaturationThreshold);

						// PERF: We tick only one type of valid queue at a time
						// if AI gets enough cash, it can fill all of its queues with enough ticks
						BuildUnit(bot, Info.UnitQueues[currentQueueIndex], idleUnitCount < Info.IdleBaseUnitsMaximum, false, saturated, batch);

						if (ProductionSaturationCA.StopCategorySweep(saturated, playerResources.Cash + playerResources.Resources,
							BotPersonalityLeads.Scaled(MaximiseProductionCash, cashLean)))
							break;
					}
				}
			}
		}

		void TryBuildOpeningDefense(IBot bot)
		{
			if (baseBuilder == null || !baseBuilder.CanTrainOpeningDefense || Info.OpeningDefenseUnitTypes.Count == 0)
				return;

			foreach (var queue in Info.UnitQueues.SelectMany(category => AIUtils.FindQueues(player, category)).Distinct())
			{
				if (queue.AllQueued().Any())
					continue;

				var unit = queue.BuildableItems()
					.FirstOrDefault(a => Info.OpeningDefenseUnitTypes.Contains(a.Name) && ShouldBuild(a.Name, false, queue.Info.Type));
				if (unit == null)
					continue;

				var cost = queue.GetProductionCost(unit);
				if (playerResources.GetCashAndResources() < cost || !baseBuilder.TryCommitOpeningDefenseCost(cost))
					return;

				SetUnitInterval(unit.Name);
				bot.QueueOrder(Order.StartProduction(queue.Actor, unit.Name, 1));
				NotifyStarted(unit.Name, queue.Info.Type);
				AIUtils.BotDebug("AI: {0} decided to build {1} from the opening defense budget.", player, unit.Name);
				return;
			}
		}

		// Record-only diagnostics seam (WBL-P1): resolved lazily from the world actor like the
		// placement observer — absent observers keep the production path allocation-free.
		IBotUnitProductionObserver[] UnitObservers =>
			unitObservers ??= world.WorldActor.TraitsImplementing<IBotUnitProductionObserver>().ToArray();

		void NotifyStarted(string name, string queueType)
		{
			foreach (var observer in UnitObservers)
				observer.UnitProductionStarted(player, world.WorldTick, name, queueType);
		}

		// Record-only gate view (WBL-P1): the poll reads enforced truth from the same
		// fields this module's own gates consult, so it never approximates internals.
		// baseBuilder's pause vote DOES starve every ordinary unit queue (the module
		// only exempts its opening-defense path inside BotTick), so it stays a voter.
		int IBotUnitProductionGateInfo.UnitProductionCashFloor =>
			BotPersonalityLeads.Scaled(Info.ProductionMinCashRequirement,
				BotPersonalityLeads.Lean(leadProviders, "steamroller"));

		int IBotUnitProductionGateInfo.UnitProductionSaturationCash => BankSaturationThreshold;

		string[] IBotUnitProductionGateInfo.UnitProductionReservedBy =>
			requestPause == null
				? []
				: requestPause
					.Where(rp => rp.IsTraitEnabled() && rp.PauseUnitProduction)
					.Select(rp => rp.GetType().Name)
					.OrderBy(n => n, StringComparer.Ordinal)
					.ToArray();

		bool IBotUnitProductionGateInfo.UnitProductionLimited(string actor, int owned, int queued) =>
			Info.UnitLimits != null && Info.UnitLimits.TryGetValue(actor, out var limit) && owned >= limit;

		void IBotRequestUnitProduction.RequestUnitProduction(IBot bot, string requestedActor)
		{
			foreach (var observer in UnitObservers)
				observer.UnitProductionRequested(player, world.WorldTick, requestedActor);
			queuedBuildRequests.Add(requestedActor);
		}

		int IBotRequestUnitProduction.RequestedProductionCount(IBot bot, string requestedActor)
		{
			return queuedBuildRequests.Count(r => r == requestedActor);
		}

		void BuildUnit(IBot bot, string category, bool buildRandom, bool excludeLimited, bool saturated = false,
			ProducerServiceBatch batch = null)
		{
			// For queues that support parallel production (e.g. Zerg hatchery), find one with a free slot.
			// For standard queues, require the queue to be completely empty.
			// Cameo (§12.21, PP-1): an enabled IBotProductionWidth provider lifts the one-queue-per-call
			// cap — three war factories produce in parallel instead of feeding one and idling the rest.
			// No provider = first free queue only, byte-identical to upstream; classic mounts none.
			productionWidth ??= player.PlayerActor.TraitsImplementing<IBotProductionWidth>().FirstEnabledTraitOrDefault();
			var maxQueues = ProductionSaturationCA.QueuesPerCategory(saturated, productionWidth?.MaxQueuesPerCategory ?? 0);

			// U1 (service-contract §3): under the switch the category is served by the fair
			// path — rotation cursor, capability-matched slots, pre-pick admissibility and
			// the shared ephemeral batch reservations. Flag off runs the legacy loop
			// byte-identically (batch is null and unused).
			if (FairService)
			{
				BuildUnitFairService(bot, category, buildRandom, excludeLimited, saturated, maxQueues, batch);
				return;
			}

			var queues = AIUtils.FindQueues(player, category)
				.Where(q =>
				{
					if (q is IHasParallelQueueSlots p)
						return p.AvailableSlots > 0;
					return !q.AllQueued().Any();
				})
				.Take(maxQueues);

			foreach (var queue in queues)
			{
				// Fill all available parallel slots in one pass so that every larva stays occupied.
				var slotsToFill = (queue is IHasParallelQueueSlots parallelQueue)
					? parallelQueue.AvailableSlots
					: 1;

				for (var slot = 0; slot < slotsToFill; slot++)
				{
					// Saturation lapses mid-fill: stop slotting the moment the bank drops below the bar.
					if (saturated && !ProductionSaturationCA.Saturated(
						playerResources.Cash + playerResources.Resources, BankSaturationThreshold))
						break;

					var unit = buildRandom ?
						ChooseRandomUnitToBuild(queue, excludeLimited) :
						ChooseUnitToBuild(queue, excludeLimited, saturated);

					if (unit == null)
					{
						if (activeComposition != null && CompositionAppliesToCategory(activeComposition, queue.Info.Type))
							RevertToBaselineComposition();

						break;
					}

					var name = unit.Name;

					if (!ShouldBuild(name, false, queue.Info.Type))
					{
						if (!excludeLimited)
						{
							BuildUnit(bot, category, buildRandom, true, saturated);
							return;
						}

						break;
					}

					SetUnitInterval(name);
					bot.QueueOrder(Order.StartProduction(queue.Actor, name, 1));
					NotifyStarted(name, queue.Info.Type);
					counters.Record(unit);
					if (activeComposition != null && CompositionAppliesToCategory(activeComposition, queue.Info.Type))
						AddToActiveCompositionProducedValue(unit);
				}
			}
		}

		// U1 (service-contract §3): bounded fair producer service for one category visit.
		// Queues are visited in stable key order resuming AFTER the last examined producer
		// (busy producers advance the cursor without consuming an offer); a slot is filled
		// only from candidates admissible BEFORE the pick — membership, delays, intervals,
		// limits, capturer cap and aircraft admissibility — so a rejected candidate leaves
		// the retry set alone rather than excluding every limited name. Emitted orders hold
		// ephemeral reservations on the shared batch — slots, bank, cap headroom — which is
		// why an unaffordable pick retries the remaining (cheaper) candidates instead of
		// ending the fill. An active composition that yields no admissible unit falls back
		// to the declared baseline shares once per queue before reverting.
		void BuildUnitFairService(IBot bot, string category, bool buildRandom, bool excludeLimited, bool saturated, int maxQueues,
			ProducerServiceBatch batch)
		{
			var queues = AIUtils.FindQueues(player, category)
				.OrderBy(q => q.Actor.ActorID)
				.ThenBy(q => q.Info.Type)
				.ToList();
			if (queues.Count == 0)
				return;

			// Sol F3: same-actor/same-type siblings are distinct producers — the nth
			// occurrence suffix keeps service keyed per queue instance while a missing
			// or mismatched cursor still resumes at the documented first key.
			var keys = queues
				.GroupBy(q => (q.Actor.ActorID, q.Info.Type))
				.SelectMany(g => g.Select((q, i) => QueueServiceKey(q, i)))
				.ToList();
			var start = UnitProducerServiceCA.ResumeIndex(keys, serviceCursorByCategory.GetValueOrDefault(category));
			var offers = UnitProducerServiceCA.OfferBudget(queues.Count, maxQueues);

			// P4: armed-infantry floor — in-pass emissions count toward it so one batch
			// cannot overshoot; the census is one tick-cached scan, skipped when off.
			var armedInfantryFloor = Info.CombatInfantryFloor;
			var armedInfantryAlive = 0;
			if (armedInfantryFloor > 0)
				CapturerArmyCounts(out _, out armedInfantryAlive);

			for (var k = 0; k < queues.Count && offers > 0; k++)
			{
				var index = (start + k) % queues.Count;
				var queue = queues[index];

				// The cursor tracks every examined producer — successful or not — but never
				// a producer the budget stopped us from reaching; that one stays in rotation.
				serviceCursorByCategory[category] = keys[index];

				var freeSlots = LiveFreeSlots(queue);
				if (freeSlots <= 0)
					continue;

				offers--;
				var skip = new HashSet<string>(StringComparer.Ordinal);
				var shares = GetUnitsToBuildForCategory(queue.Info.Type);
				var baselineTried = false;

				while (UnitProducerServiceCA.SlotAvailable(freeSlots, batch.PendingOf(queue)))
				{
					// Saturation lapses mid-fill: stop slotting the moment the bank drops below
					// the bar. The reserved bank counts — emitted orders are spent cash.
					if (saturated && !ProductionSaturationCA.Saturated(batch.BankLeft, BankSaturationThreshold))
						break;

					// P4: below the armed-infantry floor this queue prefers the best
					// admissible armed infantry it can serve — the normal pick only runs
					// when none is left (a rejected floor pick is retried next-best first).
					var fromFloor = armedInfantryFloor > 0 &&
						armedInfantryAlive + batch.PendingArmedInfantry < armedInfantryFloor
						? PickArmedInfantry(queue, shares, skip)
						: null;
					var unit = fromFloor ?? (buildRandom
						? ChooseRandomUnitToBuild(queue, excludeLimited, shares, skip)
						: ChooseUnitToBuild(queue, excludeLimited, saturated, shares, skip));

					// §3.3/P4: an active composition that provides no feasible unit for this
					// category falls back to the declared baseline shares once — then to the
					// existing revert path if the baseline is also empty.
					if (unit == null && !baselineTried &&
						activeComposition != null && CompositionAppliesToCategory(activeComposition, queue.Info.Type) &&
						Info.UnitsToBuild != null && Info.UnitsToBuild.Count > 0)
					{
						baselineTried = true;
						shares = Info.UnitsToBuild;
						AIUtils.BotDebug("AI: {0} composition {1} has no feasible unit for {2}; falling back to baseline shares",
							player, activeComposition.Id, queue.Info.Type);
						continue;
					}

					if (unit == null)
					{
						if (activeComposition != null && CompositionAppliesToCategory(activeComposition, queue.Info.Type))
							RevertToBaselineComposition();

						break;
					}

					var name = unit.Name;

					// Rejections drop only this name from the retry set; a floor-demand
					// rejection is recorded so barracks idle-while-demanded is measurable.
					void Reject()
					{
						skip.Add(name);
						if (fromFloor != null)
							foreach (var observer in UnitObservers)
								observer.UnitProductionSkipped(player, world.WorldTick, name, "combat_floor");
					}

					if (!ShouldBuild(name, baselineTried, queue.Info.Type))
					{
						Reject();
						continue;
					}

					// In-pass cap headroom: UnitLimits counts only living actors, so an order
					// emitted seconds ago is invisible to ShouldBuild — the shared batch
					// spends the headroom the census cannot see.
					if (!LimitHeadroomAdmits(batch, name))
					{
						Reject();
						continue;
					}

					if (!CapturerCapAdmitsWithPending(unit, batch))
					{
						Reject();
						continue;
					}

					var isAircraft = unit.HasTraitInfo<AircraftInfo>();
					var isAirToAir = isAircraft && Info.AirToAirUnits.Contains(name);

					// The shared class-delta seam (ECAFF220): same calculation the external
					// path uses — one batch, one headroom, one admission rule.
					var aircraftPending = UnitProducerServiceCA.PendingAircraftDelta(isAircraft, isAirToAir,
						Info.MaintainAirSuperiority, batch);
					if (!CanBuildMoreOfAircraft(unit, aircraftPending))
					{
						Reject();
						continue;
					}

					// Sol F2: an unaffordable pick retries the remaining candidates — the
					// batch keeps the bank accounting so a cheaper feasible one can land.
					var cost = Math.Max(0, queue.GetProductionCost(unit));
					if (!batch.Affords(cost))
					{
						Reject();
						continue;
					}

					SetUnitInterval(name);
					bot.QueueOrder(Order.StartProduction(queue.Actor, name, 1));
					NotifyStarted(name, queue.Info.Type);
					counters.Record(unit);
					batch.Reserve(queue, cost);
					SpendLimitHeadroom(batch, name);
					if (isAircraft)
					{
						batch.PendingAircraft++;
						if (isAirToAir)
							batch.PendingAirToAir++;
					}
					if (armedInfantryFloor > 0 && IsArmedInfantry(unit))
						batch.PendingArmedInfantry++;
					if (unit.HasTraitInfo<CapturesInfo>())
						batch.PendingCapturers++;
					if (activeComposition != null && CompositionAppliesToCategory(activeComposition, queue.Info.Type))
						AddToActiveCompositionProducedValue(unit);
				}
			}
		}

		static int LiveFreeSlots(ProductionQueue queue) =>
			queue is IHasParallelQueueSlots p ? p.AvailableSlots : (queue.AllQueued().Any() ? 0 : 1);

		// U1: per-name cap headroom shared across the batch — UnitLimits counts only
		// living actors; in-pass emissions spend the headroom the census cannot see.
		// Scope note (Sol ECAFF220): this preserves the legacy owned-alive census
		// semantics — pre-existing queued items are NOT counted here (the same blind
		// spot ShouldBuild always had); the map adds only this batch's emissions on
		// top of that census. A total committed-unit ceiling (live + queued + pending)
		// is a different cap definition and is NOT claimed by this increment.
		bool LimitHeadroomAdmits(ProducerServiceBatch batch, string name)
		{
			if (batch == null || Info.UnitLimits == null || !Info.UnitLimits.TryGetValue(name, out var limit))
				return true;

			if (!batch.LimitHeadroom.TryGetValue(name, out var headroom))
			{
				headroom = limit - world.Actors.Count(a => !a.IsDead && a.Owner == player && a.Info.Name == name);
				batch.LimitHeadroom[name] = headroom;
			}

			return headroom > 0;
		}

		static void SpendLimitHeadroom(ProducerServiceBatch batch, string name)
		{
			if (batch != null && batch.LimitHeadroom.ContainsKey(name))
				batch.LimitHeadroom[name]--;
		}

		// U1: the capturer cap counts queued capturers — an emitted order has not reached
		// AllQueued yet, so the batch's pending capturers join the live count. Identical
		// to ShouldBuild's admit call except for the pending delta.
		bool CapturerCapAdmitsWithPending(ActorInfo candidate, ProducerServiceBatch batch)
		{
			if (!CapturerCapActive(CapturerLimitPct) || !candidate.HasTraitInfo<CapturesInfo>())
				return true;

			CapturerArmyCounts(out var capturerUnits, out var armedInfantryUnits);
			return CapturerCapAdmits(CapturerLimitPct, capturerUnits, armedInfantryUnits,
				Info.CapturerMinimumUnits, Info.CapturerAllowanceMax, CapturerAllowance(),
				QueuedCapturers() + (batch?.PendingCapturers ?? 0),
				TicksSinceLastCapturerCompletion(), EngineerCompletionInterval);
		}

		// Sol F3: the nth same-actor/same-type sibling gets an ordinal suffix so service is
		// keyed per queue instance. A stored key that no longer matches (reload, replaced
		// producer, rebounded ID) resumes at the documented first key via ResumeIndex.
		static string QueueServiceKey(ProductionQueue queue, int ordinal) =>
			ordinal == 0
				? $"{queue.Actor.ActorID}|{queue.Info.Type}"
				: $"{queue.Actor.ActorID}|{queue.Info.Type}|{ordinal}";

		// P4: the best armed-infantry candidate this queue can serve from the current
		// shares — combat value per production cost, ordinal-name tiebreak. The caller's
		// gates (ShouldBuild, limits, aircraft cap, cost) still decide admissibility;
		// this only orders the preference.
		ActorInfo PickArmedInfantry(ProductionQueue queue, Dictionary<string, int> shares, HashSet<string> skip)
		{
			if (shares == null || shares.Count == 0)
				return null;

			var items = queue.BuildableItems();
			var pickName = UnitProducerServiceCA.CombatValueOrder(items
				.Where(b => shares.ContainsKey(b.Name) && IsArmedInfantry(b) && !skip.Contains(b.Name))
				.Select(b => (b.Name, DerivedUnitStrength(b.Name), Math.Max(1, queue.GetProductionCost(b)))))
				.FirstOrDefault();
			return pickName == null ? null : items.First(b => b.Name == pickName);
		}

		// In cases where we want to build a specific unit but don't know the queue name (because there's more than one possibility)
		// U1: returns true only when a StartProduction order was emitted — the fair-service
		// caller keeps a blocked request queued; legacy callers ignore the result. The
		// batch is the shared ephemeral reservation ledger (Sol F1): slot, cash and
		// headroom accounting join the category visits of the same decision pass.
		bool BuildUnit(IBot bot, string name, ProducerServiceBatch batch = null)
		{
			// Actors[] throws KeyNotFoundException for names absent from the ruleset —
			// reachable when a queued request survives a save whose version renamed the actor.
			if (!world.Map.Rules.Actors.TryGetValue(name, out var actorInfo))
				return false;

			// Upstream iterates every Buildable trait — an actor with several (alternate
			// queue sets) would otherwise have requests routed only through the first.
			foreach (var buildableInfo in actorInfo.TraitInfos<BuildableInfo>())
			{
				if (!ShouldBuild(name, true))
				{
					foreach (var observer in UnitObservers)
						observer.UnitProductionSkipped(player, world.WorldTick, name, "should_build");
					return false;
				}

				ProductionQueue queue = null;
				if (FairService)
				{
					// U1 (service-contract §4): match capability AND a free slot AND the
					// shared batch's reservations together — the legacy first-empty queue
					// can be an incapable producer, and a slot or the bank this batch
					// already spent must not be spent twice.
					queue = buildableInfo.Queue
						.SelectMany(pq => AIUtils.FindQueues(player, pq))
						.Where(q => q.BuildableItems().Any(b => b.Name == name))
						.FirstOrDefault(q =>
							batch.SlotsFree(q, LiveFreeSlots(q)) > 0 &&
							batch.Affords(q.GetProductionCost(actorInfo)));
				}
				else
				{
					foreach (var pq in buildableInfo.Queue)
					{
						queue = AIUtils.FindQueues(player, pq).FirstOrDefault(q => !q.AllQueued().Any());
						if (queue != null)
							break;
					}
				}

				var admit = queue != null && queue.BuildableItems().Any(b => b.Name == name);
				if (admit && FairService)
				{
					// Sol ECAFF220: the external request must pass the SAME ordinary
					// aircraft admission BEFORE emit — ShouldBuild does not air-check, so
					// a capped external aircraft (or two external names sharing one
					// headroom unit) would otherwise still emit.
					admit = LimitHeadroomAdmits(batch, name)
						&& CapturerCapAdmitsWithPending(actorInfo, batch)
						&& CanBuildMoreOfAircraft(actorInfo, UnitProducerServiceCA.PendingAircraftDelta(
							actorInfo.HasTraitInfo<AircraftInfo>(), Info.AirToAirUnits.Contains(name),
							Info.MaintainAirSuperiority, batch));
				}

				if (admit)
				{
					SetUnitInterval(name);
					bot.QueueOrder(Order.StartProduction(queue.Actor, name, 1));
					NotifyStarted(name, queue.Info.Type);
					AIUtils.BotDebug("AI: {0} decided to build {1} (external request)", queue.Actor.Owner, name);
					if (FairService)
					{
						batch.Reserve(queue, queue.GetProductionCost(actorInfo));
						SpendLimitHeadroom(batch, name);
						if (actorInfo.HasTraitInfo<AircraftInfo>())
						{
							batch.PendingAircraft++;
							if (Info.AirToAirUnits.Contains(name))
								batch.PendingAirToAir++;
						}
						if (IsArmedInfantry(actorInfo))
							batch.PendingArmedInfantry++;
						if (actorInfo.HasTraitInfo<CapturesInfo>())
							batch.PendingCapturers++;
					}
					return true;
				}
			}

			foreach (var observer in UnitObservers)
				observer.UnitProductionSkipped(player, world.WorldTick, name, "no_queue");
			return false;
		}

		// U1: a request that can never be served — the actor is absent from the ruleset, or
		// none of its Buildable queues is among this module's configured UnitQueues — is
		// dropped once instead of pinning the pending list. Every other failure is
		// transient (delays, intervals, limits, a missing free producer) and survives.
		bool PermanentlyInvalidBuildRequest(string name)
		{
			if (!world.Map.Rules.Actors.TryGetValue(name, out var actorInfo))
				return true;

			return !actorInfo.TraitInfos<BuildableInfo>().Any(b =>
				b.Queue != null && b.Queue.Any(pq => Info.UnitQueues.Contains(pq)));
		}

		void SetUnitInterval(string name)
		{
			if (Info.UnitIntervals == null || !Info.UnitIntervals.ContainsKey(name))
				return;

			activeUnitIntervals[name] = Info.UnitIntervals[name] * unitIntervalModifier / 100;
		}

		// TF-2: the reservation's per-tick upkeep — reconcile every producer's in-flight items so a
		// refund/cancel/elimination settles promptly, refresh the standing share's conditions at the
		// module cadence (the actor scan is the only expensive part, so it does not run per tick).
		void UpdateMobileReserve()
		{
			if (Info.ReserveMobileProductionShare <= 0)
				return;

			var queues = Info.UnitQueues.SelectMany(c => AIUtils.FindQueues(player, c)).Distinct().ToList();
			var feasible = queues.Any(q => q.Enabled);
			var capacity = queues.Any(q => q is IHasParallelQueueSlots p ? p.AvailableSlots > 0 : !q.AllQueued().Any());
			if (world.WorldTick >= nextPoolCheckTick)
			{
				nextPoolCheckTick = world.WorldTick + FeedbackTime;
				poolNeedsReplenish = OffensivePoolNeedsReplenish();
			}

			mobileReserve.SetStanding(feasible, capacity, poolNeedsReplenish,
				Info.ReserveMobileProductionShare, playerResources.GetCashAndResources());

			// TF-2 F1: feed a LIST — same-name queued units must count per instance, a
			// name-keyed dict would collapse them. F2: after the sweep, producers this
			// enumeration can no longer see (destroyed or disabled) flush their tracked
			// items — a dead producer can never reconcile itself.
			var liveOwners = new HashSet<object>();
			foreach (var queue in queues)
			{
				liveOwners.Add(queue.Actor);
				var items = new List<(string Item, int Total, int Remaining)>();
				foreach (var i in queue.AllQueued())
					items.Add((i.Item, i.TotalCost, i.RemainingCost));
				mobileReserve.ReconcileUnits(queue.Actor, items);
			}

			mobileReserve.FlushUnseenUnits(liveOwners);
		}

		// TF-2: the eligible offensive pool is the alive combat-role mass — under the configured
		// floor it needs replenishment. No roles provider means no eligible pool at all, which is
		// the conservative "needs replenishment" answer (the flag itself is opt-in).
		bool OffensivePoolNeedsReplenish()
		{
			unitRoles ??= player.PlayerActor.TraitsImplementing<IBotUnitRoles>().FirstEnabledTraitOrDefault();
			if (unitRoles == null)
				return true;

			var alive = 0;
			foreach (var a in world.Actors)
				if (!a.IsDead && a.Owner == player && OffensiveRoles.Contains(unitRoles.PrimaryRoleOf(a.Info.Name) ?? ""))
					alive++;

			return alive < Info.ReserveArmyFloor;
		}

		int IBotMobileSpendReserve.MobileSpendFloor =>
			Info.ReserveMobileProductionShare > 0 && playerResources != null
				? mobileReserve.Floor(playerResources.GetCashAndResources()) : 0;

		int IBotMobileSpendReserve.PaidUnitSharePct => mobileReserve.PaidSharePct();

		void IBotMobileSpendReserve.ReconcileUnitSpend(object owner, IReadOnlyList<(string Item, int Total, int Remaining)> items) =>
			mobileReserve.ReconcileUnits(owner, items);

		void IBotMobileSpendReserve.ReconcileBuildingSpend(object owner, IReadOnlyList<(string Item, int Total, int Remaining, bool Essential)> items) =>
			mobileReserve.ReconcileBuildings(owner, items);

		void IBotMobileSpendReserve.FlushUnitSpendOwners(IReadOnlyCollection<object> liveOwners) =>
			mobileReserve.FlushUnseenUnits(liveOwners);

		void IBotMobileSpendReserve.FlushBuildingSpendOwners(IReadOnlyCollection<object> liveOwners) =>
			mobileReserve.FlushUnseenBuildings(liveOwners);

		bool ShouldBuild(string name, bool ignoreUnitsToBuild, string queueCategory = null)
		{
			var unitsToBuildShares = GetUnitsToBuildForCategory(queueCategory);
			if (!ignoreUnitsToBuild && unitsToBuildShares != null && !unitsToBuildShares.ContainsKey(name))
				return false;

			if (Info.UnitDelays != null &&
				Info.UnitDelays.ContainsKey(name) &&
				Info.UnitDelays[name] * unitDelayModifier / 100 > world.WorldTick)
				return false;

			if (Info.UnitIntervals != null &&
				Info.UnitIntervals.ContainsKey(name) &&
				activeUnitIntervals.ContainsKey(name))
				return false;

			if (Info.UnitLimits != null &&
				Info.UnitLimits.ContainsKey(name) &&
				world.Actors.Count(a => !a.IsDead && a.Owner == player && a.Info.Name == name) >= Info.UnitLimits[name])
				return false;

			// S1/F1: the capturer ratio — counts only while the cap is on AND the candidate
			// IS a capturer; the off cap (classic: CapturerLimitPct reads 0) short-circuits
			// before both the ruleset lookup and the army scan — no wasted work.
			if (CapturerCapActive(CapturerLimitPct) &&
				world.Map.Rules.Actors.TryGetValue(name, out var candidate) &&
				candidate.HasTraitInfo<CapturesInfo>())
			{
				CapturerArmyCounts(out var capturerUnits, out var armedInfantryUnits);
				if (!CapturerCapAdmits(CapturerLimitPct, capturerUnits, armedInfantryUnits, Info.CapturerMinimumUnits,
						Info.CapturerAllowanceMax, CapturerAllowance(), QueuedCapturers(),
						TicksSinceLastCapturerCompletion(), EngineerCompletionInterval))
					return false;
			}

			return true;
		}

		ActorInfo ChooseRandomUnitToBuild(ProductionQueue queue, bool excludeLimited,
			Dictionary<string, int> shares = null, HashSet<string> retrySkip = null)
		{
			var unitsToBuildShares = shares ?? GetUnitsToBuildForCategory(queue.Info.Type);
			if (unitsToBuildShares == null || unitsToBuildShares.Count == 0)
				return null;

			var buildableThings = queue.BuildableItems().Where(a => unitsToBuildShares.ContainsKey(a.Name) &&
				(!excludeLimited || Info.UnitLimits == null || !Info.UnitLimits.ContainsKey(a.Name)) &&
				(retrySkip == null || !retrySkip.Contains(a.Name)));
			if (!buildableThings.Any())
				return null;

			var counter = ChooseCounter(buildableThings);
			if (counter != null)
				return counter;

			var deficit = ChooseRoleDeficit(buildableThings, unitsToBuildShares, excludeLimited, retrySkip);
			if (deficit != null)
				return deficit;

			var weights = ActiveProductionWeights();
			var unit = weights.Count > 0 ? ChooseWeighted(buildableThings.ToList(), weights) : buildableThings.Random(BotRng.For(player, nameof(UnitBuilderBotModuleCA)));
			return CanBuildMoreOfAircraft(unit) ? unit : null;
		}

		// The ACTIVE IBotProductionWeight providers (Cameo's learned priors, genericbot only, off by default). Resolved
		// from the player actor on each use: their conditions settle after Created, so an empty set is never cached.
		// None active = the unit builder's original path, random draws included.
		List<IBotProductionWeight> ActiveProductionWeights() =>
			player.PlayerActor.TraitsImplementing<IBotProductionWeight>().Where(p => p.IsActive).ToList();

		// Product of the providers' factors, in percent (100 = neutral).
		int LearnedWeightPercent(ActorInfo unit, List<IBotProductionWeight> providers)
		{
			var weight = 100L;
			foreach (var provider in providers)
				weight = weight * provider.WeightPercent(player, unit) / 100;

			return (int)Math.Max(weight, 0);
		}

		ActorInfo ChooseWeighted(List<ActorInfo> candidates, List<IBotProductionWeight> providers)
		{
			var weights = candidates.Select(c => LearnedWeightPercent(c, providers)).ToList();
			var total = weights.Sum();
			if (total <= 0)
				return candidates.Random(BotRng.For(player, nameof(UnitBuilderBotModuleCA)));

			var roll = BotRng.For(player, nameof(UnitBuilderBotModuleCA)).Next(total);
			for (var i = 0; i < candidates.Count; i++)
			{
				roll -= weights[i];
				if (roll < 0)
					return candidates[i];
			}

			return candidates[^1];
		}

		ActorInfo ChooseUnitToBuild(ProductionQueue queue, bool excludeLimited, bool saturated = false,
			Dictionary<string, int> shares = null, HashSet<string> retrySkip = null)
		{
			var buildableThings = queue.BuildableItems();
			if (!buildableThings.Any())
				return null;

			var unitsToBuildShares = shares ?? GetUnitsToBuildForCategory(queue.Info.Type);
			if (unitsToBuildShares == null || unitsToBuildShares.Count == 0)
				return null;

			var counter = ChooseCounter(buildableThings.Where(b => unitsToBuildShares.ContainsKey(b.Name) &&
				(!excludeLimited || Info.UnitLimits == null || !Info.UnitLimits.ContainsKey(b.Name)) &&
				(retrySkip == null || !retrySkip.Contains(b.Name))));
			if (counter != null)
				return counter;

			var deficit = ChooseRoleDeficit(buildableThings, unitsToBuildShares, excludeLimited, retrySkip);
			if (deficit != null)
				return deficit;

			var myUnits = player.World
				.ActorsHavingTrait<IPositionable>()
				.Where(a => a.Owner == player)
				.Select(a => a.Info.Name).ToList();

			foreach (var unit in unitsToBuildShares.OrderBy(kv => kv.Key).Shuffle(BotRng.For(player, nameof(UnitBuilderBotModuleCA))))
				if (retrySkip == null || !retrySkip.Contains(unit.Key))
					if (buildableThings.Any(b => b.Name == unit.Key))
						if (!excludeLimited || Info.UnitLimits == null || !Info.UnitLimits.ContainsKey(unit.Key))
							if (myUnits.Count(a => a == unit.Key) * 100 < ShareFor(unit.Key, unit.Value) * myUnits.Count)
								if (CanBuildMoreOfAircraft(world.Map.Rules.Actors[unit.Key]))
									return world.Map.Rules.Actors[unit.Key];

			// BV_m13_liveness: with a saturated bank the share test alone must not idle the queue —
			// one deterministic (no extra RNG) retry that skips only the proportional-share check.
			if (saturated)
			{
				var fallback = ProductionSaturationCA.PickExcludingShare(
					unitsToBuildShares.OrderBy(kv => kv.Key).Select(kv => kv.Key),
					name => buildableThings.Any(b => b.Name == name),
					name => (!excludeLimited || Info.UnitLimits == null || !Info.UnitLimits.ContainsKey(name))
						&& (retrySkip == null || !retrySkip.Contains(name))
						&& ShouldBuild(name, false, queue.Info.Type)
						&& CanBuildMoreOfAircraft(world.Map.Rules.Actors[name]));

				if (fallback != null)
					return world.Map.Rules.Actors[fallback];
			}

			return null;
		}

		int ShareFor(string name, int share)
		{
			var providers = ActiveProductionWeights();
			return providers.Count > 0 ? share * LearnedWeightPercent(world.Map.Rules.Actors[name], providers) / 100 : share;
		}

		// CA-3 (AI_ARCHITECTURE.md 12.5): the enabled personality's RoleMix is a target
		// army composition by role, in percent of own mobile combat units. Each pick
		// fills the largest deficit — counter production still gets first refusal, and
		// a fully satisfied mix falls through to the proportional pick. Roles the mix
		// does not name get RoleMixRoleFloorPct whenever this queue can serve them.
		// The provider resolves lazily: BotUnitRoles is a genericbot-gated
		// ConditionalTrait whose condition settles after Created.
		ActorInfo ChooseRoleDeficit(IEnumerable<ActorInfo> buildableThings, Dictionary<string, int> unitsToBuildShares, bool excludeLimited,
			HashSet<string> retrySkip = null)
		{
			var manager = player.PlayerActor.TraitsImplementing<SquadManagerBotModuleCA>().FirstEnabledTraitOrDefault();
			var mix = manager?.Info.UseRoleMix == true ? manager.Info.RoleMix : null;
			if (mix == null || mix.Count == 0)
				return null;

			var roles = unitRoles ??= player.PlayerActor.TraitsImplementing<IBotUnitRoles>().FirstEnabledTraitOrDefault();
			if (roles == null)
				return null;

			var counts = new Dictionary<string, int>(StringComparer.Ordinal);
			var total = 0;
			foreach (var a in world.ActorsHavingTrait<IPositionable>())
			{
				if (a.IsDead || a.Owner != player)
					continue;

				var primary = roles.PrimaryRoleOf(a.Info.Name);
				if (primary == null)
					continue;

				counts[primary] = counts.GetValueOrDefault(primary) + 1;
				total++;
			}

			// The deficit domain is the combat taxonomy: list/doctrine roles (guerrilla,
			// firesupport, navalunit, ...) and the building target tags never become
			// mix dimensions — a unit counts toward exactly one primary role.
			foreach (var role in mix.Keys
				.Concat(roles.RoleMembers.Keys)
				.Where(r => BotUnitRole.CombatRoles.Contains(r))
				.Distinct(StringComparer.Ordinal)
				.Select(r => (Role: r, Target: mix.TryGetValue(r, out var t) ? t : manager.Info.RoleMixRoleFloorPct))
				.Where(rt => rt.Target > 0)
				.OrderByDescending(rt => rt.Target - 100.0 * counts.GetValueOrDefault(rt.Role) / Math.Max(1, total)))
			{
				var members = roles.RoleMembers.GetValueOrDefault(role.Role);
				if (members == null || members.Count == 0)
					continue;

				var options = buildableThings.Where(b => members.Contains(b.Name) &&
					unitsToBuildShares.ContainsKey(b.Name) &&
					(!excludeLimited || Info.UnitLimits == null || !Info.UnitLimits.ContainsKey(b.Name)) &&
					(retrySkip == null || !retrySkip.Contains(b.Name)) &&
					ShouldBuild(b.Name, false) && CanBuildMoreOfAircraft(b)).ToList();

				if (options.Count == 0)
					continue;

				// A multi-role unit counts toward its primary role only, so prefer
				// members that actually relieve this deficit (e.g. a dual-role Orca
				// fields as fighter, not the gunship share it also belongs to).
				var relieving = options.Where(b => roles.PrimaryRoleOf(b.Name) == role.Role).ToList();
				return (relieving.Count > 0 ? relieving : options).Random(BotRng.For(player, nameof(UnitBuilderBotModuleCA)));
			}

			return null;
		}

		// Adaptive counters (DESIGN §19.1): below this tier's share of counter picks, the best counter to the
		// observed enemy army among the units this composition allows; otherwise the normal choice.
		ActorInfo ChooseCounter(IEnumerable<ActorInfo> allowed)
		{
			counters.LastChoiceAdaptive = false;
			if (!AdaptiveCounterProduction.CounterPickAllowed(counters.AdaptiveSelections, counters.TotalSelections, CounterWeight))
				return null;

			var owned = player.World.ActorsHavingTrait<IPositionable>().Where(a => a.Owner == player)
				.GroupBy(a => a.Info.Name).ToDictionary(g => g.Key, g => g.Count());
			var choice = counters.Choose(allowed.Where(a => AdaptiveCounterProduction.IsMobileCombat(a) &&
				ShouldBuild(a.Name, false) && CanBuildMoreOfAircraft(a)), n => owned.GetValueOrDefault(n));
			counters.LastChoiceAdaptive = choice != null;
			return choice;
		}

		Dictionary<string, int> GetUnitsToBuildForCategory(string queueCategory)
		{
			var compositionWeights = compositionsModule != null && compositionsModule.UnitCompositions.Count != 0 &&
				activeComposition != null && CompositionAppliesToCategory(activeComposition, queueCategory)
					? activeComposition.UnitsToBuild : null;

			// UW-1 (AI_ARCHITECTURE.md §2.8b item 3): the inputs are resolved only when the flag
			// is on and no composition claims the category — an enabled squad manager carrying a
			// RoleMix plus an enabled IBotUnitRoles provider (genericbot-gated). Flag off skips
			// every lookup and returns the pre-change table byte-identical.
			SquadManagerBotModuleCA manager = null;
			IReadOnlyDictionary<string, int> mix = null;
			IBotUnitRoles roles = null;
			if (compositionWeights == null && Info.UseDerivedUnitWeights &&
				(manager = player.PlayerActor.TraitsImplementing<SquadManagerBotModuleCA>().FirstEnabledTraitOrDefault()) != null &&
				(mix = manager.Info.RoleMix) != null && mix.Count != 0)
				roles = unitRoles ??= player.PlayerActor.TraitsImplementing<IBotUnitRoles>().FirstEnabledTraitOrDefault();

			return derivedUnitWeights.Select(Info.UseDerivedUnitWeights, compositionWeights, Info.UnitsToBuild,
				mix, manager?.Info.RoleMixRoleFloorPct ?? 0, roles, DerivedUnitStrength);
		}

		// A candidate's strength is read off its own type profile — no enemy is ever
		// enumerated (fog-honest, DESIGN §19.5).
		double DerivedUnitStrength(string name)
		{
			return world.Map.Rules.Actors.TryGetValue(name, out var actorInfo)
				? DerivedUnitWeights.Strength(BotUnitProfiles.Get(world.Map.Rules, actorInfo, Info.UseEffectiveDamageModel),
					Info.UseEffectiveDamageModel)
				: 1.0;
		}

		void UpdateComposition()
		{
			if (compositionsModule == null || compositionsModule.UnitCompositions.Count == 0)
				return;

			if (activeComposition != null)
			{
				var exceededDuration = activeComposition.MaxDuration > 0 &&
					world.WorldTick - activeCompositionSelectedTick >= activeComposition.MaxDuration;
				var exceededValue = activeComposition.MaxProducedValue > 0 &&
					activeCompositionProducedValue >= activeComposition.MaxProducedValue;

				if (exceededDuration || exceededValue)
					RevertToBaselineComposition();
			}
			else if (world.WorldTick >= nextCompositionSelectTick)
			{
				var newActiveComposition = ChooseActiveComposition();
				if (newActiveComposition != null)
				{
					SetActiveComposition(newActiveComposition);
					activeCompositionProducedValue = 0;
					activeCompositionSelectedTick = world.WorldTick;
					if (!string.IsNullOrEmpty(activeComposition.Id))
						compositionLastUsedTickById[activeComposition.Id] = world.WorldTick;
				}
			}
		}

		void RevertToBaselineComposition()
		{
			SetActiveComposition(null);
			activeCompositionProducedValue = 0;
			nextCompositionSelectTick = GetNextCompositionSelectTick();
		}

		void SetActiveComposition(UnitComposition next)
		{
			var nextId = next?.Id ?? string.Empty;
			if (nextId == ActiveCompositionId)
				return;

			activeComposition = next;
			ActiveCompositionChanged?.Invoke(world.WorldTick, nextId);
		}

		UnitComposition ChooseActiveComposition()
		{
			if (possibleActiveCompositions == null || possibleActiveCompositions.Count == 0)
				return null;

			nextCompositionSelectTick = GetNextCompositionSelectTick();

			var playerQueues = OpenRA.Mods.Common.AIUtils.FindQueuesByCategory(player);
			var candidates = possibleActiveCompositions
				.Where(c => IsCompositionTimeValid(c)
					&& IsCompositionIntervalValid(c)
					&& AreCompositionPrerequisitesMet(c)
					&& CanProduceAnyUnitInCompositionForEachQueueCategory(c, playerQueues))
				.ToArray();

			return candidates.Length != 0 ? candidates.Random(BotRng.For(player, nameof(UnitBuilderBotModuleCA))) : null;
		}

		bool IsCompositionIntervalValid(UnitComposition composition)
		{
			if (composition.MinInterval <= 0 || string.IsNullOrEmpty(composition.Id))
				return true;

			if (!compositionLastUsedTickById.TryGetValue(composition.Id, out var lastTick))
				return true;

			return world.WorldTick - lastTick >= composition.MinInterval;
		}

		bool IsCompositionTimeValid(UnitComposition composition)
		{
			var tick = world.WorldTick;
			if (composition.MinTime > 0 && tick < composition.MinTime)
				return false;
			if (composition.MaxTime > 0 && tick > composition.MaxTime)
				return false;

			return true;
		}

		bool CanProduceAnyUnitInCompositionForQueueCategory(UnitComposition composition, string queueCategory)
		{
			if (string.IsNullOrEmpty(queueCategory))
				return false;

			if (techTree == null)
				return true;

			var byQueue = composition.UnitPrerequisitesByQueue;
			if (byQueue == null || !byQueue.TryGetValue(queueCategory, out var unitPrereqs) ||
				unitPrereqs == null || unitPrereqs.Count == 0)
				return false;

			foreach (var prereqs in unitPrereqs.Values)
				if (prereqs == null || prereqs.Length == 0 || techTree.HasPrerequisites(prereqs))
					return true;

			return false;
		}

		bool CanProduceAnyUnitInCompositionForEachQueueCategory(UnitComposition composition, ILookup<string, ProductionQueue> playerQueues)
		{
			var byQueue = composition.UnitPrerequisitesByQueue;
			if (byQueue == null || byQueue.Count == 0)
				return false;

			foreach (var queueCategory in byQueue.Keys)
			{
				if (!playerQueues.Contains(queueCategory))
					continue;

				if (!CanProduceAnyUnitInCompositionForQueueCategory(composition, queueCategory))
					return false;
			}

			return true;
		}

		bool CompositionAppliesToCategory(UnitComposition composition, string queueCategory)
		{
			if (composition.UnitQueues == null || composition.UnitQueues.Length == 0)
				return true;

			return composition.UnitQueues.Any(q => q != null && q.Equals(queueCategory, StringComparison.OrdinalIgnoreCase));
		}

		bool AreCompositionPrerequisitesMet(UnitComposition composition)
		{
			if (composition.Prerequisites == null || composition.Prerequisites.Length == 0)
				return true;

			return techTree == null || techTree.HasPrerequisites(composition.Prerequisites);
		}

		int GetNextCompositionSelectTick()
		{
			var min = Math.Max(0, Info.MinCompositionSelectInterval);
			var max = Math.Max(0, Info.MaxCompositionSelectInterval);

			if (min == 0 && max == 0)
				return int.MaxValue / 4;

			if (max < min)
				max = min;

			var interval = min == max ? min : BotRng.For(player, nameof(UnitBuilderBotModuleCA)).Next(min, max + 1);
			return world.WorldTick + interval;
		}

		void AddToActiveCompositionProducedValue(ActorInfo builtUnit)
		{
			if (activeComposition == null || compositionsModule == null || builtUnit == null)
				return;

			compositionsModule.UnitCosts.TryGetValue(builtUnit.Name, out var unitCost);
			if (unitCost <= 0)
				return;

			activeCompositionProducedValue += unitCost;
		}

		bool IBotAircraftBuilder.CanBuildMoreOfAircraft(ActorInfo actorInfo)
		{
			return CanBuildMoreOfAircraft(actorInfo);
		}

		bool CanBuildMoreOfAircraft(ActorInfo actorInfo) => CanBuildMoreOfAircraft(actorInfo, 0);

		// U1: pendingDelta = same-class aircraft emitted earlier in this decision batch —
		// an issued StartProduction has not reached AllQueued yet, so it counts here.
		bool CanBuildMoreOfAircraft(ActorInfo actorInfo, int pendingDelta)
		{
			var attackAircraftInfo = actorInfo.TraitInfoOrDefault<AircraftInfo>();
			if (attackAircraftInfo == null)
				return true;

			// Scale targets (DESIGN 19.10): an enabled provider's aircraft target replaces MaxAircraft, and scales
			// MaxAirSuperiority in the same proportion; without one both are the yaml numbers.
			var limit = Info.MaxAircraft;
			var maxAirSuperiority = Info.MaxAirSuperiority;
			scaleTargets ??= player.PlayerActor.TraitsImplementing<IBotScaleTargets>().ToArray();
			if (scaleTargets.TryTarget("aircraft", out var scaledAircraft))
			{
				if (maxAirSuperiority > 0 && Info.MaxAircraft > 0)
					maxAirSuperiority = Math.Max(1, (int)((long)maxAirSuperiority * scaledAircraft / Info.MaxAircraft));

				limit = scaledAircraft;
			}

			var currentCount = 0;
			var isAirToAir = Info.AirToAirUnits.Contains(actorInfo.Name);

			// One enumeration of our own and allied aircraft (own/allied actors are always visible: not a fog read).
			var alliedAircraft = AIUtils.GetActorsWithTrait<Aircraft>(player.World)
				.Where(a => a.Owner.RelationshipWith(player) == PlayerRelationship.Ally || a.Owner == player).ToList();
			var ownAllAircraft = alliedAircraft.Where(a => a.Owner == player).ToList();
			var ownAircraft = ownAllAircraft.Where(a => a.Info.HasTraitInfo<BuildableInfo>()).ToList();

			// CA F2p2 (71754fc06): aircraft already in a production queue count towards the limit.
			var queued = CountQueued ? QueuedAircraft(!Info.MaintainAirSuperiority || !isAirToAir) : null;

			if (Info.MaintainAirSuperiority)
			{
				var numAirToAirUnits = ownAllAircraft.Count(a => Info.AirToAirUnits.Contains(a.Info.Name));

				if (isAirToAir)
				{
					currentCount = numAirToAirUnits;
					var numFriendlyAirToAirUnits = alliedAircraft.Count(a => a.Owner.RelationshipWith(player) == PlayerRelationship.Ally && Info.AirToAirUnits.Contains(a.Info.Name));
					var numEnemyAirThreatUnits = EnemyAirThreatCount();

					if (CountQueued)
					{
						// Upstream: friendly (allied) A2A plus everything queued by the team is the current count.
						currentCount = numFriendlyAirToAirUnits + queued.Count(n => Info.AirToAirUnits.Contains(n));
					}

					limit = AirLimits.AirSuperiorityLimit(limit, numEnemyAirThreatUnits, numFriendlyAirToAirUnits, CountQueued, maxAirSuperiority);
				}
				else
				{
					currentCount = ownAircraft.Count - numAirToAirUnits;
					if (CountQueued)
						currentCount += queued.Count(n => !Info.AirToAirUnits.Contains(n));
				}
			}
			else
			{
				currentCount = ownAircraft.Count;
				if (CountQueued)
					currentCount += queued.Count;
			}

			return currentCount + pendingDelta < limit;
		}

		// Names of aircraft sitting in production queues: our own queues, plus allied queues unless `ownOnly`
		// (the air-to-air pool is shared with allies, upstream counts their queues too).
		List<string> QueuedAircraft(bool ownOnly)
		{
			var result = new List<string>();
			foreach (var p in world.Players)
			{
				if (p != player && (ownOnly || !p.IsAlliedWith(player)))
					continue;

				foreach (var group in OpenRA.Mods.Common.AIUtils.FindQueuesByCategory(p))
					foreach (var q in group)
						foreach (var item in q.AllQueued())
							if (world.Map.Rules.Actors.TryGetValue(item.Item, out var ai) && ai.HasTraitInfo<AircraftInfo>())
								result.Add(item.Item);
			}

			return result;
		}

		// CA F2p2 / DESIGN 19.5: with ObservedAirThreats the enemy air threats come from the OBSERVED enemy composition
		// (fog memory). Without a provider that observes through fog (the classic bot) - or with the switch off - the
		// legacy omniscient world count stays, so the classic A/B reference is unchanged.
		int EnemyAirThreatCount()
		{
			if (ObservedThreats && compositionProvider != null && compositionProvider.TryGetEnemyComposition(out var valueByType))
				return AirLimits.EstimateObservedThreatCount(valueByType, Info.AirThreatUnits,
					type => world.Map.Rules.Actors.TryGetValue(type, out var ai) ? ai.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0 : 0);

			return player.World.Actors.Count(a => a.Owner.RelationshipWith(player) == PlayerRelationship.Enemy && Info.AirThreatUnits.Contains(a.Info.Name));
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled)
				return null;

			var nodes = new List<MiniYamlNode>()
			{
				new("QueuedBuildRequests", FieldSaver.FormatValue(queuedBuildRequests.ToArray())),
				new("IdleUnitCount", FieldSaver.FormatValue(idleUnitCount)),
				new("ReservePaidUnitCost", FieldSaver.FormatValue(mobileReserve.PaidUnitCost)),
				new("ReservePaidOptionalCost", FieldSaver.FormatValue(mobileReserve.PaidOptionalCost)),
				new("ReservePaidEssentialCost", FieldSaver.FormatValue(mobileReserve.PaidEssentialCost)),
				new("CompositionLastUsed", "", compositionLastUsedTickById
					.Select(kvp => new MiniYamlNode(kvp.Key, FieldSaver.FormatValue(kvp.Value)))
					.ToList())
			};

			// Sol F3: the cursor node exists only when the armed path actually examined a
			// producer — flag-off saves carry no new node, keeping the blob byte-identical.
			if (serviceCursorByCategory.Count > 0)
				nodes.Add(new MiniYamlNode("ServiceCursors", "", serviceCursorByCategory
					.OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
					.Select(kvp => new MiniYamlNode(kvp.Key, kvp.Value))
					.ToList()));

			return nodes.Concat(counters.SaveNodes()).ToList();
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, MiniYaml data)
		{
			if (self.World.IsReplay)
				return;

			var queuedBuildRequestsNode = data.NodeWithKeyOrDefault("QueuedBuildRequests");
			if (queuedBuildRequestsNode != null)
			{
				queuedBuildRequests.Clear();
				queuedBuildRequests.AddRange(FieldLoader.GetValue<string[]>("QueuedBuildRequests", queuedBuildRequestsNode.Value.Value));
			}

			var idleUnitCountNode = data.NodeWithKeyOrDefault("IdleUnitCount");
			if (idleUnitCountNode != null)
				idleUnitCount = FieldLoader.GetValue<int>("IdleUnitCount", idleUnitCountNode.Value.Value);

			// TF-2: the settled paid counters persist across saves; in-flight items re-track
			// themselves on the next reconcile from live queue state.
			mobileReserve.RestoreSettled(
				ReadSaveInt(data, "ReservePaidUnitCost"),
				ReadSaveInt(data, "ReservePaidOptionalCost"),
				ReadSaveInt(data, "ReservePaidEssentialCost"));

			var compositionLastUsedNode = data.NodeWithKeyOrDefault("CompositionLastUsed");
			if (compositionLastUsedNode != null)
			{
				compositionLastUsedTickById.Clear();
				foreach (var n in compositionLastUsedNode.Value.Nodes)
					compositionLastUsedTickById[n.Key] = FieldLoader.GetValue<int>("CompositionLastUsed", n.Value.Value);
			}

			var cursorsNode = data.NodeWithKeyOrDefault("ServiceCursors");
			if (cursorsNode != null)
			{
				serviceCursorByCategory.Clear();
				foreach (var n in cursorsNode.Value.Nodes)
					serviceCursorByCategory[n.Key] = n.Value.Value;
			}

			counters.Load(data);
		}

		static int ReadSaveInt(MiniYaml data, string key)
		{
			var node = data.NodeWithKeyOrDefault(key);
			return node != null ? FieldLoader.GetValue<int>(key, node.Value.Value) : 0;
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			unitsToBuild?.Dispose();
		}
	}
}
