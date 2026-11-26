// MCV-DEPLOY-FAIL-01 (RCA a61964a8): pure tests for the per-MCV deploy-failure breaker, and source pins for its wiring in the planner.
using System.IO;
using NUnit.Framework;
using DeployBreaker = OpenRA.Mods.Cameo.Traits.BotModules.ExpansionPlannerBotModule.McvDeployFailure;
using DeployPhase = OpenRA.Mods.Cameo.Traits.BotModules.ExpansionPlannerBotModule.McvDeployPhase;
using DeployState = OpenRA.Mods.Cameo.Traits.BotModules.ExpansionPlannerBotModule.McvDeployFailState;
using HandoutKind = OpenRA.Mods.Cameo.Traits.BotModules.ExpansionPlannerBotModule.McvHandoutKind;

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
				s = DeployBreaker.OnFailed(s, HandoutKind.Field, 100 + i, 1500);

			return s;
		}

		[Test]
		public void FreshMcvHandsOutFields()
		{
			Assert.That(DeployBreaker.Decide(default(DeployState), 0, 6), Is.EqualTo(DeployPhase.Fields));
		}

		[Test]
		public void FieldFailuresBelowMaxStayOnFields()
		{
			var s = FailFields(5);
			Assert.That(s.Failures, Is.EqualTo(5));
			Assert.That(DeployBreaker.Decide(s, 200, 6), Is.EqualTo(DeployPhase.Fields));
		}

		[Test]
		public void SixthFieldFailureSendsOneBaseTry()
		{
			Assert.That(DeployBreaker.Decide(FailFields(6), 200, 6), Is.EqualTo(DeployPhase.Base));
		}

		[Test]
		public void BaseFailureHoldsForHoldTicks()
		{
			var s = DeployBreaker.OnFailed(FailFields(6), HandoutKind.Base, 1000, 1500);
			Assert.That(s.HoldUntil, Is.EqualTo(2500));
			Assert.That(DeployBreaker.Decide(s, 1001, 6), Is.EqualTo(DeployPhase.Hold));
			Assert.That(DeployBreaker.Decide(s, 2499, 6), Is.EqualTo(DeployPhase.Hold));
		}

		[Test]
		public void HoldExpiryResetsToFields()
		{
			var held = DeployBreaker.OnFailed(FailFields(6), HandoutKind.Base, 1000, 1500);
			var stillHeld = DeployBreaker.Expire(held, 2499);
			Assert.That(stillHeld.Failures, Is.EqualTo(7));
			Assert.That(stillHeld.HoldUntil, Is.EqualTo(2500));

			var expired = DeployBreaker.Expire(held, 2500);
			Assert.That(expired.Failures, Is.EqualTo(0));
			Assert.That(expired.HoldUntil, Is.EqualTo(0));
			Assert.That(DeployBreaker.Decide(expired, 2500, 6), Is.EqualTo(DeployPhase.Fields));
		}

		[Test]
		public void FieldFailureNeverHolds()
		{
			var s = DeployBreaker.OnFailed(default(DeployState), HandoutKind.Field, 1000, 1500);
			Assert.That(s.Failures, Is.EqualTo(1));
			Assert.That(s.HoldUntil, Is.EqualTo(0));
		}

		[Test]
		public void DisabledBreakerAlwaysHandsOutFields()
		{
			Assert.That(DeployBreaker.Decide(new DeployState(99, 0), 0, 0), Is.EqualTo(DeployPhase.Fields));
		}

		[Test]
		public void IsFailureNeedsIdleAliveAndALaterTick()
		{
			Assert.That(DeployBreaker.IsFailure(100, 100, true, true), Is.False);
			Assert.That(DeployBreaker.IsFailure(100, 101, false, true), Is.False);
			Assert.That(DeployBreaker.IsFailure(100, 101, true, false), Is.False);
			Assert.That(DeployBreaker.IsFailure(100, 101, true, true), Is.True);
		}

		[Test]
		public void SourcePinSwitchAndTunablesHaveTheirDefaults()
		{
			var planner = Planner();
			Assert.That(planner, Does.Contain("public readonly bool McvDeployFailPark = false;"));
			Assert.That(planner, Does.Contain("public readonly int DeployFailParkTicks = 600;"));
			Assert.That(planner, Does.Contain("public readonly int McvMaxFailedDeploys = 6;"));
			Assert.That(planner, Does.Contain("public readonly int McvFailHoldTicks = 1500;"));
		}

		[Test]
		public void SourcePinBreakerAndAblationAreWired()
		{
			var planner = Planner();
			Assert.That(planner, Does.Contain("AblationIds.McvDeployFailPark"));
			Assert.That(planner, Does.Contain("McvDeployFailure.Decide(failState, tick, Info.McvMaxFailedDeploys)"));
			Assert.That(planner, Does.Contain("NoteMcvHandout(mcv, McvHandoutKind.Field, s.Index, s.Center, tick);"));
		}

		[Test]
		public void SourcePinGuardCallsDeployFailedOnlyOnIdleMcvs()
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
			Assert.That(safe, Does.Contain("source=planner_handout"));
		}
	}
}
