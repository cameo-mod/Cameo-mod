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

namespace OpenRA.Mods.Cameo.Test
{
	// CONSOLIDATE-03 P6 (a): the one registry of pending starts. The owners keep their lifetimes: a pass batch counts emissions per queue, the upgrade ledger keeps the first issue tick.
	[TestFixture]
	public sealed class PendingStartsTest
	{
		const string Dir = "mods/CA/Traits/BotModules/BotModuleLogic/";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void AStartKeepsItsFirstTickAndItsQueueBinding()
		{
			var r = new PendingStarts<string>();
			Assert.That(r.TryStart("ub_x", 100), Is.True);
			Assert.That(r.TryStart("ub_x", 250), Is.False, "a second start of the same key does not move the tick");
			r.SetQueue("ub_x", "Building");
			Assert.That(r.TryGet("ub_x", out var e), Is.True);
			Assert.That(e.Tick, Is.EqualTo(100));
			Assert.That(e.Queue, Is.EqualTo("Building"));
			Assert.That(r.Count, Is.EqualTo(1));
		}

		[Test]
		public void AKeyLeavesOnlyByRemove()
		{
			var r = new PendingStarts<string>();
			r.TryStart("a", 1);
			r.TryStart("b", 2);
			Assert.That(r.Keys().Count, Is.EqualTo(2));
			r.Remove("a");
			Assert.That(r.Contains("a"), Is.False);
			Assert.That(r.Contains("b"), Is.True);
		}

		[Test]
		public void TheKeysAreASnapshotSafeToRemoveFromWhileIterating()
		{
			var r = new PendingStarts<string>();
			r.TryStart("a", 1);
			r.TryStart("b", 1);
			foreach (var k in r.Keys())
				r.Remove(k);

			Assert.That(r.Count, Is.EqualTo(0));
		}

		[Test]
		public void AnEmissionCountIsPerKeyAndStartsAtZero()
		{
			var queue1 = new object();
			var queue2 = new object();
			var r = new PendingStarts<object>();
			Assert.That(r.CountOf(queue1), Is.EqualTo(0));
			Assert.That(r.Increment(queue1), Is.EqualTo(1));
			Assert.That(r.Increment(queue1), Is.EqualTo(2));
			Assert.That(r.CountOf(queue1), Is.EqualTo(2));
			Assert.That(r.CountOf(queue2), Is.EqualTo(0), "same-type siblings reserve independently");
		}

		[Test]
		public void TheOwnersUseTheRegistryAndKeepTheirLifetimes()
		{
			var upgrade = Source(Dir + "UpgradeCompletions.cs");
			Assert.That(upgrade, Does.Contain("readonly PendingStarts<string> pending = new();"));
			Assert.That(upgrade, Does.Contain("public void Started(string name, int tick) => pending.TryStart(name, tick);"));
			Assert.That(upgrade, Does.Contain("pending.SetQueue(name, queueKey);"));
			Assert.That(upgrade, Does.Contain("pending.TryGet(name, out var entry);"));
			Assert.That(upgrade, Does.Contain("if (tick - started > pendingTimeoutTicks)"), "the 300-tick timeout is unchanged");

			var batch = Source(Dir + "UnitProducerServiceCA.cs");
			Assert.That(batch, Does.Contain("readonly PendingStarts<object> pending = new();"));
			Assert.That(batch, Does.Contain("pending.Increment(queue);"));
			Assert.That(batch, Does.Contain("public int PendingOf(object queue) => pending.CountOf(queue);"));
			Assert.That(batch, Does.Not.Contain("Dictionary<object, int> pending"), "the batch no longer keeps its own dictionary");
		}

		[Test]
		public void TheDevHashIsCompiledOutAndFoldsEveryStartAndRefusal()
		{
			var bb = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.PendingStream.cs");
			Assert.That(bb, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]"));
			Assert.That(bb, Does.Contain("void PendingStreamNote(string kind, string name, int tick, int amount)"));
			Assert.That(bb, Does.Contain("pst_hash,{pendingStream:x16},,{pendingStreamCount}"));

			var ub = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(ub, Does.Contain("PendingStreamNote(\"upg_start\", item2.Name, world.WorldTick, 0);"));
			Assert.That(ub, Does.Contain("PendingStreamNote(\"unit_emit\", name, world.WorldTick, fullCost);"));
			Assert.That(ub, Does.Contain("PendingStreamNote(\"refuse_capturer\", unit.Name, world.WorldTick, 0);"));
			Assert.That(ub, Does.Contain("PendingStreamNote(\"refuse_aircraft\", unit.Name, world.WorldTick, 0);"));
		}

		[Test]
		public void TheLoadedIntentsStayOutsideTheRegistry()
		{
			// BuildIntentLedger keeps its own state machine (Waiting, EmittedPending, Unresolved) and its save key; P6 (a) does not route it.
			var ub = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(ub, Does.Contain("buildIntents.LoadPending(FieldLoader.GetValue<string[]>(\"PendingBuildIntents\""));
		}
	}
}
