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

using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// ABLATE-01 hook, shared half (ABLATE_01_PLAN_03 section 2). Written with the code; not run (no build in the PERF WINDOW).
	// The provider, the ServerTrait and the lobby option live in the DEV assembly and are covered by the
	// DevelopmentLoggingPackagingContractTest extension, not here.
	[TestFixture]
	public class AblationGateTest
	{
		static string[] Ids() => typeof(AblationIds)
			.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
			.Where(f => f.IsLiteral && f.FieldType == typeof(string))
			.Select(f => (string)f.GetRawConstantValue())
			.ToArray();

		[Test]
		public void NoOwnerOrNoIdIsNeverOff()
		{
			Assert.That(AblationGate.Off(null, AblationIds.ArmyRatio), Is.False);
			Assert.That(AblationGate.Off(null, null), Is.False);
			Assert.That(AblationGate.Off(null, ""), Is.False);
		}

		[Test]
		public void LatchWithoutAnOwnerStaysOnAndNeverThrows()
		{
			var latch = new AblationLatch();
			Assert.That(latch.Off(null, AblationIds.FairProducer), Is.False);
			Assert.That(latch.Off(null, AblationIds.FairProducer), Is.False);
		}

		[Test]
		public void NineG1RowIdsAreDistinctPiPrefixedRegistryKeys()
		{
			var ids = Ids();
			Assert.That(ids.Length, Is.EqualTo(9));
			Assert.That(ids.Distinct().Count(), Is.EqualTo(9));
			Assert.That(ids.All(i => i.StartsWith("pi_") && i == i.ToLowerInvariant() && !i.Contains(' ') && !i.Contains(',') && !i.Contains('@')), Is.True,
				"ids go into a comma list with an @team suffix, so they carry none of those characters");
			Assert.That(ids, Is.SupersetOf(new[]
			{
				"pi_fair_producer", "pi_army_ratio", "pi_overflow", "pi_lean", "pi_demand_capturers",
				"pi_role_mix", "pi_derived_weights", "pi_queue_depth", "pi_recovery",
			}));
		}
	}
}
