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
	// CONSOLIDATE-03 P1 (b), row bb_s5_capture_reason (CONSOLIDATE_03_P1_DESIGN_01.md): with listed-only first copies (bb_build_all_listed) the S5 source was never asked, so
	// captureUnlockOffered stayed empty and cs_capture_unlock never fired in @generic. On, S5 asks the source after the listed roll and offers only the capture unlocks. The tech-depth
	// fallback stays off.
	[TestFixture]
	public sealed class S5CaptureReasonTest
	{
		const string Dir = "mods/CA/Traits/BotModules/BotModuleLogic/";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		sealed class AllFeasible : IBaseBuildFeasibility
		{
			public readonly List<string> Tested = new();

			public bool StageAdmits(BaseBuildCandidate candidate) => true;

			public bool Feasible(BaseBuildCandidate candidate, out string admitted)
			{
				Tested.Add(candidate.Name);
				admitted = candidate.Name;
				return true;
			}

			public bool TryClaim(BaseBuildCandidate candidate, string admitted) => true;
		}

		// Every listed type stands once, so the listed roll offers nothing. The S5 source offers a tech-depth first copy ("fix") and a capture unlock ("npower").
		static BaseBuildSnapshot Snapshot(bool captureReason, bool listedOnly = true) => new()
		{
			WorldTick = 1000,
			Bank = 5000,
			TierLine = 3500,
			LowCashCut = true,
			CoreOnceListedOnly = listedOnly,
			S5CaptureReason = captureReason,
			CaptureUnlockOf = name => name == "npower",
			Owned = new Dictionary<string, int> { ["barr"] = 1 },
			InFlight = new Dictionary<string, int>(),
			BuildingCount = 10,
			Fractions = new[] { new KeyValuePair<string, int>("barr", 10) },
			StageSource = stage => stage == BaseBuildStage.S5CoreOnce ? new[] { "fix", "npower" } : null,
		};

		static IEnumerable<string> S5(BaseBuildSnapshot s) =>
			BaseBuildPlanCA.StageCandidates(BaseBuildStage.S5CoreOnce, s, new MersenneTwister(1)).Select(c => c.Name);

		[Test]
		public void ListedOnlyOffersTheCaptureUnlockOnlyWhenTheRowIsOn()
		{
			Assert.That(S5(Snapshot(false)), Is.Empty, "off: the listed-only S5 stops after the roll, as before");
			Assert.That(S5(Snapshot(true)), Is.EqualTo(new[] { "npower" }), "on: the capture unlock, never the tech-depth first copy");
			Assert.That(S5(Snapshot(true, listedOnly: false)), Is.EqualTo(new[] { "fix", "npower" }), "without listed-only the whole fallback runs, unchanged");
			Assert.That(S5(Snapshot(false, listedOnly: false)), Is.EqualTo(new[] { "fix", "npower" }));
		}

		[Test]
		public void TheCaptureUnlockIsPickedAtS5AndNamedByItsReason()
		{
			var decision = BuildingPickArbiter.Decide(Snapshot(true), new AllFeasible(), new MersenneTwister(1), null);
			Assert.That(decision.Pick.Stage, Is.EqualTo(BaseBuildStage.S5CoreOnce));
			Assert.That(decision.Pick.Candidate, Is.EqualTo("npower"));
			Assert.That(decision.Decider, Is.EqualTo(BuildingRule.FirstCopyCaptureUnlock));

			var off = BuildingPickArbiter.Decide(Snapshot(false), new AllFeasible(), new MersenneTwister(1), null);
			Assert.That(off.Pick.Candidate, Is.Not.EqualTo("npower"), "off: S5 offers no capture unlock");
		}

		[Test]
		public void NoCaptureLookupMeansNoExtraOffer()
		{
			var s = Snapshot(true);
			s.CaptureUnlockOf = null;
			Assert.That(S5(s), Is.Empty);
		}

		[Test]
		public void TheRowIsWiredArmedOnlyWithBothParentRowsAndOnInGeneric()
		{
			var module = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			Assert.That(module, Does.Contain("public readonly bool BbS5CaptureReason = false;"), "default off in code");
			Assert.That(module, Does.Contain("public bool S5CaptureReasonArmed => Info.BbS5CaptureReason && CaptureUnlockArmed && BuildAllListedArmed && !ablateS5CaptureReason.Off(player, AblationIds.BbS5CaptureReason);"));

			var p1 = Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01.cs");
			Assert.That(p1, Does.Contain("S5CaptureReason = baseBuilder.S5CaptureReasonArmed,"));
			Assert.That(p1, Does.Contain("CaptureUnlockOf = captureUnlockOf ??= name => captureUnlockOffered.Contains(name),"), "one delegate per manager, no allocation per sweep");

			var plan = Source(Dir + "BaseBuildPlanCA.cs");
			Assert.That(plan, Does.Contain("if (!s.S5CaptureReason || s.CaptureUnlockOf == null)\n\t\t\t\t\tyield break;"));

			Assert.That(AblationSpec.RegistryIds(), Does.Contain(AblationIds.BbS5CaptureReason));

			var yaml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", "packaging", "ai.yaml")).Replace("\r\n", "\n");
			Assert.That(yaml, Does.Contain("\t\tBbS8RejectedFallthrough: true\n"));
			Assert.That(yaml, Does.Contain("\t\tBbS5CaptureReason: true\n"));
		}
	}
}
