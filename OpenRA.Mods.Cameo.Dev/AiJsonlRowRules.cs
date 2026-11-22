#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Text;

namespace OpenRA.Mods.Cameo.Traits
{
	// PERF-SPK-01: the one row-admission rule shared by AiEconomyHealthLogWriter (synchronous) and AiQueuedJsonlWriter (queued),
	// so both writers accept and refuse exactly the same rows. Pure: no I/O, no state.
	internal static class AiJsonlRowRules
	{
		static readonly Encoding Utf8 = new UTF8Encoding(false, true);

		// True when json may be written as one more line. count receives its encoded size including the newline.
		// Throws EncoderFallbackException for an invalid UTF-16 sequence; callers treat that as a refusal.
		internal static bool TryCount(string json, long bytes, int records, long byteLimit, int recordLimit, out int count)
		{
			count = 0;
			if (json == null || json.Length > AiEconomyHealthLogWriter.MaximumLineBytes || json.Contains('\n') || json.Contains('\r'))
				return false;

			count = Utf8.GetByteCount(json) + 1;
			return count <= AiEconomyHealthLogWriter.MaximumLineBytes && count <= byteLimit - bytes && records < recordLimit;
		}
	}
}
