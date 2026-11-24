#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Dev
{
	// INFANTRY-REJECT-LOG-01 c2 (DEV only): the infantry_reject.csv rows get a writer of their own. The context_features writer flushes only when
	// that condition is granted, so these rows were never written (V05). This trait flushes every FlushTicks and once more at GameOver.
	[TraitLocation(SystemActors.World)]
	[Desc("Development-only infantry_reject.csv writer. Independent of context_features.")]
	public sealed class InfantryRejectCsvWriterDevInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new InfantryRejectCsvWriterDev();
	}

	public sealed class InfantryRejectCsvWriterDev : ITick, IGameOver
	{
		public const int FlushTicks = 750;

		readonly ContextLogWriter writer = new();
		int nextFlushTick;

		public void Enqueue(int playerIndex, string fileName, string row, int tick) => writer.Enqueue(playerIndex, fileName, row, tick);

		void ITick.Tick(Actor self)
		{
			if (self.World.WorldTick < nextFlushTick)
				return;

			nextFlushTick = self.World.WorldTick + FlushTicks;
			FlushAll(self.World, false);
		}

		void IGameOver.GameOver(World world) => FlushAll(world, true);

		void FlushAll(World world, bool finalRecord)
		{
			for (var i = 0; i < world.Players.Length; i++)
				writer.Flush(i, world.WorldTick, finalRecord);
		}
	}
}
