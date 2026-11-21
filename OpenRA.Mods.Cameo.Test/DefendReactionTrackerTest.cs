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
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class DefendReactionTrackerTest
	{
		[Test]
		public void FirstSquadOrderAfterTheTriggerClosesItAndCoalescedHitsAreCounted()
		{
			var lines = new List<string>();
			var t = new DefendReactionTracker("Multi0", lines.Add);
			Assert.That(t.Trigger(1000, DefendReactionTracker.BuildingAttacked, 42, 10, 12), Is.EqualTo("42@1000"));
			Assert.That(t.Trigger(1003, DefendReactionTracker.BuildingAttacked, 43, 10, 12), Is.Null, "one open trigger per kind");
			t.Reaction(1020, "UnitBuilderBotModuleCA");
			Assert.That(lines, Has.Count.EqualTo(1));
			Assert.That(lines[0], Does.Contain("trigger_tick=1000").And.Contain("hits=2").And.Contain("reaction_tick=1020")
				.And.Contain("reaction_kind=production_switch").And.Contain("reaction_issuer=UnitBuilderBotModuleCA"));
			Assert.That(t.OpenCount, Is.EqualTo(0));
		}

		[Test]
		public void ANonDefendIssuerIsNotAReactionAndTheTimeoutClosesAsNone()
		{
			var lines = new List<string>();
			var t = new DefendReactionTracker("Multi1", lines.Add);
			t.Trigger(500, DefendReactionTracker.UnitAttacked, 7, 1, 1);
			t.Reaction(510, "ScoutBotModule");
			Assert.That(lines, Is.Empty);
			t.Expire(799);
			Assert.That(lines, Is.Empty);
			t.Expire(800);
			Assert.That(lines, Has.Count.EqualTo(1));
			Assert.That(lines[0], Does.Contain("reaction_tick=-1").And.Contain("reaction_kind=none").And.Contain("timeout=300"));
		}

		[Test]
		public void GameEndFlushClosesOpenTriggersAsCensoredNotNone()
		{
			var lines = new List<string>();
			var t = new DefendReactionTracker("Multi0", lines.Add);
			t.Trigger(100, DefendReactionTracker.HarvesterAttacked, 1, 0, 0);
			t.Trigger(500, DefendReactionTracker.UnitAttacked, 2, 0, 0);
			t.Flush(520); // the 100 trigger is past the 300 timeout (none); the 500 trigger is still open at game end (censored)
			Assert.That(lines, Has.Count.EqualTo(2));
			Assert.That(lines[0], Does.Contain("trigger_tick=100").And.Contain("reaction_kind=none").And.Not.Contain("censored_at"));
			Assert.That(lines[1], Does.Contain("trigger_tick=500").And.Contain("reaction_kind=censored").And.Contain("reaction_tick=-1").And.Contain("censored_at=520"));
			Assert.That(t.OpenCount, Is.EqualTo(0));

			t.Flush(530);
			Assert.That(lines, Has.Count.EqualTo(2), "a second flush writes nothing");
		}
	}
}
