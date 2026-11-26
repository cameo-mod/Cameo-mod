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
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	// fix-03 (Sol V08F_CUMREV_SOL_02): item 1 (the pincer-at-push debug line is DEV-guarded) and item 2 (row pincer_arrived_arms: the arm plan reads the arrived units and value).
	[TestFixture]
	public sealed class PincerArrivedArmsTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void TheDebugLineIsDevGuarded()
		{
			var push = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.PincerPush.cs");
			Assert.That(push, Does.Contain("#if CAMEO_DEV_LOGGING\n\t\t\tAIUtils.BotDebug(\"AI ({0}): pincer at push seq {1}"));
		}

		[Test]
		public void TheSwitchIsDefaultOffAndTheRowIsRegistered()
		{
			Assert.That(AblationIds.PincerArrivedArms, Is.EqualTo("pincer_arrived_arms"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain(AblationIds.PincerArrivedArms));

			var info = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCAInfo.TeamLead.cs");
			Assert.That(info, Does.Contain("public readonly bool PincerArrivedArms = false;"));

			var yaml = Source("packaging/ai.yaml");
			// V08H: the six launcher blocks ship it on, as measured in V08G (Opus ruling); the trait default above stays false.
			Assert.That(System.Text.RegularExpressions.Regex.Matches(yaml, "\t\tPincerArrivedArms: true\n").Count, Is.EqualTo(6), "the six launcher blocks");
			Assert.That(yaml, Does.Not.Contain("PincerArrivedArms: false"));
		}

		// Sol V08F_CUMREV_SOL_02 fixture: a squad with ZERO arrival and one with PARTIAL arrival. Off (Reported) the arm plan shares out units that never arrived; on (ArrivedReported)
		// the plan is built from the arrived blob only, and its arm totals equal ArrivedTotals (the number the pincer gate measured).
		[Test]
		public void ZeroAndPartialArrivalAreSharedOutByWhatArrived()
		{
			var order = new TeamLeadOrder(1, "leader", new WPos(0, 0, 0), new WPos(40000, 0, 0), 5, 0);
			var full = new object();
			var none = new object();
			var part = new object();
			order.Report(full, 1000, true, 10);
			order.Report(none, 1000, false, 10);
			order.Report(part, 800, false, 8, 4);

			// ArrivedReported: the full squad (10 / 1000) and half of the partial one (4 / 400); the squad that arrived with nobody is skipped.
			var arrived = order.ArrivedReported();
			Assert.That(arrived.Count, Is.EqualTo(2));
			Assert.That(arrived.Exists(r => ReferenceEquals(r.Squad, none)), Is.False, "zero arrival is no arm squad");
			Assert.That(arrived.Find(r => ReferenceEquals(r.Squad, full)), Is.EqualTo((full, 10, 1000L)));
			Assert.That(arrived.Find(r => ReferenceEquals(r.Squad, part)), Is.EqualTo((part, 4, 400L)));
			var (arrivedUnits, arrivedValue) = order.ArrivedTotals();
			Assert.That((arrivedUnits, arrivedValue), Is.EqualTo((14, 1400L)), "the same count the pincer gate measured");

			var keys = new System.Collections.Generic.Dictionary<object, uint> { [full] = 1, [none] = 2, [part] = 3 };
			ArmPlan Plan(System.Collections.Generic.List<(object Squad, int Units, long Value)> reported)
			{
				var squads = new System.Collections.Generic.List<ArmSquad>();
				foreach (var (squad, units, value) in reported)
					squads.Add(new ArmSquad(keys[squad], units, value));

				return PincerArms.Balance(squads, 5, 500);
			}

			// Off: the committed totals. All three squads are arm squads, 28 units / 2800 value shared out, 14 of them never arrived.
			var off = Plan(order.Reported());
			Assert.That(off, Is.Not.Null);
			Assert.That(off.Places.Count, Is.EqualTo(3));
			Assert.That(off.Units0 + off.Units1, Is.EqualTo(28));
			Assert.That(off.Split, Is.Null, "the committed blob needs no division");

			// On: the arrived blob. The partial arrival leaves arm 1 under the bar, so the full squad is divided; both arms hold the bar and the arms sum to what arrived.
			var on = Plan(arrived);
			Assert.That(on, Is.Not.Null);
			Assert.That(on.Places.Count, Is.EqualTo(2));
			Assert.That(System.Linq.Enumerable.Any(on.Places, p => p.Key == 2), Is.False, "the zero-arrival squad is not placed");
			Assert.That(on.Units0 + on.Units1, Is.EqualTo(arrivedUnits));
			Assert.That(on.Value0 + on.Value1, Is.EqualTo(arrivedValue));
			Assert.That(PincerArms.Holds(on.Units0, on.Value0, 5, 500), Is.True);
			Assert.That(PincerArms.Holds(on.Units1, on.Value1, 5, 500), Is.True);
			Assert.That(on.Split, Is.Not.Null, "the split counts: the full squad gives units to the light arm");
			Assert.That(on.Split.Value.Key, Is.EqualTo(1u));
		}

		[Test]
		public void OffIsTheCommittedTotalsAndOnIsTheArrivedArms()
		{
			var push = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.PincerPush.cs");
			Assert.That(push, Does.Contain("internal bool PincerArrivedArmsOn => Info.PincerArrivedArms && !ablatePincerArrivedArms.Off(Player, AblationIds.PincerArrivedArms);"));
			Assert.That(push, Does.Contain("foreach (var (squad, squadUnits, squadValue) in PincerArrivedArmsOn ? order.ArrivedReported() : order.Reported())"));

			var logic = Source("mods/CA/Traits/BotModules/BotModuleLogic/TeamLeadCA.cs");
			Assert.That(logic, Does.Contain("public List<(object Squad, int Units, long Value)> ArrivedReported()"));
			Assert.That(logic, Does.Contain("if (n > 0)"), "a squad with none arrived is not an arm");
		}
	}
}
