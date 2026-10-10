using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class McvHealthObservationTest
	{
		static McvHealthObservation Actor(uint id = 1, int x = 0, string activity = "idle",
			string role = "construction_mcv", bool live = true) =>
			new(id, "configured-vehicle", role, "configured-yard", x, 0, live, true, activity, null);

		[Test]
		public void ExactIdleAgeResetsOnCellProgressAndSameCellBusyActivity()
		{
			var state = new McvHealthState();
			state.Observe(0, new[] { Actor() });
			state.Observe(1, new[] { Actor() });
			Assert.That(state.Snapshots.Single().IdleSinceTick, Is.Zero);
			state.Observe(2, new[] { Actor(x: 1) });
			Assert.That(state.Snapshots.Single().IdleSinceTick, Is.EqualTo(2));
			Assert.That(state.Snapshots.Single().LastCellChangeTick, Is.EqualTo(2));
			state.Observe(3, new[] { Actor(x: 1, activity: "deploying") });
			Assert.That(state.Snapshots.Single().IdleSinceTick, Is.Null);
			state.Observe(4, new[] { Actor(x: 1) });
			Assert.That(state.Snapshots.Single().IdleSinceTick, Is.EqualTo(4));
		}

		[Test]
		public void RemovalAndOwnerTransferPruneWithoutInventingTransformation()
		{
			var state = new McvHealthState();
			state.Observe(0, new[] { Actor() });
			Assert.That(state.Observe(1, Array.Empty<McvHealthObservation>()), Is.True);
			Assert.That(state.Snapshots, Is.Empty);
			var row = McvHealthSchema.Row("game", "player", 0, 1, "transition", true,
				true, true, state.Snapshots, new Dictionary<string, string> { ["configured-vehicle"] = "construction_mcv" });
			using var parsed = JsonDocument.Parse(row);
			Assert.That(parsed.RootElement.GetProperty("transform_complete").GetBoolean(), Is.False);
		}

		[TestCase(1, true, true, true, false, "construction_mcv")]
		[TestCase(1, true, true, false, true, "unknown")]
		[TestCase(1, false, true, false, true, "field_refinery_vehicle")]
		[TestCase(1, false, true, false, false, "base_building_vehicle")]
		[TestCase(1, false, false, false, false, "unknown")]
		public void RolesUseConfigurationAndLoadedTransformTarget(int unused, bool construction,
			bool known, bool yard, bool refinery, string role)
		{
			Assert.That(McvHealthState.ResolveRole(true, construction, known, yard, refinery, true), Is.EqualTo(role));
		}

		[Test]
		public void UnsupportedRoleGapAndActorOverflowPermanentlyInvalidateCoverage()
		{
			var state = new McvHealthState();
			state.Observe(0, new[] { Actor(role: "unknown") });
			state.Observe(2, new[] { Actor() });
			Assert.That(state.Complete, Is.False);
			Assert.That(state.Snapshots, Is.Empty, "a gapped census cannot relabel stale actor facts as current");
			var overflow = new McvHealthState();
			overflow.Observe(0, Enumerable.Range(1, 65).Select(i => Actor((uint)i)).ToArray());
			Assert.That(overflow.Complete, Is.False);
			Assert.That(overflow.Snapshots, Is.Empty);
		}

		[Test]
		public void ActualCanonicalSerializerFixtureKeepsMissingSeamsUnknown()
		{
			var support = Path.Combine(TestContext.CurrentContext.WorkDirectory, "TestResults", "mcv-health-cli-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(Path.Combine(support, "Logs"));
			var path = Path.Combine(support, "Logs", "cameo-ai-mcv-health.jsonl");
			var roles = new Dictionary<string, string> { ["configured-vehicle"] = "construction_mcv" };
			var state = new McvHealthState();
			using (var writer = new AiEconomyHealthLogWriter(path))
			{
				var seq = 0;
				for (var tick = 0; tick <= 150; tick++)
				{
					state.Observe(tick, new[] { Actor() });
					if (tick % 50 == 0)
						Assert.That(writer.TryWrite(McvHealthSchema.Row("game", "player", seq++, tick,
							tick == 0 ? "start" : "pulse", true, true, state.Complete, state.Snapshots, roles)), Is.True);
				}
				Assert.That(writer.TryWrite(McvHealthSchema.Row("game", "player", seq, 150,
					"end", true, true, state.Complete, state.Snapshots, roles)), Is.True);
			}
			File.WriteAllText(Path.Combine(TestContext.CurrentContext.WorkDirectory,
				"TestResults", "mcv-health-cli-input.txt"), support);
			using var row = JsonDocument.Parse(File.ReadLines(path).First());
			var actor = row.RootElement.GetProperty("actors")[0];
			Assert.That(actor.GetProperty("order_accepted").ValueKind, Is.EqualTo(JsonValueKind.Null));
			Assert.That(actor.GetProperty("hold").GetString(), Is.EqualTo("unknown"));
			Assert.That(actor.GetProperty("proven_transform_actor_id").ValueKind, Is.EqualTo(JsonValueKind.Null));
		}
	}
}
