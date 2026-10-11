using System;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	public enum PersonalityCandidateReason { Candidate, LegacyEmergency, TerminalPressure }
	public readonly record struct PersonalityChoice(string Personality, PersonalityCandidateReason Reason);
	public readonly record struct TurtleFallbackDecision(string Personality, string Reason, long ExpiresTick);

	// One controller per player; pressure pulses cannot extend an original hold.
	public sealed class TurtleFallbackPolicy
	{
		long expires = -1;
		int lastTick = -1;
		bool invalid;
		public void Invalidate() => invalid = true;
		public TurtleFallbackDecision Select(int tick, bool enabled, PersonalityChoice choice,
			string incumbent, bool legalOffensiveIncumbent, bool baseDanger, int holdTicks)
		{
			if (!enabled) return new(choice.Personality, "Off", -1);
			if (tick < 0 || tick < lastTick) throw new ArgumentOutOfRangeException(nameof(tick));
			lastTick = tick;
			if (invalid || holdTicks <= 0) return new(choice.Personality, "Unsupported", expires);
			if (choice.Reason != PersonalityCandidateReason.TerminalPressure || choice.Personality != "turtle")
			{
				expires = -1;
				return new(choice.Personality, choice.Reason.ToString(), -1);
			}
			if (baseDanger) return new(choice.Personality, "BaseDanger", expires);
			if (!legalOffensiveIncumbent)
			{
				expires = -1;
				return new(choice.Personality, "Ineligible", -1);
			}
			if (expires < 0) expires = (long)tick + holdTicks;
			return tick < expires ? new(incumbent, "Held", expires) : new(choice.Personality, "Expired", expires);
		}
	}
}
