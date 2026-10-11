#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING. In addition, please see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class BotOrderGroupTest
	{
		// The gate judges one member at a time: a member that is dead, foreign-owned, the player actor or
		// non-moving passes through unjudged, exactly like a single order on such a subject always has.
		static Actor NewActor(Player owner, ActorInfo info, bool dead = false)
		{
			var a = (Actor)RuntimeHelpers.GetUninitializedObject(typeof(Actor));
			Info(a) = info;
			Owner(a) = owner;
			Disposed(a) = dead;
			return a;
		}

		static Player NewPlayer(Actor playerActor = null)
		{
			var p = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
			PlayerActor(p) = playerActor;
			return p;
		}

		[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "Info")]
		static extern ref ActorInfo Info(Actor a);

		[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<Owner>k__BackingField")]
		static extern ref Player Owner(Actor a);

		[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<Disposed>k__BackingField")]
		static extern ref bool Disposed(Actor a);

		[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "PlayerActor")]
		static extern ref Actor PlayerActor(Player p);

		static readonly ActorInfo Tank = new("tank", new MobileInfo());
		static readonly ActorInfo Building = new("proc", new BuildableInfo(), new BuildingInfo());

		[Test]
		public void UnitInScopeMirrorsTheSingleOrderSubjectChecks()
		{
			var me = NewPlayer();
			var enemy = NewPlayer();
			var unit = NewActor(me, Tank);

			Assert.That(BotOrderGroup.UnitInScope(null, me), Is.False);
			Assert.That(BotOrderGroup.UnitInScope(NewActor(me, Tank, dead: true), me), Is.False, "dead");
			Assert.That(BotOrderGroup.UnitInScope(NewActor(enemy, Tank), me), Is.False, "foreign-owned");
			Assert.That(BotOrderGroup.UnitInScope(NewActor(me, Building), me), Is.False, "non-moving");
			Assert.That(BotOrderGroup.UnitInScope(unit, me), Is.True);
		}

		[Test]
		public void ThePlayerActorIsNeverAJudgeableUnit()
		{
			var me = NewPlayer();
			var actor = NewActor(me, Tank);
			PlayerActor(me) = actor;
			Assert.That(BotOrderGroup.UnitInScope(actor, me), Is.False);
		}

		[Test]
		public void FilterKeepsTheOriginalArrayWhenNothingDrops()
		{
			var group = new[] { 1, 2, 3 };
			var (members, filtered) = BotOrderGroup.Filter(group, _ => true);
			Assert.That(filtered, Is.False);
			Assert.That(members, Is.SameAs(group));
		}

		[Test]
		public void FilterStripsOnlyTheRefusedMembers()
		{
			var group = new[] { 1, 2, 3, 4 };
			var (members, filtered) = BotOrderGroup.Filter(group, k => k != 2 && k != 4);
			Assert.That(filtered, Is.True);
			Assert.That(members, Is.EqualTo(new[] { 1, 3 }));
		}

		[Test]
		public void FilterHandlesRefusalsAtTheEnds()
		{
			var (first, f1) = BotOrderGroup.Filter(new[] { 1, 2, 3 }, k => k != 1);
			Assert.That(f1, Is.True);
			Assert.That(first, Is.EqualTo(new[] { 2, 3 }));

			var (last, f2) = BotOrderGroup.Filter(new[] { 1, 2, 3 }, k => k != 3);
			Assert.That(f2, Is.True);
			Assert.That(last, Is.EqualTo(new[] { 1, 2 }));

			var (empty, f3) = BotOrderGroup.Filter(new int[0], _ => true);
			Assert.That(f3, Is.False);
			Assert.That(empty, Is.Empty);
		}

		[Test]
		public void FilterReturnsEmptyWhenEveryMemberDrops()
		{
			var (members, filtered) = BotOrderGroup.Filter(new[] { 1, 2 }, _ => false);
			Assert.That(filtered, Is.True);
			Assert.That(members, Is.Empty);
		}

		[Test]
		public void AMixedLeaseGroupLosesOnlyTheUsurpedMember()
		{
			// The end-to-end shape: three members, Decide verdicts differ per member.
			var emergency = new HashSet<string>();
			var group = new[] { 1, 2, 3 };
			var holders = new Dictionary<int, string> { { 1, null }, { 2, "OtherModule" }, { 3, "Me" } };

			var (members, filtered) = BotOrderGroup.Filter(group,
				k => BotOrderGate<int>.Decide("Me", holders[k], enforce: true, emergency: false, emergency) != BotOrderVerdict.Refuse);

			Assert.That(filtered, Is.True);
			Assert.That(members, Is.EqualTo(new[] { 1, 3 }),
				"the unleased member and the one leased to the issuer survive; the usurped member is stripped");
		}

		[Test]
		public void AFullyUsurpedGroupDies()
		{
			var emergency = new HashSet<string>();
			var (members, filtered) = BotOrderGroup.Filter(new[] { 1, 2 },
				_ => BotOrderGate<int>.Decide("Me", "OtherModule", true, false, emergency) != BotOrderVerdict.Refuse);
			Assert.That(filtered, Is.True);
			Assert.That(members, Is.Empty, "every member refused -> nothing to rebuild");
		}

		[Test]
		public void UnleasedGroupsPassUnjudged()
		{
			var group = new[] { 1, 2, 3 };
			var (members, filtered) = BotOrderGroup.Filter(group,
				_ => BotOrderGate<int>.Decide("Me", null, true, false, null) != BotOrderVerdict.Refuse);
			Assert.That(filtered, Is.False);
			Assert.That(members, Is.SameAs(group));
		}

		[Test]
		public void RebuildWithMembersPreservesEverySerializedField()
		{
			var group = new[] { NewActor(null, Tank), NewActor(null, Tank) };
			var survivors = new[] { group[0] };
			var extra = new[] { NewActor(null, Tank) };
			var order = new Order("AttackMove", null, Target.Invalid, queued: true,
				extraActors: extra, groupedActors: group)
			{
				TargetString = "ts",
				ExtraLocation = new CPos(7, 8),
				ExtraData = 42u,
				IsImmediate = true,
				SuppressVisualFeedback = true,
			};

			var rebuilt = BotOrderGroup.RebuildWithMembers(order, survivors);

			Assert.That(rebuilt, Is.Not.SameAs(order));
			Assert.That(rebuilt.OrderString, Is.EqualTo("AttackMove"));
			Assert.That(rebuilt.Type, Is.EqualTo(order.Type));
			Assert.That(rebuilt.Subject, Is.Null);
			Assert.That(rebuilt.Queued, Is.True);
			Assert.That(rebuilt.Target.Type, Is.EqualTo(order.Target.Type));
			Assert.That(rebuilt.TargetString, Is.EqualTo("ts"));
			Assert.That(rebuilt.ExtraActors, Is.SameAs(extra));
			Assert.That(rebuilt.ExtraLocation, Is.EqualTo(new CPos(7, 8)));
			Assert.That(rebuilt.ExtraData, Is.EqualTo(42u));
			Assert.That(rebuilt.IsImmediate, Is.True);
			Assert.That(rebuilt.SuppressVisualFeedback, Is.True);
			Assert.That(rebuilt.GroupedActors, Is.SameAs(survivors));
		}
	}
}
