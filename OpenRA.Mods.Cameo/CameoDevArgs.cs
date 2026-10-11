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

namespace OpenRA.Mods.Cameo
{
	/// <summary>
	/// DEV-ONLY launch-argument gate for the takeover smoke harness (TAKEOVER-SMOKE task).
	/// The mod never reads engine-side settings classes for this: the raw command line is
	/// scanned for an exact `Name=True` argument (case-insensitive on the name). Missing
	/// or any other value means OFF. Never enables anything by itself — consumers also
	/// require their plan file, so a normal server or client is always inert.
	/// </summary>
	public static class CameoDevArgs
	{
		public static bool IsEnabled(string name)
		{
			var expect = name + "=";
			foreach (var arg in Environment.GetCommandLineArgs())
			{
				if (!arg.StartsWith(expect, StringComparison.OrdinalIgnoreCase))
					continue;

				var value = arg[expect.Length..];
				return value.Equals("True", StringComparison.OrdinalIgnoreCase) || value == "1";
			}

			return false;
		}

		/// <summary>The text after `Name=` (any value), or null when the arg is absent.</summary>
		public static string Value(string name)
		{
			var expect = name + "=";
			foreach (var arg in Environment.GetCommandLineArgs())
				if (arg.StartsWith(expect, StringComparison.OrdinalIgnoreCase))
					return arg[expect.Length..];

			return null;
		}
	}
}
