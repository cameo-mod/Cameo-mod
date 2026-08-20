#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.AS.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Master teleports to position upon expiry.")]
	public class ShadeSlaveInfo : BaseSpawnerSlaveInfo
	{
		[Desc("DeathType that triggers the actor spawn. " +
			"Leave empty to spawn an actor ignoring the DeathTypes.")]
		public readonly string DeathType = null;

		public override object Create(ActorInitializer init) { return new ShadeSlave(this); }
	}

	public class ShadeSlave : BaseSpawnerSlave, INotifyKilled
	{
		ShadeMaster spawnerMaster;
		Actor master;
		public readonly ShadeSlaveInfo Info;

		public ShadeSlave(ShadeSlaveInfo info)
			: base(info)
		{
			Info = info;
		}

		public override void LinkMaster(Actor self, Actor master, BaseSpawnerMaster spawnerMaster)
		{
			base.LinkMaster(self, master, spawnerMaster);
			this.master = master;
			this.spawnerMaster = spawnerMaster as ShadeMaster;
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (!self.IsInWorld)
				return;

			if (Info.DeathType != null && !e.Damage.DamageTypes.Contains(Info.DeathType))
				return;

			if (!master.IsInWorld || master.IsDead || master == null)
				return;

			spawnerMaster.TeleportToSlave(master, self);
		}
	}
}
