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
				"\tSchema: 1\n" +
				"\tEvidence@any: 150\n" +
				"\tEvidence@td_gdi: 150\n" +
				"\tEvidence@td_gdi__vs__td_nod: 150\n" +
				"\tRetreatRatioPct@any: 40\n" +
				"\tRetreatRatioPct@td_gdi: 45\n" +
				"\tRetreatRatioPct@td_gdi__vs__td_nod: 55\n" +
				"\tEngageMarginPct@any: 140\n");
			Assert.That(rows.TryRetreatRatioPct("td_gdi", "td_nod", out var exact), Is.True);
			Assert.That(exact, Is.EqualTo(55));
			Assert.That(rows.TryRetreatRatioPct("td_gdi", "ra1_soviets", out var faction), Is.True);
			Assert.That(faction, Is.EqualTo(45));
			Assert.That(rows.TryRetreatRatioPct("ra1_allies", "ra1_soviets", out var any), Is.True);
			Assert.That(any, Is.EqualTo(40));
			Assert.That(rows.TryEngageMarginPct("td_gdi", "td_nod", out var engage), Is.True);
			Assert.That(engage, Is.EqualTo(140));
		}

		[Test]
		public void MissingOrMalformedRowsKeepTheExistingThreshold()
		{
			var rows = Load("BotFightLearning:\n\tSchema: 1\n\tRetreatRatioPct@td_gdi: no\n");
			Assert.That(rows.TryRetreatRatioPct("td_gdi", "td_nod", out _), Is.False);
			Assert.That(rows.TryEngageMarginPct("td_gdi", "td_nod", out _), Is.False);
		}

		[Test]
		public void InvalidSchemaSparseEvidenceAndUnsafeScopesAreIgnored()
		{
			var invalid = Load("BotFightLearning:\n\tSchema: 999\n\tEvidence@td_gdi: 200\n\tRetreatRatioPct@td_gdi: 90\n");
			Assert.That(invalid.TryRetreatRatioPct("td_gdi", "", out _), Is.False);

			var sparse = Load("BotFightLearning:\n\tSchema: 1\n\tEvidence@td_gdi: 149\n\tRetreatRatioPct@td_gdi: 90\n");
			Assert.That(sparse.TryRetreatRatioPct("td_gdi", "", out _), Is.False);

			var unsafeScope = Load("BotFightLearning:\n\tSchema: 1\n\tEvidence@PrivatePlayerName: 200\n\tRetreatRatioPct@PrivatePlayerName: 90\n");
			Assert.That(unsafeScope.TryRetreatRatioPct("privateplayername", "", out _), Is.False);
		}

		[Test]
		public void FamilyScopeIsTheParentOfAFaction()
		{
			var rows = Load("BotFightLearning:\n\tSchema: 1\n\tEvidence@family_td: 150\n\tRetreatRatioPct@family_td: 65\n");
			Assert.That(rows.TryRetreatRatioPct("td_gdi", "", out var value), Is.True);
			Assert.That(value, Is.EqualTo(65));
		}
	}
}
