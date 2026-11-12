// IMPL02 MCV-SAFE-01: the seam between the expansion planner (OpenRA.Mods.Cameo) and the CG switch group in the squad manager (OpenRA.Mods.CA).
// The planner cannot name SquadManagerBotModuleCA's switch, and the Cameo assembly must not read Info fields directly (A9: every threshold is a
// named, Range-documented parameter that goes through the layered resolver).
namespace OpenRA.Mods.CA.Traits.BotModules
{
	public interface IBotMcvSafetyPolicy
	{
		/// <summary>True while the CG group is on (AggTeamEnabled) and the provider's trait is enabled; false means the planner behaves as FREEZE8.</summary>
		bool McvSafeEnabled { get; }

		/// <summary>The effective value of a named AggTeamMcv* parameter (Global x Personality x Faction x Matchup, clamped to its Range).</summary>
		int McvSafeParam(string name, Player enemy = null);
	}
}
