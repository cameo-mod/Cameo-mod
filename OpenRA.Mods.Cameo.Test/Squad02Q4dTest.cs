#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// CONSOLIDATE-02 Q4d (Code Monkey, Q4b/Q4c review): in ArbiterProtectOwn the pool inside the scan radius is already counted in `defence` by ArbiterAssess, so the nearest-first
	/// pick that covers `need` must not count it a second time. The pool inside the radius joins the answer (it is at the scene) but covers none of the need.
	/// </summary>
	[TestFixture]
	public class Squad02Q4dTest
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		static string Between(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text[start..end];
		}

		// Attack 1000, the defence at the scene 800 (two in-radius pool units of 300 are part of it): the open need is 200 x 100% = 200. The old pick took the nearest pool unit first,
		// an in-radius one of 300 that was already in the 800, and stopped: nothing new came. The new pick draws from the outside pool only and covers the need with a unit that is new.
		[Test]
		public void AnInRadiusPoolUnitCoversNoNeedOfItsOwn()
		{
			var need = DefendArbiterCA.Need(1000, 800, 100);
			Assert.That(need, Is.EqualTo(200));

			// Candidate 0 and 1 stand inside the scan radius (near), 2 and 3 outside it (far).
			var all = new[]
			{
				new DefendCandidate(0, 300, 100), new DefendCandidate(1, 300, 200), new DefendCandidate(2, 250, 90000), new DefendCandidate(3, 250, 160000),
			};

			var oldPick = DefendArbiterCA.PickNearest(all, need);
			Assert.That(oldPick, Is.EqualTo(new[] { 0 }), "old: the in-radius unit (already counted in the defence) ended the pick, no new value arrived");

			var outside = all.Where(c => c.DistSq > 10000).Select((c, i) => new DefendCandidate(i, c.Value, c.DistSq)).ToList();
			var newPick = DefendArbiterCA.PickNearest(outside, need);
			Assert.That(newPick, Is.EqualTo(new[] { 0 }), "new: the nearest OUTSIDE unit (250 >= 200) covers the need");
			Assert.That(newPick.Sum(i => outside[i].Value), Is.GreaterThanOrEqualTo(need));
		}

		[Test]
		public void TheCoveringPickDrawsFromTheOutsidePoolAndTheInRadiusPoolOnlyJoins()
		{
			var own = Between(RepoFile(Bots + "SquadManagerBotModuleCA.Pipeline02.cs"), "void ArbiterProtectOwn(", "// ---- Q4: the one launcher");

			Assert.That(own, Does.Contain("var outside = new List<UnitWposWrapper>();"));
			Assert.That(own, Does.Contain("if ((u.Actor.CenterPosition - at).HorizontalLengthSquared > scanSquared)\n\t\t\t\t\t\toutside.Add(u);\n\t\t\t\t\telse if (need > 0)\n\t\t\t\t\t{\n\t\t\t\t\t\tprotectSq.Units.Add(u);\n\t\t\t\t\t\tunitsHangingAroundTheBase.Remove(u);\n\t\t\t\t\t\tLedgerClaim(ArmyDuty.OwnBaseDefend, nameof(ArbiterProtectOwn), \"own_pool_in_radius\", new[] { u });\n\t\t\t\t\t\tfromPool++;\n\t\t\t\t\t}"),
				"the in-radius pool joins the answer (need > 0) but its value never reaches `got`");
			Assert.That(own, Does.Contain("outside.Select((u, i) => new DefendCandidate(i, UnitValue(u.Actor), (u.Actor.CenterPosition - at).HorizontalLengthSquared)), need);"));
			Assert.That(own, Does.Contain("foreach (var i in pick)\n\t\t\t\t\tTakeFromPool(outside[i]);"));
			Assert.That(own, Does.Not.Contain("draftable.Select((u, i) => new DefendCandidate("), "the pool pick no longer runs over the whole draftable list");

			// The in-radius test is the one poolValue uses, and `scanSquared` is declared before both.
			Assert.That(own.IndexOf("var scanSquared", StringComparison.Ordinal), Is.LessThan(own.IndexOf("var outside", StringComparison.Ordinal)));
			Assert.That(own, Does.Contain("if ((u.Actor.CenterPosition - at).HorizontalLengthSquared > scanSquared)\n\t\t\t\t\tpoolValue += UnitValue(u.Actor);"));
		}
	}
}
