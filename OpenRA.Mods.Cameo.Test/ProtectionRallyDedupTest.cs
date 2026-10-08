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

using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class ProtectionRallyDedupTest
	{
		// AR-S2 (BO_squad_move_dedup, 2026-10-05): tagged-emitter attribution (attrib3)
		// pinned the last big churn emitter on PrepositionDefenceTick — an unconditional
		// AttackMove(rally) re-push to every protection member each ProtectInterval (~50t).
		// The dedup re-pushes only on a real rally move (past the cell band) or for joiners.

		const int Band = 4;

		static readonly string[] Members = { "a", "b", "c", "d" };

		[Test]
		public void FirstPushAlwaysEmitsAll()
		{
			var dedup = new ProtectionRallyDedup<string>();
			var set = dedup.EmitSet(Members, new CPos(10, 10), Band);
			Assert.That(set, Is.EqualTo(Members), "first rally must order every member");
		}

		[Test]
		public void SameRallySuppressesRepush()
		{
			var dedup = new ProtectionRallyDedup<string>();
			dedup.EmitSet(Members, new CPos(10, 10), Band);
			Assert.That(dedup.EmitSet(Members, new CPos(10, 10), Band), Is.Null,
				"the every-interval same-cell resend is the churn being removed");
		}

		[Test]
		public void JitterInsideBandSuppressesRepush()
		{
			var dedup = new ProtectionRallyDedup<string>();
			dedup.EmitSet(Members, new CPos(10, 10), Band);
			Assert.That(dedup.EmitSet(Members, new CPos(12, 11), Band), Is.Null,
				"a predicted-threat target drifting 2-3 cells per eval must not re-push");
		}

		[Test]
		public void RallyMovePastBandRepushAll()
		{
			var dedup = new ProtectionRallyDedup<string>();
			dedup.EmitSet(Members, new CPos(10, 10), Band);
			var set = dedup.EmitSet(Members, new CPos(20, 10), Band);
			Assert.That(set, Is.EqualTo(Members), "a real redirect re-orders everyone");
		}

		[Test]
		public void JitterNeverAccumulates()
		{
			// The band is measured from the last PUSHED rally, not the last eval — a ward
			// creeping 1 cell per eval escapes the band only once it is truly displaced.
			var dedup = new ProtectionRallyDedup<string>();
			dedup.EmitSet(Members, new CPos(10, 10), Band);
			Assert.That(dedup.EmitSet(Members, new CPos(11, 10), Band), Is.Null);
			Assert.That(dedup.EmitSet(Members, new CPos(12, 10), Band), Is.Null);
			Assert.That(dedup.EmitSet(Members, new CPos(13, 10), Band), Is.Null);
			Assert.That(dedup.EmitSet(Members, new CPos(14, 10), Band), Is.Not.Null,
				"cumulative creep past the band eventually re-pushes");
		}

		[Test]
		public void JoinerGetsItsFirstOrderOnly()
		{
			var dedup = new ProtectionRallyDedup<string>();
			dedup.EmitSet(new[] { "a", "b" }, new CPos(10, 10), Band);
			var set = dedup.EmitSet(new[] { "a", "b", "c" }, new CPos(10, 10), Band);
			Assert.That(set, Is.EqualTo(new[] { "c" }),
				"a drafted member needs its rally order without restarting the others");
		}

		[Test]
		public void DepartedMemberRejoinIsOrderedAgain()
		{
			var dedup = new ProtectionRallyDedup<string>();
			dedup.EmitSet(Members, new CPos(10, 10), Band);
			dedup.EmitSet(new[] { "a", "b" }, new CPos(10, 10), Band); // c,d released out
			var set = dedup.EmitSet(new[] { "a", "b", "c" }, new CPos(10, 10), Band);
			Assert.That(set, Is.EqualTo(new[] { "c" }), "a rejoiner counts as a joiner");
		}

		[Test]
		public void ResetForcesNextPush()
		{
			var dedup = new ProtectionRallyDedup<string>();
			dedup.EmitSet(Members, new CPos(10, 10), Band);
			dedup.Reset();
			var set = dedup.EmitSet(Members, new CPos(10, 10), Band);
			Assert.That(set, Is.EqualTo(Members),
				"ReleaseDefenders clears the lattice — the next protect episode starts fresh");
		}
	}
}
