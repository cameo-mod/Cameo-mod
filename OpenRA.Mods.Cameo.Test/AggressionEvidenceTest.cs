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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	// IMPL02 A6 telemetry (AGGRESSION_PLAN_01 section 3 A6, ADDENDUM_01): the evidence an aggression rule held when a squad retreated,
	// put on the retreat_start row. Switch-ON paths only; the OFF arm never records any, so its rows keep their legacy fields and bytes.
	// Pure data and the wire are tested on state; the world call sites are pinned in the source (no headless world fixture exists).
	[TestFixture]
	public class AggressionEvidenceTest
	{
		static readonly string[] Envelope = { "schema", "record", "game_uid", "seat", "bot_type", "tick", "seq" };

		static readonly string[] LegacySquadEvent =
		{
			"squad_id", "generation", "event", "member_actor_id", "size", "value", "state_from", "state_to", "target_type", "target_cell", "reason", "lease_owner"
		};

		static readonly string[] EvidenceFields =
		{
			"predicted_ratio_milli", "coalition_ratio_milli", "band", "trade_delta", "allies_engaged", "own_faction", "enemy_faction"
		};

		string directory;
		string path;

		[SetUp]
		public void SetUp()
		{
			directory = Path.Combine(Path.GetTempPath(), "aggression-evidence-test-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(directory);
			path = Path.Combine(directory, "seat0.jsonl");
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(directory))
				Directory.Delete(directory, true);
		}

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file));

		static string Module(string file) => Source("mods/CA/Traits/BotModules/" + file);

		static string Section(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start + from.Length, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text.Substring(start, end - start);
		}

		static int Count(string text, string needle) => Regex.Matches(text, Regex.Escape(needle)).Count;

		JsonElement WriteOne(BotPassiveSquadEventSnapshot snapshot)
		{
			BotPassiveDiagnosticWriter.TryCreate(true, path, "game-1", "seat0", "normal", out var writer);
			Assert.That(writer.Enabled, Is.True);
			Assert.That(writer.TryEnqueueSquadEvent(snapshot), Is.True);
			writer.Complete(45000);
			var first = File.ReadAllLines(path).First(l => l.Contains("\"squad_event\""));
			return JsonDocument.Parse(first).RootElement.Clone();
		}

		static BotPassiveSquadEventSnapshot Retreat() => new()
		{
			Tick = 900, SquadId = "sq-1", Generation = 2, Event = "retreat_start", MemberActorId = null, Size = 5, Value = 4000,
			StateFrom = "idle", StateTo = "flee", TargetType = "fact", TargetCell = "10,12", Reason = "idle_engage", LeaseOwner = null
		};

		static string[] Names(JsonElement row) => row.EnumerateObject().Select(p => p.Name).ToArray();

		// ---- the wire ----

		[Test]
		public void ARowWithoutEvidenceKeepsExactlyItsLegacyFields()
		{
			var row = WriteOne(Retreat());
			Assert.That(Names(row), Is.EqualTo(Envelope.Concat(LegacySquadEvent).ToArray()), "the OFF arm and every non-aggression row");
		}

		[Test]
		public void ARowWithEvidenceAppendsTheSevenFieldsAfterTheLegacyOnesInOrder()
		{
			var snapshot = new BotPassiveSquadEventSnapshot
			{
				Tick = 900, SquadId = "sq-1", Generation = 2, Event = "retreat_start", MemberActorId = null, Size = 5, Value = 4000,
				StateFrom = "idle", StateTo = "flee", TargetType = "fact", TargetCell = "10,12", Reason = "idle_regroup_expired", LeaseOwner = null,
				PredictedRatioMilli = 420, CoalitionRatioMilli = 1350, Band = "regroup", TradeDelta = -4500, AlliesEngaged = 7,
				OwnFaction = "ra1_soviets", EnemyFaction = "td_gdi"
			};
			var row = WriteOne(snapshot);
			Assert.That(Names(row), Is.EqualTo(Envelope.Concat(LegacySquadEvent).Concat(EvidenceFields).ToArray()));
			Assert.That(row.GetProperty("predicted_ratio_milli").GetInt32(), Is.EqualTo(420));
			Assert.That(row.GetProperty("coalition_ratio_milli").GetInt32(), Is.EqualTo(1350));
			Assert.That(row.GetProperty("band").GetString(), Is.EqualTo("regroup"));
			Assert.That(row.GetProperty("trade_delta").GetInt64(), Is.EqualTo(-4500));
			Assert.That(row.GetProperty("allies_engaged").GetInt32(), Is.EqualTo(7));
			Assert.That(row.GetProperty("own_faction").GetString(), Is.EqualTo("ra1_soviets"));
			Assert.That(row.GetProperty("enemy_faction").GetString(), Is.EqualTo("td_gdi"));
			Assert.That(row.GetProperty("reason").GetString(), Is.EqualTo("idle_regroup_expired"));
		}

		[Test]
		public void OnlyThePresentEvidenceMembersAreWritten()
		{
			// An idle squad has no commit (no trade_delta) and a squad without a target has no enemy faction.
			var snapshot = new BotPassiveSquadEventSnapshot
			{
				Tick = 900, SquadId = "sq-1", Generation = 2, Event = "retreat_start", Size = 5, Value = 4000, Reason = "idle_engage",
				PredictedRatioMilli = 0, CoalitionRatioMilli = 0, Band = "flee", AlliesEngaged = 0, OwnFaction = "ra1_allies"
			};
			var row = WriteOne(snapshot);
			Assert.That(Names(row), Is.EqualTo(Envelope.Concat(LegacySquadEvent).Concat(
				new[] { "predicted_ratio_milli", "coalition_ratio_milli", "band", "allies_engaged", "own_faction" }).ToArray()));
			Assert.That(row.GetProperty("predicted_ratio_milli").GetInt32(), Is.EqualTo(0), "a measured zero is written, not skipped");
			Assert.That(row.GetProperty("allies_engaged").GetInt32(), Is.EqualTo(0));
		}

		[Test]
		public void AnUnboundedRatioAndALargeTradeSurviveTheWire()
		{
			var snapshot = new BotPassiveSquadEventSnapshot
			{
				Tick = 900, SquadId = "sq-1", Generation = 2, Event = "retreat_start", Size = 5, Value = 4000, Reason = "attack_should_flee",
				PredictedRatioMilli = AggressionEvidenceCA.RatioMilliCap, CoalitionRatioMilli = AggressionEvidenceCA.ToMilli(double.PositiveInfinity),
				Band = "attack", TradeDelta = 5_000_000_000L
			};
			var row = WriteOne(snapshot);
			Assert.That(row.GetProperty("predicted_ratio_milli").GetInt32(), Is.EqualTo(AggressionEvidenceCA.RatioMilliCap));
			Assert.That(row.GetProperty("coalition_ratio_milli").GetInt32(), Is.EqualTo(AggressionEvidenceCA.RatioMilliCap));
			Assert.That(row.GetProperty("trade_delta").GetInt64(), Is.EqualTo(5_000_000_000L), "a long, not an int");
		}

		// ---- the pure data ----

		[TestCase(1.0, 1000)]
		[TestCase(0.0, 0)]
		[TestCase(0.5, 500)]
		[TestCase(1.2346, 1235)]
		[TestCase(1.2344, 1234)]
		[TestCase(0.0004, 0)]
		[TestCase(-3.0, 0)]
		[TestCase(1000.0, 1000000)]
		[TestCase(5000.0, AggressionEvidenceCA.RatioMilliCap)]
		[TestCase(double.PositiveInfinity, AggressionEvidenceCA.RatioMilliCap)]
		[TestCase(double.NegativeInfinity, 0)]
		[TestCase(double.NaN, 0)]
		public void ARatioIsRoundedToThousandthsAndClamped(double ratio, int expected)
		{
			Assert.That(AggressionEvidenceCA.ToMilli(ratio), Is.EqualTo(expected));
		}

		[Test]
		public void EveryBandHasItsWireNameAndNothingElseIsAccepted()
		{
			Assert.That(AggressionEvidenceCA.BandName(AggressionBandCA.Flee), Is.EqualTo("flee"));
			Assert.That(AggressionEvidenceCA.BandName(AggressionBandCA.Regroup), Is.EqualTo("regroup"));
			Assert.That(AggressionEvidenceCA.BandName(AggressionBandCA.Attack), Is.EqualTo("attack"));
			Assert.That(Enum.GetValues(typeof(AggressionBandCA)).Length, Is.EqualTo(3), "a new band needs a wire name");
			Assert.Throws<ArgumentOutOfRangeException>(() => AggressionEvidenceCA.BandName((AggressionBandCA)99));
		}

		[Test]
		public void EvidenceIsFreshOnlyInTheTickItWasMeasured()
		{
			var evidence = new AggressionEvidenceCA();
			Assert.That(evidence.IsFresh(0), Is.False, "a new record is not evidence of tick 0");
			Assert.That(evidence.IsFresh(-1), Is.False);
			evidence.Tick = 1200;
			Assert.That(evidence.IsFresh(1200), Is.True);
			Assert.That(evidence.IsFresh(1201), Is.False, "next tick it is stale and never logged");
			Assert.That(evidence.IsFresh(1199), Is.False);
		}

		// ---- the OFF arm: no evidence is ever created or found ----

		[Test]
		public void AShellModuleFindsNoEvidenceAndTouchesNoWorld()
		{
			var m = (SquadManagerBotModuleCA)RuntimeHelpers.GetUninitializedObject(typeof(SquadManagerBotModuleCA));
			var lookup = typeof(SquadManagerBotModuleCA).GetMethod("AggressionEvidenceFor", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(lookup, Is.Not.Null);
			Assert.That(lookup.Invoke(m, new object[] { null }), Is.Null, "no squad, no evidence, World is never read");
		}

		static SquadManagerBotModuleCA Shell(bool diagnostics)
		{
			var info = new SquadManagerBotModuleCAInfo();
			typeof(SquadManagerBotModuleCAInfo).GetField(nameof(SquadManagerBotModuleCAInfo.StalemateDiagnosticsEnabled)).SetValue(info, diagnostics);
			var m = (SquadManagerBotModuleCA)RuntimeHelpers.GetUninitializedObject(typeof(SquadManagerBotModuleCA));
			typeof(SquadManagerBotModuleCA).BaseType.GetField("Info", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(m, info);
			return m;
		}

		[Test]
		public void NothingIsMeasuredOrRecordedWhileDiagnosticsAreOff()
		{
			// Review A6-TELEMETRY-01 F1: the only reader returns at once with diagnostics off, so the record must not run its ratio,
			// ally scan or allocation either. The shell has no World, Player or squad: only the gate can return before they are touched
			// (without it the null squad throws).
			var m = Shell(diagnostics: false);
			const BindingFlags any = BindingFlags.Instance | BindingFlags.NonPublic;
			var record = typeof(SquadManagerBotModuleCA).GetMethod("AggressionRecordEvidence", any);
			var fight = typeof(SquadManagerBotModuleCA).GetMethod("AggressionRecordFightEvidence", any);
			Assert.That(record, Is.Not.Null);
			Assert.That(fight, Is.Not.Null);
			Assert.DoesNotThrow(() => record.Invoke(m, new object[] { null, AggressionBandCA.Flee, 1.0, null, null }));
			Assert.DoesNotThrow(() => fight.Invoke(m, new object[] { null, 1.0, null, 0L }));
		}

		[Test]
		public void TheDiagnosticEmitterKeepsItsThreeParameterShape()
		{
			// StalemateOffIdentityTest reaches it by reflection with three arguments; the evidence overload has its own name.
			var all = typeof(SquadManagerBotModuleCA).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Where(x => x.Name == "DiagnosticSquadEvent").ToList();
			Assert.That(all.Count, Is.EqualTo(1));
			Assert.That(all[0].GetParameters().Select(p => p.Name).ToArray(), Is.EqualTo(new[] { "squad", "evt", "reason" }));
		}

		// ---- the call sites (source pins) ----

		[Test]
		public void OnlyTheRetreatStartRowCarriesEvidenceAndTheOtherEventsPassNone()
		{
			var stalemate = Module("SquadManagerBotModuleCA.Stalemate.cs");
			Assert.That(Count(stalemate, "DiagnosticSquadEventWith("), Is.EqualTo(4), "the three-argument delegator, the A10 cell delegator, the definition and the retreat_start call");
			Assert.That(stalemate, Does.Contain("DiagnosticSquadEventWith(squad, \"retreat_start\", site, AggressionEvidenceFor(squad));"));
			Assert.That(stalemate, Does.Contain("void DiagnosticSquadEvent(SquadCA squad, string evt, string reason = null) => DiagnosticSquadEventWith(squad, evt, reason, null);"));
			Assert.That(stalemate, Does.Not.Contain("AggressionRecord"), "the emitter reads evidence; only the aggression paths record it");

			var note = Section(stalemate, "internal void NoteRetreatStart(", "\n\t\t}\n");
			Assert.That(note.IndexOf("if (!Info.StalemateDiagnosticsEnabled)", StringComparison.Ordinal),
				Is.LessThan(note.IndexOf("AggressionEvidenceFor(", StringComparison.Ordinal)), "diagnostics off returns before any lookup");
		}

		[Test]
		public void TheIdleRetreatRecordsItsEvidenceJustBeforeTheRetreatRowAndOnlyThere()
		{
			var states = Module("Squads/States/GroundStatesCA.cs");
			var body = Section(states, "void AggressionTick(SquadCA owner, List<Actor> enemyUnits)", "// A2: move to the forward regroup point");
			Assert.That(Count(states, "AggressionRecordEvidence("), Is.EqualTo(1), "one record site in the states");
			Assert.That(Count(states, "AggressionRecordFightEvidence("), Is.EqualTo(0), "the fight helper is called from the module only");
			var record = body.IndexOf("manager.AggressionRecordEvidence(owner, band, ratio, enemyUnits, null);", StringComparison.Ordinal);
			var note = body.IndexOf("manager.NoteRetreatStart(owner, site);", StringComparison.Ordinal);
			var defers = body.IndexOf("manager.StalemateDefersRetreat(owner, site)", StringComparison.Ordinal);
			Assert.That(defers, Is.GreaterThanOrEqualTo(0));
			Assert.That(record, Is.GreaterThan(defers), "a deferred retreat records nothing");
			Assert.That(note, Is.GreaterThan(record), "recorded before the row is written");
		}

		[Test]
		public void TheFightRecordsOnlyWhenNothingHoldsTheSquad()
		{
			var module = Module("SquadManagerBotModuleCA.Aggression.cs");
			var body = Section(module, "internal AggressionPolicyCA.HoldReason AggressionHoldsInFight(", "\n\t\t}\n");
			Assert.That(Count(body, "AggressionRecordFightEvidence("), Is.EqualTo(1));
			var guard = body.IndexOf("if (hold == AggressionPolicyCA.HoldReason.None)", StringComparison.Ordinal);
			var record = body.IndexOf("AggressionRecordFightEvidence(squad, withAllies, enemies, lost);", StringComparison.Ordinal);
			Assert.That(guard, Is.GreaterThan(0));
			Assert.That(record, Is.GreaterThan(guard), "inside the no-hold branch");
			Assert.That(body.Substring(record), Does.Contain("return hold;"), "the decision is returned unchanged");
			Assert.That(Count(module, "AggressionRecordEvidence("), Is.EqualTo(0), "the Aggression.cs holds no record logic of its own");
		}

		[Test]
		public void TheEvidencePartialLooksUpWithoutCreatingAndReadsThroughTheA9Accessor()
		{
			var partial = Module("SquadManagerBotModuleCA.AggressionEvidence.cs");
			var lookup = Section(partial, "internal AggressionEvidenceCA AggressionEvidenceFor(", ";\n");
			Assert.That(lookup, Does.Contain("TryGetValue("));
			Assert.That(lookup, Does.Not.Contain("new AggressionEvidenceCA"), "a lookup never allocates");
			Assert.That(Count(partial, "GetValue(squad, _ => new AggressionEvidenceCA())"), Is.EqualTo(1), "only the record creates");
			Assert.That(Regex.IsMatch(partial, @"\bInfo\.Aggression"), Is.False, "A9: no direct threshold read (SquadManagerBotModuleCAInfo.X inside nameof is fine)");
			Assert.That(Count(partial, "AggressionParam(nameof(SquadManagerBotModuleCAInfo."), Is.EqualTo(2), "FleeRatioPct and EngageRatioPct for the band");
			Assert.That(partial, Does.Contain("IsFresh(World.WorldTick)"));

			// F1: both record entry points return before measuring anything while diagnostics are off.
			Assert.That(partial, Does.Contain("bool AggressionEvidenceWanted => Info.StalemateDiagnosticsEnabled;"));
			foreach (var entry in new[] { "internal void AggressionRecordEvidence(", "internal void AggressionRecordFightEvidence(" })
			{
				var body = Section(partial, entry, "\n\t\t}\n");
				var gate = body.IndexOf("if (!AggressionEvidenceWanted)\n\t\t\t\treturn;", StringComparison.Ordinal);
				Assert.That(gate, Is.GreaterThanOrEqualTo(0), entry);
				foreach (var work in new[] { "AggressionRatio(", "AggressionAllyEngagedUnits(", "GetValue(", "AggressionParam(", "AggressionKillsCost(" })
				{
					var at = body.IndexOf(work, StringComparison.Ordinal);
					Assert.That(at < 0 || at > gate, Is.True, entry + " measures " + work + " before the gate");
				}
			}
		}
	}
}
