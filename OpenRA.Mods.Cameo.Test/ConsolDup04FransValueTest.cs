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

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// CONSOL-DUP-04 item 3: the positive actor cost floor max(1, ValuedInfo.Cost ?? 1) has one owner (FransCommanderCoreBotModule.PositiveActorValue) for every non-null actor call site.
	/// The nullable wrappers stay where they are: the core service returns 0 for a null actor, the General's GetTargetValue returns 1. No world fixture drives an actor here, so the wiring is source-pinned.
	/// </summary>
	[TestFixture]
	public class ConsolDup04FransValueTest
	{
		const string Frans = "OpenRA.Mods.Fransbot/Traits/";
		const string Floor = "Math.Max(1, actor.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 1)";

		static string Source(string file)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null);
			return File.ReadAllText(Path.Combine(dir.FullName, Frans + file)).Replace("\r\n", "\n");
		}

		static int Count(string text, string term) => text.Split(new[] { term }, StringSplitOptions.None).Length - 1;

		[Test]
		public void TheCostFloorIsWrittenOnceAndTheFourCallSitesShareIt()
		{
			var core = Source("FransCommanderCoreBotModule.cs");
			var general = Source("FransGeneralBotModule.cs");
			var ground = Source("FransGroundCommanderBotModule.cs");
			var intel = Source("FransCombatIntelBotModule.cs");

			Assert.That(core, Does.Contain("internal static int PositiveActorValue(Actor actor) => " + Floor + ";"));
			Assert.That(Count(core, "ValuedInfo>()?.Cost ?? 1"), Is.EqualTo(1));
			Assert.That(Count(general, "ValuedInfo>()?.Cost ?? 1"), Is.EqualTo(0));
			Assert.That(Count(ground, "ValuedInfo>()?.Cost ?? 1"), Is.EqualTo(0));
			Assert.That(Count(intel, "ValuedInfo>()?.Cost ?? 1"), Is.EqualTo(0));

			Assert.That(ground, Does.Contain("static int GetCombatValue(Actor actor) => FransCommanderCoreBotModule.PositiveActorValue(actor);"));
			Assert.That(intel, Does.Contain("var value = FransCommanderCoreBotModule.PositiveActorValue(actor);\n\t\t\tif (!actor.Info.HasTraitInfo<BuildingInfo>())\n\t\t\t\treturn value;"));
			Assert.That(intel, Does.Contain("var percent = IsDefensiveBuilding(actor) ? Info.StaticGroundDefenseValuePercent : Info.PassiveBuildingValuePercent;\n\t\t\treturn Math.Max(1, value * percent / 100);"), "the building percent stays");
		}

		[Test]
		public void TheNullableWrappersKeepTheirOwnNullPolicy()
		{
			var core = Source("FransCommanderCoreBotModule.cs");
			Assert.That(core, Does.Contain("public int GetActorValue(Actor actor)\n\t\t{\n\t\t\treturn actor == null ? 0 : PositiveActorValue(actor);\n\t\t}"), "the service: null is 0");

			var general = Source("FransGeneralBotModule.cs");
			Assert.That(general, Does.Contain("static int GetTargetValue(Actor actor) => actor == null ? 1 : FransCommanderCoreBotModule.PositiveActorValue(actor);"), "the General: null is 1");

			// The Air and Sea commanders already delegate to the service and stay as they are.
			Assert.That(Source("FransAirCommanderBotModule.cs"), Does.Contain("int GetCombatValue(Actor actor) => commanderCoreService.GetActorValue(actor);"));
			Assert.That(Source("FransSeaCommanderBotModule.cs"), Does.Contain("int GetCombatValue(Actor actor) => commanderCoreService.GetActorValue(actor);"));
		}
	}
}
