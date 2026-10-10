using System;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	public static class M13LaunchPolicyCA
	{
		// Preserve legacy integer arithmetic exactly while off. M13's count valve is
		// the configured absolute limit, independent of the provider's value target.
		public static int MaxIdleUnits(bool enabled, int configured, int baseValue, int targetValue) =>
			enabled ? configured : Math.Max(1, (int)((long)configured * targetValue / baseValue));
	}
}
