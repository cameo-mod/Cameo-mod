#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Globalization;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Traits
{
	// B4-C2 (DEV only, row `style_obs`): one row per (viewer, enemy) every 300 ticks, carrying the bucketed fog-only features in the order of StyleFeatures.Names, the six
	// posteriors (StyleReading.StyleNames order), the confidence C (permille) and the likelihood-table sha8. It carries NO label, NO personality, NO enemy faction or player
	// name and NO client id: the viewer is its seat label (as every other row), the enemy is an anonymous key (its seat label when it is a logged bot, else "p<index>").
	// The true label is joined OFFLINE from the match log by tools/observed_style/score_observed_style.py and is never an input of the classifier.
	public sealed partial class AiPlayerTelemetryDev : IBotObservedStyleObserver
	{
		/// <summary>Version of the row layout; bumped when a field is added.</summary>
		internal const int StyleObsRowVersion = 2;

		void IBotObservedStyleObserver.StyleObserved(OpenRA.Player viewer, OpenRA.Player enemy, int tick, int remembered, int[] features, StyleReading reading, string tableVersion)
		{
			ObserverRow(viewer, "style_obs", b =>
			{
				AiMatchLogWriter.AppendString(b, "enemy", EnemyKey(enemy));
				AiMatchLogWriter.AppendNumber(b, "row_version", StyleObsRowVersion);
				AiMatchLogWriter.AppendNumber(b, "remembered", remembered);
				b.Append(",\"features\":[");
				for (var i = 0; i < features.Length; i++)
				{
					if (i > 0)
						b.Append(',');

					b.Append(features[i].ToString(CultureInfo.InvariantCulture));
				}

				b.Append(']');
				b.Append(",\"posterior\":[");
				var posterior = reading.PosteriorPermille;
				for (var i = 0; i < posterior.Length; i++)
				{
					if (i > 0)
						b.Append(',');

					b.Append(posterior[i].ToString(CultureInfo.InvariantCulture));
				}

				b.Append(']');
				AiMatchLogWriter.AppendNumber(b, "confidence", reading.ConfidencePermille);
				AiMatchLogWriter.AppendString(b, "table_version", tableVersion);
			});
		}

		string EnemyKey(OpenRA.Player enemy)
		{
			var seat = BotSeat(enemy);
			return seat != null ? seat.Seat : "p" + Array.IndexOf(world.Players, enemy).ToString(CultureInfo.InvariantCulture);
		}
	}
}
