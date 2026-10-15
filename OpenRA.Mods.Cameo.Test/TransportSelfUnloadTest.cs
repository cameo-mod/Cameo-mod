#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class TransportSelfUnloadTest
	{
		[TestCase("ExternalBotOrdersManager@0", "Unload", true, true)]
		[TestCase("ExternalBotOrdersManager", "Unload", true, true)]
		[TestCase("ExternalBotOrdersManager@0", "Move", true, false)]
		[TestCase("ExternalBotOrdersManager@0", "AttackMove", true, false)]
		[TestCase("ExternalBotOrdersManager@0", "Unload", false, false)]
		[TestCase("ScoutBotModule@0", "Unload", true, false)]
		[TestCase(null, "Unload", true, false)]
		public void OnlyAutomaticCargoUnloadBypassesTheLease(string issuer, string order, bool cargo, bool expected)
		{
			Assert.That(ModularBot.IsTransportSelfUnload(issuer, order, cargo), Is.EqualTo(expected));
		}
	}
}
