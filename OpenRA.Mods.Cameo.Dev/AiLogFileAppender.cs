#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace OpenRA.Mods.Cameo.Traits
{
	internal enum AiLogAppendResult { Appended, RetryableFailure, Failed }

	// E3-3: enqueue-only. The file is written by AiLogWriteQueue's single worker thread, so the sim thread
	// never touches the mutex or the file. Accepted = Appended (the worker reports later failures on the log
	// channel); refused (queue full) = Failed and counted as dropped. Both are terminal for the writer.
	internal sealed class AiLogFileAppender
	{
		readonly string filePath;
		readonly string mutexName;
		readonly string writer;

		public bool IsTerminal { get; private set; }

		// PERF-SPK-01: the path this appender writes, so the writer can assert it matches the file it created exclusively.
		internal string FilePath => filePath;

		public AiLogFileAppender(string fileName, string writer)
		{
			this.writer = writer;
			filePath = Path.Combine(Platform.SupportDir, "Logs", fileName);
			var canonicalPath = Path.GetFullPath(filePath);
			if (OperatingSystem.IsWindows())
				canonicalPath = canonicalPath.ToUpperInvariant();

			mutexName = "OpenRA-CameoAiLog-" + Convert.ToHexString(
				SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPath)));
		}

		// finalRecord: the writer's GameOver/Disposing (or end-of-game) record, exempt from the text budget.
		public AiLogAppendResult TryAppend(string text, int worldTick, out int nextAttemptTick, bool finalRecord = false)
		{
			// No tick-based retry any more: the worker owns retry and backoff.
			nextAttemptTick = worldTick;
			IsTerminal = true;
			var request = new AiLogWriteRequest(filePath, mutexName, text, worldTick, writer, finalRecord);
			return AiLogWriteQueue.Shared.TryEnqueue(request) ? AiLogAppendResult.Appended : AiLogAppendResult.Failed;
		}
	}
}
