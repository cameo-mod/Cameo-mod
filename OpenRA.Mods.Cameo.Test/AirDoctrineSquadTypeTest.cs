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
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	// CA-5 (AI_ARCHITECTURE.md 12.8): the doctrine split maps role members to
	// Fighter/Gunship/Bomber squads; with AirDoctrineEnabled off (or an unroled
	// transport/scout) everything stays in generic Air squads.
	[TestFixture]
	public class AirDoctrineSquadTypeTest
	{
		static readonly IReadOnlySet<string> Fighters = new HashSet<string> { "mig" };
		static readonly IReadOnlySet<string> Gunships = new HashSet<string> { "orca" };
		static readonly IReadOnlySet<string> Bombers = new HashSet<string> { "b2" };

		static SquadCAType Classify(bool enabled, string name) =>
			SquadManagerBotModuleCA.AirSquadTypeFor(enabled, name, Fighters, Gunships, Bombers);

		[Test]
		public void DoctrineOffKeepsGenericAir()
		{
			Assert.That(Classify(false, "mig"), Is.EqualTo(SquadCAType.Air));
			Assert.That(Classify(false, "orca"), Is.EqualTo(SquadCAType.Air));
			Assert.That(Classify(false, "b2"), Is.EqualTo(SquadCAType.Air));
		}

		[Test]
		public void DoctrineOnMapsRoles()
		{
			Assert.That(Classify(true, "mig"), Is.EqualTo(SquadCAType.Fighter));
			Assert.That(Classify(true, "orca"), Is.EqualTo(SquadCAType.Gunship));
			Assert.That(Classify(true, "b2"), Is.EqualTo(SquadCAType.Bomber));
		}

		[Test]
		public void UnroledStaysGenericAir()
		{
			Assert.That(Classify(true, "chinook"), Is.EqualTo(SquadCAType.Air));
		}
	}
}
