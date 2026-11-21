#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is made available under the GNU General Public License
 * version 3 or any later version. See COPYING for details.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class HarvesterLogisticsCooldownTest
	{
		[Test]
		public void ReassignCooldownMatchesInclusiveAbsoluteDeadline()
		{
			var cooldown = HarvesterLogisticsCooldown.Start(10, 750);
			Assert.That(HarvesterLogisticsCooldown.IsDue(cooldown, 759), Is.False);
			Assert.That(HarvesterLogisticsCooldown.IsDue(cooldown, 760), Is.True);
			Assert.That(HarvesterLogisticsCooldown.IsDue(cooldown, 900), Is.True,
				"A late eligible scan is ready without catch-up.");

			cooldown = HarvesterLogisticsCooldown.Start(900, 750);
			Assert.That(HarvesterLogisticsCooldown.IsDue(cooldown, 1649), Is.False);
			Assert.That(HarvesterLogisticsCooldown.IsDue(cooldown, 1650), Is.True);
		}
	}
}
