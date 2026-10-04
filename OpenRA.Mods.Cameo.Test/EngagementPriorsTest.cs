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

		[Test]
		public void GlobalScaleMilliReabsolutizesRelativeFactors()
		{
			// The fixed fitter emits cell/defence/into-defences factors RELATIVE to a global obs/exp
			// scale g; GlobalScaleMilli multiplies g back in so the consumer reproduces measured
			// performance. Absent key = Schema-1 absolute semantics; unfitted lookups stay Neutral.
			var yaml = "BotEngagementPriors:\n" +
				"\tGlobalScaleMilli: 1130\n" +
				"\tIntoDefencesMilli: 1006\n" +
				"\tDeliveryArmour@CannonAP_Medium__x__Heavy: 1250\n" +
				"\tPriorPct@CannonAP_Medium__x__Heavy: 100\n" +
				"\tDefenceState@CannonAP_Medium: 800\n";
			var priors = Load(yaml);
			Assert.That(priors.FactorPermille("CannonAP_Medium", "Heavy"), Is.EqualTo(1412));   // 1250 x 1.13
			Assert.That(priors.DefenceFactorPermille("CannonAP_Medium"), Is.EqualTo(904));      // 800 x 1.13
			Assert.That(priors.IntoDefencesPermille, Is.EqualTo(1136));                         // 1006 x 1.13
			Assert.That(priors.FactorPermille("Bullet_Light", "Heavy"), Is.EqualTo(BotEngagementPriors.Neutral));
		}

		[Test]
		public void FitterMetadataBlocksAreSkipped()
		{
			// The real fitter appends header and analysis blocks beyond the consumption contract —
			// Schema/LedgerHash/Engagements + AttackTiming@/Response@/SuicideIndex@ (comma-list and
			// matchup values) — and yaml comments. The consumer must skip all of them, not throw.
			var yaml = "BotEngagementPriors:\n" +
				"\tSchema: 1\n" +
				"\tLedgerHash: b2da2f1f2f5a499c7768a43872a65981fee666013789f258124af3ecdb7f272f\n" +
				"\tEngagements: 2182\n" +
				"\t# global_scale 1.130 — relative factors below (v2 fitter)\n" +
				"\tDeliveryArmour@Bullet_Medium__x__Plate: 2000\n" +
				"\tPriorPct@Bullet_Medium__x__Plate: 100\n" +
				"\tDefenceState@Flak_Heavy: 2000\n" +
				"\tAttackTiming@td_gdi: 7944, 13615, 24084\n" +
				"\tAttackTiming@td_nod: 9219, 15874, 34130\n" +
				"\tResponse@td_gdi: 72, 419, 17\n" +
				"\tSuicideIndex@td_nod__vs__td_nod: 620\n";
			var priors = Load(yaml);
			Assert.That(priors.FactorPermille("Bullet_Medium", "Plate"), Is.EqualTo(2000));
			Assert.That(priors.DefenceFactorPermille("Flak_Heavy"), Is.EqualTo(2000));
			Assert.That(priors.IntoDefencesPermille, Is.EqualTo(BotEngagementPriors.Neutral));
		}

		[Test]
		public void AbsentGlobalScaleKeepsAbsoluteSemantics()
		{
			// A file with no GlobalScaleMilli row reads factors verbatim (Schema-1 absolute).
			Assert.That(Load(Yaml).FactorPermille("CannonAP_Medium", "Heavy"), Is.EqualTo(1250));
			Assert.That(Load("BotEngagementPriors:\n\tGlobalScaleMilli: 900\n").IntoDefencesPermille,
				Is.EqualTo(BotEngagementPriors.Neutral));  // no fitted row: g alone never fabricates a correction
		}
	}
}
