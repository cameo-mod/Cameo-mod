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

namespace OpenRA.Mods.Cameo.Contracts
{
	/// <summary>
	/// Dependency-light contract assembly for the trait-unification programme
	/// (SPEC_2026-10-05_trait_unification §3.1). Holds interfaces, enums and value
	/// records shared between CA, Cameo, Fransbot and Unified implementations.
	/// Must reference only engine assemblies; it must never reference a gameplay
	/// implementation assembly. Defines no yaml-facing types and is therefore not
	/// listed in mod.yaml's Assemblies.
	/// </summary>
	public static class AssemblyDocs { }
}
