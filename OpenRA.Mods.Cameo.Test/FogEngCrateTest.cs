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
	/// Pins: the pure rule (fail closed with no memory, equal answers for seen and remembered), the reserve clamp (prior 25 unchanged), the
	/// switch and ablation wiring, and that classic bots keep the omniscient CaptureManager path.
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
		public void EngineerGatesEveryOmniscientScanBehindTheSwitch()
		{
			var engineer = RepoFile(EngineerPath);
			Assert.That(engineer, Does.Contain("public readonly bool FogEngCrate = false;"));
			Assert.That(engineer, Does.Contain("bool FogOn => Info.FogEngCrate && !ablateFogEngCrate.Off(player, AblationIds.FogEngCrate);"));
			Assert.That(engineer, Does.Contain("bool CaptureFilter => FogOn || Info.CheckCaptureTargetsForVisibility;"));
			Assert.That(engineer, Does.Contain("bool CaptureVisible(Actor a) => FogOn ? FogEngCrateGate.Admits(player, a) : a.CanBeViewedByPlayer(player);"));
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
