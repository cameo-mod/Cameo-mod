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
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OpenRA.Mods.Cameo
{
	public sealed class MatchPlayerRecord
	{
		public string Slot { get; set; }
		public string Faction { get; set; }
		public string BotType { get; set; }
		public bool IsBot { get; set; }
		public bool PlayableSlot { get; set; }
		public int? Team { get; set; }
		public int Handicap { get; set; }
		public string[] Allies { get; set; }
		public string InitialPersonality { get; set; }
		public string PersonalityStatus { get; set; }
		public string FinalPersonality { get; set; }
		public string FinalPersonalityStatus { get; set; }
		public string Outcome { get; set; }
		public long? ValueDestroyed { get; set; }
		public long? ValueLost { get; set; }
		public long? ResourcesEarned { get; set; }
		public long? ResourcesSpent { get; set; }
	}

	public sealed class MatchLogRecord
	{
		public int SchemaVersion { get; set; } = 1;
		public string Type { get; set; } = "match";
		public string Coverage { get; set; } = "completed_world";
		public string GameUid { get; set; }
		public string RecordedUtc { get; set; }
		public string MapUid { get; set; }
		public string MapTitle { get; set; }
		public string ModVersion { get; set; }
		public string RulesetHash { get; set; }
		public string RulesetHashScope { get; set; } = "ordered_rules_weapons_sources_v1";
		public string RulesetHashStatus { get; set; }
		public string[] CodeModules { get; set; }
		public string CodeModulesScope { get; set; } = "game_cameo_common_mvids";
		public Dictionary<string, string> LobbyOptions { get; set; }
		public int DurationTicks { get; set; }
		public int TimestepMs { get; set; }
		public long SimulationDurationMs { get; set; }
		public MatchPlayerRecord[] Players { get; set; }
	}

	public static class CameoMatchLog
	{
		public const int MaximumRecordBytes = 256 * 1024;
		static readonly JsonSerializerOptions JsonOptions = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
		};

		sealed class RecordBuffer : MemoryStream
		{
			public override void Write(ReadOnlySpan<byte> buffer)
			{
				if (Length + buffer.Length >= MaximumRecordBytes)
					throw new IOException("record exceeds 256 KiB limit");
				base.Write(buffer);
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				if (Length + count >= MaximumRecordBytes)
					throw new IOException("record exceeds 256 KiB limit");
				base.Write(buffer, offset, count);
			}
		}

		// Hash each named input independently: boundaries cannot alias concatenated
		// file contents, and order remains significant without loading files into RAM.
		public static string Fingerprint(IEnumerable<(string Name, Func<Stream> Open)> inputs)
		{
			using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
			foreach (var (name, open) in inputs)
			{
				hash.AppendData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(name)));
				using var stream = open();
				hash.AppendData(SHA256.HashData(stream));
			}

			return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
		}

		public static bool TryWrite(string directory, MatchLogRecord record, out string error)
		{
			error = null;
			string temporary = null;
			try
			{
				if (record == null)
				{
					error = "missing match record";
					return false;
				}

				using var buffer = new RecordBuffer();
				JsonSerializer.Serialize(buffer, record, JsonOptions);

				Directory.CreateDirectory(directory);
				// Neither player strings nor a server-supplied GameUid become paths.
				var id = Guid.NewGuid().ToString("N");
				temporary = Path.Combine(directory, id + ".tmp");
				using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
				{
					buffer.Position = 0;
					buffer.CopyTo(file);
					file.WriteByte((byte)'\n');
					file.Flush();
				}

				File.Move(temporary, Path.Combine(directory, id + ".jsonl"), false);
				return true;
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
			{
				error = e.GetType().Name;
				return false;
			}
			finally
			{
				if (temporary != null)
				{
					try { File.Delete(temporary); }
					catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
				}
			}
		}
	}
}
