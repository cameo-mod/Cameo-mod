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
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;
using OpenRA.Support;

namespace OpenRA.Mods.Cameo.Test
{
	// S8-CASH-HOLD (row bb_s8_cash_hold, Opus V08G ruling on S8_STARVE_01 line 35): an eligible S8 factory that fails ONLY the cash test holds the S10 fractions of the building queue
	// for at most S8HoldMaxTicks. S9 defence, the defence queue, units and upgrades are never held. The planner walk is tested with a stand-in for A1-A7, the wiring by pins.
	[TestFixture]
	public sealed class S8CashHoldTest
	{
		const string Dir = "mods/CA/Traits/BotModules/BotModuleLogic/";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static MiniYamlNode PlayerNode(string key) =>
			MiniYaml.FromString(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", "mcv_deploy_ai.yaml")).Replace("\r\n", "\n"), "ai")
				.Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == key);

		static string FieldOrNull(MiniYamlNode node, string key) => node.Value.Nodes.SingleOrDefault(n => n.Key == key)?.Value.Value?.Trim();

		sealed class FakeFeasibility : IBaseBuildFeasibility
		{
			public readonly HashSet<string> Infeasible = new(StringComparer.Ordinal);
			public readonly HashSet<string> CashOnly = new(StringComparer.Ordinal);
			public readonly List<string> Tested = new();

			public bool StageAdmits(BaseBuildCandidate candidate) => true;

			public bool Feasible(BaseBuildCandidate candidate, out string admitted)
			{
				Tested.Add(candidate.Name);
				admitted = Infeasible.Contains(candidate.Name) ? null : candidate.Name;
				return admitted != null;
			}

			public bool TryClaim(BaseBuildCandidate candidate, string admitted) => true;

			public bool CashOnlyBlocked(BaseBuildCandidate candidate, int minBankPercent) => CashOnly.Contains(candidate.Name);
		}

		// Sustained cash at the line, fallthrough armed. "barr" is the one listed fraction, owned once, so only the S10 roll can offer it.
		static BaseBuildSnapshot Snapshot(bool hold, Dictionary<BaseBuildStage, string[]> sources, int tick = 1000) => new()
		{
			WorldTick = tick,
			Bank = 5000,
			TierLine = 3500,
			LowCashCut = true,
			SustainedActive = true,
			S8RejectedFallthrough = true,
			S8CashHold = hold,
			S8HoldMaxTicks = 750,
			S8HoldMinBankPercent = 75,
			Owned = new Dictionary<string, int> { ["barr"] = 1 },
			InFlight = new Dictionary<string, int>(),
			BuildingCount = 10,
			Fractions = new[] { new KeyValuePair<string, int>("barr", 10) },
			StageSource = stage => sources.TryGetValue(stage, out var names) ? names : null,
		};

		static readonly Dictionary<BaseBuildStage, string[]> TentOnly = new() { [BaseBuildStage.S8ProductionCycle] = ["tent"] };

		static BaseBuildPick Plan(BaseBuildSnapshot s, FakeFeasibility f) => BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));

		static FakeFeasibility CashBlockedTent()
		{
			var f = new FakeFeasibility();
			f.Infeasible.Add("tent");
			f.CashOnly.Add("tent");
			return f;
		}

		[Test]
		public void ACashOnlyBlockedFactoryHoldsTheFractionsOnlyWhenTheRowIsOn()
		{
			var s = Snapshot(true, TentOnly);
			Assert.That(Plan(s, CashBlockedTent()).IsNone, Is.True, "on: the S10 roll waits for the factory");
			Assert.That(s.S8CashBlocked, Is.EqualTo("tent"));

			s = Snapshot(false, TentOnly);
			var pick = Plan(s, CashBlockedTent());
			Assert.That(pick.Candidate, Is.EqualTo("barr"), "off: the rejected offer falls through to S10, as before");
			Assert.That(s.S8CashBlocked, Is.Null, "off: nothing is recorded");
		}

		[Test]
		public void TheHoldNeverBlocksTheExtraDefenceStage()
		{
			var sources = new Dictionary<BaseBuildStage, string[]>
			{
				[BaseBuildStage.S8ProductionCycle] = ["tent"],
				[BaseBuildStage.S9ExtraDefence] = ["gtwr"],
			};

			var pick = Plan(Snapshot(true, sources), CashBlockedTent());
			Assert.That(pick.Candidate, Is.EqualTo("gtwr"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S9ExtraDefence), "defence spending is a separate queue concern: never held");
		}

		[Test]
		public void TheHoldEndsAfterHoldMaxTicksAndRestartsOnlyForAnotherType()
		{
			var s = Snapshot(true, TentOnly, 1000);
			s.S8HoldName = "tent";
			s.S8HoldStart = 251; // age 749
			Assert.That(Plan(s, CashBlockedTent()).IsNone, Is.True, "749 ticks: still held");

			s = Snapshot(true, TentOnly, 1000);
			s.S8HoldName = "tent";
			s.S8HoldStart = 250; // age 750
			Assert.That(Plan(s, CashBlockedTent()).Candidate, Is.EqualTo("barr"), "750 ticks: the hold is over, S10 runs");

			s = Snapshot(true, TentOnly, 1000);
			s.S8HoldName = "fact"; // a different type waited before: a fresh hold
			s.S8HoldStart = 0;
			Assert.That(Plan(s, CashBlockedTent()).IsNone, Is.True);
		}

		[Test]
		public void OnlyAnEligibleCashFailureOwnsTheHold()
		{
			// the factory fails for another reason (not buildable, delayed, no space, power): CashOnlyBlocked is false, the rejection falls through as before
			var f = new FakeFeasibility();
			f.Infeasible.Add("tent");
			var s = Snapshot(true, TentOnly);
			Assert.That(Plan(s, f).Candidate, Is.EqualTo("barr"));
			Assert.That(s.S8CashBlocked, Is.Null);
		}

		[Test]
		public void AnAdmittedS8StartStillWinsAndTheDefenceQueueIsNeverHeld()
		{
			var s = Snapshot(true, TentOnly);
			var pick = Plan(s, new FakeFeasibility());
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S8ProductionCycle));
			Assert.That(s.S8CashBlocked, Is.Null);

			s = Snapshot(true, TentOnly);
			s.DefenceQueue = true;
			Assert.That(Plan(s, CashBlockedTent()).Candidate, Is.EqualTo("barr"), "the defence queue never owns or obeys the hold");
			Assert.That(s.S8CashBlocked, Is.Null);
		}

		[Test]
		public void TheActivePredicateIsPureAndOffWithoutABlockedType()
		{
			var s = Snapshot(true, TentOnly);
			Assert.That(BaseBuildPlanCA.S8CashHoldActive(s), Is.False, "no blocked type");
			s.S8CashBlocked = "tent";
			Assert.That(BaseBuildPlanCA.S8CashHoldActive(s), Is.True);
			s.S8CashHold = false;
			Assert.That(BaseBuildPlanCA.S8CashHoldActive(s), Is.False, "row off");
		}

		[Test]
		public void TheWiringIsPinned()
		{
			var plan = Source(Dir + "BaseBuildPlanCA.cs");
			Assert.That(plan, Does.Contain("if (stage == BaseBuildStage.S10Fractions && S8CashHoldActive(snapshot))\n\t\t\t\t\tcontinue;"), "only S10 is held, never S9");
			Assert.That(plan, Does.Contain("bool CashOnlyBlocked(BaseBuildCandidate candidate, int minBankPercent) => false;"));
			Assert.That(plan, Does.Contain("!snapshot.DefenceQueue && feasibility.CashOnlyBlocked(candidate, snapshot.S8HoldMinBankPercent)"));
			Assert.That(plan, Does.Contain("cashHold ? \"s8_cash_hold\""), "the reason is recorded in the DEV row");

			var qm = Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01.cs");
			Assert.That(qm, Does.Contain("S8CashHold = baseBuilder.S8CashHoldArmed,"));
			Assert.That(qm, Does.Contain("S8HoldUpdate(snapshot, pick);"));
			Assert.That(qm, Does.Contain("cashCost = startCost;\n\t\t\t\t\treturn false;"), "only the A6 cash failure reports a cost");
			Assert.That(qm, Does.Contain("reason s8_cash_hold"));

			var search = Source(Dir + "RefinerySiteSearch.cs");
			Assert.That(search, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid Dev(Episode e, int now, string evt, string fields)"), "a release build drops the trace arguments");
		}

		[Test]
		public void OnlyTheGenericBaseBuilderArmsItAndTheRowIsRegisteredAndOffByDefault()
		{
			var bb = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			Assert.That(bb, Does.Contain("public bool S8CashHoldArmed => Info.BbS8CashHold && S8RejectedFallthroughArmed && !ablateS8CashHold.Off(player, AblationIds.BbS8CashHold);"));

			var generic = PlayerNode("BaseBuilderBotModuleCA@generic");
			Assert.That(FieldOrNull(generic, "BbS8CashHold"), Is.EqualTo("true"));
			Assert.That(FieldOrNull(generic, "S8HoldMaxTicks"), Is.EqualTo("750"));
			Assert.That(FieldOrNull(PlayerNode("BaseBuilderBotModuleCA@classic"), "BbS8CashHold"), Is.Null);
			Assert.That(AblationIds.BbS8CashHold, Is.EqualTo("bb_s8_cash_hold"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain(AblationIds.BbS8CashHold));

			var info = new BaseBuilderBotModuleCAInfo();
			Assert.That(info.BbS8CashHold, Is.False);
			Assert.That(info.S8HoldMaxTicks, Is.EqualTo(750));
		}
	}
}
