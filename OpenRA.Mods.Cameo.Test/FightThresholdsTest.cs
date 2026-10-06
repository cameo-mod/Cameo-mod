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
	public class FightThresholdsTest
	{
		static BotFightThresholds Load(string yaml) => BotFightThresholds.Parse(MiniYaml.FromString(yaml, "fight"));

		[Test]
		public void ExactScopeBeatsFactionAndAny()
		{
			var rows = Load("BotFightLearning:\n" +
				"\tRetreatRatioPct@any: 40\n" +
				"\tRetreatRatioPct@td_gdi: 45\n" +
				"\tRetreatRatioPct@td_gdi__vs__td_nod: 55\n" +
				"\tEngageMarginPct@any: 140\n");
			Assert.That(rows.RetreatRatioPct("td_gdi", "td_nod", 50), Is.EqualTo(55));
			Assert.That(rows.RetreatRatioPct("td_gdi", "ra1_soviets", 50), Is.EqualTo(45));
			Assert.That(rows.RetreatRatioPct("ra1_allies", "ra1_soviets", 50), Is.EqualTo(40));
			Assert.That(rows.EngageMarginPct("td_gdi", "td_nod", 150), Is.EqualTo(140));
		}

		[Test]
		public void MissingOrMalformedRowsKeepTheExistingThreshold()
		{
			var rows = Load("BotFightLearning:\n\tRetreatRatioPct@td_gdi: no\n");
			Assert.That(rows.RetreatRatioPct("td_gdi", "td_nod", 50), Is.EqualTo(50));
			Assert.That(rows.EngageMarginPct("td_gdi", "td_nod", 150), Is.EqualTo(150));
		}
	}
}

