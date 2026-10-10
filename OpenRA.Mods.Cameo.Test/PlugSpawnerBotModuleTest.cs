#region Copyright & License Information
/*
 * Copyright (c) The Cameo Developers (see AUTHORS)
 * This file is part of Cameo, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class PlugSpawnerBotModuleTest
	{
		// Playtest repair (B2/B6): the module now routes demand through ordinary
		// StartProduction — the instant PlacePlugAI install path (and its per-target
		// ownership seam) is retired, so these tests pin what remains: the single-pass
		// owned-actor scan. Uninitialized Players/Actors stand in where only identity and
		// the Owner/IsInWorld/Disposed fields matter (the same fixture trick
		// TeamBlackboardTest and ScoutBotModuleTest use).

		[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<Owner>k__BackingField")]
		static extern ref OpenRA.Player OwnerField(OpenRA.Actor actor);

		[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<IsInWorld>k__BackingField")]
		static extern ref bool IsInWorldField(OpenRA.Actor actor);

		[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<Disposed>k__BackingField")]
		static extern ref bool DisposedField(OpenRA.Actor actor);

		static OpenRA.Player FakePlayer() =>
			(OpenRA.Player)RuntimeHelpers.GetUninitializedObject(typeof(OpenRA.Player));

		static OpenRA.Actor OwnedActor(OpenRA.Player owner, bool inWorld = true, bool dead = false)
		{
			var actor = (OpenRA.Actor)RuntimeHelpers.GetUninitializedObject(typeof(OpenRA.Actor));
			OwnerField(actor) = owner;
			IsInWorldField(actor) = inWorld;
			DisposedField(actor) = dead;
			return actor;
		}

		[Test]
		public void OwnedScanCollectsOnlyLiveOwnActors()
		{
			var owner = FakePlayer();
			var other = FakePlayer();
			var mine = OwnedActor(owner);
			var theirs = OwnedActor(other);
			var dead = OwnedActor(owner, dead: true);
			var outOfWorld = OwnedActor(owner, inWorld: false);

			var owned = PlugSpawnerBotModuleCA.CollectOwnedActors(
				new[] { mine, theirs, dead, outOfWorld }, owner);

			Assert.That(owned, Is.EqualTo(new[] { mine }),
				"the cache keeps exactly the ordering player's live in-world actors");
		}

		[Test]
		public void OwnedScanIsOnePassForAllPlugKinds()
		{
			// The pre-AR-4 loop enumerated world.Actors once per plug type; the cache
			// enumerates it exactly once regardless of how many plug kinds exist.
			var owner = FakePlayer();
			var scanned = 0;
			IEnumerable<OpenRA.Actor> Counting()
			{
				scanned++;
				yield return OwnedActor(owner);
			}

			var owned = PlugSpawnerBotModuleCA.CollectOwnedActors(Counting(), owner);

			Assert.That(scanned, Is.EqualTo(1));
			Assert.That(owned, Has.Count.EqualTo(1));
		}
	}
}
