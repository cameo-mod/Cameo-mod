#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors. This file is made
 * available under the terms of the GNU General Public License, version 3.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class BaseBuilderRuleMetadataSnapshotTest
	{
		static string Source(string path) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", path)).Replace("\r\n", "\n");

		[Test]
		public void BothRuleMapsUseOneLazySnapshot()
		{
			var baseBuilder = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			var stages = Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.Pipeline01Stages.cs");
			var snapshot = Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderRuleMetadataCA.cs");

			Assert.That(baseBuilder, Does.Contain("internal BaseBuilderRuleMetadataCA RuleMetadata => ruleMetadata ??= BaseBuilderRuleMetadataCA.Capture(world.Map.Rules);"));
			Assert.That(stages, Does.Contain("baseBuilder.CaptureUnlockMaps = baseBuilder.RuleMetadata.CaptureMaps();"));
			Assert.That(stages, Does.Contain("baseBuilder.RuleMetadata.CoreDepths(player.Faction.InternalName, baseBuilder.Info.ConstructionYardTypes)"));
			Assert.That(stages, Does.Not.Contain("world.Map.Rules.Actors.Values"));
			Assert.That(Regex.Matches(snapshot, "OrderBy\\(a => a.Name").Count, Is.EqualTo(1));
		}

		[Test]
		public void SnapshotProjectionsMatchThePreviousCaptureAndFactionDepthMaps()
		{
			var yard = new BaseBuilderRuleMetadataCA.Building("yard", [], [new("yard", [])]);
			var power = new BaseBuilderRuleMetadataCA.Building("power_gdi", ["yard"], [new("power", ["gdi"])]);
			var enemyPower = new BaseBuilderRuleMetadataCA.Building("power_nod", ["yard"], [new("power", ["nod"])]);
			var factory = new BaseBuilderRuleMetadataCA.Building("factory", ["Power", "~!doctrine"], []);
			var snapshot = BaseBuilderRuleMetadataCA.FromRecords([factory, enemyPower, power, yard]);

			Assert.That(snapshot.ActorNames, Is.EqualTo(new[] { "factory", "power_gdi", "power_nod", "yard" }), "capture is ordinal and stable");

			var prereqs = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
			{
				["yard"] = Array.Empty<string>(),
				["power_gdi"] = new[] { "yard" },
				["power_nod"] = new[] { "yard" },
				["factory"] = new[] { "Power", "~!doctrine" }
			};
			var provides = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
			{
				["yard"] = new[] { "yard" },
				["power_gdi"] = new[] { "power" },
				["power_nod"] = new[] { "power" },
				["factory"] = Array.Empty<string>()
			};
			var expectedCapture = CaptureUnlockMapsCA.Build(prereqs, provides);
			var actualCapture = snapshot.CaptureMaps();
			Assert.That(actualCapture.ProvidedBy.Keys, Is.EquivalentTo(expectedCapture.ProvidedBy.Keys));
			foreach (var key in expectedCapture.ProvidedBy.Keys)
				Assert.That(actualCapture.ProvidedBy[key], Is.EqualTo(expectedCapture.ProvidedBy[key]), key);
			Assert.That(actualCapture.Gates.Keys, Is.EquivalentTo(expectedCapture.Gates.Keys));
			foreach (var key in expectedCapture.Gates.Keys)
				Assert.That(actualCapture.Gates[key], Is.EquivalentTo(expectedCapture.Gates[key]), key);

			var gdiProviders = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
			{
				["yard"] = new[] { "yard" },
				["power"] = new[] { "power_gdi" }
			};
			var expectedDepths = TechDepthCA.Compute(prereqs, gdiProviders, new[] { "yard" });
			var actualDepths = snapshot.CoreDepths("gdi", new[] { "yard" });
			Assert.That(actualDepths, Is.EqualTo(expectedDepths));
			Assert.That(actualDepths["factory"], Is.EqualTo(2));
		}
	}
}
