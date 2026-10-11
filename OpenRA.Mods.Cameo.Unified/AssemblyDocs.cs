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

namespace OpenRA.Mods.Cameo
{
	/// <summary>
	/// Selective unified assembly (SPEC_2026-10-05_trait_unification §3): migrated
	/// families move here under their plain names so they win ObjectCreator
	/// resolution before AS/CA (this dll precedes AS in mod.yaml's Assemblies).
	/// Depends on Contracts and the engine libraries (a transitional AS/CA base
	/// reference is allowed because neither can depend back). Never depends on
	/// OpenRA.Mods.Cameo. Until a family migrates this assembly defines nothing
	/// yaml-facing, so precedence is a no-op.
	/// </summary>
	public static class AssemblyDocs { }
}
