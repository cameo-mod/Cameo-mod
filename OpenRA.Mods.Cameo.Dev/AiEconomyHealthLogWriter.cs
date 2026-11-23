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
using System.IO;
using System.Text;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	// Owns a new capture file only. Never appends to a previous cohort's evidence.
	internal sealed class AiEconomyHealthLogWriter : IDisposable
	{
		internal const int MaximumLineBytes = 65536;
		internal const long MaximumFileBytes = 128 * 1024 * 1024;
		internal const int MaximumRecords = 200000;
		static readonly Encoding Utf8 = new UTF8Encoding(false, true);
		readonly Stream stream;
		readonly long byteLimit;
		readonly int recordLimit;
		long bytes;
		int records;
		int maximumRecordBytes;
		bool failed;
		bool disposed;

		internal AiEconomyHealthLogWriter(string path)
			: this(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read)) { }

		internal AiEconomyHealthLogWriter(Stream stream, long byteLimit = MaximumFileBytes,
			int recordLimit = MaximumRecords)
		{
			ArgumentNullException.ThrowIfNull(stream);
			if (byteLimit < 1 || recordLimit < 1)
				throw new ArgumentOutOfRangeException(nameof(byteLimit));
			this.stream = stream;
			this.byteLimit = byteLimit;
			this.recordLimit = recordLimit;
		}

		internal bool Complete => !failed && !disposed;
		internal int Records => records;
		internal long Bytes => bytes;
		internal int MaximumRecordBytes => maximumRecordBytes;

		internal bool TryWrite(string json)
		{
			if (failed || disposed)
				return false;

			try
			{
				// Enforce the bound before allocating an encoded record. JSON must occupy one line.
				if (json == null || json.Length > MaximumLineBytes || json.Contains('\n') || json.Contains('\r'))
					return Fail();
				var count = Utf8.GetByteCount(json) + 1;
				if (count > MaximumLineBytes || count > byteLimit - bytes || records >= recordLimit)
					return Fail();

				var data = Utf8.GetBytes(json + "\n");
				stream.Write(data);
				// Each record is a read watermark only after flushing. Cross-file watermark is separate.
				stream.Flush();
				bytes += count;
				records++;
				maximumRecordBytes = Math.Max(maximumRecordBytes, count);
				return true;
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException or
				ObjectDisposedException or EncoderFallbackException)
			{
				return Fail();
			}
		}

		bool Fail()
		{
			failed = true;
			return false;
		}

		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			try { stream.Dispose(); }
			catch (IOException) { failed = true; }
		}
	}
}
