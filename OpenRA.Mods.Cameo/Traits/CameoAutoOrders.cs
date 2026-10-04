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
using System.Collections.Generic;
using System.IO;
using OpenRA.Graphics;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	/// <summary>
	/// DEV-ONLY order injector for the TAKEOVER-SMOKE multi-client harness.
	/// Double-gated: inert unless BOTH `Cameo.DevAutoOrders=True` appears on the
	/// client command line AND `devautoorders.plan` exists in that client's support dir.
	/// Each plan line `TICK ORDER` issues `new Order(ORDER, localPlayerActor, false)`
	/// once — the exact order shape the pause menu sends for Surrender
	/// (IngameMenuLogic.cs:386). Registered on the world actor; a normal client always
	/// runs it inert. Only regular games are driven, never the shellmap.
	/// </summary>
	[TraitLocation(SystemActors.World)]
	[Desc("Dev-only timed order injector (TAKEOVER-SMOKE); double-gated by Cameo.DevAutoOrders + devautoorders.plan.")]
	public class CameoAutoOrdersInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new CameoAutoOrders(); }
	}

	public sealed class CameoAutoOrders : ITick, IWorldLoaded
	{
		const string PlanFile = "devautoorders.plan";

		readonly List<(int Tick, string OrderString)> orders = [];
		World world;
		bool armed;

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer worldRenderer)
		{
			armed = false;
			orders.Clear();

			if (w.Type != WorldType.Regular || !CameoDevArgs.IsEnabled("Cameo.DevAutoOrders"))
				return;

			var path = Path.Combine(Platform.SupportDir, PlanFile);
			if (!File.Exists(path))
				return;

			foreach (var line in File.ReadAllLines(path))
			{
				var text = line.Trim();
				if (text.Length == 0 || text.StartsWith('#'))
					continue;

				var split = text.IndexOf(' ');
				if (split > 0 && int.TryParse(text[..split], out var tick))
					orders.Add((tick, text[(split + 1)..].Trim()));
			}

			if (orders.Count == 0)
				return;

			world = w;
			armed = true;
			Log.Write("debug", $"CAMEO DEV AUTO-ORDERS ACTIVE - {orders.Count} timed order(s) scheduled");
		}

		void ITick.Tick(Actor self)
		{
			if (!armed || world.LocalPlayer == null || world.LocalPlayer.PlayerActor == null)
				return;

			for (var i = orders.Count - 1; i >= 0; i--)
			{
				if (world.WorldTick < orders[i].Tick)
					continue;

				world.IssueOrder(new Order(orders[i].OrderString, world.LocalPlayer.PlayerActor, false));
				Log.Write("debug", $"CAMEO DEV AUTO-ORDERS issued '{orders[i].OrderString}' at tick {world.WorldTick}");
				orders.RemoveAt(i);
			}

			if (orders.Count == 0)
				armed = false;
		}
	}
}
