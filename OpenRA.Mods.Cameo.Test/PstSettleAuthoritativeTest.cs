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
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	// CONSOLIDATE-03 P6 (b) row pst_settle_authoritative (Opus ruling, option a), and the P6 (a) settle_release fold braces (fce112fb1).
	[TestFixture]
	public sealed class PstSettleAuthoritativeTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void TheRowIsRegisteredAndDefaultOff()
		{
			Assert.That(AblationIds.PstSettleAuthoritative, Is.EqualTo("pst_settle_authoritative"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain(AblationIds.PstSettleAuthoritative));

			var unitBuilder = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(unitBuilder, Does.Contain("public readonly bool PstSettleAuthoritative = false;"));

			var yaml = Source("packaging/ai.yaml");
			Assert.That(yaml, Does.Contain("PstSettleAuthoritative: false"));
			Assert.That(yaml, Does.Not.Contain("PstSettleAuthoritative: true"), "master ships with the row off (untested)");
		}

		[Test]
		public void OffIsTheOwnScanAndOnIsTheOwnTechTree()
		{
			var partial = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.PstSettle.cs");
			Assert.That(partial, Does.Contain("bool PstSettleAuthoritativeOn => Info.PstSettleAuthoritative && !ablatePstSettleAuthoritative.Off(player, AblationIds.PstSettleAuthoritative);"));
			Assert.That(partial, Does.Contain("if (!PstSettleAuthoritativeOn)\n\t\t\t\treturn OwnedPrerequisites();"));
			Assert.That(partial, Does.Contain("if (techTree.HasPrerequisites(new[] { p }))"));

			var unitBuilder = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(unitBuilder, Does.Contain("upgradeDone.Settle(world.WorldTick, queuedIn, UpgradeGrantsOf, SettleOwned(), OfferedUpgradeNames,"));
		}

		[Test]
		public void SettleReleaseFoldIsInsideItsLoop()
		{
			var unitBuilder = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(unitBuilder, Does.Contain("foreach (var (name, reason) in upgradeDone.Released)\n\t\t\t{\n\t\t\t\tUpgradeReleaseLog(name, reason);\n\t\t\t\tPendingStreamNote(\"settle_release\""));
		}
	}
}
