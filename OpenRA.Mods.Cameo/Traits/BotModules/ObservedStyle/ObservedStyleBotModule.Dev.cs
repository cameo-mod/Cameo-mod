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

#if CAMEO_DEV_LOGGING
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// B4-C2 (DEV only): the DEV `style_obs` row sink. Implemented by the DEV telemetry trait; absent in a release build, where this whole file is compiled out.
	/// The module never reads anything back from an observer.
	/// </summary>
	public interface IBotObservedStyleObserver
	{
		/// <summary>One bucketed feature row of <paramref name="viewer"/>'s view of <paramref name="enemy"/> (the enemy is only a key to derive an anonymous label).
		/// <paramref name="remembered"/> is the size of the fog snapshot; <paramref name="features"/> follows <c>StyleFeatures.Names</c> and is reused by the caller;
		/// <paramref name="reading"/> is the classifier output and <paramref name="tableVersion"/> the sha8 of the table it used.</summary>
		void StyleObserved(OpenRA.Player viewer, OpenRA.Player enemy, int tick, int remembered, int[] features, StyleReading reading, string tableVersion);
	}

	public sealed partial class ObservedStyleBotModule
	{
		/// <summary>Ticks between `style_obs` rows per (viewer, enemy).</summary>
		const int RowIntervalTicks = 300;

		readonly Dictionary<OpenRA.Player, int> nextRow = new();
		IBotObservedStyleObserver[] observers;

		partial void StyleRow(OpenRA.Player enemy, int now, int remembered, StyleClassifier classifier)
		{
			observers ??= world.WorldActor.TraitsImplementing<IBotObservedStyleObserver>().ToArray();
			if (observers.Length == 0)
				return;

			if (nextRow.TryGetValue(enemy, out var due) && now < due)
				return;

			nextRow[enemy] = now + RowIntervalTicks;
			foreach (var observer in observers)
				observer.StyleObserved(viewer, enemy, now, remembered, features, classifier.Reading, classifier.TableVersion);
		}
	}
}
#endif
