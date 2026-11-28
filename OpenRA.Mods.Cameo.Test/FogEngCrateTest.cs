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
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// FOG-ENGCRATE-01 (Opus 11-27, decision D2): generic-bot capture, bridge-hut and crate targets are seen now or remembered, never omniscient.
	/// FOG-REMEMBER-01: a remembered (not seen now) candidate is judged by its frozen owner and validity, never the live owner or IsDead; no record fails closed.
	/// Perf shape (Leader FIX, Sonnet review of 018dcbb3): one judgment per actor (Read, or Judge for a scan), at most one CanBeViewedByPlayer call,
	/// and the static pre-filters (trait, type name, AttackBase) run before it.
	/// Pins: the pure rule (fail closed with no memory, equal answers for seen and remembered), the reserve clamp (prior 25 unchanged), the
	/// switch and ablation wiring, the frozen-record reads, and that classic bots keep the omniscient CaptureManager path.
	/// </summary>
	[TestFixture]
	public sealed class FogEngCrateTest
	{
		const string EngineerPath = "OpenRA.Mods.Cameo/Traits/BotModules/EngineerBotModule.cs";
		const string CratePath = "OpenRA.Mods.Cameo/Traits/BotModules/CratePickupBotModule.cs";
		const string GatePath = "OpenRA.Mods.CA/Traits/BotModules/BotModuleLogic/FogEngCrateGate.cs";
		const string YamlPath = "mods/cameo/ai/ai.yaml";

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		[Test]
		public void TheRowIdIsFogEngCrate()
		{
			Assert.That(AblationIds.FogEngCrate, Is.EqualTo("fog_engcrate"));
		}

		[Test]
		public void AnUnseenAndUnrememberedTargetIsRefused()
		{
			// Fail closed: with no fog memory (no fog provider) and no sight, nothing is admitted.
			Assert.That(FogEngCrateGate.Admits(false, false), Is.False);
		}

		[Test]
		public void ASeenOrRememberedTargetIsAdmitted()
		{
			Assert.That(FogEngCrateGate.Admits(true, false), Is.True, "seen now");
			Assert.That(FogEngCrateGate.Admits(false, true), Is.True, "remembered in the frozen layer");
		}

		[Test]
		public void SeenAndRememberedGiveTheSameAnswer()
		{
			// Equivalent worlds: a target the bot sees now and the same target seen a tick ago must be admitted alike.
			Assert.That(FogEngCrateGate.Admits(true, false), Is.EqualTo(FogEngCrateGate.Admits(false, true)));
		}

		[Test]
		public void TheReserveRadiusIsClampedAndThePriorIsUnchanged()
		{
			Assert.That(FogEngCrateGate.ClampReserveRadius(25), Is.EqualTo(25), "the prior is unchanged");
			Assert.That(FogEngCrateGate.ClampReserveRadius(5), Is.EqualTo(15), "lower clamp");
			Assert.That(FogEngCrateGate.ClampReserveRadius(99), Is.EqualTo(40), "upper clamp");
		}

		[Test]
		public void MissingFrozenLayerFailsClosedInTheSource()
		{
			var gate = RepoFile(GatePath);
			Assert.That(gate, Does.Contain("player.FrozenActorLayer != null && player.FrozenActorLayer.FromID(actor.ActorID) != null"),
				"no frozen layer means no memory, so only the seen path admits");
		}

		[Test]
		public void TheGateJudgesARememberedActorByItsFrozenRecordOnly()
		{
			var gate = RepoFile(GatePath);
			Assert.That(gate, Does.Contain("player.FrozenActorLayer?.FromID(actor.ActorID)?.Owner"),
				"the owner of a remembered actor is its last frozen sighting, not the live owner");
			Assert.That(gate, Does.Contain("player.FrozenActorLayer?.FromID(actor.ActorID) is { IsValid: true }"),
				"alive = a valid frozen record; no record = not alive (fail closed)");
		}

		[Test]
		public void ReadAndJudgeTouchTheFogOncePerActor()
		{
			var engineer = RepoFile(EngineerPath);
			Assert.That(engineer, Does.Contain("bool Read(Actor a, out OpenRA.Player owner)"),
				"Read: alive and the owner the bot judges by, from one judgment");
			Assert.That(engineer, Does.Contain("if (RememberOn && !a.CanBeViewedByPlayer(player))"),
				"Read asks the fog once, and only with the row on; row off = the live reads");
			Assert.That(engineer, Does.Contain("owner = FogEngCrateGate.RememberedOwner(player, a);"));
			Assert.That(engineer, Does.Contain("return FogEngCrateGate.RememberedAlive(player, a);"));
			Assert.That(engineer, Does.Contain("bool Judge(Actor a, out OpenRA.Player owner)"),
				"Judge: the scan admission (the old CaptureFilter gate) in the same single call");
			Assert.That(engineer, Does.Contain("if (!FogOn && Info.CheckCaptureTargetsForVisibility && !a.CanBeViewedByPlayer(player))"));
			Assert.That(engineer, Does.Contain("return Read(a, out owner);"));
		}

		[Test]
		public void TheCandidateScansRunTheStaticPreFiltersBeforeTheFog()
		{
			var engineer = RepoFile(EngineerPath);
			Assert.That(engineer, Does.Contain("u.Info.HasTraitInfo<AttackBaseInfo>() && Judge(u, out var judged)"), "guarding enemies: trait first");
			Assert.That(engineer, Does.Contain("a.TraitOrDefault<CaptureManager>() != null"), "demand capturers: trait check");
			Assert.That(engineer, Does.Contain("&& Judge(a, out var judged)"), "demand capturers: then the one judgment");
			Assert.That(engineer, Does.Contain("priorityCapturableTypes.Contains(a.Info.Name.ToLowerInvariant()) && Judge(a, out var judged)"),
				"priority pass: type name first");
			Assert.That(engineer, Does.Contain("Judge(a, out var judged) && judged == randPlayer"), "options pool: owner from the judgment");
			Assert.That(engineer, Does.Contain(".Where(t => Judge(t, out var judged)"), "drop targets: judgment first, escort after");
			Assert.That(engineer, Does.Contain("&& !EscortEligible(t, judged)"), "drop targets: escort eligibility on the judged owner");
			Assert.That(engineer, Does.Contain(".Where(u => Judge(u, out var judged) && capturer.Owner.RelationshipWith(judged)"), "safe path");
		}

		[Test]
		public void TheCandidateSitesUseReadForTheirOwnerAndState()
		{
			var engineer = RepoFile(EngineerPath);
			Assert.That(engineer, Does.Contain("if (target == null || !Read(target, out _) || job.Job != EngineerJob.Capture)"), "recheck");
			Assert.That(engineer, Does.Contain("if (!Read(job.Target, out var jobOwner) || jobOwner == null"), "transport target gone");
			Assert.That(engineer, Does.Contain("bool EscortEligible(Actor target) => Read(target, out var owner) && EscortEligible(target, owner);"));
			Assert.That(engineer, Does.Contain("bool EscortEligible(Actor target, OpenRA.Player owner) =>"));
			Assert.That(engineer, Does.Contain("if (!Read(t, out var escortOwner) || escortOwner == player)"), "escort target gone");
			Assert.That(engineer, Does.Contain("escort == null || !Read(escort.Target, out _)"), "protection request");
			Assert.That(engineer, Does.Contain("var targetGone = target == null || !Read(target, out targetOwner);"), "verdict target gone");
			Assert.That(engineer, Does.Contain("CaptureVerdict(!targetGone && targetOwner == player"), "verdict owner (frozen for an unseen target: accounting only)");
		}

		[Test]
		public void TheCandidateSitesNoLongerReadTheLiveOwnerOrDeadFlagOrTheFogTwice()
		{
			var engineer = RepoFile(EngineerPath);
			Assert.That(engineer, Does.Not.Contain("!u.IsDead && u.IsInWorld && player.RelationshipWith(u.Owner)"));
			Assert.That(engineer, Does.Not.Contain("!a.IsDead && a.IsInWorld && Info.CapturableRelationships"));
			Assert.That(engineer, Does.Not.Contain("a.Owner == randPlayer && !a.IsDead"));
			Assert.That(engineer, Does.Not.Contain("!t.IsDead && t.IsInWorld && t.Owner != null"));
			Assert.That(engineer, Does.Not.Contain("job.Target.Owner == null"));
			Assert.That(engineer, Does.Not.Contain("!u.IsDead && capturer.Owner.RelationshipWith(u.Owner)"));
			Assert.That(engineer, Does.Not.Contain("player.RelationshipWith(target.Owner)"));
			Assert.That(engineer, Does.Not.Contain("escort.Target.IsDead"));
			// The second, separate fog filter and the helpers that asked the fog a second and third time are gone.
			Assert.That(engineer, Does.Not.Contain("CaptureVisible("));
			Assert.That(engineer, Does.Not.Contain("CaptureFilter"));
			Assert.That(engineer, Does.Not.Contain("SeenNow("));
			Assert.That(engineer, Does.Not.Contain("OwnerOf("));
			Assert.That(engineer, Does.Not.Contain("!Alive("));
		}

		[Test]
		public void TheDefenceValueScanReadsEachDefenderThroughReadAfterTheStaticPreFilter()
		{
			// The escort defence value counts defenders near a target. Row off, Read is the live owner and dead flag (the old text); row on, an
			// unseen defender counts only through its frozen record. The AttackBase pre-filter runs before the fog read.
			var engineer = string.Join(" ", RepoFile(EngineerPath).Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
			Assert.That(engineer, Does.Contain(
				".Where(u => u.Info.HasTraitInfo<AttackBaseInfo>() && Read(u, out var judged) && player.RelationshipWith(judged) == PlayerRelationship.Enemy) .Sum(CostOf);"));
		}

		[Test]
		public void CandidateCaptureEligibilityComesFromTheFrozenRecordForAnUnseenTarget()
		{
			// Sol FIX: CaptureManager.CanTarget(CaptureManager) reads the live owner through target.self. An unseen target is judged by its frozen
			// record (CanTarget(FrozenActor, forceCapture)), so a hidden ownership change cannot alter eligibility. Shape pin only: a behavioural
			// hidden-ownership test needs a live world harness, which static review cannot run.
			var engineer = RepoFile(EngineerPath);
			Assert.That(engineer, Does.Contain("bool CanCapture(CaptureManager captor, Actor target, CaptureManager targetCapture)"));
			Assert.That(engineer, Does.Contain("return frozen != null && (captor.CanTarget(frozen, false) || captor.CanTarget(frozen, true));"),
				"an unseen target uses the frozen owner; no record fails closed");
			Assert.That(engineer, Does.Contain("CanCapture(capturer.Trait, target, captureManager)"));
			Assert.That(engineer, Does.Contain("CanCapture(tp.Trait, target, captureManager)"));
			Assert.That(engineer, Does.Contain("CanCapture(c.TraitOrDefault<CaptureManager>(), t, t.TraitOrDefault<CaptureManager>())"));
			Assert.That(engineer, Does.Not.Contain("capturer.Trait.CanTarget(captureManager)"), "no live CanTarget on a candidate");
			Assert.That(engineer, Does.Not.Contain("tp.Trait.CanTarget(captureManager)"));
			Assert.That(engineer, Does.Not.Contain("c.TraitOrDefault<CaptureManager>().CanTarget("));
		}

		[Test]
		public void TheRememberRowIsItsOwnSwitchAndRowOffKeepsTheEngCrateAdmission()
		{
			// FOG-REMEMBER-01 row fog_remember: OFF in code, armed in the generic engineer block only (the ARM commit). Row off with the fog on = the
			// FOG-ENGCRATE scans (seen or remembered actors only, read live); row on = the frozen-record reads in Read and CanCapture.
			Assert.That(AblationIds.FogRemember, Is.EqualTo("fog_remember"));
			var engineer = RepoFile(EngineerPath);
			Assert.That(engineer, Does.Contain("public readonly bool FogRemember = false;"));
			Assert.That(engineer, Does.Contain("bool RememberOn => FogOn && Info.FogRemember && !ablateFogRemember.Off(player, AblationIds.FogRemember);"));
			Assert.That(engineer, Does.Contain("if (FogOn && !RememberOn && !FogEngCrateGate.Admits(player, a))"),
				"row off with the fog on: the FOG-ENGCRATE admission, then the live reads");
			Assert.That(engineer, Does.Contain("if (RememberOn && !target.CanBeViewedByPlayer(player))"), "capture eligibility from the frozen record only with the row on");
			Assert.That(engineer.Split("if (RememberOn && !").Length - 1, Is.EqualTo(2), "only Read and CanCapture read a frozen record");
		}

		[Test]
		public void EngineerGatesEveryOmniscientScanBehindTheSwitch()
		{
			var engineer = RepoFile(EngineerPath);
			Assert.That(engineer, Does.Contain("public readonly bool FogEngCrate = false;"));
			Assert.That(engineer, Does.Contain("bool FogOn => Info.FogEngCrate && !ablateFogEngCrate.Off(player, AblationIds.FogEngCrate);"));
			Assert.That(engineer, Does.Contain("if (FogOn || Info.CheckRepairTargetsForVisibility)"), "bridge huts follow the same switch");
			// The old flag-only expressions are gone from every engineer scan site.
			Assert.That(engineer, Does.Not.Contain("(!Info.CheckCaptureTargetsForVisibility || u.CanBeViewedByPlayer(player))"));
			Assert.That(engineer, Does.Not.Contain("(!Info.CheckCaptureTargetsForVisibility || t.CanBeViewedByPlayer(player))"));
			Assert.That(engineer, Does.Not.Contain("if (Info.CheckCaptureTargetsForVisibility)"));
		}

		[Test]
		public void CratePickupGatesTheCrateScanBehindTheSwitchAndTheSwitchCondition()
		{
			var crate = RepoFile(CratePath);
			Assert.That(crate, Does.Contain("public readonly bool FogEngCrate = false;"));
			Assert.That(crate, Does.Contain("bool FogOn => Info.FogEngCrate && switchesActive && !ablateFogEngCrate.Off(player, AblationIds.FogEngCrate);"),
				"the genericbot SwitchCondition gates it, so classic crate pickup is unchanged");
			Assert.That(crate, Does.Contain("if (FogOn || Info.CheckTargetsForVisibility)"));
			Assert.That(crate, Does.Contain("FogEngCrateGate.ClampReserveRadius(Info.ReserveYardRadiusCells)"));
		}

		[Test]
		public void TheArmedYamlIsGenericOnlyAndClassicCaptureStaysOmniscient()
		{
			var yaml = RepoFile(YamlPath);
			Assert.That(yaml.Split("FogEngCrate: true").Length - 1, Is.EqualTo(2), "engineer and crate pickup only");

			// CaptureManagerBotModuleCA is classicbot: its omniscient flag stays and it never gets the fog row.
			var classicStart = yaml.IndexOf("\tCaptureManagerBotModuleCA:", StringComparison.Ordinal);
			Assert.That(classicStart, Is.GreaterThan(0));
			var next = System.Text.RegularExpressions.Regex.Match(yaml.Substring(classicStart + 1), "\n\t[^\t\n]");
			var classicBlock = yaml.Substring(classicStart, next.Success ? next.Index + 1 : yaml.Length - classicStart);
			Assert.That(classicBlock, Does.Contain("CheckCaptureTargetsForVisibility: false"));
			Assert.That(classicBlock, Does.Not.Contain("FogEngCrate"));
		}
	}
}
