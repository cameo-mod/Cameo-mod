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
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class BotIssuerTest
	{
		// Stand-ins for module traits: two types, several instances each, to drive the ordinal math.
		sealed class FakeModuleA { }
		sealed class FakeModuleB { }

		[Test]
		public void OrdinalPositionsNameSameTypeSiblingsInOrder()
		{
			var a0 = new FakeModuleA();
			var a1 = new FakeModuleA();
			var a2 = new FakeModuleA();
			var b0 = new FakeModuleB();
			var siblings = new object[] { b0, a0, a1, new object(), a2 };

			Assert.That(BotIssuer.OrdinalOf(a0, siblings), Is.EqualTo("FakeModuleA@0"));
			Assert.That(BotIssuer.OrdinalOf(a1, siblings), Is.EqualTo("FakeModuleA@1"));
			Assert.That(BotIssuer.OrdinalOf(a2, siblings), Is.EqualTo("FakeModuleA@2"));
			Assert.That(BotIssuer.OrdinalOf(b0, siblings), Is.EqualTo("FakeModuleB@0"),
				"other types do not consume the ordinal");
		}

		[Test]
		public void AStrangerTraitGetsTheAnomalySentinel()
		{
			var outsider = new FakeModuleA();
			Assert.That(BotIssuer.OrdinalOf(outsider, new object[] { new FakeModuleA(), new FakeModuleA() }),
				Is.EqualTo("FakeModuleA@?"), "not among the enumerated siblings — a caller bug made visible");
			Assert.That(BotIssuer.OrdinalOf(outsider, new object[0]), Is.EqualTo("FakeModuleA@?"));
		}

		[Test]
		public void TypeOfStripsTheOrdinalAndLeavesBareNamesAlone()
		{
			Assert.That(BotIssuer.TypeOf("SquadManagerBotModuleCA@3"), Is.EqualTo("SquadManagerBotModuleCA"));
			Assert.That(BotIssuer.TypeOf("SquadManagerBotModuleCA"), Is.EqualTo("SquadManagerBotModuleCA"));
			Assert.That(BotIssuer.TypeOf("SquadManagerBotModuleCA@?"), Is.EqualTo("SquadManagerBotModuleCA"));
			Assert.That(BotIssuer.TypeOf(null), Is.Null);
		}

		[Test]
		public void TheAmbientScopeNestsAndRestores()
		{
			Assert.That(BotIssuer.Current, Is.Null, "no scope outside a provider call");

			using (BotIssuer.IssueAs("FransTransportCommanderBotModule@0"))
			{
				Assert.That(BotIssuer.Current, Is.EqualTo("FransTransportCommanderBotModule@0"));

				using (BotIssuer.IssueAs("NestedProvider@0"))
					Assert.That(BotIssuer.Current, Is.EqualTo("NestedProvider@0"), "an inner provider wins");

				Assert.That(BotIssuer.Current, Is.EqualTo("FransTransportCommanderBotModule@0"), "dispose restores the outer scope");
			}

			Assert.That(BotIssuer.Current, Is.Null);
		}

		[Test]
		public void InstancesOfTheIssuingTypeOwnTheirUnits()
		{
			// Six SquadManager instances are one subsystem: any instance's order clears a lease
			// owned by the bare type name, whichever instance claimed it.
			Assert.That(BotOrderGate<int>.Decide("SquadManagerBotModuleCA@0", "SquadManagerBotModuleCA", true, false, null),
				Is.EqualTo(BotOrderVerdict.Allow));
			Assert.That(BotOrderGate<int>.Decide("SquadManagerBotModuleCA@5", "SquadManagerBotModuleCA", true, false, null),
				Is.EqualTo(BotOrderVerdict.Allow), "a sibling instance is the same owner, not a rival");
			Assert.That(BotOrderGate<int>.Decide("SquadManagerBotModuleCA@0", "ScoutBotModule", true, false, null),
				Is.EqualTo(BotOrderVerdict.Refuse), "a different TYPE is still a rival");
		}

		[Test]
		public void EmergencyModulesMatchByTypeOrByInstance()
		{
			var byType = new HashSet<string> { "SquadManagerBotModuleCA" };
			var byInstance = new HashSet<string> { "SquadManagerBotModuleCA@0" };

			Assert.That(BotOrderGate<int>.Decide("SquadManagerBotModuleCA@0", "CratePickupBotModule", true, true, byType),
				Is.EqualTo(BotOrderVerdict.Preempt), "the yaml type name still lists the whole subsystem");
			Assert.That(BotOrderGate<int>.Decide("SquadManagerBotModuleCA@0", "CratePickupBotModule", true, true, byInstance),
				Is.EqualTo(BotOrderVerdict.Preempt), "an instanced entry lists that one instance");
			Assert.That(BotOrderGate<int>.Decide("SquadManagerBotModuleCA@1", "CratePickupBotModule", true, true, byInstance),
				Is.EqualTo(BotOrderVerdict.Refuse), "a sibling instance is not the listed one");
		}

		[Test]
		public void SameTypeInstanceHandOffsAreNotCrossedOrders()
		{
			// Ownership is type-scoped, so squad 0 handing a unit to squad 3 inside the window is the
			// subsystem re-drafting — invisible to the crossed counter, exactly as bare-name issuers were.
			var gate = new BotOrderGate<int>();
			Assert.That(gate.NoteIssued(7, "SquadManagerBotModuleCA@0", 100, 100, "SquadManagerBotModuleCA"), Is.Null);
			Assert.That(gate.NoteIssued(7, "SquadManagerBotModuleCA@3", 150, 100, "SquadManagerBotModuleCA"), Is.Null);
			Assert.That(gate.Crossed, Is.EqualTo(0));
		}

		[Test]
		public void ACrossTypeCrossRecordsTheInstancedPair()
		{
			var gate = new BotOrderGate<int>();
			Assert.That(gate.NoteIssued(7, "SquadManagerBotModuleCA@2", 100, 100, "SquadManagerBotModuleCA"), Is.Null);
			Assert.That(gate.NoteIssued(7, "ScoutBotModule@0", 150, 100, "SquadManagerBotModuleCA"),
				Is.EqualTo("SquadManagerBotModuleCA@2"), "the named earlier issuer carries its ordinal");
			Assert.That(gate.Crossed, Is.EqualTo(1));
			Assert.That(gate.CrossedPairs[("SquadManagerBotModuleCA@2", "ScoutBotModule@0")], Is.EqualTo(1));
		}

		[Test]
		public void AReleasedClaimStillReadsAsAHandOffWithInstancedIssuers()
		{
			var gate = new BotOrderGate<int>();
			Assert.That(gate.NoteIssued(7, "SquadManagerBotModuleCA@2", 100, 100, "SquadManagerBotModuleCA"), Is.Null);
			Assert.That(gate.NoteIssued(7, "ScoutBotModule@0", 150, 100, null), Is.Null,
				"the squad type released the unit — a clean hand-off, not a fight");
			Assert.That(gate.Crossed, Is.EqualTo(0));
		}
	}
}
