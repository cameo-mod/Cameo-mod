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
using OpenRA.Mods.Common;
using OpenRA.Support;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>CONSOLIDATE-01 (CONSOLIDATE_01_DESIGN.md section 3.1): the ordered stages of the base-build pipeline. None = no pick.</summary>
	public enum BaseBuildStage
	{
		None = 0,
		S1Power = 1,
		S2ExpansionRefinery = 2,
		S3Opening = 3,
		S4AntiRush = 4,
		S5CoreOnce = 5,
		S6PassiveIncome = 6,
		S7RefineryPerField = 7,
		S8ProductionCycle = 8,
		S9ExtraDefence = 9,
		S10Fractions = 10,
	}

	/// <summary>One candidate of a stage: the building type and the stage that yielded it.</summary>
	public readonly record struct BaseBuildCandidate(string Name, BaseBuildStage Stage);

	/// <summary>
	/// The planner's answer. <see cref="Name"/> is the type to start (the candidate, or the power plant A5 swapped in), null = no pick this sweep.
	/// <see cref="Candidate"/> is the stage candidate that was admitted, <see cref="Tests"/> the feasibility tests run (at most K).
	/// </summary>
	public readonly record struct BaseBuildPick(string Name, BaseBuildStage Stage, string Candidate, int Tests)
	{
		public bool IsNone => Name == null;
	}

	/// <summary>
	/// Step A (A1-A7) and the stage caps that need the world, injected so the planner runs without a World. The base-builder queue manager
	/// implements it with its existing checks. TEST and COMMIT are separate: <see cref="StageAdmits"/> and <see cref="Feasible"/> must leave no
	/// state behind, <see cref="TryClaim"/> is the only step that may hold anything and runs only for the candidate about to be admitted, and
	/// every other side effect of a start (the order, StaticRatio.Record, lastCycledProduction, intervals) belongs to the caller AFTER the
	/// planner returned its pick. A candidate that falls through therefore leaves no partial state.
	/// </summary>
	public interface IBaseBuildFeasibility
	{
		/// <summary>The stage caps of <paramref name="candidate"/>'s stage that need the world. False = the candidate is skipped (it costs no test). No side effect.</summary>
		bool StageAdmits(BaseBuildCandidate candidate);

		/// <summary>
		/// A1-A6: buildable, limits, water and no-space ban, delays, power (A5 may substitute the power plant), affordable. True = feasible,
		/// <paramref name="admitted"/> = the type that would start. No side effect.
		/// </summary>
		bool Feasible(BaseBuildCandidate candidate, out string admitted);

		/// <summary>
		/// A7: the atomic admission claims (crawl link, front defence, refinery reservation) for the candidate that passed everything else.
		/// False = refused: nothing is held and the planner falls through to the next candidate.
		/// </summary>
		bool TryClaim(BaseBuildCandidate candidate, string admitted);

		/// <summary>
		/// S8-CASH-HOLD (row bb_s8_cash_hold): the candidate that just failed A1-A6 failed ONLY the cash test (it is buildable, within its limit, not delayed, no power or space refusal)
		/// and the bank is at least <paramref name="minBankPercent"/> percent of its cost: an eligible factory the bank reaches soon. No side effect. Default false.
		/// </summary>
		bool CashOnlyBlocked(BaseBuildCandidate candidate, int minBankPercent) => false;
	}

	/// <summary>
	/// Step 0: the per-sweep snapshot one producer plans from. Plain data plus a few lookups; the planner only looks values up and never
	/// enumerates a dictionary, so its result does not depend on hash order. Delegates may be null (= the stage has nothing to offer).
	/// Fields marked "C4-C6" are carried for the later stages; c2 fills them but no c2 stage reads them.
	/// </summary>
	public sealed class BaseBuildSnapshot
	{
		public int WorldTick;
		public bool DefenceQueue;

		// A6: the bank after this sweep's orders (ProdBusyCA.SweepBank) and the tier ProductionCashReserve (-1 = unset = 0).
		public int Bank;
		public int ProductionCashReserve = -1;

		// C4-C6: the tier line L (BotLimits MaximiseProductionCashRequirement, 0 = none), SUSTAINED-CASH active, the threat-seen latch, and the
		// RATIO-2X inputs (mobile army value and live + queued static defence value; HasArmyValues false = no provider or no usable measure).
		public int TierLine;
		public bool LowCashCut; // c6: R9 (pb_essentials_low_cash): below the tier line only S1-S7 run
		public bool SustainedActive;
		public bool DefenceThreatSeen;
		public bool HasArmyValues;
		public long MobileValue;
		public long StaticDefenceValue;

		// The types this queue can build now (queue.BuildableItems(), in queue order).
		public IReadOnlyList<string> Buildable;

		// S1: the resolved power state (PowerManager present, its excess, the margin wanted) and the best power plant this queue can build.
		public bool HasPowerManager;
		public int ExcessPower;
		public int PowerMargin;
		public string PowerType;
		public int PowerTypeAmount;

		// Owned (standing, by actor name) and in-flight (BuildingsBeingProduced, C1 recount) counts; VariantsOf = the PlaceBuildingVariants of a type.
		public IReadOnlyDictionary<string, int> Owned;
		public IReadOnlyDictionary<string, int> InFlight;
		public Func<string, IReadOnlyList<string>> VariantsOf;

		// C3-C6 plug point: the candidate names a world-dependent stage yields, in its own order, read lazily when the planner reaches the stage
		// (S2, S4-S9; null or an empty sequence = the stage offers nothing). S1, S3 and S10 never read it.
		public Func<BaseBuildStage, IEnumerable<string>> StageSource;

		// S3: the opening standard order (BaseFix01CA.StandardOrder) and its per-role status: the role's pick (null = none) and how many it has.
		public bool OpeningActive;
		public IReadOnlyList<OpeningSlot> OpeningOrder;
		public Func<OpeningRole, (string Pick, int Have)> OpeningStatus;
		public Func<OpeningRole, int, bool> OpeningStuck;
		public Action<OpeningRole, int> OpeningSatisfied;

		// S10: the BuildingFractions table (per 100), the standing building count, the knob milli per type (null = neutral) and the interval test.
		public IEnumerable<KeyValuePair<string, int>> Fractions;
		public int BuildingCount;
		public Func<string, int> FractionMilli;
		public Func<string, bool> IntervalActive;

		// BUILD-ALL-B (row bb_build_all_listed): S5 offers only the listed BuildingFractions first copies (the roll); the depth fallback of unlisted
		// first-copy kinds and listed weight-0 entries is off. False = S5 as before.
		public bool CoreOnceListedOnly;

		// MODE-BIAS-01: the milli weight multiplier of a type's BuildingFractions entry while a mode bias profile is active (1000 = neutral, composed by ModeBias).
		// Null = no profile active: the roll is the uniform shuffle and the share cap is unchanged, exactly as before.
		public Func<string, int> BiasMilli;

		// QUEUE-SPLIT-01 (row bb_queue_split): this player has a defence queue of its own, so the walk keeps to its queue's stages (QueueStageSplit.Allowed).
		// False = every stage, as before.
		public bool QueueSplit;

		// bb_skip, DEV only: the types this walk actually offered (a candidate that reached the stage's admission or feasibility tests) and rejected, with the
		// offering stage and the reject reason. Null = not collected; only the DEV-only recorder in the queue manager ever sets it.
		public Dictionary<string, (BaseBuildStage Stage, string Reason)> Offered;

		// S8-STARVE-01 (row bb_s8_rejected_fallthrough, Sol 58ADBC25): true = an S8 candidate that was only offered and then rejected no longer holds S9 and S10 back under
		// SUSTAINED cash (an admitted S8 still wins first). False = the offer alone holds them, as before.
		public bool S8RejectedFallthrough;

		// S8-CASH-HOLD (row bb_s8_cash_hold, Opus V08G ruling, S8_STARVE_01 line 35): while an eligible, affordable-soon S8 factory waits only for cash, the S10 fractions of THIS
		// (building) queue are held back for at most S8HoldMaxTicks. S9 (defence), units and upgrades are never held. S8HoldName/S8HoldStart carry the running hold of the
		// previous sweeps (start -1 = none); S8CashBlocked is written by the planner (the blocked type). False/0 = no hold, as before.
		public bool S8CashHold;
		public int S8HoldMaxTicks;
		public int S8HoldMinBankPercent;
		public string S8HoldName;
		public int S8HoldStart = -1;
		public string S8CashBlocked;

		// DEV only: how many S8 candidates this walk offered (written only by the compiled-out recorder; always 0 in a release build).
		public int S8Offers;

		// CONSOLIDATE-03 P1 (a): whether an S5 first copy that is no listed BuildingFractions entry is a capture unlock (cs_capture_unlock) rather than a tech-depth
		// first copy. Read only to NAME the deciding rule (BuildingPickArbiter.RuleOf), never to pick. Null = tech depth.
		public Func<string, bool> CaptureUnlockOf;
	}

	/// <summary>
	/// CONSOLIDATE-01 c2 (row bb_pipeline_01): the pure base-build planner. The stages are walked in order; each yields 0..n candidates in its own
	/// order. A candidate that fails its stage caps or the feasibility test falls through to the next candidate, then to the next stage: there is
	/// no veto after the pick. At most <see cref="MaxFeasibilityTests"/> feasibility tests per producer per sweep, then no pick. Deterministic: only
	/// the snapshot, the injected tests and the given synced random are read, keys are sorted ordinally, and the random is drawn lazily in the S10
	/// roll only (the same snapshot and seed give the same pick and the same number of draws).
	/// c2 wires S1 (power), S3 (opening order) and S10 (the fraction roll); S2 and S4-S9 yield only what BaseBuildSnapshot.StageSource offers
	/// (nothing until C3-C6). c6: the cash tiers, i.e. the walk order and which stages are open at the bank (BaseBuildTiersCA: the cut below the tier line,
	/// S8 at the line, the SUSTAINED order).
	/// </summary>
	public static class BaseBuildPlanCA
	{
		/// <summary>K: the feasibility tests one producer may run per sweep (perf bound).</summary>
		public const int MaxFeasibilityTests = 8;

		static readonly BaseBuildStage[] StageOrder =
		[
			BaseBuildStage.S1Power,
			BaseBuildStage.S2ExpansionRefinery,
			BaseBuildStage.S3Opening,
			BaseBuildStage.S4AntiRush,
			BaseBuildStage.S5CoreOnce,
			BaseBuildStage.S6PassiveIncome,
			BaseBuildStage.S7RefineryPerField,
			BaseBuildStage.S8ProductionCycle,
			BaseBuildStage.S9ExtraDefence,
			BaseBuildStage.S10Fractions,
		];

		public static IReadOnlyList<BaseBuildStage> Stages => StageOrder;

		/// <summary>The first candidate, over the ordered stages, that passes its stage caps and A1-A7. None after K failed tests or when every stage is exhausted.</summary>
		public static BaseBuildPick Plan(BaseBuildSnapshot snapshot, IBaseBuildFeasibility feasibility, MersenneTwister rng) => Plan(snapshot, feasibility, rng, null);

		/// <summary>
		/// CONSOLIDATE-03 P1 (a): the walk of <see cref="BuildingPickArbiter.Decide"/>. <paramref name="refusals"/> null = exactly the walk above (a release build passes null);
		/// non-null = every refusal of this walk is appended in walk order: a closed stage (queue split, S8 cash hold, tier cut, cycle hold), a refused candidate (stage cap,
		/// infeasible, S8 cash only, claim refused, already failed this sweep) and the K stop. Recording reads only pure functions of the snapshot: the walk, its tests and its
		/// random draws are the same either way.
		/// </summary>
		public static BaseBuildPick Plan(BaseBuildSnapshot snapshot, IBaseBuildFeasibility feasibility, MersenneTwister rng, List<BuildingRefusal> refusals)
		{
			var tests = 0;
			HashSet<string> failed = null;
			var cycleOffered = false;

			// c6: the cash tiers. Below the tier line the walk stops after S7 (S9 only with a threat seen), S8 needs the bank at the line, and SUSTAINED cash moves
			// S8 above S7 and holds S9 and S10 back while the cycle still offers a type (BaseBuildTiersCA).
			foreach (var stage in BaseBuildTiersCA.Order(snapshot.SustainedActive, snapshot.TierLine))
			{
				if (refusals != null)
					BuildingPickArbiter.NoteClosed(refusals, stage, snapshot, cycleOffered);

				if (!QueueStageSplit.Allowed(stage, snapshot.QueueSplit, snapshot.DefenceQueue))
					continue;

				if (stage == BaseBuildStage.S10Fractions && S8CashHoldActive(snapshot))
					continue;

				if (!BaseBuildTiersCA.Open(stage, snapshot.TierLine, snapshot.Bank, snapshot.LowCashCut, snapshot.SustainedActive, snapshot.DefenceThreatSeen, BaseBuildTiersCA.CycleHolds(cycleOffered, snapshot.S8RejectedFallthrough)))
					continue;

				foreach (var candidate in StageCandidates(stage, snapshot, rng))
				{
					cycleOffered |= stage == BaseBuildStage.S8ProductionCycle && candidate.Name != null;
					NoteS8Offer(snapshot, stage, candidate);

					// A type that already failed A this sweep fails again (the state is the same, the reserve only grows with the stage): no second test.
					if (candidate.Name == null || (failed != null && failed.Contains(candidate.Name)))
					{
						if (candidate.Name != null)
							BuildingPickArbiter.Refuse(refusals, snapshot, candidate, BuildingReason.AlreadyFailed);

						continue;
					}

					if (!feasibility.StageAdmits(candidate))
					{
						OfferRejected(snapshot, candidate, "stage_not_admitted");
						BuildingPickArbiter.Refuse(refusals, snapshot, candidate, BuildingReason.StageCap);
						continue;
					}

					// One test = A1-A6 and, only when those pass, the A7 claim (the single step allowed to hold state).
					tests++;
					var feasible = feasibility.Feasible(candidate, out var admitted) && admitted != null;
					if (feasible && feasibility.TryClaim(candidate, admitted))
						return new BaseBuildPick(admitted, candidate.Stage, candidate.Name, tests);

					// S8-CASH-HOLD: the first S8 factory that failed only the cash test, while the row is armed, owns the hold (reason s8_cash_hold in the DEV rows).
					var cashHold = !feasible && stage == BaseBuildStage.S8ProductionCycle && snapshot.S8CashHold && snapshot.S8CashBlocked == null
						&& !snapshot.DefenceQueue && feasibility.CashOnlyBlocked(candidate, snapshot.S8HoldMinBankPercent);
					if (cashHold)
						snapshot.S8CashBlocked = candidate.Name;

					OfferRejected(snapshot, candidate, cashHold ? "s8_cash_hold" : feasible ? "claim_refused" : "infeasible");
					BuildingPickArbiter.Refuse(refusals, snapshot, candidate, cashHold ? BuildingReason.S8CashOnly : feasible ? BuildingReason.ClaimRefused : BuildingReason.Infeasible);

					(failed ??= new HashSet<string>(StringComparer.Ordinal)).Add(candidate.Name);
					if (tests >= MaxFeasibilityTests)
					{
						refusals?.Add(new BuildingRefusal(null, stage, BuildingReason.TestCap, BuildingPickArbiter.RuleOf(stage, snapshot, null)));
						return new BaseBuildPick(null, BaseBuildStage.None, null, tests);
					}
				}
			}

			return new BaseBuildPick(null, BaseBuildStage.None, null, tests);
		}

		/// <summary>S8-CASH-HOLD: a cash-blocked S8 factory was seen this walk and its hold (counted from the first sweep that saw this type blocked) is younger than S8HoldMaxTicks.</summary>
		public static bool S8CashHoldActive(BaseBuildSnapshot s) =>
			s.S8CashHold && s.S8CashBlocked != null && !s.DefenceQueue
			&& (s.S8HoldName != s.S8CashBlocked || s.S8HoldStart < 0 || s.WorldTick - s.S8HoldStart < s.S8HoldMaxTicks);

		// S8-STARVE-01, DEV only: counts the S8 offers of a walk (the queue manager reports the fall-through when the pick comes from S9 or S10).
		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		static void NoteS8Offer(BaseBuildSnapshot s, BaseBuildStage stage, BaseBuildCandidate candidate)
		{
			if (stage == BaseBuildStage.S8ProductionCycle && candidate.Name != null)
				s.S8Offers++;
		}

		// bb_skip, DEV only (compiled out of a release build with its arguments): the first rejection of an offered type this walk.
		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		static void OfferRejected(BaseBuildSnapshot s, BaseBuildCandidate candidate, string reason)
		{
			if (s.Offered != null && !s.Offered.ContainsKey(candidate.Name))
				s.Offered[candidate.Name] = (candidate.Stage, reason);
		}

		/// <summary>The candidates of one stage, lazily, in the stage's own order.</summary>
		public static IEnumerable<BaseBuildCandidate> StageCandidates(BaseBuildStage stage, BaseBuildSnapshot s, MersenneTwister rng)
		{
			switch (stage)
			{
				case BaseBuildStage.S1Power: return PowerCandidates(s);
				case BaseBuildStage.S2ExpansionRefinery: return ExpansionRefineryCandidates(s);
				case BaseBuildStage.S3Opening: return OpeningCandidates(s);
				case BaseBuildStage.S4AntiRush: return AntiRushCandidates(s);
				case BaseBuildStage.S5CoreOnce: return CoreOnceCandidates(s, rng);
				case BaseBuildStage.S6PassiveIncome: return PassiveIncomeCandidates(s);
				case BaseBuildStage.S7RefineryPerField: return RefineryPerFieldCandidates(s);
				case BaseBuildStage.S8ProductionCycle: return ProductionCycleCandidates(s);
				case BaseBuildStage.S9ExtraDefence: return ExtraDefenceCandidates(s);
				case BaseBuildStage.S10Fractions: return RollCandidates(s, rng, BaseBuildStage.S10Fractions);
				default: return Enumerable.Empty<BaseBuildCandidate>();
			}
		}

		/// <summary>S1: power is short now (the old low-power override): a power manager, excess under the margin, and a plant that gives power.</summary>
		public static bool PowerShort(BaseBuildSnapshot s) =>
			s.HasPowerManager && s.ExcessPower < s.PowerMargin && s.PowerType != null && s.PowerTypeAmount > 0;

		/// <summary>The fraction roll's count: standing buildings of the type or one of its placement variants, plus the type in flight.</summary>
		public static int CountOf(BaseBuildSnapshot s, string name)
		{
			var count = Lookup(s.Owned, name) + Lookup(s.InFlight, name);
			var variants = s.VariantsOf?.Invoke(name);
			if (variants == null)
				return count;

			for (var i = 0; i < variants.Count; i++)
			{
				var v = variants[i];
				if (v == null || v == name)
					continue;

				// A building is counted once even when a variant is listed twice.
				var seen = false;
				for (var j = 0; j < i && !seen; j++)
					seen = variants[j] == v;

				if (!seen)
					count += Lookup(s.Owned, v);
			}

			return count;
		}

		/// <summary>A6: the reserve the bank must keep after the start. 0 for S1-S7, the tier ProductionCashReserve (unset = 0) for S8-S10.</summary>
		public static int ReserveFor(BaseBuildStage stage, int productionCashReserve) =>
			stage >= BaseBuildStage.S8ProductionCycle ? Math.Max(0, productionCashReserve) : 0;

		/// <summary>A6: the start is affordable with the reserve kept.</summary>
		public static bool Affordable(int bank, int cost, int reserve) => (long)bank - Math.Max(0, cost) >= reserve;

		/// <summary>S10 stage caps that need no world: the build interval and the BuildingFractions share (knob milli, neutral when unset).</summary>
		public static bool FractionStageAdmits(BaseBuildSnapshot s, string name, int fractionPer100)
		{
			if (s.IntervalActive != null && s.IntervalActive(name))
				return false;

			return BaseBuilderQueueEvalCA.FractionAdmits(CountOf(s, name), fractionPer100, CombinedMilli(s, name), s.BuildingCount);
		}

		/// <summary>The ONE combined weight of a type (milli, 1000 = neutral): the knob milli (personality x demand) times the mode bias milli. Used by the share cap
		/// (<see cref="FractionStageAdmits"/>) and by the S5 first-copy order (<see cref="CombinedWeight"/>).</summary>
		public static int CombinedMilli(BaseBuildSnapshot s, string name)
		{
			var milli = s.FractionMilli?.Invoke(name) ?? BuildOrderKnob.Neutral;
			if (s.BiasMilli != null)
				milli = ModeBias.CombineMilli(milli, s.BiasMilli(name)); // MODE-BIAS-01: max(1, milli x bias / 1000)
			return milli;
		}

		/// <summary>The S5 order weight of a listed type: base fraction x <see cref="CombinedMilli"/>, floored at 1 (every type keeps a chance) and capped.</summary>
		public static int CombinedWeight(BaseBuildSnapshot s, string name, int fractionPer100) =>
			(int)Math.Clamp((long)Math.Max(1, fractionPer100) * CombinedMilli(s, name), 1, 1_000_000);

		static int Lookup(IReadOnlyDictionary<string, int> counts, string name) =>
			counts != null && counts.TryGetValue(name, out var n) ? n : 0;

		// S1 (power when short). "Would be short" is A5's swap on every candidate.
		static IEnumerable<BaseBuildCandidate> PowerCandidates(BaseBuildSnapshot s)
		{
			if (PowerShort(s))
				yield return new BaseBuildCandidate(s.PowerType, BaseBuildStage.S1Power);
		}

		// S3 (opening standard order, BaseFix01CA). NextRole gives the next open step; a step whose pick then fails A is marked tried and NextRole
		// runs again, so the order goes on to the next open step (the old "Can = false" skip) instead of stalling. Each role's status is read once.
		static IEnumerable<BaseBuildCandidate> OpeningCandidates(BaseBuildSnapshot s)
		{
			if (!s.OpeningActive || s.OpeningOrder == null || s.OpeningStatus == null)
				yield break;

			var status = new Dictionary<OpeningRole, (string Pick, int Have)>();
			var tried = new HashSet<OpeningRole>();
			Func<OpeningRole, int, bool> stuck = s.OpeningStuck ?? ((_, _) => false);

			(bool Can, int Have) Status(OpeningRole role)
			{
				if (!status.TryGetValue(role, out var st))
					status[role] = st = s.OpeningStatus(role);

				return (st.Pick != null && !tried.Contains(role), st.Have);
			}

			while (true)
			{
				var next = BaseFix01CA.NextRole(s.OpeningOrder, Status, stuck, s.OpeningSatisfied);
				if (next == OpeningRole.None)
					yield break;

				tried.Add(next);
				yield return new BaseBuildCandidate(status[next].Pick, BaseBuildStage.S3Opening);
			}
		}

		// The ONE BuildingFractions roll (S5 and S10): ordinal key order, shuffled with the synced random (lazily, one draw per entry reached), then
		// the interval and the fraction share. S5 asks it for first copies only (count 0, weight > 0), S10 for every entry that passes. A type with count 0
		// always passes the share. The world caps (air intel, defence need, refinery safety, RATIO-2X, army share) are StageAdmits.
		static IEnumerable<BaseBuildCandidate> RollCandidates(BaseBuildSnapshot s, MersenneTwister rng, BaseBuildStage stage)
		{
			if (s.Fractions == null)
				yield break;

			// MODE-BIAS-01: with a profile active the order is the weighted one (ModeBias.WeightedOrder, lazy, one draw per entry reached); else the uniform shuffle.
			// BUILD-ALL-B (FullStack finding, Opus ruling): the S5 first-copy order uses the same combined weight as the share cap (base x personality x demand x mode).
			var per100 = stage == BaseBuildStage.S5CoreOnce && (s.BiasMilli != null || s.FractionMilli != null) ? FractionTable(s.Fractions) : null;
			IEnumerable<KeyValuePair<string, int>> order = per100 != null
				? ModeBias.WeightedOrder(s.Fractions, name => CombinedWeight(s, name, per100.TryGetValue(name, out var v) ? v : 0), rng)
				: s.BiasMilli != null
				? ModeBias.WeightedOrder(s.Fractions, s.BiasMilli, rng)
				: s.Fractions.OrderBy(kv => kv.Key, StringComparer.Ordinal).Shuffle(rng);
			foreach (var frac in order)
			{
				if (stage == BaseBuildStage.S5CoreOnce && (frac.Value <= 0 || CountOf(s, frac.Key) != 0))
					continue;

				if (FractionStageAdmits(s, frac.Key, frac.Value))
					yield return new BaseBuildCandidate(frac.Key, stage);
			}
		}

		static Dictionary<string, int> FractionTable(IEnumerable<KeyValuePair<string, int>> fractions)
		{
			var table = new Dictionary<string, int>(StringComparer.Ordinal);
			foreach (var kv in fractions)
				table.TryAdd(kv.Key, kv.Value);

			return table;
		}

		// S5 (every listed building once): the original roll on first copies with a positive weight, then the depth fallback (StageSource) for the
		// types the roll cannot offer (not with CoreOnceListedOnly). Nothing on the defence queue (S4 and S9 own it).
		static IEnumerable<BaseBuildCandidate> CoreOnceCandidates(BaseBuildSnapshot s, MersenneTwister rng)
		{
			if (s.DefenceQueue)
				yield break;

			foreach (var c in RollCandidates(s, rng, BaseBuildStage.S5CoreOnce))
				yield return c;

			// BUILD-ALL-B: the build-once set is the listed fractions only (user ruling 11-25), so the fallback is not asked at all.
			if (s.CoreOnceListedOnly)
				yield break;

			foreach (var c in SourceCandidates(s, BaseBuildStage.S5CoreOnce))
				yield return c;
		}

		// Stubs, filled by C3-C6 (CONSOLIDATE_01_DESIGN.md section 4). Each yields what the snapshot's StageSource offers for it (nothing in c2).
		static IEnumerable<BaseBuildCandidate> ExpansionRefineryCandidates(BaseBuildSnapshot s) => SourceCandidates(s, BaseBuildStage.S2ExpansionRefinery); // S2 + S2b planner wants (C4)
		static IEnumerable<BaseBuildCandidate> AntiRushCandidates(BaseBuildSnapshot s) => SourceCandidates(s, BaseBuildStage.S4AntiRush); // S4 (C5)
		static IEnumerable<BaseBuildCandidate> PassiveIncomeCandidates(BaseBuildSnapshot s) => SourceCandidates(s, BaseBuildStage.S6PassiveIncome); // S6 (C4)
		static IEnumerable<BaseBuildCandidate> RefineryPerFieldCandidates(BaseBuildSnapshot s) => SourceCandidates(s, BaseBuildStage.S7RefineryPerField); // S7 (C4)
		static IEnumerable<BaseBuildCandidate> ProductionCycleCandidates(BaseBuildSnapshot s) => SourceCandidates(s, BaseBuildStage.S8ProductionCycle); // S8 (C6)
		static IEnumerable<BaseBuildCandidate> ExtraDefenceCandidates(BaseBuildSnapshot s) => SourceCandidates(s, BaseBuildStage.S9ExtraDefence); // S9 (C5)

		// The StageSource is invoked only when the planner reaches the stage (lazy), so an earlier admission never runs a later stage's helper.
		static IEnumerable<BaseBuildCandidate> SourceCandidates(BaseBuildSnapshot s, BaseBuildStage stage)
		{
			var names = s.StageSource?.Invoke(stage);
			if (names == null)
				yield break;

			foreach (var name in names)
				yield return new BaseBuildCandidate(name, stage);
		}
	}
}
