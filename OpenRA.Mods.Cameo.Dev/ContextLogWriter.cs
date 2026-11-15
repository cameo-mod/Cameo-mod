using System.Collections.Generic;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Dev
{
	// Keeps each player's original bounded retry queue in the development assembly.
	internal sealed class ContextLogWriter
	{
		sealed class PendingRow
		{
			public string Text;
			public AiLogFileAppender Appender;
			public int NextAttemptTick;
		}

		readonly Dictionary<int, List<PendingRow>> byPlayer = new();

		public void Enqueue(int playerIndex, string fileName, string text, int tick)
		{
			if (text == null)
				return;
			if (!byPlayer.TryGetValue(playerIndex, out var rows))
				byPlayer[playerIndex] = rows = new List<PendingRow>();
			if (rows.Count >= 64)
				rows.RemoveAt(0);
			rows.Add(new PendingRow { Text = text + "\n", Appender = new AiLogFileAppender(fileName), NextAttemptTick = tick });
		}

		public void Flush(int playerIndex, int tick)
		{
			if (!byPlayer.TryGetValue(playerIndex, out var rows))
				return;
			for (var i = rows.Count - 1; i >= 0; i--)
			{
				var row = rows[i];
				if (tick < row.NextAttemptTick)
					continue;
				var result = row.Appender.TryAppend(row.Text, tick, out var next);
				if (result == AiLogAppendResult.RetryableFailure && !row.Appender.IsTerminal)
					row.NextAttemptTick = next;
				else
					rows.RemoveAt(i);
			}
		}
	}
}
