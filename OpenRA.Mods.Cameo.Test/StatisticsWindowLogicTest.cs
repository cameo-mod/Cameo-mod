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

using System.Globalization;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Cameo.Widgets.Logic;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class StatisticsWindowLogicTest
	{
		[TestCase(999, "999")]
		[TestCase(1000, "1K")]
		[TestCase(1250, "1.3K")]
		[TestCase(999999, "1M")]
		[TestCase(999999999, "1B")]
		[TestCase(999999999999, "1T")]
		[TestCase(-999999, "-1M")]
		public void MetricUsesNormalizedSuffixes(long value, string expected)
		{
			var previousCulture = CultureInfo.CurrentCulture;
			try
			{
				CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
				Assert.That(StatisticsWindowLogic.Metric(value), Is.EqualTo(expected));
			}
			finally
			{
				CultureInfo.CurrentCulture = previousCulture;
			}
		}

		[TestCase(WinState.Undefined, null)]
		[TestCase(WinState.Won, "Won")]
		[TestCase(WinState.Lost, "Lost")]
		public void RecorderAcceptsOnlyFinalOutcomes(WinState state, string expected)
		{
			Assert.That(CameoCareerRecorder.FinalOutcome(state), Is.EqualTo(expected));
		}
	}
}
