using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.CA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class OffensiveActivityObservationTest
	{
		sealed class ObservedAttack : Activity
		{
			public Target Goal = Target.FromPos(new WPos(100, 0, 0));
			public bool LinesOnly;
			public ObservedAttack() { ActivityType = ActivityType.Attack; }
			public override bool Tick(Actor self) => false;
			public override IEnumerable<Target> GetTargets(Actor self) => LinesOnly ? Target.None : new[] { Goal };
			public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self) =>
				new[] { new TargetLineNode(Goal, default) };
		}

		sealed class ObservedMove : AttackMoveActivity
		{
			public Target Goal;
			// Uninitialized test instance avoids world-bound engine trait construction.
			public ObservedMove() : base(null, null) { }
			public void Configure() { ActivityType = ActivityType.Move; }
			protected override void OnFirstRun(Actor self) { }
			public override bool Tick(Actor self) => false;
			public override IEnumerable<Target> GetTargets(Actor self) => new[] { Goal };
		}

		sealed class GenericMove : Activity
		{
			public GenericMove() { ActivityType = ActivityType.Move; }
			public override bool Tick(Actor self) => false;
			public override IEnumerable<Target> GetTargets(Actor self) => new[] { Target.FromPos(new WPos(100, 0, 0)) };
		}

		static ObservedMove Move()
		{
			var move = (ObservedMove)RuntimeHelpers.GetUninitializedObject(typeof(ObservedMove));
			move.Configure();
			move.Goal = Target.FromPos(new WPos(100, 0, 0));
			move.TickOuter(null);
			return move;
		}

		[TestCase(false)]
		[TestCase(true)]
		public void ActualPublicActiveAttackTargetsAreObservedWithoutDispatchProof(bool linesOnly)
		{
			var attack = new ObservedAttack { LinesOnly = linesOnly };
			var observer = new OffensiveActivityObservationCA();
			Assert.That(observer.Observe(0, attack, null, WPos.Zero, true, false, _ => true), Is.False, "queued intent is not active");
			attack.TickOuter(null);
			Assert.That(observer.Observe(1, attack, null, WPos.Zero, true, false, _ => true), Is.True);
			var state = new AttackLivenessEvalCA();
			state.Observe(0, true, true, true, true, 1000, 1000, 7500, 0, 0);
			Assert.That(state.ObserveOffensiveActivity(1, 1, 1, true, true, true, true), Is.True);
			Assert.That(state.LastObservedOffensiveTick, Is.EqualTo(1));
		}

		[TestCase(false, false, true)]
		[TestCase(true, true, true)]
		[TestCase(true, false, false)]
		public void ForeignIdleOrHiddenIllegalTargetCannotReset(bool owned, bool idle, bool legal)
		{
			var attack = new ObservedAttack();
			attack.TickOuter(null);
			Assert.That(new OffensiveActivityObservationCA().Observe(0, attack, null, WPos.Zero, owned, idle, _ => legal), Is.False);
		}

		[Test]
		public void AttackMoveRequiresSameActivityAndActualForwardProgressNotStuckOrTargetDrift()
		{
			var observer = new OffensiveActivityObservationCA();
			var move = Move();
			Assert.That(observer.Observe(0, move, null, WPos.Zero, true, false, _ => true), Is.False);
			Assert.That(observer.Observe(1, move, null, WPos.Zero, true, false, _ => true), Is.False);
			move.Goal = Target.FromPos(new WPos(90, 0, 0));
			Assert.That(observer.Observe(2, move, null, WPos.Zero, true, false, _ => true), Is.False);
			Assert.That(observer.Observe(3, move, null, new WPos(10, 0, 0), true, false, _ => true), Is.True);
			Assert.That(observer.Observe(3, move, null, new WPos(20, 0, 0), true, false, _ => true), Is.False);
			Assert.That(observer.Observe(4, move, null, new WPos(10, 0, 0), true, false, _ => true), Is.False, "moving away");
			Assert.That(observer.Observe(5, Move(), null, new WPos(20, 0, 0), true, false, _ => true), Is.False, "replacement activity must establish a new baseline");
		}

		[Test]
		public void RetreatTransportAndCancelingActivitiesNeverCountEvenTowardSameCell()
		{
			var observer = new OffensiveActivityObservationCA();
			var generic = new GenericMove();
			generic.TickOuter(null);
			observer.Observe(0, generic, null, WPos.Zero, true, false, _ => true);
			Assert.That(observer.Observe(1, generic, null, new WPos(10, 0, 0), true, false, _ => true), Is.False);
			var move = Move();
			move.Cancel(null);
			Assert.That(observer.Observe(2, move, null, new WPos(20, 0, 0), true, false, _ => true), Is.False);
			Assert.That(observer.Observe(3, null, null, WPos.Zero, true, false, _ => true), Is.False);
		}

		static IEnumerable<Type> RetreatStates => new[] {
			typeof(GroundUnitsFleeStateCA), typeof(NavyUnitsFleeStateCA), typeof(AirFleeStateCA),
			typeof(StealthFleeStateCA), typeof(UnitsForProtectionFleeState) };

		[TestCaseSource(nameof(RetreatStates))]
		public void ActualRetreatStateIsExcludedBeforeReadingActivityTargets(Type type)
		{
			var squad = (SquadCA)RuntimeHelpers.GetUninitializedObject(typeof(SquadCA));
			squad.FuzzyStateMachine = new StateMachineCA();
			// Install without Activate: this test checks the real manager's state exclusion.
			typeof(StateMachineCA).GetField("currentState", System.Reflection.BindingFlags.NonPublic
				| System.Reflection.BindingFlags.Instance).SetValue(squad.FuzzyStateMachine,
				RuntimeHelpers.GetUninitializedObject(type));
			Assert.That(SquadManagerBotModuleCA.IsRetreatingForLiveness(squad), Is.True);
		}

		[Test]
		public void AttackMoveWithUnrelatedAbilityChildCannotCountItsParentMove()
		{
			var move = Move();
			var child = new GenericMove();
			move.QueueChild(child);
			Assert.That(new OffensiveActivityObservationCA().Observe(0, move, null, WPos.Zero, true, false, _ => true), Is.False, "queued child is not active");
		}
	}
}
