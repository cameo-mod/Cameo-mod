#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>The resident boundary between simulation code and optional development logging.</summary>
	public interface ICameoLogSink
	{
		bool Enabled { get; }
	}

	/// <summary>The release default: no logger is installed.</summary>
	public sealed class NullCameoLogSink : ICameoLogSink
	{
		public static readonly NullCameoLogSink Instance = new();

		NullCameoLogSink() { }

		public bool Enabled => false;
	}
}
