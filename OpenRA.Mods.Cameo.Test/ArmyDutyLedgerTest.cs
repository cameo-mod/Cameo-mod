#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// CONSOLIDATE-03 P7 phase (a) (design 01 5e100b94 + Opus review changes): the write-only army duty ledger. Pure: units are actor ids, no engine state. Nothing reads the ledger in phase (a).
	/// </summary>
	[TestFixture]
	public class ArmyDutyLedgerTest
	{
		static readonly ArmyDuty[] All =
		[
			ArmyDuty.OwnBaseDefend, ArmyDuty.AllyDefend, ArmyDuty.GuardLend, ArmyDuty.TeamPush, ArmyDuty.PincerArm,
			ArmyDuty.AllIn, ArmyDuty.Stalemate, ArmyDuty.AttackLiveness, ArmyDuty.StandaloneAttack, ArmyDuty.Raid, ArmyDuty.Idle,
		];

		[Test]
		public void TheRanksFollowTheTargetOrderWithTheOpusChanges()
		{
			Assert.That(ArmyDutyLedger.Rank(ArmyDuty.None), Is.EqualTo(0));
			Assert.That(ArmyDutyLedger.Rank(ArmyDuty.AllyDefend), Is.LessThan(ArmyDutyLedger.Rank(ArmyDuty.OwnBaseDefend)), "R2 ally falls above R3 own-base threat");
			Assert.That(ArmyDutyLedger.Rank(ArmyDuty.OwnBaseDefend), Is.LessThan(ArmyDutyLedger.Rank(ArmyDuty.GuardLend)), "R3 above the R4 guard");
			Assert.That(ArmyDutyLedger.Rank(ArmyDuty.GuardLend), Is.LessThan(ArmyDutyLedger.Rank(ArmyDuty.TeamPush)));
			foreach (var order in new[] { ArmyDuty.TeamPush, ArmyDuty.PincerArm, ArmyDuty.AllIn, ArmyDuty.AttackLiveness })
				Assert.That(ArmyDutyLedger.Rank(order), Is.EqualTo(5), order + " is a team order, R5");

			Assert.That(ArmyDutyLedger.Rank(ArmyDuty.Stalemate), Is.EqualTo(5), "Opus change 1: the stalemate breaker is an attack order (R5), not R7");
			Assert.That(ArmyDutyLedger.Rank(ArmyDuty.Raid), Is.EqualTo(6));
			Assert.That(ArmyDutyLedger.Rank(ArmyDuty.StandaloneAttack), Is.EqualTo(6), "Sol FIX_STATIC: a wave with no team order open is not a team order, so it ranks with the raid, below every team order");
			Assert.That(ArmyDutyLedger.Rank(ArmyDuty.StandaloneAttack), Is.GreaterThan(ArmyDutyLedger.Rank(ArmyDuty.TeamPush)));
			Assert.That(ArmyDutyLedger.Rank(ArmyDuty.Idle), Is.EqualTo(7), "only idle, reserve and preposition stay at R7");
			foreach (var duty in All)
				Assert.That(ArmyDutyLedger.Rank(duty), Is.InRange(2, 7), duty.ToString());
		}

		[Test]
		public void AFreshWaveIsATeamOrderOnlyWhenOneIsOpenAndAStandaloneAttackOtherwise()
		{
			Assert.That(ArmyDutyLedger.WaveDuty(false, false), Is.EqualTo(ArmyDuty.StandaloneAttack), "no team order open: a standalone attack, never a team order");
			Assert.That(ArmyDutyLedger.WaveDuty(false, true), Is.EqualTo(ArmyDuty.StandaloneAttack), "an all-in flag without an open order is not an order");
			Assert.That(ArmyDutyLedger.WaveDuty(true, false), Is.EqualTo(ArmyDuty.TeamPush));
			Assert.That(ArmyDutyLedger.WaveDuty(true, true), Is.EqualTo(ArmyDuty.AllIn));

			var ledger = new ArmyDutyLedger();
			ledger.Claim(1, ArmyDuty.TeamPush, 0, "p", "TeamLeadMergeIntoGatherSquad");
			Assert.That(ledger.Claim(1, ArmyDuty.StandaloneAttack, 10, "wave_launch", "CreateAttackForce").Kind, Is.EqualTo(ArmyDutyLedger.ClaimKind.TakeEqualOrHigher), "a standalone wave taking a unit of a team order is a violation of the rank order");
		}

		[Test]
		public void TheCoreDefinitionNeedsAHitCoreBuildingAndAnObservedForceAtTheThreshold()
		{
			Assert.That(ArmyDutyLedger.IsCoreUnderAttack(true, false, false, 1000, 1000), Is.True);
			Assert.That(ArmyDutyLedger.IsCoreUnderAttack(false, true, false, 5000, 1000), Is.True);
			Assert.That(ArmyDutyLedger.IsCoreUnderAttack(false, false, true, 1000, 1000), Is.True);
			Assert.That(ArmyDutyLedger.IsCoreUnderAttack(true, true, true, 999, 1000), Is.False, "below the observed threat threshold");
			Assert.That(ArmyDutyLedger.IsCoreUnderAttack(false, false, false, 9999, 1000), Is.False, "no core building hit");
		}

		[Test]
		public void AFirstClaimIsNewAndARepeatByTheSameDutyKeepsTheSinceTick()
		{
			var ledger = new ArmyDutyLedger();
			var first = ledger.Claim(7, ArmyDuty.Raid, 100, "harass", "AddToHarassSquad");
			Assert.That(first.Kind, Is.EqualTo(ArmyDutyLedger.ClaimKind.New));
			Assert.That(first.Steal, Is.False);

			var again = ledger.Claim(7, ArmyDuty.Raid, 400, "harass", "AddToHarassSquad");
			Assert.That(again.Kind, Is.EqualTo(ArmyDutyLedger.ClaimKind.Same));
			Assert.That(ledger.TryGet(7, out var entry), Is.True);
			Assert.That((entry.Duty, entry.Since, entry.Caller), Is.EqualTo((ArmyDuty.Raid, 100, "AddToHarassSquad")), "a refresh keeps the original tick");
			Assert.That(ledger.Count, Is.EqualTo(1));
		}

		[Test]
		public void AHigherRankTakingFromALowerIsAllowedAndAnEqualOrLowerOneIsAViolation()
		{
			var ledger = new ArmyDutyLedger();
			ledger.Claim(1, ArmyDuty.Raid, 0, "r", "a");
			var take = ledger.Claim(1, ArmyDuty.AllyDefend, 10, "d", "b");
			Assert.That(take.Kind, Is.EqualTo(ArmyDutyLedger.ClaimKind.TakeLower));
			Assert.That(take.Previous.Duty, Is.EqualTo(ArmyDuty.Raid));

			var equal = ledger.Claim(2, ArmyDuty.TeamPush, 0, "p", "a");
			Assert.That(ledger.Claim(2, ArmyDuty.AllIn, 5, "all-in", "b").Kind, Is.EqualTo(ArmyDutyLedger.ClaimKind.TakeEqualOrHigher), "equal ranks never take from each other");
			Assert.That(equal.Kind, Is.EqualTo(ArmyDutyLedger.ClaimKind.New));

			ledger.Claim(3, ArmyDuty.OwnBaseDefend, 0, "d", "a");
			Assert.That(ledger.Claim(3, ArmyDuty.AllIn, 5, "all-in", "b").Kind, Is.EqualTo(ArmyDutyLedger.ClaimKind.TakeEqualOrHigher), "Opus change 3: all-in does not take from the own-base defence");
			Assert.That(ledger.Violations, Is.EqualTo(2));
		}

		[Test]
		public void AStealIsADifferentDutyWithinTheWindow()
		{
			var ledger = new ArmyDutyLedger();
			ledger.Claim(1, ArmyDuty.Raid, 1000, "r", "a");
			Assert.That(ledger.Claim(1, ArmyDuty.AllyDefend, 1000 + ArmyDutyLedger.StealWindowTicks, "d", "b").Steal, Is.True, "inside the window");
			ledger.Claim(2, ArmyDuty.Raid, 1000, "r", "a");
			Assert.That(ledger.Claim(2, ArmyDuty.AllyDefend, 1001 + ArmyDutyLedger.StealWindowTicks, "d", "b").Steal, Is.False, "later than the window");
			Assert.That(ledger.Steals, Is.EqualTo(1));
		}

		[Test]
		public void ALentUnitThatADefendDutyDraftsBackInsideTheWindowIsCounted()
		{
			var ledger = new ArmyDutyLedger();
			ledger.Claim(5, ArmyDuty.GuardLend, 100, "lend", "ArmyGuardLendTo");
			ledger.Release(5, 100, out _);
			ledger.NoteLend(5, 100);
			var drafted = ledger.Claim(5, ArmyDuty.AllyDefend, 600, "d", "DefendAllyRespond");
			Assert.That(drafted.DraftBack, Is.True);
			Assert.That(ledger.DraftBacks, Is.EqualTo(1));

			ledger.NoteLend(6, 100);
			Assert.That(ledger.Claim(6, ArmyDuty.TeamPush, 200, "p", "TeamLeadJoin").DraftBack, Is.False, "only the defend duties draft back");
			ledger.NoteLend(8, 0);
			Assert.That(ledger.Claim(8, ArmyDuty.OwnBaseDefend, 1 + ArmyDutyLedger.LendWindowTicks, "d", "ArbiterProtectOwn").DraftBack, Is.False, "after the window it is a normal claim");
		}

		[Test]
		public void AReleaseDropsTheEntryAndTheCountersAndHeartbeatAreSummed()
		{
			var ledger = new ArmyDutyLedger();
			ledger.Claim(1, ArmyDuty.Idle, 0, "pool", "FindNewUnits");
			Assert.That(ledger.Release(1, 50, out var previous), Is.True);
			Assert.That(previous.Duty, Is.EqualTo(ArmyDuty.Idle));
			Assert.That(ledger.Release(1, 60, out _), Is.False, "a second release finds nothing");
			Assert.That(ledger.Count, Is.EqualTo(0));

			ledger.NoteHeartbeatDoubles(3);
			ledger.NoteHeartbeatDoubles(-2);
			Assert.That(ledger.HeartbeatDoubles, Is.EqualTo(3));
			Assert.That(ledger.CountersDetail(), Is.EqualTo("claims=1;releases=2;steals=0;violations=0;draftbacks=0;heartbeat_doubles=3;pruned=0;held=0"));
		}

		[Test]
		public void AHandBackToThePoolIsNeverATakeAStealOrAViolation()
		{
			var ledger = new ArmyDutyLedger();
			ledger.Claim(1, ArmyDuty.OwnBaseDefend, 100, "d", "ArbiterProtectOwn");
			Assert.That(ledger.ToPool(1, 120, "defence_over", "ReleaseDefenders", out var previous), Is.True);
			Assert.That(previous.Duty, Is.EqualTo(ArmyDuty.OwnBaseDefend));
			Assert.That(ledger.TryGet(1, out var entry), Is.True);
			Assert.That((entry.Duty, entry.Since, entry.Caller), Is.EqualTo((ArmyDuty.Idle, 120, "ReleaseDefenders")));
			Assert.That((ledger.Violations, ledger.Steals), Is.EqualTo((0, 0)), "Idle (R7) after R3 is a hand-back, not a take");

			Assert.That(ledger.ToPool(2, 130, "new_unit", "FindNewUnits", out _), Is.False, "a new unit had no duty");
			Assert.That(ledger.Count, Is.EqualTo(2));
			Assert.That(ledger.Claim(1, ArmyDuty.TeamPush, 140, "p", "TeamLeadMergeIntoGatherSquad").Kind, Is.EqualTo(ArmyDutyLedger.ClaimKind.TakeLower), "the pool is the lowest rank: any duty takes from it");
		}

		[Test]
		public void PruneDropsTheEntriesOfGoneUnitsAndCountsThem()
		{
			var ledger = new ArmyDutyLedger();
			ledger.Claim(1, ArmyDuty.Raid, 0, "r", "a");
			ledger.Claim(2, ArmyDuty.Idle, 0, "i", "a");
			ledger.Claim(3, ArmyDuty.AllIn, 0, "x", "a");
			Assert.That(ledger.Prune(id => id != 2), Is.EqualTo(2));
			Assert.That(ledger.Count, Is.EqualTo(1));
			Assert.That(ledger.Pruned, Is.EqualTo(2));
			Assert.That(ledger.TryGet(2, out _), Is.True);
		}

		[Test]
		public void TheTraceDetailsAreFixedAndCarryNoComma()
		{
			var kinds = new Dictionary<ArmyDutyLedger.ClaimKind, int> { [ArmyDutyLedger.ClaimKind.New] = 2, [ArmyDutyLedger.ClaimKind.TakeLower] = 1 };
			var claim = ArmyDutyLedger.ClaimDetail(ArmyDuty.AllyDefend, "DefendAllyRespond", "ally_attacked", kinds, new Dictionary<ArmyDuty, int> { [ArmyDuty.Raid] = 1 }, 1, 0, new uint[] { 30, 4, 12 }, "0badf00d");
			Assert.That(claim, Is.EqualTo("duty=AllyDefend;rank=2;caller=DefendAllyRespond;reason=ally_attacked;new=2;same=0;take=1;viol=0;from=Raid:1;steals=1;draftbacks=0;units=4|12|30;pool=0badf00d"));
			Assert.That(ArmyDutyLedger.ClaimDetail(ArmyDuty.Raid, "c", "r", new Dictionary<ArmyDutyLedger.ClaimKind, int>(), new Dictionary<ArmyDuty, int>(), 0, 0, new uint[] { 1 }, "00000000"), Does.Contain("from=-;"));

			var release = ArmyDutyLedger.ReleaseDetail("ReleaseDefenders", "defence_over", new Dictionary<ArmyDuty, int> { [ArmyDuty.OwnBaseDefend] = 3, [ArmyDuty.AllyDefend] = 1 }, new uint[] { 9, 2 }, "0badf00d");
			Assert.That(release, Is.EqualTo("caller=ReleaseDefenders;reason=defence_over;prev=OwnBaseDefend:3|AllyDefend:1;units=2|9;pool=0badf00d"));
			Assert.That(claim + release, Does.Not.Contain(","));

			var split = ArmyDutyLedger.SplitDetail(7, 1, new uint[] { 30, 4 }, new uint[] { 9, 2, 5 }, "0badf00d");
			Assert.That(split, Is.EqualTo("final=1;seq=7;to_arm=1;moved=4|30;kept=2|5|9;pool=0badf00d"), "the split row is marked final: the plan-time claim is only provisional");
			Assert.That(split, Does.Not.Contain(","));
		}

		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		// True when some occurrence of `first` is followed (after whitespace and closing braces of its loop or block) by `second`.
		static bool Follows(string text, string first, string second)
		{
			for (var at = text.IndexOf(first, StringComparison.Ordinal); at >= 0; at = text.IndexOf(first, at + 1, StringComparison.Ordinal))
				if (text[(at + first.Length)..].TrimStart(' ', '\t', '\n', '}').StartsWith(second, StringComparison.Ordinal))
					return true;

			return false;
		}

		[Test]
		public void EveryHookIsConditionalAndNothingButTheLedgerPartialMentionsTheLedger()
		{
			var partial = RepoFile(Bots + "SquadManagerBotModuleCA.DutyLedger.cs");
			var hooks = partial.Split('\n').Count(l => l.TrimStart().StartsWith("void Ledger", StringComparison.Ordinal));
			Assert.That(hooks, Is.EqualTo(9), "claim x2, release x2, to-pool x2, lend, split, heartbeat");
			Assert.That(partial.Split(new[] { "[Conditional(\"CAMEO_DEV_LOGGING\")]" }, StringSplitOptions.None).Length - 1, Is.EqualTo(hooks), "every hook is [Conditional]: the call and its arguments are gone from release");

			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null);
			var others = Directory.GetFiles(Path.Combine(dir.FullName, Bots), "SquadManagerBotModuleCA*.cs").Where(f => !f.EndsWith("DutyLedger.cs", StringComparison.Ordinal)).ToList();
			Assert.That(others, Is.Not.Empty);
			foreach (var file in others)
			{
				var text = File.ReadAllText(file);
				Assert.That(text, Does.Not.Contain("dutyLedger"), Path.GetFileName(file) + ": phase (a) writes the ledger only, no decision reads it");
				Assert.That(text, Does.Not.Contain("ArmyDutyLedger"), Path.GetFileName(file));
			}
		}

		[Test]
		public void TheHooksSitDirectlyBehindTheWritesTheyRecord()
		{
			var sites = new (string File, string Write, string Hook)[]
			{
				("SquadManagerBotModuleCA.Pipeline02.cs", "unitsHangingAroundTheBase.Remove(u);", "LedgerClaim(ArmyDuty.OwnBaseDefend, nameof(ArbiterProtectOwn), \"own_pool_pick\""),
				("SquadManagerBotModuleCA.Pipeline02.cs", "unitsHangingAroundTheBase.Remove(u);", "LedgerClaim(ArmyDuty.OwnBaseDefend, nameof(ArbiterProtectOwn), \"own_pool_in_radius\""),
				("SquadManagerBotModuleCA.Pipeline02.cs", "protectSq.Units.Add(unit);", "LedgerClaim(ArmyDuty.OwnBaseDefend, nameof(ArbiterProtectOwn), \"own_squad_pick\""),
				("SquadManagerBotModuleCA.DefendAlly.cs", "unitsHangingAroundTheBase.Remove(draftable[i]);", "LedgerClaim(ArmyDuty.AllyDefend, nameof(DefendAllyRespond)"),
				("SquadManagerBotModuleCA.V07Defend.cs", "unitsHangingAroundTheBase.Remove(u);", "LedgerClaim(ArmyDuty.AllyDefend, nameof(TeamDefendTick)"),
				("SquadManagerBotModuleCA.GuardLend.cs", "unitsHangingAroundTheBase.Add(u);", "LedgerLend(nameof(ArmyGuardLendTo)"),
				("SquadManagerBotModuleCA.AggTeam.cs", "unitsHangingAroundTheBase.Add(u);", "LedgerToPool(nameof(AggTeamApplyDetachment)"),
				("SquadManagerBotModuleCA.TeamLead.cs", "bot.QueueOrder(new Order(\"Move\", u.Actor, Target.FromCell(World, cell), false));", "LedgerClaim(order.AllIn ? ArmyDuty.AllIn : ArmyDuty.TeamPush, nameof(TeamLeadMergeIntoGatherSquad)"),
				("SquadManagerBotModuleCA.Stalemate.cs", "squad.Units.Add(new UnitWposWrapper(a));", "LedgerClaim(ArmyDuty.Stalemate, nameof(StalemateCreateSquad)"),
				("SquadManagerBotModuleCA.PincerPush.cs", "order.AssignProng(byKey[place.Key].Squad, place.Arm, byKey[place.Key].Value);", "LedgerClaim(ArmyDuty.PincerArm, nameof(TeamLeadPincerAtPush)"),
				("SquadManagerBotModuleCA.cs", "unitsHangingAroundTheBase.Remove(u);", "LedgerClaim(ArmyDuty.OwnBaseDefend, nameof(PrepositionDefenceTick)"),
				("SquadManagerBotModuleCA.cs", "unitsHangingAroundTheBase.Remove(u);", "LedgerClaim(ArmyDuty.OwnBaseDefend, nameof(ProtectOwn)"),
				("SquadManagerBotModuleCA.cs", "squad.Units.Add(unit);", "LedgerClaim(ArmyDuty.Raid, nameof(AddToHarassSquad)"),
				("SquadManagerBotModuleCA.cs", "unitsHangingAroundTheBase.Add(u);", "LedgerToPool(nameof(ReleaseDefenders)"),
				("SquadManagerBotModuleCA.cs", "unitsHangingAroundTheBase.Add(u);", "LedgerToPool(nameof(ReleaseM13Response)"),
				("SquadManagerBotModuleCA.cs", "unitsHangingAroundTheBase.AddRange(squad.Units);", "LedgerToPool(nameof(DismissSquad)"),
				("SquadManagerBotModuleCA.cs", "unitsHangingAroundTheBase.Add(new UnitWposWrapper(actor));", "LedgerToPool(nameof(ReturnToIdlePool)"),
				("SquadManagerBotModuleCA.cs", "unitsHangingAroundTheBase.Add(new UnitWposWrapper(a));", "LedgerToPool(nameof(FindNewUnits)"),
				("SquadManagerBotModuleCA.cs", "unitsHangingAroundTheBase.Remove(u);", "LedgerClaim(ArmyDuty.AttackLiveness, nameof(TryM13UrgentAttack)"),
				("SquadManagerBotModuleCA.cs", "unitsHangingAroundTheBase.Clear();", "LedgerClaim(WaveDuty(TeamLeadOpenOrder()), nameof(CreateAttackForce)"),
				("SquadManagerBotModuleCA.PincerPush.cs", "order.AssignProng(half, toProng, SquadValueOf(half));", "LedgerSplit(nameof(TeamLeadSplitOff), order.Seq, toProng, half.Units, squad.Units);"),
				("SquadManagerBotModuleCA.cs", "activeUnits.Remove(u.Actor);", "LedgerHeartbeat(\"pool_claimed_by_other\""),
			};

			foreach (var (file, write, hook) in sites)
			{
				var text = RepoFile(Bots + file);
				var holds = Follows(text, write, hook) || text.Contains(hook, StringComparison.Ordinal) && Precedes(text, hook, write);
				Assert.That(holds, Is.True, file + ": " + hook + " behind " + write);
			}
		}

		[Test]
		public void AWaveIsLabelledByTheOpenOrderAndAPincerSplitIsProvisionalUntilItHappens()
		{
			var sm = RepoFile(Bots + "SquadManagerBotModuleCA.cs");
			Assert.That(sm, Does.Not.Contain("is { AllIn: true } ? ArmyDuty.AllIn : ArmyDuty.TeamPush"), "Sol FIX_STATIC: no unconditional TeamPush label for an ordinary wave");
			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.DutyLedger.cs"), Does.Contain("static ArmyDuty WaveDuty(TeamLeadOrder openOrder) => ArmyDutyLedger.WaveDuty(openOrder != null, openOrder != null && openOrder.AllIn);"),
				"a wave is a team order only while one is open");
			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs"), Does.Contain("LedgerClaim(order.AllIn ? ArmyDuty.AllIn : ArmyDuty.TeamPush, nameof(TeamLeadMergeIntoGatherSquad)"), "the gather merge always has its order in hand");

			var pincer = RepoFile(Bots + "SquadManagerBotModuleCA.PincerPush.cs");
			Assert.That(pincer, Does.Contain("\"_provisional_split_pending\" : \"_final\""), "the squad the plan divides is provisional, every other arm claim is final");
			Assert.That(pincer, Does.Contain("plan.Split is { } pending && pending.Key == place.Key"));
			Assert.That(pincer, Does.Contain("LedgerSplit(nameof(TeamLeadSplitOff), order.Seq, toProng, half.Units, squad.Units);"), "the real reassignment is its own final event");
		}

		// A hook that sits right before the write it records (heartbeat: the unit is released from the ledger as the pool drops it).
		static bool Precedes(string text, string hook, string write)
		{
			var at = text.IndexOf(hook, StringComparison.Ordinal);
			return at >= 0 && text[(at + hook.Length)..].Contains(write, StringComparison.Ordinal) && text[(at + hook.Length)..].IndexOf(write, StringComparison.Ordinal) < 400;
		}

		[Test]
		public void TheSwitchDefaultsOffAndIsOnInEveryNonClassicSquadManagerBlock()
		{
			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.cs"), Does.Contain("public readonly bool ArmyDutyTrace = false;"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			Assert.That(yaml.Split(new[] { "ArmyDutyTrace: true" }, StringSplitOptions.None).Length - 1, Is.EqualTo(6), "rush, turtle, tech, expansion, steamroller, guerrilla");

			var classic = yaml.IndexOf("\tSquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThanOrEqualTo(0));
			var end = yaml.IndexOf("\n\t", classic + 10, StringComparison.Ordinal);
			while (end >= 0 && end + 2 < yaml.Length && yaml[end + 2] == '\t')
				end = yaml.IndexOf("\n\t", end + 1, StringComparison.Ordinal);

			Assert.That(yaml[classic..(end < 0 ? yaml.Length : end)], Does.Not.Contain("ArmyDutyTrace"), "classic stays byte-identical");
		}

		[Test]
		public void ThePoolHashDependsOnTheOrderOfTheIds()
		{
			Assert.That(ArmyDutyLedger.PoolHash(new uint[] { 1, 2, 3 }), Is.EqualTo(ArmyDutyLedger.PoolHash(new uint[] { 1, 2, 3 })));
			Assert.That(ArmyDutyLedger.PoolHash(new uint[] { 1, 2, 3 }), Is.Not.EqualTo(ArmyDutyLedger.PoolHash(new uint[] { 3, 2, 1 })), "the pool order matters for equivalence");
			Assert.That(ArmyDutyLedger.PoolHash(new uint[] { 1, 2, 3 }), Is.EqualTo(BotRng.Fnv1a("1,2,3,").ToString("x8")));
			Assert.That(ArmyDutyLedger.PoolHash([]), Has.Length.EqualTo(8));
		}
	}
}
