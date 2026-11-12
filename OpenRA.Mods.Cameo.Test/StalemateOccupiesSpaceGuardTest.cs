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
using System.Runtime.CompilerServices;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// IMPL01-HOTFIX01 regression (spectate crash 2026-11-08T07:20:17Z, Ice Cold, arm_on). With
	// StalemateUrgencyEnabled the switch-ON branch of CheckEmergency read a.Location over
	// ActorsHavingTrait<Production>(); the Player actor carries Production (promotions.yaml) but no
	// IOccupySpace, so Actor.Location => OccupiesSpace.TopLeft threw NullReferenceException 9 s in.
	// Same contract as AiPlayerTelemetryOccupiesSpaceGuardTest: the test project has no world fixture,
	// so these pin the guard (behaviourally on a positionless actor, and in the source text).
	[TestFixture]
	public class StalemateOccupiesSpaceGuardTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file));

		static string Section(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start + from.Length, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text.Substring(start, end - start);
		}

		static void GuardPrecedes(string body, string guard, string read, string what)
		{
			var g = body.IndexOf(guard, StringComparison.Ordinal);
			Assert.That(g, Is.GreaterThanOrEqualTo(0), what + ": missing guard " + guard);
			var r = body.IndexOf(read, StringComparison.Ordinal);
			Assert.That(r, Is.GreaterThan(g), what + ": the guard must precede the first " + read);
		}

		[Test]
		public void HasSpaceRejectsPositionlessActorsAndNull()
		{
			// An actor with no IOccupySpace trait (the Player actor) has OccupiesSpace == null.
			var positionless = (Actor)RuntimeHelpers.GetUninitializedObject(typeof(Actor));
			Assert.That(positionless.OccupiesSpace, Is.Null);
			Assert.Throws<NullReferenceException>(() => _ = positionless.Location);
			Assert.That(MasterAiBotModule.HasSpace(positionless), Is.False);
			Assert.That(MasterAiBotModule.HasSpace(null), Is.False);
		}

		[Test]
		public void SwitchOnEmergencyLiveSetSkipsPositionlessProducers()
		{
			var body = Section(Source("mods/Cameo/Traits/BotModules/BotSituation.cs"),
				"// BOTPASSIVE F4 Emergency' (switch ON).", "// BOTPASSIVE F3/F4 Urgency' at a snapshot");
			var local = Section(body, "IEnumerable<Actor> LiveProductionAndCy() =>", "}");
			Assert.That(local, Does.Contain("a.Owner == player && HasSpace(a)"),
				"LiveProductionAndCy must exclude the Player actor (Production, no IOccupySpace)");
		}

		[Test]
		public void SwitchOnUrgencyAnchorsAndHomeThreatAreGuarded()
		{
			var body = Section(Source("mods/Cameo/Traits/BotModules/BotSituation.cs"),
				"BotUrgency StalemateUrgency(", "stalemate.HomeThreat = homeThreat;");
			GuardPrecedes(body, "HasSpace(b)", "b.Location", "anchors");
			GuardPrecedes(body, "list.Any(a => HasSpace(a)", "a.Location", "HomeThreat visible-actor check");
		}

		[Test]
		public void BreakerObserveAndDiagnosticsSkipPositionlessActors()
		{
			var src = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.Stalemate.cs");
			var observe = Section(src, "void StalemateObserve(", "seat.TargetSeen[a.ActorID] = t;");
			GuardPrecedes(observe, "a.OccupiesSpace == null", "a.Location", "StalemateObserve world.Actors loop");

			var combat = Section(src, "IEnumerable<Actor> DiagnosticCombatUnits() =>", ".Distinct()");
			Assert.That(combat, Does.Contain("a.OccupiesSpace != null"), "DiagnosticCombatUnits");

			var health = Section(src, "void DiagnosticHealth(", "owner.diagnosticRetreatTicks");
			GuardPrecedes(health, "s.Units[0].Actor?.OccupiesSpace != null", "s.Units[0].Actor.Location", "stuck sampler");
		}
	}
}
