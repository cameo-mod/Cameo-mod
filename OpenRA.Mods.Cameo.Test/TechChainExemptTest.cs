#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// TECHCHAIN-EXEMPT-01 (BINDING tech chain, 11-23): the pure exemption of a faction's T2 / T3 gate from the army-first pause
	// and the mobile reserve. The caller (IsArmyFirstEssential) supplies list membership, owned count and queued flag; the names
	// in the cases document which building each row stands for. Config-level pins (the six ai.yaml lists) are not covered here.
	[TestFixture]
	public class TechChainExemptTest
	{
		// td_nod: td_nod_communicationscenter (T2 gate, G10 root-cause building) and td_nod_templeofnod (T3).
		[TestCase(true, 0, false, TestName = "td_nod_communicationscenter_first_copy_exempt")]
		[TestCase(true, 1, false, TestName = "td_nod_communicationscenter_owned_not_exempt")]
		[TestCase(true, 0, true, TestName = "td_nod_communicationscenter_queued_not_exempt")]
		[TestCase(true, 2, true, TestName = "td_nod_communicationscenter_owned_and_queued_not_exempt")]
		public void TdNodTechChainGate(bool inList, int owned, bool queued)
		{
			var expected = owned == 0 && !queued;
			Assert.AreEqual(expected, TechChainExemptCA.Exempt(inList, owned, queued));
		}

		// RA2 Allies: ra2_allies_battlelab is the T3 gate by role (Opus ruling: RA2 Battle Lab is T3).
		[TestCase(true, 0, false, ExpectedResult = true, TestName = "ra2_allies_battlelab_first_copy_exempt")]
		[TestCase(true, 1, false, ExpectedResult = false, TestName = "ra2_allies_battlelab_owned_not_exempt")]
		[TestCase(true, 0, true, ExpectedResult = false, TestName = "ra2_allies_battlelab_queued_not_exempt")]
		public bool Ra2AlliesTechChainGate(bool inList, int owned, bool queued)
		{
			return TechChainExemptCA.Exempt(inList, owned, queued);
		}

		// TiberianDawn GDI: td_gdi_advancedcommunicationscenter (T3 by role).
		[TestCase(true, 0, false, ExpectedResult = true, TestName = "td_gdi_advancedcommunicationscenter_first_copy_exempt")]
		[TestCase(true, 0, true, ExpectedResult = false, TestName = "td_gdi_advancedcommunicationscenter_queued_not_exempt")]
		public bool TdGdiTechChainGate(bool inList, int owned, bool queued)
		{
			return TechChainExemptCA.Exempt(inList, owned, queued);
		}

		// A building outside the faction's TechChainTypes list is never exempt, whatever its owned or queued state.
		[TestCase(0, false, TestName = "not_in_list_unowned_not_exempt")]
		[TestCase(1, false, TestName = "not_in_list_owned_not_exempt")]
		[TestCase(0, true, TestName = "not_in_list_queued_not_exempt")]
		public void BuildingOutsideTechChainListIsNeverExempt(int owned, bool queued)
		{
			Assert.IsFalse(TechChainExemptCA.Exempt(false, owned, queued));
		}
	}
}
