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
using System.Reflection;
using System.Runtime.CompilerServices;
using OpenRA.Mods.Common.Traits;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class SquadDesireRegressionTest
	{
		static SquadDesireSignals Signals(int ratio, int tick, int health = 1000) =>
			new(3000, 1500, 0, ratio, health, 0, true, false, tick);

		[Test]
		public void LosingRushCannotAttackEvenWhenDesireAndDwellRetainAttack()
		{
			var state = new SquadDesireMemory();
			var winning = Signals(4000, 0);
			Assert.That(state.Evaluate(in winning, "rush", 100, 150, 100, 200), Is.EqualTo(SquadDesireStance.Attack));
			var losing = Signals(500, 75);
			var retained = state.Evaluate(in losing, "rush", 100, 150, 100, 200);
			Assert.That(retained, Is.EqualTo(SquadDesireStance.Attack), "the historical desire is intentionally retained");
			var safe = SquadDesireOrders.CanEngage(losing.PredictedRatioMilli, 50, 150);
			Assert.That(safe, Is.False, "the old margin rejected ratio 0.5; desire cannot grant permission");
			Assert.That(SquadDesireOrders.Plan(retained, safe, true),
				Is.EqualTo(new SquadDesireOrder("Move", SquadDesireDestination.Home)));
			var fresh = new SquadDesireMemory();
			Assert.That(fresh.Evaluate(in losing, "rush", 100, 150, 100, 200), Is.EqualTo(SquadDesireStance.Attack));
			Assert.That(SquadDesireOrders.CanEngage(999, 50, 150), Is.False, "no losing square-law commit");
			Assert.That(SquadDesireOrders.CanEngage(1000, 50, 150), Is.True);
		}

		[Test]
		public void SixStancesHaveDistinctOrderRoutesAndHarassHasAConsumer()
		{
			var routes = Enum.GetValues<SquadDesireStance>().Select(s => SquadDesireOrders.Plan(s, true, true)).ToArray();
			Assert.That(routes.Distinct().Count(), Is.EqualTo(6));
			Assert.That(routes[(int)SquadDesireStance.Defend], Is.EqualTo(new SquadDesireOrder("AttackMove", SquadDesireDestination.Home)));
			Assert.That(routes[(int)SquadDesireStance.Regroup].Destination, Is.EqualTo(SquadDesireDestination.Assembly));
			Assert.That(routes[(int)SquadDesireStance.Harass].Destination, Is.EqualTo(SquadDesireDestination.Raid));
			Assert.That(routes[(int)SquadDesireStance.Reinforce].Destination, Is.EqualTo(SquadDesireDestination.Reinforcement));
			Assert.That(SquadDesireOrders.Supports(SquadCAType.Harass), Is.True);
			Assert.That(SquadDesireOrders.Supports(SquadCAType.Protection), Is.True);
			Assert.That(SquadDesireOrders.Supports(SquadCAType.Rush), Is.True);
			Assert.That(SquadDesireOrders.Supports(SquadCAType.Air), Is.True);
			Assert.That(SquadDesireOrders.Supports((SquadCAType)(-1)), Is.False);
		}

		sealed class MemoryProvider : IBotSquadDesire
		{
			readonly SquadDesireMemory memory = new();
			public SquadDesireStance Last;
			public int Calls;
			public SquadCA LastSquad;
			public SquadDesireStance StanceFor(SquadCA squad, in SquadDesireSignals signals)
			{
				Calls++;
				LastSquad = squad;
				return Last = memory.Evaluate(in signals, "rush", 100, 150, 100, 200);
			}
		}

		sealed class FixedProvider : IBotSquadDesire
		{
			public SquadDesireStance Stance;
			public SquadCA LastSquad;
			public SquadDesireStance StanceFor(SquadCA squad, in SquadDesireSignals signals)
			{
				LastSquad = squad;
				return Stance;
			}
		}

		sealed class Execution : ISquadDesireExecution<int>
		{
			public IReadOnlyList<int> Members { get; set; } = new[] { 1, 2 };
			public readonly Dictionary<int, CPos> Cells = new() { [1] = new(8, 8), [2] = new(10, 8) };
			public readonly List<(int Member, string Order, CPos Cell)> Emitted = new();
			public SquadDesireSignals Signals = SquadDesireRegressionTest.Signals(4000, 0);
			public CPos? Target = new(20, 20);
			public CPos Home => new(2, 2);
			public CPos? Reinforcement => new(12, 12);
			public bool Idle = false;
			public CPos Location(int member) => Cells[member];
			public bool IsIdle(int member) => Idle;
			public (SquadDesireStance Stance, bool Safe, CPos? Target) Evaluate(IBotSquadDesire provider) =>
				(provider.StanceFor(null, in Signals), SquadDesireOrders.CanEngage(Signals.PredictedRatioMilli, 50, 150), Target);
			public void QueueOrder(int member, string order, CPos cell) => Emitted.Add((member, order, cell));
		}

		[Test]
		public void ProductionControllerReplacesInFlightAttackWithHomeOrdersDespiteRetainedDesire()
		{
			var controller = new SquadDesireController<int>();
			var provider = new MemoryProvider();
			var execution = new Execution();
			controller.Tick(execution, provider);
			Assert.That(execution.Emitted, Is.EqualTo(new[] {
				(1, "AttackMove", new CPos(20, 20)), (2, "AttackMove", new CPos(20, 20)) }));
			execution.Signals = Signals(500, 75);
			controller.Tick(execution, provider);
			Assert.That(provider.Last, Is.EqualTo(SquadDesireStance.Attack));
			Assert.That(execution.Emitted.Skip(2), Is.EqualTo(new[] {
				(1, "Move", execution.Home), (2, "Move", execution.Home) }));
			controller.Tick(execution, provider);
			Assert.That(provider.Calls, Is.EqualTo(3), "active movement still evaluates at manager cadence");
			Assert.That(execution.Emitted.Count, Is.EqualTo(4), "identical in-flight retreat is suppressed");
		}

		[Test]
		public void ProductionControllerDropsDepartedMemberMemoryAndSkipsAnEmptyRoster()
		{
			var controller = new SquadDesireController<int>();
			var provider = new MemoryProvider();
			var execution = new Execution();
			controller.Tick(execution, provider);
			execution.Members = new[] { 1 };
			controller.Tick(execution, provider);
			execution.Members = new[] { 1, 2 };
			controller.Tick(execution, provider);
			Assert.That(execution.Emitted.Last().Member, Is.EqualTo(2));
			Assert.That(execution.Emitted.Count, Is.EqualTo(3));
			execution.Members = Array.Empty<int>();
			controller.Tick(execution, provider);
			Assert.That(provider.Calls, Is.EqualTo(3));
		}

		sealed class RecordingState : IState
		{
			public int Ticks;
			public void Activate(SquadCA squad) { }
			public void Deactivate(SquadCA squad) { }
			public void Tick(SquadCA squad) => Ticks++;
		}

		static IEnumerable<SquadCAType> AllSquadTypes => Enum.GetValues<SquadCAType>();

		static SquadManagerBotModuleCA Manager(bool armed, IBotSquadDesire provider = null)
		{
			var info = new SquadManagerBotModuleCAInfo();
			typeof(SquadManagerBotModuleCAInfo).GetField(nameof(info.UseSquadDesire)).SetValue(info, armed);
			var manager = (SquadManagerBotModuleCA)RuntimeHelpers.GetUninitializedObject(typeof(SquadManagerBotModuleCA));
			typeof(ConditionalTrait<SquadManagerBotModuleCAInfo>).GetField("Info").SetValue(manager, info);
			if (armed)
			{
				// Exercise the actual manager's tick cache/provider getter without a loaded world.
				// Fact gathering remains the execution-port boundary.
				typeof(SquadManagerBotModuleCA).GetField("World").SetValue(manager,
					RuntimeHelpers.GetUninitializedObject(typeof(World)));
				typeof(SquadManagerBotModuleCA).GetField("squadDesire", BindingFlags.Instance | BindingFlags.NonPublic)
					.SetValue(manager, provider);
				typeof(SquadManagerBotModuleCA).GetField("squadDesireTick", BindingFlags.Instance | BindingFlags.NonPublic)
					.SetValue(manager, 0);
			}
			return manager;
		}

		sealed class ActorExecution : ISquadDesireExecution<Actor>
		{
			public SquadCA Squad;
			public SquadDesireSignals Signals = SquadDesireRegressionTest.Signals(4000, 0);
			public readonly List<(string Order, CPos Cell)> Emitted = new();
			public IReadOnlyList<Actor> Members { get; } = new[] {
				(Actor)RuntimeHelpers.GetUninitializedObject(typeof(Actor)) };
			public CPos Location(Actor member) => new(8, 8);
			public bool IsIdle(Actor member) => false;
			public CPos Home => new(2, 2);
			public CPos? Reinforcement => new(12, 12);
			public (SquadDesireStance Stance, bool Safe, CPos? Target) Evaluate(IBotSquadDesire provider) =>
				(provider.StanceFor(Squad, in Signals), SquadDesireOrders.CanEngage(Signals.PredictedRatioMilli, 50, 150), new CPos(20, 20));
			public void QueueOrder(Actor member, string order, CPos cell) => Emitted.Add((order, cell));
		}

		sealed class ObservedSquad : SquadCA
		{
			public ActorExecution Execution;
			public int FactoryCalls;
			// Tests instantiate without the game-bound constructor using GetUninitializedObject.
			public ObservedSquad() : base(null, null, default) { }
			internal override ISquadDesireExecution<Actor> CreateDesireExecution()
			{
				FactoryCalls++;
				return Execution;
			}
		}

		static ObservedSquad Squad(SquadCAType type, bool armed, IBotSquadDesire provider, out RecordingState legacy)
		{
			var squad = (ObservedSquad)RuntimeHelpers.GetUninitializedObject(typeof(ObservedSquad));
			squad.Type = type;
			squad.SquadManager = Manager(armed, provider);
			squad.Units = new() { default };
			squad.FuzzyStateMachine = new StateMachineCA();
			legacy = new RecordingState();
			squad.FuzzyStateMachine.ChangeState(squad, legacy, false);
			squad.Execution = new ActorExecution { Squad = squad };
			return squad;
		}

		[TestCaseSource(nameof(AllSquadTypes))]
		public void ActualSquadUpdateKeepsItsLegacyFsmWhenOff(SquadCAType type)
		{
			// No world exists on the off path: accidental provider lookup fails the test.
			var squad = Squad(type, false, null, out var legacy);
			squad.Update();
			squad.Update();
			Assert.That(legacy.Ticks, Is.EqualTo(2));
			Assert.That(squad.FactoryCalls, Is.Zero);
			Assert.That(squad.FuzzyStateMachine.CurrentState, Is.SameAs(legacy));
		}

		[TestCaseSource(nameof(AllSquadTypes))]
		public void EveryTypeUsesActualManagerProviderAndControllerWithImmediateSafety(SquadCAType type)
		{
			Assert.That(SquadDesireOrders.Supports(type), Is.True);
			var provider = new MemoryProvider();
			var squad = Squad(type, true, provider, out var legacy);
			squad.Update();
			Assert.That(squad.Execution.Emitted, Is.EqualTo(new[] { ("AttackMove", new CPos(20, 20)) }));
			squad.Execution.Signals = Signals(500, 75);
			squad.Update();
			Assert.That(provider.Last, Is.EqualTo(SquadDesireStance.Attack), "history intentionally retains Attack");
			Assert.That(squad.Execution.Emitted.Last(), Is.EqualTo(("Move", squad.Execution.Home)));
			Assert.That(provider.LastSquad, Is.SameAs(squad), "manager's provider receives the actual typed squad");
			Assert.That(provider.Calls, Is.EqualTo(2));
			Assert.That(squad.FactoryCalls, Is.EqualTo(2));
			Assert.That(legacy.Ticks, Is.Zero, "no parallel legacy order owner while armed");
		}

		[TestCaseSource(nameof(AllSquadTypes))]
		public void EveryTypeWithoutAnEnabledProviderUsesTheLegacyFsm(SquadCAType type)
		{
			var squad = Squad(type, true, null, out var legacy);
			squad.Update();
			Assert.That(legacy.Ticks, Is.EqualTo(1));
			Assert.That(squad.FactoryCalls, Is.Zero);
			Assert.That(squad.Execution.Emitted, Is.Empty);
		}

		static IEnumerable<TestCaseData> AllTypeRoutes
		{
			get
			{
				var routes = new[] {
					(SquadDesireStance.Attack, "AttackMove", new CPos(20, 20)),
					(SquadDesireStance.Defend, "AttackMove", new CPos(2, 2)),
					(SquadDesireStance.Retreat, "Move", new CPos(2, 2)),
					(SquadDesireStance.Regroup, "Move", new CPos(8, 8)),
					(SquadDesireStance.Harass, "AttackMove", new CPos(20, 20)),
					(SquadDesireStance.Reinforce, "Move", new CPos(12, 12)) };
				foreach (var type in AllSquadTypes)
					foreach (var (stance, order, cell) in routes)
						yield return new TestCaseData(type, stance, order, cell);
			}
		}

		[TestCaseSource(nameof(AllTypeRoutes))]
		public void ActualUpdateConsumesEveryStanceForEveryType(SquadCAType type,
			SquadDesireStance stance, string order, CPos cell)
		{
			var provider = new FixedProvider { Stance = stance };
			var squad = Squad(type, true, provider, out var legacy);
			squad.Update();
			squad.Update();
			Assert.That(provider.LastSquad, Is.SameAs(squad));
			Assert.That(squad.Execution.Emitted, Is.EqualTo(new[] { (order, cell) }),
				"the actual controller emits the requested route once and deduplicates its in-flight order");
			Assert.That(legacy.Ticks, Is.Zero);
		}

		static readonly BitSet<TargetableType> Ground = new("Ground");
		static IntegerCombatPredictor.Unit Unit(int hp, int damage, int cycle = 1, string armor = null,
			Dictionary<string, int> versus = null) => new(hp, armor, Ground,
			[new(damage, 1, cycle, Ground, default, versus)]);

		[Test]
		public void IntegerPredictionPinsBoundaryAndIsIndependentOfEnumerationAndOverflow()
		{
			var own = Unit(1000, 3, 7);
			var enemy = Unit(1000, 6, 7);
			Assert.That(IntegerCombatPredictor.RatioMilli([(own, 1)], [(enemy, 1)]), Is.EqualTo(500));
			Assert.That(IntegerCombatPredictor.RatioMilli([(own, 2)], [(own, 1)]), Is.EqualTo(4000));
			var antiHeavy = Unit(500, 300, 11, versus: new() { ["Heavy"] = 200 });
			var heavy = Unit(int.MaxValue, int.MaxValue, 13, "Heavy");
			var a = new[] { (antiHeavy, int.MaxValue), (own, 43) };
			var b = new[] { (heavy, 51), (enemy, int.MaxValue) };
			var ratio = IntegerCombatPredictor.RatioMilli(a, b);
			for (var i = 0; i < 10; i++)
				Assert.That(IntegerCombatPredictor.RatioMilli(a.Reverse().ToArray(), b.Reverse().ToArray()), Is.EqualTo(ratio));
			Assert.That(SquadDesireOrders.CanEngage(1499, 100, 150), Is.False);
			Assert.That(SquadDesireOrders.CanEngage(1500, 100, 150), Is.True);
		}

		[Test]
		public void RegularEvaluationsChangeAnActiveAttackAndDuplicateTickDoesNotAccelerateLeak()
		{
			var a = new SquadDesireMemory();
			var b = new SquadDesireMemory();
			var winning = Signals(4000, 0);
			a.Evaluate(in winning, "rush", 100, 150, 100, 200);
			b.Evaluate(in winning, "rush", 100, 150, 100, 200);
			SquadDesireStance stance = SquadDesireStance.Attack;
			for (var tick = 75; tick < 2500; tick += 75)
			{
				var signals = Signals(0, tick, 0);
				stance = a.Evaluate(in signals, "rush", 100, 150, 100, 200);
				Assert.That(b.Evaluate(in signals, "rush", 100, 150, 100, 200), Is.EqualTo(stance));
				for (var repeat = 0; repeat < 5; repeat++)
					Assert.That(b.Evaluate(in signals, "rush", 100, 150, 100, 200), Is.EqualTo(stance));
			}
			Assert.That(stance, Is.EqualTo(SquadDesireStance.Retreat));
		}
	}
}
