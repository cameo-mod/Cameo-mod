// BOTPASSIVE-FIX01: deterministic home-pressure hysteresis (design _02 latch; _06 §5.4 HomeLatch when the switch is on).
using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>
	/// Breaker-only home-pressure hysteresis. An immediate breach remains a separate,
	/// higher-priority input and must never wait for this latch.
	/// </summary>
	public sealed class StalemateHomePressureCA
	{
		public bool Active { get; private set; }
		public int FirstQualifyingTick { get; private set; } = -1;
		public int LastQualifyingTick { get; private set; } = -1;
		public int LastObservedTick { get; private set; } = -1;

		/// <summary>Out-of-order or incomplete observations invalidate admission.</summary>
		public bool Observe(int tick, bool complete, bool visibleHomeThreat)
		{
			if (!complete || tick < 0 || tick < LastObservedTick)
				return false;

			LastObservedTick = tick;
			if (visibleHomeThreat)
			{
				if (FirstQualifyingTick < 0)
					FirstQualifyingTick = tick;
				else if ((long)tick - FirstQualifyingTick >= 150)
					Active = true;
				LastQualifyingTick = tick;
			}
			else
			{
				FirstQualifyingTick = -1;
				if (Active && LastQualifyingTick >= 0 && (long)tick - LastQualifyingTick >= 500)
					Active = false;
			}

			return true;
		}

		/// <summary>Persisted only while the switch is on (design _06 §5.4, _08 §3).</summary>
		public List<MiniYamlNode> Save() => new()
		{
			new("Active", FieldSaver.FormatValue(Active)),
			new("FirstQualifyingTick", FieldSaver.FormatValue(FirstQualifyingTick)),
			new("LastQualifyingTick", FieldSaver.FormatValue(LastQualifyingTick)),
			new("LastObservedTick", FieldSaver.FormatValue(LastObservedTick))
		};

		public static StalemateHomePressureCA Load(MiniYaml data)
		{
			var latch = new StalemateHomePressureCA();
			if (data == null)
				return latch;
			var nodes = data.ToDictionary();
			if (nodes.TryGetValue("Active", out var active))
				latch.Active = FieldLoader.GetValue<bool>("Active", active.Value);
			if (nodes.TryGetValue("FirstQualifyingTick", out var first))
				latch.FirstQualifyingTick = FieldLoader.GetValue<int>("FirstQualifyingTick", first.Value);
			if (nodes.TryGetValue("LastQualifyingTick", out var last))
				latch.LastQualifyingTick = FieldLoader.GetValue<int>("LastQualifyingTick", last.Value);
			if (nodes.TryGetValue("LastObservedTick", out var observed))
				latch.LastObservedTick = FieldLoader.GetValue<int>("LastObservedTick", observed.Value);
			return latch;
		}
	}
}
