// MCV-DEPLOY-FAIL-01 (RCA a61964a8): pure tests for the per-MCV deploy-failure breaker, and source pins for its wiring in the planner.
using System;
using System.IO;
using NUnit.Framework;
using DeployBreaker = OpenRA.Mods.Cameo.Traits.BotModules.ExpansionPlannerBotModule.McvDeployFailure;
using DeployPhase = OpenRA.Mods.Cameo.Traits.BotModules.ExpansionPlannerBotModule.McvDeployPhase;
using DeployState = OpenRA.Mods.Cameo.Traits.BotModules.ExpansionPlannerBotModule.McvDeployFailState;
using McvKind = OpenRA.Mods.Cameo.Traits.BotModules.ExpansionPlannerBotModule.McvHandoutKind;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class McvDeployFailTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string Planner() => Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.cs");

		static string Safe() => Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.McvSafe.cs");

		static DeployState FailFields(int count)
		{
			var s = default(DeployState);
			for (var i = 0; i < count; i++)
				s = DeployBreaker.OnFailed(s);

			return s;
		}

		[Test]
		public void FreshMcvHandsOutFields()
		{
			Assert.That(DeployBreaker.Decide(default(DeployState), 6), Is.EqualTo(DeployPhase.Fields));
		}

		[Test]
		public void FieldFailuresBelowMaxStayOnFields()
		{
			var s = FailFields(5);
			Assert.That(s.Failures, Is.EqualTo(5));
			Assert.That(DeployBreaker.Decide(s, 6), Is.EqualTo(DeployPhase.Fields));
		}

		[Test]
		public void MaxFailureCountSendsOneBaseTry()
		{
			Assert.That(DeployBreaker.Decide(FailFields(6), 6), Is.EqualTo(DeployPhase.Base));
		}

		[Test]
		public void AfterTheBaseTryFieldsResume()
		{
			var s = DeployBreaker.OnFailed(FailFields(6));
			Assert.That(s.Failures, Is.EqualTo(7));
			Assert.That(DeployBreaker.Decide(s, 6), Is.EqualTo(DeployPhase.Fields));
		}

		[Test]
		public void BaseTryIsNotRepeatedWhileFailuresKeepGrowing()
		{
			Assert.That(DeployBreaker.Decide(FailFields(8), 6), Is.EqualTo(DeployPhase.Fields));
			Assert.That(DeployBreaker.Decide(FailFields(9), 6), Is.EqualTo(DeployPhase.Fields));
		}

		[Test]
		public void DisabledBreakerAlwaysHandsOutFields()
		{
			Assert.That(DeployBreaker.Decide(new DeployState(6), 0), Is.EqualTo(DeployPhase.Fields));
			Assert.That(DeployBreaker.Decide(new DeployState(99), 0), Is.EqualTo(DeployPhase.Fields));
		}

		[Test]
		public void IsFailureNeedsIdleAliveAndPastTheGrace()
		{
			Assert.That(DeployBreaker.IsFailure(100, 100, true, true), Is.False);
			Assert.That(DeployBreaker.IsFailure(100, 101, false, true), Is.False);
			Assert.That(DeployBreaker.IsFailure(100, 130, true, false), Is.False);
			Assert.That(DeployBreaker.IsFailure(100, 130, true, true), Is.True);
		}

		[Test]
		public void JustIssuedIdleMcvIsNotAFailureInsideTheGrace()
		{
			// A bot order is not executed until its order latency has passed (FeedbackTime = 30 "must be >= netlag"): a pass inside that window can see the MCV still idle.
			Assert.That(DeployBreaker.IsFailure(100, 101, true, true), Is.False);
			Assert.That(DeployBreaker.IsFailure(100, 115, true, true), Is.False);
			Assert.That(DeployBreaker.IsFailure(100, 129, true, true), Is.False);
			Assert.That(DeployBreaker.DeployGraceTicks, Is.EqualTo(30));
		}

		[Test]
		public void TheEnginesReissueOfTheSameSiteKeepsTheFirstHandoutTick()
		{
			// The engine re-asks an idle MCV every 20 ticks, which is inside the 30-tick grace: a re-issue that restarted the clock would hide every failure.
			Assert.That(DeployBreaker.SameHandout(McvKind.Field, 4, McvKind.Field, 4), Is.True);
			Assert.That(DeployBreaker.SameHandout(McvKind.Field, 4, McvKind.Field, 5), Is.False, "another field is a new handout");
			Assert.That(DeployBreaker.SameHandout(McvKind.Field, -1, McvKind.Base, -1), Is.False, "a base try is a new handout");
			Assert.That(DeployBreaker.SameHandout(McvKind.Base, -1, McvKind.Base, -1), Is.True);
			Assert.That(Safe(), Does.Contain("McvDeployFailure.SameHandout(held.Kind, held.Field, kind, field))\n\t\t\t\treturn;"));
		}

		[Test]
		public void TheBreakerNeverStarvesAnMcvWhenThereIsNoBaseSite()
		{
			// Sonnet FIX A: at the max count with no base fallback the field loop goes on (a failure there counts, the breaker leaves Base).
			var planner = Planner();
			Assert.That(planner, Does.Contain("== McvDeployPhase.Base && McvSafeFallback(safe, mcv) is CPos baseCell)"));
			Assert.That(planner, Does.Not.Contain("return baseSite;"));
			var s = FailFields(6);
			Assert.That(DeployBreaker.Decide(s, 6), Is.EqualTo(DeployPhase.Base));
			Assert.That(DeployBreaker.Decide(DeployBreaker.OnFailed(s), 6), Is.EqualTo(DeployPhase.Fields), "one more failed field handout and the base phase is over");
		}

		[Test]
		public void SourcePinSwitchAndTunablesHaveTheirDefaults()
		{
			var planner = Planner();
			Assert.That(planner, Does.Contain("public readonly bool McvDeployFailPark = false;"));
			Assert.That(planner, Does.Contain("public readonly int DeployFailParkTicks = 600;"));
			Assert.That(planner, Does.Contain("public readonly int McvMaxFailedDeploys = 6;"));
		}

		[Test]
		public void SourcePinNoHoldIsLeft()
		{
			Assert.That(Planner(), Does.Not.Contain("McvFailHoldTicks"));
			Assert.That(Safe(), Does.Not.Contain("HoldUntil"));
		}

		[Test]
		public void SourcePinBreakerAndAblationAreWired()
		{
			var planner = Planner();
			Assert.That(planner, Does.Contain("AblationIds.McvDeployFailPark"));
			Assert.That(planner, Does.Contain("McvDeployFailure.Decide(known, Info.McvMaxFailedDeploys) == McvDeployPhase.Base"));
			Assert.That(planner, Does.Contain("NoteMcvHandout(mcv, McvHandoutKind.Field, s.Index, s.Center, tick);"));
		}

		[Test]
		public void SourcePinGuardCallsDeployFailedOnIdleMcvs()
		{
			var safe = Safe();
			Assert.That(safe, Does.Contain("McvDeployFailed(mcv, now);"));
			Assert.That(safe, Does.Contain("parkedUntil[pending.Field] = now + Info.DeployFailParkTicks;"));
			Assert.That(safe, Does.Contain("mcvPending.Remove(id);"));
		}

		[Test]
		public void SourcePinDevRowCarriesTheDiagnosticFields()
		{
			var safe = Safe();
			Assert.That(safe, Does.Contain("mcv_deploy_fail"));
			Assert.That(safe, Does.Contain("move_result="));
			Assert.That(safe, Does.Contain("can_place="));
			Assert.That(safe, Does.Contain("order_source=unknown"));
		}
	}
}
