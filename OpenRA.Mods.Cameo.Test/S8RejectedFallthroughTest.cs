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
	// S8-STARVE-01 (row bb_s8_rejected_fallthrough, Sol 58ADBC25, Opus V08G): under SUSTAINED cash an S8 type that is only offered and then rejected no longer holds S9 and
	// S10 back. The pure rule and the walk (stand-in for A1-A7) are tested directly, the wiring by pins. Off = the old offer-only hold, byte for byte.
	[TestFixture]
	public sealed class S8RejectedFallthroughTest
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
			public readonly HashSet<BaseBuildStage> StagesHeld = new();
			public readonly List<string> Tested = new();

			public bool StageAdmits(BaseBuildCandidate candidate) => !StagesHeld.Contains(candidate.Stage);

			public bool Feasible(BaseBuildCandidate candidate, out string admitted)
			{
				Tested.Add(candidate.Name);
				admitted = Infeasible.Contains(candidate.Name) ? null : candidate.Name;
				return admitted != null;
			}

			public bool TryClaim(BaseBuildCandidate candidate, string admitted) => true;
		}

		// Sustained cash at the line: bank 5000, line 3500. "barr" is the one listed fraction, owned once, so only the S10 roll can offer it.
		static BaseBuildSnapshot Snapshot(bool fallthrough, Dictionary<BaseBuildStage, string[]> sources) => new()
		{
			Bank = 5000,
			TierLine = 3500,
			LowCashCut = true,
			SustainedActive = true,
			S8RejectedFallthrough = fallthrough,
			Owned = new Dictionary<string, int> { ["barr"] = 1 },
			InFlight = new Dictionary<string, int>(),
			BuildingCount = 10,
			Fractions = new[] { new KeyValuePair<string, int>("barr", 10) },
			StageSource = stage => sources.TryGetValue(stage, out var names) ? names : null,
		};

		static BaseBuildPick Plan(BaseBuildSnapshot s, FakeFeasibility f) => BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));

		[Test]
		public void CycleHoldsIsTheOldRuleWhenOffAndNeverWhenOn()
		{
			Assert.That(BaseBuildTiersCA.CycleHolds(true, false), Is.True, "off: an enumerated offer holds S9 and S10");
			Assert.That(BaseBuildTiersCA.CycleHolds(false, false), Is.False, "off: an empty cycle holds nothing");
			Assert.That(BaseBuildTiersCA.CycleHolds(true, true), Is.False, "on: a bare offer holds nothing");
			Assert.That(BaseBuildTiersCA.CycleHolds(false, true), Is.False);
		}

		[Test]
		public void ARejectedOfferFallsThroughToTheExtrasOnlyWhenTheRowIsOn()
		{
			var sources = new Dictionary<BaseBuildStage, string[]>
			{
				[BaseBuildStage.S8ProductionCycle] = ["tent"],
				[BaseBuildStage.S9ExtraDefence] = ["gtwr"],
			};

			// a rejected S8 with one failed feasibility test still falls through under the budget
			var f = new FakeFeasibility();
			f.Infeasible.Add("tent");
			var pick = Plan(Snapshot(true, sources), f);
			Assert.That(pick.Candidate, Is.EqualTo("gtwr"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S9ExtraDefence));
			Assert.That(f.Tested, Is.EqualTo(new[] { "tent", "gtwr" }));

			f = new FakeFeasibility();
			f.Infeasible.Add("tent");
			pick = Plan(Snapshot(false, sources), f);
			Assert.That(pick.IsNone, Is.True, "off: S9 and S10 are held back by the offer alone");
			Assert.That(f.Tested, Is.EqualTo(new[] { "tent" }));
		}

		[Test]
		public void AnOfferRejectedBeforeFeasibilityFallsThroughToo()
		{
			var sources = new Dictionary<BaseBuildStage, string[]>
			{
				[BaseBuildStage.S8ProductionCycle] = ["tent"],
				[BaseBuildStage.S9ExtraDefence] = ["gtwr"],
			};

			var f = new FakeFeasibility();
			f.StagesHeld.Add(BaseBuildStage.S8ProductionCycle);
			var pick = Plan(Snapshot(true, sources), f);
			Assert.That(pick.Candidate, Is.EqualTo("gtwr"), "the stage hold of S8 costs no feasibility test and no longer starves S9");
			Assert.That(f.Tested, Is.EqualTo(new[] { "gtwr" }));

			f = new FakeFeasibility();
			f.StagesHeld.Add(BaseBuildStage.S8ProductionCycle);
			pick = Plan(Snapshot(false, sources), f);
			Assert.That(pick.IsNone, Is.True, "off: the offer alone holds S9 and S10 back");
			Assert.That(f.Tested, Is.Empty);
		}

		[Test]
		public void TheRollAtS10FallsThroughToo()
		{
			var sources = new Dictionary<BaseBuildStage, string[]> { [BaseBuildStage.S8ProductionCycle] = ["tent"] };

			var f = new FakeFeasibility();
			f.Infeasible.Add("tent");
			var pick = Plan(Snapshot(true, sources), f);
			Assert.That(pick.Candidate, Is.EqualTo("barr"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S10Fractions));

			f = new FakeFeasibility();
			f.Infeasible.Add("tent");
			Assert.That(Plan(Snapshot(false, sources), f).IsNone, Is.True);
		}

		[Test]
		public void AnAdmittedS8StillWinsAndAnEmptyCycleIsTheBaseline()
		{
			var sources = new Dictionary<BaseBuildStage, string[]>
			{
				[BaseBuildStage.S8ProductionCycle] = ["tent"],
				[BaseBuildStage.S9ExtraDefence] = ["gtwr"],
			};

			foreach (var on in new[] { true, false })
			{
				var f = new FakeFeasibility();
				var pick = Plan(Snapshot(on, sources), f);
				Assert.That(pick.Candidate, Is.EqualTo("tent"), "admitted S8 first, row " + on);
				Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S8ProductionCycle));
				Assert.That(f.Tested, Is.EqualTo(new[] { "tent" }));
			}

			// no S8 candidate at all: both settings walk exactly the same
			var empty = new Dictionary<BaseBuildStage, string[]> { [BaseBuildStage.S9ExtraDefence] = ["gtwr"] };
			foreach (var on in new[] { true, false })
			{
				var f = new FakeFeasibility();
				var pick = Plan(Snapshot(on, empty), f);
				Assert.That(pick.Candidate, Is.EqualTo("gtwr"), "empty cycle, row " + on);
				Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S9ExtraDefence));
				Assert.That(f.Tested, Is.EqualTo(new[] { "gtwr" }));
			}
		}

		[Test]
		public void LowCashAndTheFeasibilityBudgetKeepTheirSemantics()
		{
			// below the line S8 is closed (nothing offered), S9 needs a seen threat: identical with the row on and off
			var sources = new Dictionary<BaseBuildStage, string[]>
			{
				[BaseBuildStage.S8ProductionCycle] = ["tent"],
				[BaseBuildStage.S9ExtraDefence] = ["gtwr"],
			};

			foreach (var on in new[] { true, false })
			{
				var s = Snapshot(on, sources);
				s.Bank = 1800;
				var f = new FakeFeasibility();
				Assert.That(Plan(s, f).IsNone, Is.True, "low cash, row " + on);
				Assert.That(f.Tested, Is.Empty);

				s.DefenceThreatSeen = true;
				f = new FakeFeasibility();
				Assert.That(Plan(s, f).Candidate, Is.EqualTo("gtwr"), "a seen threat keeps S9 open below the line, row " + on);
			}

			// an exhausted test budget stops the walk before any later stage, row on or off
			var many = Enumerable.Range(0, 9).Select(i => "t" + i).ToArray();
			var capped = new Dictionary<BaseBuildStage, string[]>
			{
				[BaseBuildStage.S8ProductionCycle] = many,
				[BaseBuildStage.S9ExtraDefence] = ["gtwr"],
			};

			foreach (var on in new[] { true, false })
			{
				var f = new FakeFeasibility();
				foreach (var n in many)
					f.Infeasible.Add(n);

				var pick = Plan(Snapshot(on, capped), f);
				Assert.That(pick.IsNone, Is.True, "budget exhausted, row " + on);
				Assert.That(f.Tested.Count, Is.EqualTo(BaseBuildPlanCA.MaxFeasibilityTests), "row " + on);
				Assert.That(f.Tested, Does.Not.Contain("gtwr"));
			}
		}

		[Test]
		public void TheRefineryStagesAreUntouchedAndS8StaysAboveS7()
		{
			var sources = new Dictionary<BaseBuildStage, string[]>
			{
				[BaseBuildStage.S2ExpansionRefinery] = ["proc2"],
				[BaseBuildStage.S7RefineryPerField] = ["proc"],
				[BaseBuildStage.S8ProductionCycle] = ["tent"],
			};

			foreach (var on in new[] { true, false })
			{
				var f = new FakeFeasibility();
				f.Infeasible.Add("tent");
				var pick = Plan(Snapshot(on, sources), f);
				Assert.That(pick.Candidate, Is.EqualTo("proc2"), "S2 stays first, row " + on);
				Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S2ExpansionRefinery));

				f = new FakeFeasibility();
				f.Infeasible.Add("proc2");
				f.Infeasible.Add("tent");
				pick = Plan(Snapshot(on, sources), f);
				Assert.That(pick.Candidate, Is.EqualTo("proc"), "S7 after the refused S8, row " + on);
				Assert.That(f.Tested, Is.EqualTo(new[] { "proc2", "tent", "proc" }));
			}
		}

		[Test]
		public void SharedAndSplitQueuesBothFollowTheRow()
		{
			var sources = new Dictionary<BaseBuildStage, string[]>
			{
				[BaseBuildStage.S8ProductionCycle] = ["tent"],
				[BaseBuildStage.S9ExtraDefence] = ["gtwr"],
			};

			// split, building queue: S4 and S9 are not walked, the roll at S10 is what the row opens
			var s = Snapshot(true, sources);
			s.QueueSplit = true;
			s.DefenceQueue = false;
			var f = new FakeFeasibility();
			f.Infeasible.Add("tent");
			var pick = Plan(s, f);
			Assert.That(pick.Candidate, Is.EqualTo("barr"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S10Fractions));

			s = Snapshot(false, sources);
			s.QueueSplit = true;
			s.DefenceQueue = false;
			f = new FakeFeasibility();
			f.Infeasible.Add("tent");
			Assert.That(Plan(s, f).IsNone, Is.True, "off: held");

			// split, defence queue: it never walks S8, so nothing is offered and the row changes nothing
			foreach (var on in new[] { true, false })
			{
				s = Snapshot(on, sources);
				s.QueueSplit = true;
				s.DefenceQueue = true;
				f = new FakeFeasibility();
				pick = Plan(s, f);
				Assert.That(pick.Candidate, Is.EqualTo("gtwr"), "defence queue, row " + on);
				Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S9ExtraDefence));
			}
		}

		[Test]
		public void TheWiringIsGenericOnlyOffByDefaultRegisteredAndTheDevTraceIsCompiledOut()
		{
			var plan = Source(Dir + "BaseBuildPlanCA.cs");
			Assert.That(plan, Does.Contain("cycleOffered |= stage == BaseBuildStage.S8ProductionCycle && candidate.Name != null;"), "the enumerated-offer bit is unchanged");
			Assert.That(plan, Does.Contain("BaseBuildTiersCA.CycleHolds(cycleOffered, snapshot.S8RejectedFallthrough)"));
			Assert.That(plan, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tstatic void NoteS8Offer("));

			var p = Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01.cs");
			Assert.That(p, Does.Contain("S8RejectedFallthrough = baseBuilder.S8RejectedFallthroughArmed,"));
			Assert.That(p, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid S8FallthroughObserved("));
			Assert.That(p, Does.Contain("BB-S8-FALLTHROUGH queue {Category} tick {now}"));

			var bb = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			Assert.That(bb, Does.Contain("public bool S8RejectedFallthroughArmed => Info.BbS8RejectedFallthrough && switchesActive && !ablateS8RejectedFallthrough.Off(player, AblationIds.BbS8RejectedFallthrough);"));

			Assert.That(FieldOrNull(PlayerNode("BaseBuilderBotModuleCA@generic"), "BbS8RejectedFallthrough"), Is.EqualTo("true"));
			Assert.That(FieldOrNull(PlayerNode("BaseBuilderBotModuleCA@classic"), "BbS8RejectedFallthrough"), Is.Null);
			Assert.That(AblationIds.BbS8RejectedFallthrough, Is.EqualTo("bb_s8_rejected_fallthrough"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain(AblationIds.BbS8RejectedFallthrough));
			Assert.That(new BaseBuilderBotModuleCAInfo().BbS8RejectedFallthrough, Is.False);
		}
	}
}
