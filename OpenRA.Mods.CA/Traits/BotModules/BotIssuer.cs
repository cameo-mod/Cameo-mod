#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// AR-8 / DESIGN §19.6: WHO emitted a bot order, at instance precision. Several identical module traits can sit on
	/// one PlayerActor (e.g. six SquadManagerBotModuleCA — one logical subsystem, six instances), so attribution uses
	/// `Type@ordinal`, the ordinal being the trait's position among its same-type siblings in trait order. Ownership
	/// stays type-scoped: lease `Owner` strings remain bare type names (`nameof(Module)`), and every comparison that
	/// asks "same owner?" goes through <see cref="TypeOf"/>. The ordinals live only in logs, counters and pair keys —
	/// never in a verdict — so they cannot desync: two hosts disagreeing on an ordinal would still refuse the same
	/// orders. Lives in Mods.CA so OpenRA.Mods.Fransbot (which cannot see Mods.Cameo) can use <see cref="IssueAs"/>.
	/// </summary>
	public static class BotIssuer
	{
		// Ambient emission scope. Bot order emission runs inside Sync.RunUnsynced on the host thread only, so a
		// plain static stack needs no thread safety. A provider that emits orders while running inside ANOTHER
		// module's call (FransTransport's capture-run provider called from EngineerBotModule's tick) wraps itself
		// in IssueAs so the orders charge to the provider — the module that actually owns the units — not the caller.
		static readonly Stack<string> Ambient = new();

		/// <summary>The ambient issuer override while inside an <see cref="IssueAs"/> scope (null = use the ticking module).</summary>
		public static string Current => Ambient.Count > 0 ? Ambient.Peek() : null;

		/// <summary>
		/// Charge orders emitted inside the returned scope to `issuer` instead of the module currently ticking.
		/// Nests safely: a provider called from its own tick re-pushes its own name. Dispose-order is a stack
		/// invariant — scopes are only ever used as `using` blocks.
		/// </summary>
		public static IDisposable IssueAs(string issuer)
		{
			Ambient.Push(issuer);
			return new AmbientScope();
		}

		sealed class AmbientScope : IDisposable
		{
			public void Dispose() => Ambient.Pop();
		}

		/// <summary>
		/// The instanced identity of one trait on a PlayerActor: `TypeName@N`, N = its position among same-type
		/// siblings in trait order (deterministic per match — trait order is construct order).
		/// </summary>
		public static string Of(object trait, Actor playerActor) =>
			OrdinalOf(trait, playerActor.TraitsImplementing<object>());

		/// <summary>
		/// The pure core of <see cref="Of"/>: `siblings` is the actor's trait enumeration (mixed types fine — only
		/// exact-type matches count). Returns `Type@?` when the trait is absent, a loud sentinel for a caller bug.
		/// </summary>
		public static string OrdinalOf(object trait, IEnumerable<object> siblings)
		{
			var type = trait.GetType();
			var index = 0;
			foreach (var sibling in siblings)
			{
				if (sibling.GetType() != type)
					continue;
				if (ReferenceEquals(sibling, trait))
					return string.Concat(type.Name, "@", index.ToString(System.Globalization.CultureInfo.InvariantCulture));
				index++;
			}

			return string.Concat(type.Name, "@?");
		}

		/// <summary>
		/// The owning subsystem of an issuer name: `Type@3` → `Type`, bare `Type` → `Type`, null → null.
		/// '@' cannot appear in a C# type name, so the first one always starts the ordinal. Use for every
		/// "same owner?" comparison — lease owners are never instanced, issuers always are.
		/// </summary>
		public static string TypeOf(string issuer)
		{
			var at = issuer?.IndexOf('@') ?? -1;
			return at < 0 ? issuer : issuer.Substring(0, at);
		}
	}
}
