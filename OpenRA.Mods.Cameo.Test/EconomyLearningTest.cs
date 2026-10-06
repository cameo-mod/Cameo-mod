#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors.
 * This file is part of OpenRA, which is free software.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Support;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class EconomyLearningTest
	{
		static BotEconomyLearning Load(string yaml) => BotEconomyLearning.Parse(MiniYaml.FromString(yaml, "economy"));

		[Test]
		public void ExactScopeBeatsFactionAndAny()
		{
			var rows = Load("BotEconomyLearning:\n" +
				"\tSchema: 1\n" +
				"\tHarvesterLimit@any: 6\n" +
				"\tHarvesterLimit@td_gdi: 8\n" +
				"\tHarvesterLimit@td_gdi__vs__td_nod: 10\n" +
				"\tHarvesterEvidence@any: 120\n" +
				"\tHarvesterEvidence@td_gdi: 120\n" +
				"\tHarvesterEvidence@td_gdi__vs__td_nod: 120\n" +
				"\tCreditFloat@any: 2500\n" +
				"\tCreditFloatEvidence@any: 150\n");
			Assert.That(rows.HarvesterLimit("td_gdi", "td_nod", 4), Is.EqualTo(10));
			Assert.That(rows.HarvesterLimit("td_gdi", "ra1_soviet", 4), Is.EqualTo(8));
			Assert.That(rows.HarvesterLimit("ra1_allies", "td_nod", 4), Is.EqualTo(6));
			Assert.That(rows.CreditFloat("td_gdi", "td_nod", 2000), Is.EqualTo(2500));
		}

		[Test]
		public void MissingOrMalformedRowsKeepFallback()
		{
			var rows = Load("BotEconomyLearning:\n\tSchema: 1\n\tExpansionCashDivisor@td_gdi: malformed\n");
			Assert.That(rows.ExpansionCashDivisor("td_gdi", "td_nod", 12000), Is.EqualTo(12000));
			Assert.That(rows.ProductionCashThreshold("td_gdi", "td_nod", 10000), Is.EqualTo(10000));
		}

		[Test]
		public void InvalidSchemaAndUnsafeScopeKeepFallback()
		{
			var invalid = Load("BotEconomyLearning:\n\tSchema: 2\n\tHarvesterLimit@td_gdi: 8\n");
			Assert.That(invalid.HarvesterLimit("td_gdi", "", 4), Is.EqualTo(4));

			var unsafeScope = Load("BotEconomyLearning:\n\tSchema: 1\n\tHarvesterLimit@PrivatePlayerName: 8\n");
			Assert.That(unsafeScope.HarvesterLimit("privateplayername", "", 4), Is.EqualTo(4));
		}

		[Test]
		public void SparseEvidenceKeepsFallbackAndDoesNotMaskParent()
		{
			var rows = Load("BotEconomyLearning:\n" +
				"\tSchema: 1\n" +
				"\tHarvesterLimit@any: 6\n" +
				"\tHarvesterEvidence@any: 120\n" +
				"\tHarvesterLimit@td_gdi: 10\n" +
				"\tHarvesterEvidence@td_gdi: 119\n");
			Assert.That(rows.HarvesterLimit("td_gdi", "td_nod", 4), Is.EqualTo(6));
		}
	}
}
