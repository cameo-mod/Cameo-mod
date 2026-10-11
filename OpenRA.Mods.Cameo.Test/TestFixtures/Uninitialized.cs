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

using System.Runtime.CompilerServices;

namespace OpenRA.Mods.Cameo.Test.TestFixtures
{
	/// <summary>
	/// Identity-only engine objects: uninitialized instances whose constructors
	/// never ran, for seams that only store or compare references (owners in
	/// ledgers, mission regions, scout release). Never call members on them —
	/// they carry no world or trait state.
	/// </summary>
	public static class Uninitialized
	{
		public static T Of<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

		public static Player Player() => Of<Player>();

		public static Actor Actor() => Of<Actor>();
	}
}
