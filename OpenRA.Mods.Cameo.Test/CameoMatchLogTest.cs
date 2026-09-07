#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class CameoMatchLogTest
	{
		string directory;

		[SetUp]
		public void SetUp() { directory = Path.Combine(Path.GetTempPath(), "cameo_match_test_" + Guid.NewGuid().ToString("N")); }

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(directory))
				Directory.Delete(directory, true);
		}

		[Test]
		public void RecordIsOneUtf8JsonLineWithEscapedStringsAndVersion()
		{
			var record = new MatchLogRecord
			{
				GameUid = "../../not_a_filename",
				MapTitle = "quoted \"map\"\n日本語",
				Players = new[] { new MatchPlayerRecord { Slot = "Multi0", Outcome = "Undefined" } }
			};
			Assert.That(CameoMatchLog.TryWrite(directory, record, out var error), Is.True, error);
			var file = Directory.GetFiles(directory).Single();
			Assert.That(Path.GetExtension(file), Is.EqualTo(".jsonl"));
			var text = File.ReadAllText(file, Encoding.UTF8);
			Assert.That(text.Count(c => c == '\n'), Is.EqualTo(1));
			using var json = JsonDocument.Parse(text);
			Assert.That(json.RootElement.GetProperty("schema_version").GetInt32(), Is.EqualTo(1));
			Assert.That(json.RootElement.GetProperty("map_title").GetString(), Is.EqualTo(record.MapTitle));
			Assert.That(json.RootElement.GetProperty("players")[0].GetProperty("value_lost").ValueKind, Is.EqualTo(JsonValueKind.Null));
		}

		[Test]
		public void SeparateWritesNeverOverwriteAnEarlierMatch()
		{
			Assert.That(CameoMatchLog.TryWrite(directory, new MatchLogRecord(), out _), Is.True);
			var first = Directory.GetFiles(directory).Single();
			var bytes = File.ReadAllBytes(first);
			Assert.That(CameoMatchLog.TryWrite(directory, new MatchLogRecord(), out _), Is.True);
			Assert.That(Directory.GetFiles(directory).Length, Is.EqualTo(2));
			Assert.That(File.ReadAllBytes(first), Is.EqualTo(bytes));
		}

		[Test]
		public void OversizedRecordIsRejectedBeforePublishing()
		{
			var record = new MatchLogRecord { MapTitle = new string('x', CameoMatchLog.MaximumRecordBytes + 1) };
			Assert.That(CameoMatchLog.TryWrite(directory, record, out var error), Is.False);
			Assert.That(error, Is.Not.Null);
			Assert.That(Directory.Exists(directory), Is.False);
		}

		[Test]
		public void FilesystemFailureDoesNotEscapeToTheSimulation()
		{
			Directory.CreateDirectory(directory);
			var blocked = Path.Combine(directory, "file_not_directory");
			File.WriteAllText(blocked, "keep");
			Assert.That(CameoMatchLog.TryWrite(blocked, new MatchLogRecord(), out var error), Is.False);
			Assert.That(error, Is.Not.Null);
			Assert.That(File.ReadAllText(blocked), Is.EqualTo("keep"));
		}

		[Test]
		public void NullRecordDoesNotPublish()
		{
			Assert.That(CameoMatchLog.TryWrite(directory, null, out _), Is.False);
			Assert.That(Directory.Exists(directory), Is.False);
		}

		static string Fingerprint(params (string Name, string Text)[] inputs)
		{
			return CameoMatchLog.Fingerprint(inputs.Select(i =>
				(i.Name, (Func<Stream>)(() => new MemoryStream(Encoding.UTF8.GetBytes(i.Text))))));
		}

		[Test]
		public void FingerprintIncludesNamesContentsBoundariesAndOrder()
		{
			var original = Fingerprint(("rules", "ab"), ("weapons", "c"));
			Assert.That(original, Is.EqualTo(Fingerprint(("rules", "ab"), ("weapons", "c"))));
			Assert.That(original, Is.Not.EqualTo(Fingerprint(("rules", "a"), ("weapons", "bc"))));
			Assert.That(original, Is.Not.EqualTo(Fingerprint(("weapons", "c"), ("rules", "ab"))));
			Assert.That(original, Is.Not.EqualTo(Fingerprint(("renamed", "ab"), ("weapons", "c"))));
		}

		[Test]
		public void MissingHashInputCannotProduceAValidPartialHash()
		{
			Assert.Throws<FileNotFoundException>(() => CameoMatchLog.Fingerprint(new[]
			{
				("missing", (Func<Stream>)(() => throw new FileNotFoundException()))
			}));
		}

		[Test]
		public void PersonalityComesFromConditionValuesAndAmbiguityIsExplicit()
		{
			var state = new CameoMatchPlayerState(new[] { "personality-rush", "personality-tech" });
			state.Observe(new Dictionary<string, int> { ["personality-rush"] = 1 });
			Assert.That(state.Personality, Is.EqualTo("personality-rush"));
			state.Observe(new Dictionary<string, int> { ["personality-tech"] = 2 });
			Assert.That(state.Personality, Is.EqualTo("personality-tech"));
			state.Observe(new Dictionary<string, int> { ["personality-rush"] = 1, ["personality-tech"] = 1 });
			Assert.That(state.Personality, Is.Null);
			Assert.That(state.Status, Is.EqualTo("ambiguous"));
			state.Observe(new Dictionary<string, int>());
			Assert.That(state.Status, Is.EqualTo("unavailable"));
		}

		[TestCase(WorldType.Regular, false, false, true)]
		[TestCase(WorldType.Shellmap, false, false, false)]
		[TestCase(WorldType.Editor, false, false, false)]
		[TestCase(WorldType.Regular, true, false, false)]
		[TestCase(WorldType.Regular, false, true, false)]
		public void OnlyFreshRegularMatchesAreEligible(WorldType type, bool replay, bool save, bool expected)
		{
			Assert.That(CameoMatchRecorder.Eligible(type, replay, save), Is.EqualTo(expected));
		}
	}
}
