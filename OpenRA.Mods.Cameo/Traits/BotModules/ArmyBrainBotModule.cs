#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	[TraitLocation(SystemActors.Player)]
	[Desc("ARMY-BRAIN-01 (design: ARMY_BRAIN_DESIGN_02.md). S1: the formula counter. From unit stats alone (cost, hit points, damage against",
		"each armour, what a weapon may target, range and speed) it works out which unit types gain the most, per cost, against the enemy units",
		"and defences this bot has seen and remembers, given the army it already has. The result replaces the counter term of the unit roll;",
		"the roll floor stays, so every unit keeps a chance. No learning in S1. Observation only: fog memory and public rules data.")]
	public class ArmyBrainBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("S1 switch (row ab_formula). False = the unit roll keeps its classic counter term.")]
		public readonly bool ArmyBrainFormula = false;

		[Desc("Ticks between two rebuilds of the enemy mix, the own army and the gains.")]
		public readonly int IntervalTicks = 250;

		[Desc("Prior (learnable army.formula.gain): percent added to the multiplier of the best-ranked candidate and taken from the worst.")]
		public readonly int FormulaGainPercent = 60;

		[Desc("Prior (learnable army.formula.min): lowest counter multiplier, percent. The roll floor still applies after it.")]
		public readonly int FormulaMinPercent = 50;

		[Desc("Prior (learnable army.formula.max): highest counter multiplier, percent.")]
		public readonly int FormulaMaxPercent = 300;

		[Desc("Prior (learnable army.formula.band): the dead band. The multipliers reach their full spread only when the gains differ by this",
			"share (permille) of their mean size; almost equal gains stay near 100.")]
		public readonly int FormulaBandPermille = 250;

		[Desc("Prior (learnable army.step_cost): the value of the test step added to the army for each candidate.")]
		public readonly int StepCost = 2000;

		[Desc("Prior (learnable army.step_max_n): the most units in one test step.")]
		public readonly int StepMaxCount = 8;

		[Desc("Prior (learnable army.range.free_fire_cap_ticks): the longest unanswered fire a range advantage is counted for.")]
		public readonly int FreeFireCapTicks = 250;

		[Desc("Prior (learnable army.unanswered_ticks): how long a type the other side cannot hit at all (aircraft over an army without anti-air)",
			"fires on what is left of it after the fight.")]
		public readonly int UnansweredTicks = 250;

		[Desc("Perf bound, not a behaviour knob: only the most valuable enemy types and own types, this many each, enter the fight model.")]
		public readonly int MaxTypesPerSide = 12;

		[Desc("Perf bound, not a behaviour knob: the most gains computed in one roll call. Until a type's gain of this interval is computed, its",
			"gain of the interval before is used; a pool with a type that has no gain at all yet stays neutral (100) for that call.")]
		public readonly int MaxNewGainsPerCall = 6;

		[Desc("Priors (learnables army.slot.front_q / back_q / fast_q): the percentiles of HP per cost, range and speed over all unit types of the",
			"ruleset that cut the formation slots. S1 only reports the slots (DEV).")]
		public readonly int SlotFrontPercentile = 60;
		public readonly int SlotBackPercentile = 75;
		public readonly int SlotFastPercentile = 75;

		public override object Create(ActorInitializer init) { return new ArmyBrainBotModule(init.Self, this); }
	}

	public class ArmyBrainBotModule : ConditionalTrait<ArmyBrainBotModuleInfo>, IBotFormulaCounter
	{
		readonly World world;
		readonly OpenRA.Player player;
		AblationLatch ablateFormula;

		IBotEnemyCompositionProvider compositionProvider;
		IBotRememberedDefenceProvider defenceProvider;
		BotArsenalLedger ledger;
		bool resolved;

		// Rebuilt every IntervalTicks: the enemy mix, the own army, the net result of that fight and the gain per candidate type.
		readonly List<(BotUnitProfile Unit, double Count)> enemy = [];
		readonly List<(BotUnitProfile Unit, double Count)> own = [];
		readonly Dictionary<string, double> gainByType = new(StringComparer.Ordinal);
		readonly Dictionary<string, double> lastGainByType = new(StringComparer.Ordinal);
		readonly List<double> gains = [];
		ArmyPairCache pairs;
		double baseNet;
		int builtTick = int.MinValue;

		public ArmyBrainBotModule(Actor self, ArmyBrainBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		bool IBotFormulaCounter.IsActive => Info.ArmyBrainFormula && !IsTraitDisabled && !ablateFormula.Off(player, AblationIds.ArmyBrainFormula);

		void IBotFormulaCounter.FormulaCounterPercents(IReadOnlyList<ActorInfo> pool, int[] percents)
		{
#if CAMEO_DEV_LOGGING
			// DEV proof of the perf budget (no call of 5 ms or more): every call of 1 ms or more is logged with what it computed.
			var started = System.Diagnostics.Stopwatch.GetTimestamp();
			var cachedBefore = gainByType.Count;
			var refreshedAt = builtTick;
			Compute(pool, percents);
			var ms = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
			if (ms >= 1.0)
				Log.Write("debug", $"AI {player.InternalName}: ARMY-BRAIN S1 call {ms:0.00} ms at tick {world.WorldTick}: pool {pool.Count}, " +
					$"refresh {(refreshedAt != builtTick ? "yes" : "no")}, new gains {gainByType.Count - (refreshedAt != builtTick ? 0 : cachedBefore)}, " +
					$"enemy types {enemy.Count}, own types {own.Count}");
		}

		void Compute(IReadOnlyList<ActorInfo> pool, int[] percents)
		{
#endif
			Refresh();
			if (enemy.Count == 0)
			{
				for (var i = 0; i < pool.Count; i++)
					percents[i] = 100;

				return;
			}

			// At most MaxNewGainsPerCall model runs per call, so a refresh is spread over the next rolls instead of one burst.
			gains.Clear();
			var budget = Math.Max(1, Info.MaxNewGainsPerCall);
			var unknown = false;
			foreach (var candidate in pool)
			{
				if (!gainByType.TryGetValue(candidate.Name, out var gain))
				{
					if (budget > 0)
					{
						budget--;
						gain = ArmyFightModel.Gain(BotUnitProfiles.Get(world.Map.Rules, candidate), own, enemy, baseNet,
							Info.StepCost, Info.StepMaxCount, Info.FreeFireCapTicks, pairs, Info.UnansweredTicks);
						gainByType[candidate.Name] = gain;
					}
					else if (!lastGainByType.TryGetValue(candidate.Name, out gain))
						unknown = true;
				}

				gains.Add(gain);
			}

			if (unknown)
			{
				// A rank over a half-known pool would be wrong: neutral now, the next calls fill the rest.
				for (var i = 0; i < pool.Count; i++)
					percents[i] = 100;

				return;
			}

			ArmyFightModel.Multipliers(gains, Info.FormulaGainPercent, Info.FormulaMinPercent, Info.FormulaMaxPercent, Info.FormulaBandPermille, percents);
		}

		void Refresh()
		{
			var tick = world.WorldTick;
			if (builtTick != int.MinValue && tick - builtTick < Info.IntervalTicks)
				return;

			builtTick = tick;
			if (!resolved)
			{
				compositionProvider = player.PlayerActor.TraitsImplementing<IBotEnemyCompositionProvider>().FirstOrDefault();
				defenceProvider = player.PlayerActor.TraitsImplementing<IBotRememberedDefenceProvider>().FirstOrDefault();
				ledger = player.PlayerActor.TraitOrDefault<BotArsenalLedger>();
				pairs = new ArmyPairCache(Info.FreeFireCapTicks);
				resolved = true;
#if CAMEO_DEV_LOGGING
				LogSlots();
#endif
			}

			var rules = world.Map.Rules;
			// Exactly the interval before: an older gain is never a stand-in (Sonnet note 1).
			lastGainByType.Clear();
			foreach (var (type, gain) in gainByType)
				lastGainByType[type] = gain;

			gainByType.Clear();
			enemy.Clear();
			own.Clear();

			// The enemy mix: seen and remembered units (by value, from the fog memory) and remembered defences. Observed data only.
			if (compositionProvider != null && compositionProvider.TryGetEnemyComposition(out var valueByType))
				foreach (var (type, value) in valueByType.OrderBy(kv => kv.Key, StringComparer.Ordinal))
					if (value > 0 && rules.Actors.TryGetValue(type, out var info))
						AddTo(enemy, BotUnitProfiles.Get(rules, info), value);

			if (defenceProvider != null)
				foreach (var defence in defenceProvider.RememberedDefences())
					if (defence.Observed != null && defence.Value > 0)
						AddTo(enemy, BotUnitProfiles.Get(rules, defence.Observed), defence.Value);

			// The own army: armed mobile types still alive by the event-driven ledger (created - lost). No world scan.
			if (ledger != null)
				foreach (var (type, entry) in ledger.ByType.OrderBy(kv => kv.Key, StringComparer.Ordinal))
				{
					var alive = entry.Created - entry.Lost;
					if (alive <= 0 || !rules.Actors.TryGetValue(type, out var info))
						continue;

					var profile = BotUnitProfiles.Get(rules, info);
					if (!profile.IsBuilding && profile.Weapons.Length > 0 && profile.Cost > 0)
						own.Add((profile, alive));
				}

			Trim(enemy, Info.MaxTypesPerSide);
			Trim(own, Info.MaxTypesPerSide);
			baseNet = enemy.Count == 0 ? 0 : ArmyFightModel.Evaluate(own, enemy, Info.FreeFireCapTicks, pairs, Info.UnansweredTicks).Net;
#if CAMEO_DEV_LOGGING
			Log.Write("debug", $"AI {player.InternalName}: ARMY-BRAIN S1 refresh at tick {tick}: enemy types {enemy.Count} " +
				$"(value {enemy.Sum(e => e.Count * e.Unit.Cost):0}), own types {own.Count} (value {own.Sum(o => o.Count * o.Unit.Cost):0}), net {baseNet:0}");
#endif
		}

		// Keeps the `keep` most valuable types (ties by name), so one model call stays small.
		static void Trim(List<(BotUnitProfile Unit, double Count)> side, int keep)
		{
			if (keep <= 0 || side.Count <= keep)
				return;

			side.Sort((a, b) =>
			{
				var byValue = (b.Count * b.Unit.Cost).CompareTo(a.Count * a.Unit.Cost);
				return byValue != 0 ? byValue : string.CompareOrdinal(a.Unit.Name, b.Unit.Name);
			});
			side.RemoveRange(keep, side.Count - keep);
		}

		// An enemy entry is given by value: the count is value / cost (fractional), merged per type.
		static void AddTo(List<(BotUnitProfile Unit, double Count)> side, BotUnitProfile profile, int value)
		{
			if (profile.Cost <= 0 || profile.Hp <= 0)
				return;

			var count = (double)value / profile.Cost;
			for (var i = 0; i < side.Count; i++)
				if (ReferenceEquals(side[i].Unit, profile))
				{
					side[i] = (profile, side[i].Count + count);
					return;
				}

			side.Add((profile, count));
		}

#if CAMEO_DEV_LOGGING
		static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, object> SlotsLogged = new();

		// DEV proof of 3.1 / 3.2: one line per unit type with its features and slot, once per game.
		void LogSlots()
		{
			if (SlotsLogged.TryGetValue(world, out _))
				return;

			SlotsLogged.Add(world, new object());
			var rules = world.Map.Rules;
			var units = new List<BotUnitProfile>();
			foreach (var info in rules.Actors.Values.OrderBy(a => a.Name, StringComparer.Ordinal))
			{
				if (info.Name.StartsWith('^') || !info.HasTraitInfo<ValuedInfo>() || info.HasTraitInfo<BuildingInfo>())
					continue;

				if (!info.HasTraitInfo<MobileInfo>() && !info.HasTraitInfo<AircraftInfo>())
					continue;

				var profile = BotUnitProfiles.Get(rules, info);
				if (profile.Cost > 0 && profile.Hp > 0)
					units.Add(profile);
			}

			var slots = ArmySlots.From(units.Select(ArmySlotFacts.Of), Info.SlotFrontPercentile, Info.SlotBackPercentile, Info.SlotFastPercentile);
			Log.Write("debug", $"AI {player.InternalName}: ARMY-BRAIN S1 slots over {units.Count} types: front hp/cost >= {slots.FrontHpPerCost}, " +
				$"back range >= {slots.BackRange}, fast speed >= {slots.FastSpeed}");
			foreach (var unit in units)
			{
				var facts = ArmySlotFacts.Of(unit);
				Log.Write("debug", $"ARMY-BRAIN S1 type {unit.Name}: slot {slots.Classify(facts)} cost {unit.Cost} hp {unit.Hp} " +
					$"hp/cost {facts.HpPerCostPermille} range {facts.Range} speed {facts.Speed} armor {unit.Armor}");
			}
		}
#endif
	}
}
