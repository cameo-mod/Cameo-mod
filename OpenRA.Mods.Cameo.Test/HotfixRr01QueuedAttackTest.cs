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

using System.Runtime.CompilerServices;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// HOTFIX-RR-01 B: a queued attack notice (round-robin on) can outlive the damaged actor or its attacker. The RR-on smoke
	// crashed in HarvesterBotModuleCA.RespondToAttack reading a trait of a destroyed ore truck.
	[TestFixture]
	public class HotfixRr01QueuedAttackTest
	{
		[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<Disposed>k__BackingField")]
		static extern ref bool Disposed(Actor a);

		[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<IsInWorld>k__BackingField")]
		static extern ref bool IsInWorld(Actor a);

		static Actor NewActor(bool disposed, bool inWorld)
		{
			var a = (Actor)RuntimeHelpers.GetUninitializedObject(typeof(Actor));
			Disposed(a) = disposed;
			IsInWorld(a) = inWorld;
			return a;
		}

		static AttackInfo NewInfo(Actor attacker) => new()
		{
			Attacker = attacker,
			Damage = new Damage(37),
			DamageState = DamageState.Medium,
			PreviousDamageState = DamageState.Light,
			ProjectileType = "bullet",
		};

		[Test]
		public void HotfixRr01AQueuedAttackOnAGoneActorIsDropped()
		{
			var e = NewInfo(NewActor(false, true));
			Assert.That(ModularBot.QueuedAttackInfo(null, e), Is.Null, "no damaged actor");
			Assert.That(ModularBot.QueuedAttackInfo(NewActor(false, true), null), Is.Null, "no attack info");
			Assert.That(ModularBot.QueuedAttackInfo(NewActor(true, false), e), Is.Null, "damaged actor destroyed");
			Assert.That(ModularBot.QueuedAttackInfo(NewActor(false, false), e), Is.Null, "damaged actor left the world");
		}

		[Test]
		public void HotfixRr01ALiveAttackIsDeliveredUnchanged()
		{
			var self = NewActor(false, true);
			var live = NewInfo(NewActor(false, true));
			Assert.That(ModularBot.QueuedAttackInfo(self, live), Is.SameAs(live));

			var noAttacker = NewInfo(null);
			Assert.That(ModularBot.QueuedAttackInfo(self, noAttacker), Is.SameAs(noAttacker));
		}

		[Test]
		public void HotfixRr01ADestroyedAttackerIsDeliveredAsNullOnACopy()
		{
			var attacker = NewActor(true, false);
			var e = NewInfo(attacker);
			var got = ModularBot.QueuedAttackInfo(NewActor(false, true), e);

			Assert.That(got, Is.Not.Null.And.Not.SameAs(e), "a copy, not the shared original");
			Assert.That(got.Attacker, Is.Null);
			Assert.That(got.Damage, Is.SameAs(e.Damage));
			Assert.That(got.DamageState, Is.EqualTo(e.DamageState));
			Assert.That(got.PreviousDamageState, Is.EqualTo(e.PreviousDamageState));
			Assert.That(got.ProjectileType, Is.EqualTo(e.ProjectileType));
			Assert.That(e.Attacker, Is.SameAs(attacker), "the original is untouched");
		}
	}
}
