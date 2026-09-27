#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 */
#endregion

using System.Collections.Generic;

	namespace OpenRA.Mods.CA.Traits
	{
		// Recon remains owned by ScoutBotModule; Secure is reserved for a later phase.
		public enum BotMissionType { Recon, Raid, Secure, Defend }

	public sealed class BotMission
	{
		public BotMissionType Type;
		public CPos Location;
		public Player TargetPlayer;
		public int RegionIndex;
		public int RequiredValue;
		public int Priority;
	}

	public interface IBotMissionProvider
	{
		IReadOnlyList<BotMission> Missions { get; }
		void MissionTaken(BotMission mission);
	}
}
