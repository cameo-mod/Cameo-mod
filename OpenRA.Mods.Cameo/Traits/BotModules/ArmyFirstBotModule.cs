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
using OpenRA.Mods.CA;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.Player)]
	public class ArmyFirstBotModuleInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new ArmyFirstBotModule(this, init); }

		[Desc("Cash-poor vote: hold non-essential buildings while CashAndResources is below this. Once paused, stays paused until cash "
			+ "recovers above ArmyReserveHighCash - hysteresis keeps building production from flickering every tick.")]
		public readonly int ArmyReserveLowCash = 2500;

		[Desc("Resume building production above this cash level.")]
		public readonly int ArmyReserveHighCash = 4000;

		[Desc("Army-count gate (DAWN part): while the player owns fewer combat units (own non-building actors with an attack trait) than "
			+ "this, non-essential buildings are held whenever cash is above ArmyFirstMinCash. 0 disables the gate.")]
		public readonly int MinArmyUnitsBeforeBuildings = 0;

		[Desc("Cash level above which the MinArmyUnitsBeforeBuildings gate applies.")]
		public readonly int ArmyFirstMinCash = 2000;

		[Desc("Ticks between re-evaluations of the vote.")]
		public readonly int ScanInterval = 25;

		[Desc("ARMY-FIRST-02 (user rule 11-22: every available building at least once, then army). A building type with no",
			"owned, queued or recently admitted copy passes every vote of this instance, so the votes only ever hold EXTRA copies.",
			"Off = the votes as before. Ablation row af2_first_copy.")]
		public readonly bool FirstCopyPasses = false;

		[Desc("C-04 (AUDIT_CONFIG_01): the first copy of a defence type counts toward the threat-scaled DefenceCap instead of skipping it (a first copy still passes while",
			"the cap has room). Row af2_defence_first_copy.")]
		public readonly bool DefenceFirstCopyCounts = false;

		[Desc("ARMY-FIRST-02: armed static defences (a building with an attack trait) leave the projected-share and reserve votes",
			"(defences count as army, user 11-22) and follow a threat-scaled cap instead: an extra defence is held while the bot",
			"already has this many (owned + queued + recently admitted) plus DefenceCapPerThreat x threat intensity. The first",
			"copy of each defence type passes (FirstCopyPasses); an essential building always passes. -1 = no cap, defences are",
			"voted like any building. The cap is computed at the first scan (ScanInterval): before it nothing is capped.",
			"Ablation row af2_defence_cap.")]
		public readonly int DefenceCapBase = -1;

		[Desc("Extra defences allowed at full threat intensity (IBotThreatAnalysis, the highest of the anti-air, anti-armour and",
			"anti-infantry intensities, 0..1). Rounded down.")]
		public readonly int DefenceCapPerThreat = 0;

		[Desc("Saturation of the threat intensity, in percent of the react threshold (300 = intensity 1 at 3x the threshold).")]
		public readonly int DefenceThreatSaturationPercent = 300;

		[Desc("Turtle may stay heavy on defence early to mid game only (Opus 11-23): while the bot's personality is turtle and the",
			"game is before TurtleDefenceUntilTick, the cap is scaled by this percent. 100 = no turtle bonus.")]
		public readonly int TurtleDefenceCapPercent = 100;

		[Desc("End of the turtle defence bonus (world tick).")]
		public readonly int TurtleDefenceUntilTick = 0;

		[Desc("Ticks an admitted building counts as in flight for the first-copy and defence counts (covers the order latency",
			"until the next scan sees it queued; a conservative overcount inside the window).")]
		public readonly int AdmittedHoldTicks = 50;

		[Desc("An admission older than this many ticks at a scan is in that scan's queue count already and is dropped from the",
			"defence in-flight count (orders resolve within a few ticks; the scan counts the queued item itself).")]
		public readonly int AdmittedSeenTicks = 10;

		[Desc("ABLATE-01 row that switches this whole instance off (DEV provider only; release = on). Empty = no row.")]
		public readonly string AblationRow = "";
	}

	/// <summary>Pure army-first decisions (AI_ARCHITECTURE 12.20), unit-tested without a world.</summary>
	public static class ArmyFirstEval
	{
		/// <summary>Cash-poor hysteresis; with no unit producer the vote releases entirely.</summary>
		public static bool NextPaused(bool paused, bool hasUnitProducer, int cash, int lowCash, int highCash)
		{
			if (!hasUnitProducer)
				return false;

			return paused ? cash < highCash : cash < lowCash;
		}

		/// <summary>The DAWN gate: too few combat units while cash is high enough that unit production could have used it.</summary>
		public static bool ArmyCountHolds(bool hasUnitProducer, int cash, int army, int minArmy, int minCash) =>
			minArmy > 0 && hasUnitProducer && cash > minCash && army < minArmy;

		/// <summary>Essentials (conyard, refinery, power, first factory) always pass; everything else yields to either vote.</summary>
		public static bool Holds(bool essential, bool cashPoorPaused, bool armyCountHolds) => !essential && (cashPoorPaused || armyCountHolds);

		/// <summary>Freeze only ticks spent disabled; a visit later in the transition tick has already counted.</summary>
		public static int PauseScan(int transitionTick, int lastVisitTick) => Math.Max(transitionTick, lastVisitTick + 1);

		public static void ResumeScan(ref BotCooldown cooldown, int tick, int disabledFromTick)
		{
			if (disabledFromTick >= 0)
				cooldown.Freeze(Math.Max(0, tick - disabledFromTick));
		}

		public static bool ShouldScan(ref BotCooldown cooldown, int visitTick, int interval)
		{
			if (!cooldown.Ready(visitTick))
				return false;

			cooldown.Start(visitTick, interval);
			return true;
		}

		/// <summary>
		/// PR-3 projected-share vote (PROD-IDLE_PLAN_03 section 3): only K6 orders (General class: PW, defences, walls,
		/// extra tech and support) are ratio-tested. Essentials, K4 essential buildings and K1/K2 pass untested.
		/// An unset target (negative) holds K6 closed; see <see cref="ArmyShareCA.BuildingAdmits"/>.
		/// </summary>
		public static bool ShareHolds(bool essential, RecoverySpendPolicyCA.Admission admission, ArmyShareCA share, long cost,
			int targetPermille) =>
			!essential && admission == RecoverySpendPolicyCA.Admission.General && !share.BuildingAdmits(cost, targetPermille);

		/// <summary>
		/// PR-1 production-reserve vote (PROD-IDLE_PLAN_03 section 2.2/2.3): a K6 order (General class) must also leave the
		/// tier's production reserve, C - x >= P(r), where C is the cash less every unpaid commitment counted once. M, E and R
		/// are 0 until the recovery owner is mounted. Essentials, K1-K4 and anything not General pass untested. An unset line
		/// (negative) holds General closed, like the share target.
		/// </summary>
		public static bool ReserveHolds(bool essential, RecoverySpendPolicyCA.Admission admission, long cash, long committed,
			int cost, int productionReserve) =>
			!essential && admission == RecoverySpendPolicyCA.Admission.General
			&& !RecoverySpendPolicyCA.Admits(SpendCommitmentsCA.Available(cash, committed), cost, 0, 0, 0, false,
				RecoverySpendPolicyCA.Admission.General, productionReserve);

		/// <summary>ARMY-FIRST-02 core rule: no owned, no queued and no recently admitted copy = the first copy, never held.</summary>
		public static bool IsFirstCopy(int ownedOrQueued, bool recentlyAdmitted) => ownedOrQueued <= 0 && !recentlyAdmitted;

		/// <summary>Armed static defence: a building with an attack trait (walls and unarmed support are not defences).</summary>
		public static bool IsDefence(bool isBuilding, bool hasAttack) => isBuilding && hasAttack;

		/// <summary>
		/// ARMY-FIRST-02 threat-scaled defence cap: base + floor(perThreat x intensity), intensity clamped to 0..1, then scaled by
		/// turtlePercent while the turtle bonus applies. Negative base = no cap (-1).
		/// </summary>
		public static int DefenceCap(int baseCap, int perThreat, double intensity, bool turtleBonus, int turtlePercent)
		{
			if (baseCap < 0)
				return -1;

			var cap = (long)baseCap + (long)Math.Floor(Math.Max(0, perThreat) * Math.Clamp(intensity, 0.0, 1.0));
			if (turtleBonus)
				cap = cap * Math.Max(0, turtlePercent) / 100;

			return (int)Math.Min(int.MaxValue, cap);
		}

		/// <summary>Hold an extra defence while the count is at or over the cap; no cap (negative) never holds.</summary>
		public static bool DefenceCapHolds(int cap, int defences) => cap >= 0 && defences >= cap;

		/// <summary>
		/// ARMY-FIRST-02 decision order for one building order. First copies pass (when enabled); defences follow the cap and skip
		/// the stock votes (when the cap is on); everything else goes to the existing votes (<paramref name="votesHold"/>).
		/// </summary>
		public static bool Af02Holds(bool firstCopyOn, bool isFirstCopy, bool capOn, bool isDefence, bool capHolds, bool votesHold)
		{
			if (firstCopyOn && isFirstCopy)
				return false;

			if (capOn && isDefence)
				return capHolds;

			return votesHold;
		}
	}

	/// <summary>
	/// The ONE army-first owner (AI_ARCHITECTURE 12.20, merged 2026-10-02). While the bot is cash-poor, or while its army is
	/// below MinArmyUnitsBeforeBuildings with cash to spare, unit production gets the money before optional buildings do - a
	/// base that keeps spawning structures it cannot defend loses the units that were supposed to protect it. The base builder
	/// classifies essentials (construction yard, refinery, power, first production building) and they always pass; the vote
	/// releases entirely while the bot owns no unit-producing structure, so early-tech sequencing and rebuilding a wiped
	/// production chain can never be starved out by its own guard. Counts own units only (fog honest).
	/// </summary>
	public class ArmyFirstBotModule : ConditionalTrait<ArmyFirstBotModuleInfo>, IBotTick, IBotRequestPauseBuildingProduction
	{
		readonly ArmyFirstBotModuleInfo info;
		readonly ArmyShareCA share = new();
		PlayerResources playerResources;
		OpenRA.Player player;
		AblationLatch ablateArmyRatio; // ABLATE-01 hook pi_army_ratio (DEV provider only; release = on)
		AblationLatch ablateRecovery; // ABLATE-01 hook pi_recovery: the production-reserve term of the General gate
		int committedTick = -1;
		long committedQueued;
		long recordedThisTick;
		BotLimits botLimits;
		IBotPersonalityLeadProvider[] leadProviders;
		bool paused;
		bool hasUnitProducer;
		int armyUnits;
		BotCooldown scanCooldown;
		int disabledFromTick = -1;
		int lastBotTick;
		bool scanStarted;

		// ARMY-FIRST-02 state, refreshed with the share scan (owned + queued per type, defences) plus admissions since.
		AblationLatch ablateInstance; // the instance row (Info.AblationRow)
		AblationLatch ablateFirstCopy; // af2_first_copy
		AblationLatch ablateDefenceFirstCopy; // af2_defence_first_copy
		AblationLatch ablateDefenceCap; // af2_defence_cap
		readonly Dictionary<string, int> typeCounts = new();
		readonly Dictionary<string, int> admittedAt = new();
		readonly List<int> admittedDefenceTicks = new();
		int scannedDefences;
		int defenceCap = -1;
		IBotThreatAnalysis threat;
		BotPersonalityController personality;

		public ArmyFirstBotModule(ArmyFirstBotModuleInfo info, ActorInitializer init)
			: base(info)
		{
			this.info = info;
		}

		// T(r) in permille from the tier line (500 + 25 r); -1 = unset, which holds K6 closed.
		int ShareTarget => botLimits != null ? botLimits.Info.ArmyShareTargetPermille : -1;

		bool IBotRequestPauseBuildingProduction.PausesBuilding(ActorInfo building, bool essential,
			RecoverySpendPolicyCA.Admission admission, int cost)
		{
			if (IsTraitDisabled || playerResources == null)
				return false;

			if (!string.IsNullOrEmpty(info.AblationRow) && ablateInstance.Off(player, info.AblationRow))
				return false;

			var firstCopyOn = info.FirstCopyPasses && !ablateFirstCopy.Off(player, AblationIds.Af2FirstCopy);
			var capOn = info.DefenceCapBase >= 0 && !ablateDefenceCap.Off(player, AblationIds.Af2DefenceCap);
			if (firstCopyOn || capOn)
			{
				var tick = player.World.WorldTick;
				var isFirstCopy = firstCopyOn && ArmyFirstEval.IsFirstCopy(typeCounts.GetValueOrDefault(building.Name),
					admittedAt.TryGetValue(building.Name, out var at) && tick - at < info.AdmittedHoldTicks);
				var isDefence = ArmyFirstEval.IsDefence(building.HasTraitInfo<BuildingInfo>(), building.HasTraitInfo<AttackBaseInfo>());
				if (isFirstCopy && isDefence && info.DefenceFirstCopyCounts && !ablateDefenceFirstCopy.Off(player, AblationIds.Af2DefenceFirstCopy))
					isFirstCopy = false; // C-04: counted toward the cap below, no longer exempt from it
				if (firstCopyOn && isFirstCopy || capOn && isDefence)
				{
					var capHolds = capOn && isDefence && !essential && ArmyFirstEval.DefenceCapHolds(defenceCap, DefenceCount(tick));
					var af2Holds = ArmyFirstEval.Af02Holds(firstCopyOn, isFirstCopy, capOn, isDefence, capHolds, false);
#if CAMEO_DEV_LOGGING
					CountAf2(building, isFirstCopy && firstCopyOn ? "first_copy" : af2Holds ? "cap_hold" : "cap_admit", tick);
#endif
					return af2Holds;
				}
			}

			var armyHolds = ArmyFirstEval.ArmyCountHolds(hasUnitProducer, playerResources.GetCashAndResources(), armyUnits,
				info.MinArmyUnitsBeforeBuildings, info.ArmyFirstMinCash);
			var holds = ArmyFirstEval.Holds(essential, paused, armyHolds)
				|| !ablateArmyRatio.Off(player, AblationIds.ArmyRatio)
					&& ArmyFirstEval.ShareHolds(essential, admission, share, cost, ShareTarget)
				|| !ablateRecovery.Off(player, AblationIds.Recovery)
					&& ArmyFirstEval.ReserveHolds(essential, admission, playerResources.GetCashAndResources(), Committed(), cost,
						ReserveLine());
#if CAMEO_DEV_LOGGING
			if (holds)
				CountShareHold(building, essential, admission, cost);
#endif
			return holds;
		}

#if CAMEO_DEV_LOGGING
		// DEFENCE-ZERO-FIX F-D (DEV only): share_hold per class and item, so a future arming of this module shows what the
		// projected-share vote holds (it writes no other trace). Read-only: re-asks the pure vote to attribute the hold; the
		// base builder asks twice per pick in one tick, so a repeat of the same item in the same tick counts once.
		readonly Dictionary<string, int> shareHolds = new();
		int lastShareHoldTick = -1;
		string lastShareHoldItem;

		void CountShareHold(ActorInfo building, bool essential, RecoverySpendPolicyCA.Admission admission, int cost)
		{
			if (ablateArmyRatio.Off(player, AblationIds.ArmyRatio) || !ArmyFirstEval.ShareHolds(essential, admission, share, cost, ShareTarget))
				return;

			var tick = player.World.WorldTick;
			if (tick == lastShareHoldTick && building.Name == lastShareHoldItem)
				return;

			lastShareHoldTick = tick;
			lastShareHoldItem = building.Name;
			var key = admission + "/" + building.Name;
			shareHolds.TryGetValue(key, out var n);
			shareHolds[key] = ++n;
			if (n <= 8 || n % 25 == 0)
				Log.Write("debug", $"AI ({player.ClientIndex}): SHARE-HOLD share_hold class={admission} item={building.Name} n={n} cost={cost}"
					+ $" A={share.Army} B={share.Buildings} target={ShareTarget} bank={playerResources.GetCashAndResources()} tick={tick}");
		}
#endif

		void IBotRequestPauseBuildingProduction.BuildingAdmitted(ActorInfo building, int cost)
		{
			if (IsTraitDisabled)
				return;

			share.Record(ArmyShareSide.Building, cost);
			recordedThisTick += cost;

			// ARMY-FIRST-02: the admitted order is in flight until a later scan sees it queued; count it meanwhile.
			if (!info.FirstCopyPasses && info.DefenceCapBase < 0)
				return;

			var tick = player.World.WorldTick;
			admittedAt[building.Name] = tick;
			if (ArmyFirstEval.IsDefence(building.HasTraitInfo<BuildingInfo>(), building.HasTraitInfo<AttackBaseInfo>()))
				admittedDefenceTicks.Add(tick);
		}

		// Defences owned or queued at the last scan, plus the ones admitted inside the in-flight window (a conservative
		// overcount while a scan already sees an admitted item queued: it can only hold one more sweep).
		int DefenceCount(int tick)
		{
			admittedDefenceTicks.RemoveAll(t => tick - t >= info.AdmittedHoldTicks);
			return scannedDefences + admittedDefenceTicks.Count;
		}

		int ThreatSaturationScale() => Math.Max(100, info.DefenceThreatSaturationPercent);

		void RefreshDefenceCap(int tick)
		{
			if (info.DefenceCapBase < 0)
				return;

			var intensity = 0.0;
			if (threat != null)
			{
				var saturation = ThreatSaturationScale() / 100f;
				intensity = Math.Max(threat.GetThreatIntensity(BotThreatRoles.AntiAir, saturation),
					Math.Max(threat.GetThreatIntensity(BotThreatRoles.AntiArmour, saturation),
						threat.GetThreatIntensity(BotThreatRoles.AntiInfantry, saturation)));
			}

			var turtleBonus = tick < info.TurtleDefenceUntilTick && personality?.CurrentPersonality == "turtle";
			defenceCap = ArmyFirstEval.DefenceCap(info.DefenceCapBase, info.DefenceCapPerThreat, intensity, turtleBonus,
				info.TurtleDefenceCapPercent);
		}

#if CAMEO_DEV_LOGGING
		// ARMY-FIRST-02 (DEV only): first-copy passes and defence-cap holds/admits per item, deduplicated per tick (the base
		// builder asks twice per pick in one tick). Bounded: the first 8 lines per key, then every 25th.
		readonly Dictionary<string, int> af2Counts = new();
		int lastAf2Tick = -1;
		string lastAf2Item;

		void CountAf2(ActorInfo building, string kind, int tick)
		{
			if (tick == lastAf2Tick && building.Name == lastAf2Item)
				return;

			lastAf2Tick = tick;
			lastAf2Item = building.Name;
			var key = kind + "/" + building.Name;
			af2Counts.TryGetValue(key, out var n);
			af2Counts[key] = ++n;
			if (n <= 8 || n % 25 == 0)
				Log.Write("debug", $"AI ({player.ClientIndex}): AF2 {kind} item={building.Name} n={n} defences={scannedDefences}"
					+ $"+{admittedDefenceTicks.Count} cap={defenceCap} personality={personality?.CurrentPersonality} tick={tick}");
		}
#endif

		// P_eff: the tier's reserve line scaled by the Steamroller lean (PLAN_04 row 4); -1 when the line is unset.
		int ReserveLine() => botLimits != null
			? RecoverySpendPolicyCA.EffectiveReserve(botLimits.Info.ProductionCashReserve, BotPersonalityLeads.Lean(leadProviders, "steamroller"))
			: -1;

		// Cash every queued item of the player has not paid yet, counted once, plus the orders this module admitted in the
		// current tick (they enter a queue, and so the first term, from the next tick). One queue scan per tick.
		// Orders another module issued earlier in the same tick are not visible here: that gap closes with the recovery
		// owner's commitment ledger (PLAN_03 section 2.4).
		long Committed()
		{
			var tick = player.World.WorldTick;
			if (committedTick != tick)
			{
				committedTick = tick;
				committedQueued = SpendCommitmentsCA.UnpaidQueued(player.World, player);
				recordedThisTick = 0;
			}

			return committedQueued + recordedThisTick;
		}

		// PLAN_02 section 2 values: live actors by their Valued cost (the Squad Manager's UnitValue),
		// in-flight items by their full queue cost. One scan per ScanInterval; admitted orders are
		// recorded on top by BuildingAdmitted until the next scan.
		void RefreshShare(World world, OpenRA.Player owner)
		{
			long armyLive = 0, buildingLive = 0, armyInflight = 0, buildingInflight = 0;
			var af2 = info.FirstCopyPasses || info.DefenceCapBase >= 0;
			if (af2)
			{
				typeCounts.Clear();
				scannedDefences = 0;
			}

			foreach (var a in world.ActorsHavingTrait<Building>())
				if (a.Owner == owner && !a.IsDead && a.IsInWorld)
				{
					buildingLive += a.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;
					if (af2)
						CountAf2Type(a.Info.Name, a.Info.HasTraitInfo<AttackBaseInfo>());
				}

			foreach (var a in world.ActorsHavingTrait<AttackBase>())
				if (a.Owner == owner && !a.IsDead && a.IsInWorld
					&& ArmyShareCA.Classify(a.Info.HasTraitInfo<BuildingInfo>(), true, a.Info.HasTraitInfo<HarvesterInfo>()) == ArmyShareSide.Army)
					armyLive += a.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;

			foreach (var pair in world.ActorsWithTrait<ProductionQueue>())
			{
				if (pair.Actor.Owner != owner || pair.Actor.IsDead || !pair.Trait.Enabled)
					continue;

				foreach (var item in pair.Trait.AllQueued())
				{
					if (!world.Map.Rules.Actors.TryGetValue(item.Item, out var itemInfo))
						continue;

					var side = ArmyShareCA.Classify(itemInfo.HasTraitInfo<BuildingInfo>(), itemInfo.HasTraitInfo<AttackBaseInfo>(),
						itemInfo.HasTraitInfo<HarvesterInfo>());
					if (side == ArmyShareSide.Army)
						armyInflight += item.TotalCost;
					else if (side == ArmyShareSide.Building)
					{
						buildingInflight += item.TotalCost;
						if (af2)
							CountAf2Type(itemInfo.Name, itemInfo.HasTraitInfo<AttackBaseInfo>());
					}
				}
			}

			share.Refresh(armyLive, armyInflight, buildingLive, buildingInflight);
		}

		void CountAf2Type(string name, bool hasAttack)
		{
			typeCounts[name] = typeCounts.GetValueOrDefault(name) + 1;
			if (hasAttack)
				scannedDefences++;
		}

		protected override void Created(Actor self)
		{
			player = self.Owner;
			leadProviders = self.TraitsImplementing<IBotPersonalityLeadProvider>().ToArray();
			playerResources = self.Owner.PlayerActor.TraitOrDefault<PlayerResources>();
			base.Created(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			disabledFromTick = scanStarted
				? ArmyFirstEval.PauseScan(self.World.WorldTick, lastBotTick)
				: -1;
		}

		protected override void TraitEnabled(Actor self)
		{
			ArmyFirstEval.ResumeScan(ref scanCooldown, self.World.WorldTick, disabledFromTick);
			disabledFromTick = -1;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (IsTraitDisabled || playerResources == null)
				return;

			var visitTick = BotVisitClock.Of(bot).VisitTick;
			lastBotTick = visitTick;
			if (!ArmyFirstEval.ShouldScan(ref scanCooldown, visitTick, info.ScanInterval))
				return;
			scanStarted = true;

			var world = bot.Player.World;

			// PR-3: the target is the tier's line; the share snapshot is rebuilt here, once per scan.
			botLimits = bot.Player.PlayerActor.TraitsImplementing<BotLimits>().FirstEnabledTraitOrDefault();
			RefreshShare(world, bot.Player);

			// ARMY-FIRST-02: the cap reads the threat once per scan; in-flight admissions older than the window are dropped.
			if (info.DefenceCapBase >= 0)
			{
				threat ??= bot.Player.PlayerActor.TraitsImplementing<IBotThreatAnalysis>().FirstOrDefault();
				personality ??= bot.Player.PlayerActor.TraitOrDefault<BotPersonalityController>();
				RefreshDefenceCap(world.WorldTick);
			}

			// An admission the scan above already saw queued is counted there; keep only the ones still in flight.
			var seenBefore = world.WorldTick - info.AdmittedSeenTicks;
			admittedDefenceTicks.RemoveAll(t => t <= seenBefore);

			if (admittedAt.Count > 0)
				foreach (var stale in admittedAt.Where(kv => world.WorldTick - kv.Value >= info.AdmittedHoldTicks).Select(kv => kv.Key).ToList())
					admittedAt.Remove(stale);

			// The vote only exists while at least one unit producer does - otherwise it would block the
			// very structures that make an army-first policy meaningful (first barracks/war factory, or
			// rebuilding a wiped production chain).
			hasUnitProducer = world.ActorsHavingTrait<Building>()
				.Any(a => a.Owner == bot.Player && a.TraitsImplementing<Production>().Any());

			if (info.MinArmyUnitsBeforeBuildings > 0)
				armyUnits = world.ActorsHavingTrait<AttackBase>()
					.Count(a => a.Owner == bot.Player && !a.IsDead && !a.Info.HasTraitInfo<BuildingInfo>());

			paused = ArmyFirstEval.NextPaused(paused, hasUnitProducer, playerResources.GetCashAndResources(), info.ArmyReserveLowCash, info.ArmyReserveHighCash);
		}
	}
}
