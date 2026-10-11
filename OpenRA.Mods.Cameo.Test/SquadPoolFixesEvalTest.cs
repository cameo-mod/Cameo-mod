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
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class SquadPoolFixesEvalTest
	{
		[Test]
		public void AttackDispatchRetainsOnlyUnassignedPoolMembersInOrder()
		{
			var pool = new[] { "assigned-front", "orphan", "assigned-support", "leftover" };
			var retained = SquadPoolFixesEvalCA.RetainUnassigned(pool,
				new HashSet<string> { "assigned-front", "assigned-support" }, item => item);

			Assert.That(retained, Is.EqualTo(new[] { "orphan", "leftover" }));
		}

		[Test]
		public void AttackDispatchCanRetainEntirePoolWhenNothingWasAssigned()
		{
			var pool = new[] { 7, 3, 9 };
			Assert.That(SquadPoolFixesEvalCA.RetainUnassigned(pool, new int[0], item => item), Is.EqualTo(pool));
		}

		[Test]
		public void AnswerPoolGateCountsDraftableUnitsWhenFixIsEnabled()
		{
			// Six idle units include five leased/building/non-combat units; the old total passes, the fixed gate does not.
			Assert.That(SquadPoolFixesEvalCA.MeetsAnswerPoolMinimum(6, 1, 6, false), Is.True);
			Assert.That(SquadPoolFixesEvalCA.MeetsAnswerPoolMinimum(6, 1, 6, true), Is.False);
			Assert.That(SquadPoolFixesEvalCA.MeetsAnswerPoolMinimum(6, 6, 6, true), Is.True);
		}

		[Test]
		public void SwitchOffPreservesLegacyTotalPoolGate()
		{
			Assert.That(SquadPoolFixesEvalCA.MeetsAnswerPoolMinimum(8, 0, 8, false), Is.True);
			Assert.That(SquadPoolFixesEvalCA.MeetsAnswerPoolMinimum(7, 7, 8, false), Is.False);
		}
	}
}
