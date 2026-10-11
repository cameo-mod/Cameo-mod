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

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Test.TestFixtures;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class BotFactionViewTest
	{
		const string PriorsYaml = "BotArsenalPriors:\n\tTradePercent@td_gdi__vs__td_nod:\n\t\ttd_gdi_mediumtank: 135\n";

		[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "DisplayFaction")]
		static extern ref FactionInfo DisplayFaction(OpenRA.Player player);

		static OpenRA.Player PlayerWithDisplayFaction(FactionInfo faction)
		{
			var player = Uninitialized.Player();
			DisplayFaction(player) = faction;
			return player;
		}

		static FactionInfo Faction(string internalName, params string[] randomMembers)
		{
			var nodes = new List<MiniYamlNode>
			{
				new("InternalName", new MiniYaml(internalName))
			};
			if (randomMembers.Length > 0)
				nodes.Add(new MiniYamlNode("RandomFactionMembers", new MiniYaml(string.Join(", ", randomMembers))));

			return FieldLoader.Load<FactionInfo>(new MiniYaml("", nodes));
		}

		[Test]
		public void FixedLobbyFactionIsPublic()
		{
			var enemy = PlayerWithDisplayFaction(Faction("td_nod"));
			Assert.That(BotFactionView.PublicFactionOf(enemy), Is.EqualTo("td_nod"));
		}

		[Test]
		public void RandomLobbyFactionIsNotPublic()
		{
			var enemy = PlayerWithDisplayFaction(Faction("random", "td_gdi", "td_nod"));
			Assert.That(BotFactionView.PublicFactionOf(enemy), Is.EqualTo(""));
		}

		[Test]
		public void NullPlayerAndDisplayFactionAreNotPublic()
		{
			Assert.That(BotFactionView.PublicFactionOf(null), Is.EqualTo(""));
			Assert.That(BotFactionView.PublicFactionOf(PlayerWithDisplayFaction(null)), Is.EqualTo(""));
		}

		[Test]
		public void RandomEnemyFindsNoLearnedMatchup()
		{
			var priors = ArsenalPriors.Parse(MiniYaml.FromString(PriorsYaml, "priors"));
			var publicFaction = BotFactionView.PublicFactionOf(PlayerWithDisplayFaction(Faction("random", "td_gdi", "td_nod")));
			Assert.That(publicFaction, Is.EqualTo(""));
			Assert.That(priors.TradePercent("td_gdi", publicFaction, "td_gdi_mediumtank"), Is.EqualTo(100),
				"a hidden Random pick cannot key td_gdi__vs__td_nod and stays neutral");
		}

		// D2 (orders 2026-10-03): the name-level predicate the map-ref faction path shares with PublicFactionOf —
		// a Random-ref name and an unresolvable name are both "".
		[Test]
		public void PublicNamePredicate()
		{
			Assert.That(BotFactionView.PublicName(null), Is.EqualTo(""));
			Assert.That(BotFactionView.PublicName(new FactionInfo()), Is.EqualTo(""), "InternalName null");
			Assert.That(BotFactionView.PublicName(Faction("td_gdi")), Is.EqualTo("td_gdi"));
			Assert.That(BotFactionView.PublicName(Faction("random", "td_gdi", "td_nod")), Is.EqualTo(""),
				"a faction name that picks randomly is hidden, same rule as the lobby view");
		}

		// D2: the dominant-faction pick shared by the live-players and map-ref paths of
		// PlanBanditBotModule.EnemyFactionOf (BuildOrderKnobsBotModule delegates to it).
		[Test]
		public void DominantFactionPicksMostCommonThenOrdinal()
		{
			Assert.That(PlanBanditBotModule.DominantFaction(new[] { "td_nod", "td_gdi", "td_nod" }), Is.EqualTo("td_nod"));
			Assert.That(PlanBanditBotModule.DominantFaction(new[] { "td_nod", "td_gdi" }), Is.EqualTo("td_gdi"),
				"a tie breaks by ordinal faction name, deterministically");
		}

		[Test]
		public void DominantFactionSkipsUnknownAndRandom()
		{
			Assert.That(PlanBanditBotModule.DominantFaction(new[] { "", null, "td_gdi" }), Is.EqualTo("td_gdi"));
			Assert.That(PlanBanditBotModule.DominantFaction(new[] { "", null }), Is.EqualTo(""),
				"all-Random lobbies yield no public enemy faction");
		}
	}
}
