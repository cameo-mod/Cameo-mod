#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// The tier-1 return leg (DESIGN §19.13, AI_ARCHITECTURE §12.31, contract ruling F1): the canonical
	// BotEngagementPriors schema parses, every unknown is neutral, and a cell whose fitted PriorPct no
	// longer equals the resolved Versus prior reverts to neutral — per cell, not per file.
	// In tests Game.ModData is null so ResolvedTagVersus returns null and every cell's current prior
	// reads as the Versus-default 100: PriorPct 100 rows are fresh, anything else is stale.
	[TestFixture]
	public class EngagementPriorsTest
	{
		const string Yaml = "# GENERATED\nBotEngagementPriors:\n" +
			"\tSchema: 1\n" +
			"\tLedgerHash: abc123\n" +
			"\tEngagements: 42\n" +
			"\tAttritionExponentMilli: 1200\n" +
			"\tIntoDefencesMilli: 1100\n" +
			"\tDeliveryArmour@CannonAP_Medium__x__Heavy: 1250\n" +
			"\tPriorPct@CannonAP_Medium__x__Heavy: 100\n" +
			"\tDeliveryArmour@CannonAP_Medium__x__None: 700\n" +
			"\tPriorPct@CannonAP_Medium__x__None: 50\n" +   // fitted on a prior that has since moved: stale
			"\tDeliveryArmour@Bullet_Light__x__Wood: 900\n" + // no PriorPct row: unfitted, stays neutral
			"\tDefenceState@CannonAP_Medium: 800\n" +
			"\tAttackTiming@td_gdi: 6000, 12000, 18000\n";     // reserved tables ignored by the parser

		static BotEngagementPriors Load(string yaml) => BotEngagementPriors.Parse(MiniYaml.FromString(yaml, "priors"));

		[Test]
		public void ParsesCanonicalSchema()
		{
			var priors = Load(Yaml);
			Assert.That(priors.FactorCount, Is.EqualTo(3));
			Assert.That(priors.IntoDefencesPermille, Is.EqualTo(1100));
			Assert.That(priors.LedgerHash, Is.EqualTo("abc123"));
			Assert.That(priors.AttritionExponentMilli, Is.EqualTo(1200));
			Assert.That(priors.DefenceFactorPermille("CannonAP_Medium"), Is.EqualTo(800));
		}

		[Test]
		public void FreshCellAppliesFittedResidual()
		{
			var priors = Load(Yaml);
			Assert.That(priors.FactorPermille("CannonAP_Medium", "Heavy"), Is.EqualTo(1250));
			Assert.That(priors.StaleCount, Is.EqualTo(0));
		}

		[Test]
		public void StaleCellRevertsToNeutralPerCell()
		{
			var priors = Load(Yaml);
			// CannonAP_Medium x None was fitted on prior 50; the resolved prior reads 100 now → neutral.
			Assert.That(priors.FactorPermille("CannonAP_Medium", "None"), Is.EqualTo(BotEngagementPriors.Neutral));
			Assert.That(priors.StaleCount, Is.EqualTo(1));
			// The sibling cell on the same delivery is unaffected — staleness is per cell, not per file.
			Assert.That(priors.FactorPermille("CannonAP_Medium", "Heavy"), Is.EqualTo(1250));
		}

		[Test]
		public void UnfittedAndUnknownCellsAreNeutral()
		{
			var priors = Load(Yaml);
			// A DeliveryArmour cell with no PriorPct row was never staked to a prior: neutral.
			Assert.That(priors.FactorPermille("Bullet_Light", "Wood"), Is.EqualTo(BotEngagementPriors.Neutral));
			Assert.That(priors.FactorPermille("spreadhe", "Wood"), Is.EqualTo(BotEngagementPriors.Neutral));
			Assert.That(priors.FactorPermille("CannonAP_Medium", "Light"), Is.EqualTo(BotEngagementPriors.Neutral));
			Assert.That(priors.FactorPermille(null, "Heavy"), Is.EqualTo(BotEngagementPriors.Neutral));
			Assert.That(priors.FactorPermille("CannonAP_Medium", null), Is.EqualTo(BotEngagementPriors.Neutral));
		}

		[Test]
		public void DefenceStateDefaultsAndIntoDefences()
		{
			var priors = Load(Yaml);
			Assert.That(priors.DefenceFactorPermille("Bullet_Light"), Is.EqualTo(BotEngagementPriors.Neutral));
			Assert.That(priors.DefenceFactorPermille(null), Is.EqualTo(BotEngagementPriors.Neutral));
			Assert.That(Load("Other:\n\tA: 1\n").IntoDefencesPermille, Is.EqualTo(BotEngagementPriors.Neutral));
		}

		[Test]
		public void MissingOrForeignRootIsEmpty()
		{
			var priors = Load("EngagementPriors:\n\tFactor@d|a: 1500\n");  // the retired schema is not read back
			Assert.That(priors.FactorCount, Is.EqualTo(0));
			Assert.That(priors.FactorPermille("d", "a"), Is.EqualTo(BotEngagementPriors.Neutral));
		}
	}
}
