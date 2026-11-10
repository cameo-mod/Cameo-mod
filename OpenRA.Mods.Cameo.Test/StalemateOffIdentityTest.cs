#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// BOTPASSIVE IMPL01 OFF identity, pure-function harness (design _07 L5, _09 LE). The manager is an uninitialized shell with
	/// NO World, Player, bot or RNG: every breaker and telemetry entry point must return before touching any of them while its
	/// flag is off, and the breaker path must not depend on the telemetry flag. A world-fixture order-stream / RNG-count proof
	/// is not part of this harness (the test project has no headless world fixture).
	/// </summary>
	[TestFixture]
	public sealed class StalemateOffIdentityTest
	{
		const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		static SquadManagerBotModuleCA Shell(bool breaker, bool diagnostics)
		{
			var info = new SquadManagerBotModuleCAInfo();
			typeof(SquadManagerBotModuleCAInfo).GetField(nameof(SquadManagerBotModuleCAInfo.StalemateBreakerEnabled)).SetValue(info, breaker);
			typeof(SquadManagerBotModuleCAInfo).GetField(nameof(SquadManagerBotModuleCAInfo.StalemateDiagnosticsEnabled)).SetValue(info, diagnostics);
			var m = (SquadManagerBotModuleCA)RuntimeHelpers.GetUninitializedObject(typeof(SquadManagerBotModuleCA));
			var field = typeof(SquadManagerBotModuleCA).BaseType.GetField("Info", Any);
			field.SetValue(m, info);
			return m;
		}

		static object Call(object target, string name, params object[] args) =>
			typeof(SquadManagerBotModuleCA).GetMethod(name, Any).Invoke(target, args);

		static object Field(object target, string name) => typeof(SquadManagerBotModuleCA).GetField(name, Any).GetValue(target);

		[Test]
		public void OffTouchesNoWorldAllocatesNothingAndAddsNoSaveNode()
		{
			var m = Shell(false, false);
			Call(m, "StalemateTick", new object[] { null });
			Call(m, "DiagnosticTick");
			Call(m, "DiagnosticComplete");
			Call(m, "DiagnosticNoteLaunch", new object[] { null });
			Call(m, "DiagnosticSquadEvent", null, "disband", null);
			Assert.That(m.StalemateDefersRetreat(null, "attack_should_flee"), Is.False);
			m.NoteRetreatStart(null, "idle_engage");
			Assert.That(Call(m, "StalemateSkipsUpdate", new object[] { null }), Is.False);

			var data = new List<MiniYamlNode>();
			Call(m, "StalemateSave", data);
			Assert.That(data, Is.Empty, "OFF adds no save node");
			Call(m, "StalemateLoad", new Dictionary<string, MiniYaml> { ["StalemateBreaker"] = new MiniYaml("") });

			Assert.That(Field(m, "stalemateSeat"), Is.Null, "no breaker state is allocated while OFF");
			Assert.That(Field(m, "stalemateSeatOwner"), Is.Null);
			Assert.That(Field(m, "diagnosticWriter"), Is.Null, "no telemetry file while OFF");
			Assert.That(Field(m, "diagnosticPendingCommits"), Is.Null);
		}

		[Test]
		public void TelemetryOnDoesNotWakeTheBreakerPath()
		{
			// _09 LE: the two flags are independent. With the telemetry flag on and the switch off, the breaker entry points still
			// return before reading the world, so the order stream, RNG draws and save bytes cannot depend on the telemetry flag.
			var m = Shell(false, true);
			Call(m, "StalemateTick", new object[] { null });
			Assert.That(m.StalemateDefersRetreat(null, "attack_should_flee"), Is.False);
			Assert.That(Call(m, "StalemateSkipsUpdate", new object[] { null }), Is.False);
			var data = new List<MiniYamlNode>();
			Call(m, "StalemateSave", data);
			Assert.That(data, Is.Empty);
			Assert.That(Field(m, "stalemateSeat"), Is.Null);
		}

		[Test]
		public void TheBreakerNeverReadsTheTelemetryWriter()
		{
			// Decisions read only StalemateBreakerSeatCA, the wave and the urgency provider. The seat state holds no writer and its save
			// bytes therefore cannot change with the telemetry flag.
			foreach (var f in typeof(StalemateBreakerSeatCA).GetFields(Any))
				Assert.That(f.FieldType.Name, Does.Not.Contain("Diagnostic"), f.Name);
			foreach (var f in typeof(StalemateBreakerWaveCA).GetFields(Any))
				Assert.That(f.FieldType.Name, Does.Not.Contain("Diagnostic"), f.Name);
		}
	}
}
