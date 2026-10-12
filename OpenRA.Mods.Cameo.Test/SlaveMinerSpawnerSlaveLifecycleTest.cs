#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using System;
using System.IO;
using NUnit.Framework;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class SlaveMinerSpawnerSlaveLifecycleTest
	{
		static string SlaveSource => File.ReadAllText(Path.Combine(
			AppContext.BaseDirectory, "source", "SlaveMinerSpawnerSlave.cs"));

		static string MethodBody(string signature)
		{
			var source = SlaveSource;
			var method = source.IndexOf(signature, StringComparison.Ordinal);
			Assert.That(method, Is.GreaterThanOrEqualTo(0), $"Method not found: {signature}");

			var start = source.IndexOf('{', method);
			Assert.That(start, Is.GreaterThan(method));

			var depth = 0;
			for (var i = start; i < source.Length; i++)
			{
				if (source[i] == '{')
					depth++;
				else if (source[i] == '}' && --depth == 0)
					return source.Substring(start, i - start + 1);
			}

			Assert.Fail($"Unclosed method body: {signature}");
			return null;
		}

		[Test]
		public void MasterDockLookupDoesNotReserveBeforeDeliveryStarts()
		{
			var updateMasterDock = MethodBody("void UpdateMasterDock(Actor self)");

			// The engine's FindAndDeliverResources waits whenever ReservedHost is
			// non-null, assuming a MoveToDock activity already owns that reservation.
			Assert.That(updateMasterDock, Does.Not.Contain("ReserveHost("));
			Assert.That(updateMasterDock, Does.Contain("SetMasterDock(dock.HasValue ? dock.Value.Trait : null)"));
		}

		[Test]
		public void FreedSlaveRestartsHarvesterAfterMasterDisposal()
		{
			var onMasterKilled = MethodBody("public override void OnMasterKilled(Actor self, Actor attacker, SpawnerSlaveDisposal disposal)");

			Assert.That(onMasterKilled, Does.Contain("self.CancelActivity()"));
			Assert.That(onMasterKilled, Does.Contain("self.QueueActivity(new FindAndDeliverResources(self))"));
			Assert.That(onMasterKilled, Does.Contain("ClearMasterBinding()"));
		}

		[Test]
		public void CapturedLiveMasterKeepsItsSlaveBinding()
		{
			var onOwnerChanged = MethodBody("public override void OnOwnerChanged(Actor self, OpenRA.Player oldOwner, OpenRA.Player newOwner)");
			var liveMasterCheck = onOwnerChanged.IndexOf("if (!Master.IsDead)", StringComparison.Ordinal);
			var ownerMismatchCheck = onOwnerChanged.IndexOf("if (Master.Owner != newOwner)", StringComparison.Ordinal);

			Assert.That(liveMasterCheck, Is.GreaterThanOrEqualTo(0));
			Assert.That(ownerMismatchCheck, Is.GreaterThan(liveMasterCheck));

			var afterMismatchCheck = onOwnerChanged.Substring(ownerMismatchCheck);
			var mismatchBranch = MethodBodyFromFragment(afterMismatchCheck);
			Assert.That(mismatchBranch, Does.Contain("ClearMasterBinding()"));
			Assert.That(mismatchBranch, Does.Contain("self.CancelActivity()"));
			Assert.That(mismatchBranch, Does.Contain("self.QueueActivity(new FindAndDeliverResources(self))"));
		}

		static string MethodBodyFromFragment(string source)
		{
			var start = source.IndexOf('{');
			Assert.That(start, Is.GreaterThanOrEqualTo(0));

			var depth = 0;
			for (var i = start; i < source.Length; i++)
			{
				if (source[i] == '{')
					depth++;
				else if (source[i] == '}' && --depth == 0)
					return source.Substring(start, i - start + 1);
			}

			Assert.Fail("Unclosed branch body");
			return null;
		}
	}
}
