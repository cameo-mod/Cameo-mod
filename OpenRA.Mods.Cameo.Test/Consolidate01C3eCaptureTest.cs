#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;
using OpenRA.Support;

namespace OpenRA.Mods.Cameo.Test
{
	// CONSOLIDATE-01 c3e (CAPTURE-TECH-01, switch cs_capture_unlock): stage S5 also offers another faction's power plant, silo or refinery that unlocks its tech, so a
	// captured construction yard can build it. The pure rule (CaptureUnlockCA, CaptureUnlockMapsCA) is tested directly, S5's order through CoreOnceCA, and the
	// world-dependent wiring by pins. The switch is off by default and the classic ladder is untouched.
	[TestFixture]
	public sealed class Consolidate01C3eCaptureTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		const string StagesFile = "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.Pipeline01Stages.cs";
		const string DefenceFile = "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.Pipeline01Defence.cs";
		const string QmFile = "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs";

		static IReadOnlyList<string> L(params string[] v) => v;

		// A captured GDI yard for a Nod player: both power plants need the yard, the GDI barracks needs the GDI power plant, the refinery likewise.
		static readonly Dictionary<string, IReadOnlyList<string>> Prereqs = new()
		{
			["fact"] = L(),
			["nod_power"] = L("fact"),
			["gdi_power"] = L("fact"),
			["gdi_barracks"] = L("gdi_power"),
			["nod_barracks"] = L("nod_power"),
			["nod_refinery"] = L("nod_power"),
			["gdi_refinery"] = L("gdi_power"),
			["gdi_silo"] = L("fact", "!nope"),
			["gdi_hightech"] = L("~gdi_refinery"),
		};

		static readonly Dictionary<string, IReadOnlyList<string>> Provides = new()
		{
			["fact"] = L("fact"),
			["nod_power"] = L("nod_power", "anypower"),
			["gdi_power"] = L("GDI_Power", "anypower"),
			["gdi_barracks"] = L("gdi_barracks"),
			["nod_barracks"] = L("nod_barracks"),
			["nod_refinery"] = L("nod_refinery"),
			["gdi_refinery"] = L("gdi_refinery"),
			["gdi_silo"] = L("gdi_silo"),
			["gdi_hightech"] = L("gdi_hightech"),
		};

		static readonly CaptureUnlockMapsCA Maps = CaptureUnlockMapsCA.Build(Prereqs, Provides);

		static bool Unlock(string type, ISet<string> has, int categoryOwned) =>
			CaptureUnlockCA.IsUnlock(Maps.ProvidedBy[type], t => !has.Contains(t), t => Maps.GatesOther(t, type), categoryOwned);

		static ISet<string> Has(params string[] tokens) => new HashSet<string>(tokens, StringComparer.Ordinal);

		[Test]
		public void TheMapsAreLowerCaseUnfilteredAndIgnoreNegations()
		{
			Assert.That(Maps.ProvidedBy["gdi_power"], Is.EquivalentTo(new[] { "gdi_power", "anypower" }), "lower-cased");
			Assert.That(Maps.Gates["gdi_power"], Is.EquivalentTo(new[] { "gdi_barracks", "gdi_refinery" }));
			Assert.That(Maps.Gates["fact"], Does.Contain("gdi_silo"));
			Assert.That(Maps.Gates.ContainsKey("nope"), Is.False, "a negation gates nothing");
			Assert.That(Maps.Gates["gdi_refinery"], Is.EquivalentTo(new[] { "gdi_hightech" }), "a ~ prefix is stripped");
			Assert.That(Maps.GatesOther("gdi_power", "gdi_barracks"), Is.True);
			Assert.That(Maps.GatesOther("gdi_barracks", "gdi_barracks"), Is.False, "never itself");
			Assert.That(Maps.GatesOther("unknown", "x"), Is.False);
		}

		// What PipelineCoreOnceStage does for a type: an unlock is a first-copy kind, otherwise power plants, silos and refineries are no first-copy kind.
		static CoreOnceInput Input(string name, int depth, bool unlock, int count = 0) =>
			new(name, true, -1, unlock || !(name.Contains("power") || name.Contains("silo") || name.Contains("refinery")), false, count, depth, 100);

		[Test]
		public void ACapturedYardOffersTheUnlockingPowerPlantBeforeWhatItGates()
		{
			var has = Has("fact", "nod_power", "anypower", "nod_barracks");
			Assert.That(Unlock("gdi_power", has, categoryOwned: 1), Is.True, "gdi_power gives a token the player lacks and gdi_barracks needs it");

			var offered = CoreOnceCA.Order(new[]
			{
				Input("gdi_barracks", 2, false),
				Input("gdi_power", 1, Unlock("gdi_power", has, 1)),
			});
			Assert.That(offered, Is.EqualTo(new[] { "gdi_power", "gdi_barracks" }), "depth order");

			var without = CoreOnceCA.Order(new[] { Input("gdi_power", 1, false) });
			Assert.That(without, Is.Empty, "a power plant is no first-copy kind for the fallback by itself");
		}

		[Test]
		public void OnceThePlayerHasItTheTypeItGatesIsOfferedAndTheUnlockIsNot()
		{
			var has = Has("fact", "nod_power", "anypower", "gdi_power");
			Assert.That(Unlock("gdi_power", has, 1), Is.False, "the token is no longer lacking");

			var offered = CoreOnceCA.Order(new[]
			{
				Input("gdi_barracks", 2, false),
				Input("gdi_power", 1, Unlock("gdi_power", has, 1), count: 1),
			});
			Assert.That(offered, Is.EqualTo(new[] { "gdi_barracks" }));
		}

		[Test]
		public void ANormalOwnFactionGameIsUnchanged()
		{
			var has = Has("fact", "nod_power", "anypower", "nod_barracks", "nod_refinery");
			Assert.That(Unlock("nod_power", has, 1), Is.False, "every token present");
			Assert.That(Unlock("nod_refinery", has, 1), Is.False);

			// Condition (c): nothing of the category owned yet, so the opening and S1 build the first one, never S5.
			Assert.That(Unlock("gdi_power", Has("fact"), categoryOwned: 0), Is.False);
			Assert.That(Unlock("nod_power", Has("fact"), categoryOwned: 0), Is.False);
			Assert.That(Unlock("gdi_power", Has("fact"), categoryOwned: 1), Is.True, "with one owned the same type is an unlock");
		}

		[Test]
		public void ARefineryOrSiloIsAnUnlockOnlyWhenItGatesAnotherType()
		{
			var has = Has("fact", "nod_power", "anypower", "gdi_power", "nod_refinery");
			Assert.That(Unlock("gdi_refinery", has, 1), Is.True, "the only unlock: gdi_hightech needs gdi_refinery");

			var hasRefinery = Has("fact", "nod_power", "anypower", "gdi_power", "nod_refinery", "gdi_refinery");
			Assert.That(Unlock("gdi_refinery", hasRefinery, 1), Is.False);

			Assert.That(Unlock("gdi_silo", has, 1), Is.False, "a silo that unlocks nothing");
			Assert.That(Unlock("nod_refinery", Has("fact", "anypower"), 1), Is.False, "a refinery that unlocks nothing is not offered");

			// The stage only accepts a refinery the sweep marked as an unlock.
			var d = Source(DefenceFile);
			Assert.That(d, Does.Contain("(!baseBuilder.Info.RefineryTypes.Contains(candidate.Name) || (baseBuilder.CaptureUnlockArmed && captureUnlockOffered.Contains(candidate.Name)))"));
			Assert.That(d, Does.Contain("!baseBuilder.Info.ConstructionYardTypes.Contains(candidate.Name)"), "yards are still rejected");
		}

		[Test]
		public void DegenerateInputsAreNeverAnUnlock()
		{
			Assert.That(CaptureUnlockCA.IsUnlock(null, _ => true, _ => true, 1), Is.False);
			Assert.That(CaptureUnlockCA.IsUnlock(L("x"), null, _ => true, 1), Is.False);
			Assert.That(CaptureUnlockCA.IsUnlock(L("x"), _ => true, null, 1), Is.False);
			Assert.That(CaptureUnlockCA.IsUnlock(L("x"), _ => true, _ => true, 0), Is.False);
			Assert.That(CaptureUnlockCA.IsUnlock(L(), _ => true, _ => true, 1), Is.False);
			Assert.That(CaptureUnlockCA.IsUnlock(L("a", "b"), t => t == "b", t => t == "b", 2), Is.True, "any one token is enough");
		}

		[Test]
		public void SwitchOffMeansNoUnlockIsEverOfferedAndTheLadderIsUntouched()
		{
			var stages = Source(StagesFile);
			Assert.That(stages, Does.Contain("var captureOn = baseBuilder.CaptureUnlockArmed;"));
			Assert.That(stages, Does.Contain("if (!firstCopy && captureOn)"), "the capture check runs only with the switch on and never for a first-copy kind");
			Assert.That(stages, Does.Contain("if (!firstCopy && !unlock)"));
			Assert.That(stages, Does.Contain("OwnRuleStage: b.ConstructionYardTypes.Contains(name) || (b.RefineryTypes.Contains(name) && !unlock),"));
			Assert.That(stages, Does.Contain("CaptureUnlockCA.IsUnlock(provided,"));
			Assert.That(stages, Does.Contain("tree.HasPrerequisites(new[] { token })"));
			Assert.That(stages, Does.Contain("if (!lacksCache.TryGetValue(token, out var lacks))"), "once per token per sweep");

			var qm = Source(QmFile);
			Assert.That(qm, Does.Not.Contain("CaptureUnlock"), "the classic ladder does not know the capture rule");

			var module = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			Assert.That(module, Does.Contain("public readonly bool CsCaptureUnlock = false;"));
			Assert.That(module, Does.Contain("public bool CaptureUnlockArmed => Info.CsCaptureUnlock && switchesActive && !ablateCaptureUnlock.Off(player, AblationIds.CsCaptureUnlock);"));
			Assert.That(new BaseBuilderBotModuleCAInfo().CsCaptureUnlock, Is.False);
			// C7 arms it in BaseBuilderBotModuleCA@generic only (Consolidate01C7Test).
		}

		[Test]
		public void TheSwitchRowIsRegistered()
		{
			Assert.That(AblationIds.CsCaptureUnlock, Is.EqualTo("cs_capture_unlock"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("cs_capture_unlock"));
			Assert.That(AblationSpec.TryParse("cs_capture_unlock", AblationSpec.RegistryIds(), out _, out _), Is.True);
		}
	}
}
