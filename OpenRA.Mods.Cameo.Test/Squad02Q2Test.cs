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
using System.IO;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// CONSOLIDATE-02 Q2 (CONSOLIDATE_02_SQUAD_DESIGN.md 4.1 Step 1): the ArmyPlanCA skeleton and the role split behind the master switch sq_pipeline_02 (Info
	/// SquadPipeline02Enabled, default off, never in @classic) and the row sq_roles. Harass cap 25% by VALUE (Opus D5), raids >= 3 units, attached squads follow MAIN.
	/// </summary>
	[TestFixture]
	public class Squad02Q2Test
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		static ArmyPlanSquad Rush(int key, int units, long value, ArmyHolder holder = ArmyHolder.Quiet) => new(key, ArmySquadKind.Rush, holder, units, value);

		static ArmyPlanSquad Raid(int key, int units, long value, ArmyHolder holder = ArmyHolder.Quiet) => new(key, ArmySquadKind.Raid, holder, units, value);

		[Test]
		public void EverySquadGetsExactlyOneRoleAndThePoolIsMain()
		{
			var squads = new[]
			{
				Rush(0, 10, 8000),
				Raid(1, 4, 2000),
				new ArmyPlanSquad(2, ArmySquadKind.Protection, ArmyHolder.Busy, 3, 1500),
				new ArmyPlanSquad(3, ArmySquadKind.Attached, ArmyHolder.Quiet, 2, 1800),
				new ArmyPlanSquad(4, ArmySquadKind.Domain, ArmyHolder.Busy, 5, 6000),
			};

			var plan = ArmyPlanCA.Assign(squads, 6, 3000, 25, 3);
			Assert.That(plan.RoleOf(0), Is.EqualTo(ArmyRole.Main));
			Assert.That(plan.RoleOf(1), Is.EqualTo(ArmyRole.Harass), "2000 of 14500 combat value is inside 25%");
			Assert.That(plan.RoleOf(2), Is.EqualTo(ArmyRole.DefenceHome));
			Assert.That(plan.RoleOf(3), Is.EqualTo(ArmyRole.Attached));
			Assert.That(plan.RoleOf(4), Is.EqualTo(ArmyRole.Other));
			Assert.That(plan.Roles.Count, Is.EqualTo(squads.Length), "one role per squad");

			Assert.That(plan.CombatValue, Is.EqualTo(3000 + 8000 + 2000 + 1500), "pool + Rush + raid + home guard; attached and domain squads do not count");
			Assert.That(plan.MainUnits, Is.EqualTo(16));
			Assert.That(plan.MainValue, Is.EqualTo(11000));
			Assert.That(plan.HarassUnits, Is.EqualTo(4));
			Assert.That(plan.AttachedUnits, Is.EqualTo(2));
			Assert.That(plan.DefenceUnits, Is.EqualTo(3));
			Assert.That(plan.OtherUnits, Is.EqualTo(5));
			Assert.That(plan.MainSquadKey, Is.EqualTo(0));
		}

		// D5: the cap is 25% by army VALUE. Four cheap raiders are fine by value while four expensive ones are not, even though the count is the same.
		[Test]
		public void TheHarassCapIsMeasuredByValueNotByCount()
		{
			var cheap = ArmyPlanCA.Assign(new[] { Rush(0, 12, 12000), Raid(1, 4, 1200) }, 0, 0, 25, 3);
			Assert.That(cheap.RoleOf(1), Is.EqualTo(ArmyRole.Harass), "1200 of 13200 = 9%");

			var dear = ArmyPlanCA.Assign(new[] { Rush(0, 12, 6000), Raid(1, 4, 4000) }, 0, 0, 25, 3);
			Assert.That(dear.HarassCapValue, Is.EqualTo(2500), "25% of 10000");
			Assert.That(dear.RoleOf(1), Is.EqualTo(ArmyRole.Main), "4000 of 10000 = 40%: the raid counts as MAIN");
			Assert.That(dear.DemotedOverCap, Is.EqualTo(1));
			Assert.That(dear.MainUnits, Is.EqualTo(16));
		}

		[Test]
		public void ARaidBelowThreeUnitsIsMainAndTheBusyRaidsArePlacedFirst()
		{
			var plan = ArmyPlanCA.Assign(new[]
			{
				Rush(0, 20, 20000),
				Raid(1, 2, 600),
				Raid(2, 5, 3000, ArmyHolder.Quiet),
				Raid(3, 4, 3000, ArmyHolder.Busy),
			}, 0, 0, 25, 3);

			// Combat 26600, cap 6650: the busy raid (3000) first, then the quiet one (3000) still fits; the 2-unit raid is below the minimum raid.
			Assert.That(plan.RoleOf(1), Is.EqualTo(ArmyRole.Main), "2 units < 3");
			Assert.That(plan.DemotedSmall, Is.EqualTo(1));
			Assert.That(plan.RoleOf(3), Is.EqualTo(ArmyRole.Harass));
			Assert.That(plan.RoleOf(2), Is.EqualTo(ArmyRole.Harass));

			var tight = ArmyPlanCA.Assign(new[] { Rush(0, 20, 14000), Raid(2, 5, 3000, ArmyHolder.Quiet), Raid(3, 4, 3000, ArmyHolder.Busy) }, 0, 0, 25, 3);
			Assert.That(tight.HarassCapValue, Is.EqualTo(5000));
			Assert.That(tight.RoleOf(3), Is.EqualTo(ArmyRole.Harass), "the raid that is already out keeps the cap");
			Assert.That(tight.RoleOf(2), Is.EqualTo(ArmyRole.Main), "the quiet one does not fit any more");
		}

		[Test]
		public void TheCapPercentEdges()
		{
			Assert.That(ArmyPlanCA.HarassCap(10000, 100), Is.EqualTo(long.MaxValue), "100 = no cap");
			Assert.That(ArmyPlanCA.HarassCap(10000, 0), Is.EqualTo(0), "0 = no raids");
			Assert.That(ArmyPlanCA.HarassCap(-5, 25), Is.EqualTo(0));
			Assert.That(ArmyPlanCA.Assign(new[] { Raid(0, 4, 100) }, 0, 0, 0, 3).RoleOf(0), Is.EqualTo(ArmyRole.Main));
			Assert.That(ArmyPlanCA.Assign(null, 3, 900, 25, 3).MainUnits, Is.EqualTo(3), "no squads: the pool alone");
		}

		[Test]
		public void TheValueCapAdmitsANewRaiderOnlyWhileTheRaidValueStaysInsideAndAFirstRaidCanReachTheMinimum()
		{
			// 10000 total with the candidate: cap 2500.
			Assert.That(ArmyPlanCA.HarassAllowsByValue(1500, 3, 10000, 900, 25, 3), Is.True, "1500 + 900 <= 2500");
			Assert.That(ArmyPlanCA.HarassAllowsByValue(1700, 3, 10000, 900, 25, 3), Is.False, "1700 + 900 > 2500");
			Assert.That(ArmyPlanCA.HarassAllowsByValue(0, 0, 10000, 900, 25, 3), Is.False, "a first raid of three 900 units can never fit 2500");
			Assert.That(ArmyPlanCA.HarassAllowsByValue(0, 0, 10000, 800, 25, 3), Is.True, "three 800 units fit 2500");
			Assert.That(ArmyPlanCA.HarassAllowsByValue(99999, 9, 10000, 900, 100, 3), Is.True, "100 = no cap");
		}

		[Test]
		public void TheMainSquadIsTheOneInTheTeamOrderThenTheMostValuable()
		{
			var plan = ArmyPlanCA.Assign(new[] { Rush(0, 30, 30000), Rush(1, 8, 6000, ArmyHolder.Gathering), Rush(2, 9, 6000, ArmyHolder.Pushed) }, 0, 0, 25, 3);
			Assert.That(plan.MainSquadKey, Is.EqualTo(1), "in the order first; equal value: the lower key");

			var none = ArmyPlanCA.Assign(new[] { Raid(0, 5, 2000), Rush(3, 2, 900), Rush(4, 6, 2400) }, 0, 0, 25, 3);
			Assert.That(none.MainSquadKey, Is.EqualTo(4), "no order: the most valuable Rush squad (never a raid)");
		}

		// Design L18: orphan artillery took the NEWEST assault, which can be a one-unit follower squad.
		[Test]
		public void AttachedSquadsFollowTheMainSquadNotAOneUnitFollower()
		{
			Assert.That(ArmyPlanCA.AttachedKeepsParent(true, true, 1, 20), Is.False, "a one-unit follower is not the main squad");
			Assert.That(ArmyPlanCA.AttachedKeepsParent(true, true, 10, 20), Is.True, "half of the main squad keeps the parent (no flapping)");
			Assert.That(ArmyPlanCA.AttachedKeepsParent(false, true, 10, 20), Is.False, "an empty parent");
			Assert.That(ArmyPlanCA.AttachedKeepsParent(true, false, 30, 20), Is.False, "a raid or a home guard is not MAIN");
		}

		[Test]
		public void TheWiringIsBehindTheMasterSwitchAndTheRolesRow()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.Pipeline02.cs");
			Assert.That(wiring, Does.Contain("internal bool ArmyPipelineOn => Info.SquadPipeline02Enabled && !ablatePipeline02.Off(Player, AblationIds.SqPipeline02);"));
			Assert.That(wiring, Does.Contain("internal bool ArmyRolesOn => ArmyPipelineOn && !ablateSqRoles.Off(Player, AblationIds.SqRoles);"));
			Assert.That(wiring, Does.Contain("if (!ArmyRolesOn || !BotPeriod.CarryDue(ref armyPlanNextTick, World.WorldTick, Info.ArmyPlanTicks))\n\t\t\t\treturn;"));
			Assert.That(wiring, Does.Contain("#if CAMEO_DEV_LOGGING"), "the plan record is DEV only");

			var sm = RepoFile(Bots + "SquadManagerBotModuleCA.cs");
			Assert.That(sm, Does.Contain("ArmyPlanTick();"));
			Assert.That(sm, Does.Not.Contain("squad.Parent = attackForce.IsValid ? attackForce : squad.Parent;"), "orphans go through ArmyAttachParent");
			Assert.That(wiring, Does.Contain("if (ArmyRolesOn && ArmyMainSquad() is { } main)\n\t\t\t\treturn main;\n\n\t\t\treturn newWave.IsValid ? newWave : null;"), "off = the old newest-wave rule");

			var cap = RepoFile(Bots + "SquadManagerBotModuleCA.HarassCap.cs");
			Assert.That(cap, Does.Contain("if (ArmyRolesOn)\n\t\t\t\treturn HarassCapAllowsByValue(candidate);"), "D5: by value under the pipeline, by count otherwise");
		}

		[Test]
		public void TheMasterSwitchIsOffByDefaultNeverInClassicAndTheRowsAreRegistered()
		{
			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.Pipeline02.cs");
			Assert.That(info, Does.Contain("public readonly bool SquadPipeline02Enabled = false;"));
			Assert.That(info, Does.Contain("public readonly int ArmyPlanTicks = 100;"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			Assert.That(yaml[classic..], Does.Not.Contain("SquadPipeline02"), "never in @classic");

			Assert.That(AblationIds.SqPipeline02, Is.EqualTo("sq_pipeline_02"));
			Assert.That(AblationIds.SqRoles, Is.EqualTo("sq_roles"));
		}
	}
}
