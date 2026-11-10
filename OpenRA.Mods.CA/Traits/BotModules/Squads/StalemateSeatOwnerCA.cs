// BOTPASSIVE-FIX01 draft: seat-level ownership policy, not wired to a live manager.
using System;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>
	/// Serializes a seat's single live wave and two-phase manager handoff.
	/// Real lease operations and save/load are the caller's responsibility.
	/// </summary>
	public sealed class StalemateSeatOwnerCA
	{
		public StalemateBreakerWaveCA Wave { get; private set; }
		public string Manager { get; private set; }
		public string PendingManager { get; private set; }
		public long HandoffToken { get; private set; }

		public bool TryStart(string manager, StalemateBreakerWaveCA wave)
		{
			if (string.IsNullOrEmpty(manager) || wave == null || wave.End != StalemateWaveEndCA.None
				|| Wave != null || PendingManager != null)
				return false;
			Manager = manager;
			Wave = wave;
			return true;
		}

		public bool TryBeginHandoff(string source, string destination, out long token)
		{
			token = 0;
			if (Wave == null || Wave.End != StalemateWaveEndCA.None || PendingManager != null
				|| string.IsNullOrEmpty(source) || string.IsNullOrEmpty(destination)
				|| !string.Equals(source, Manager, StringComparison.Ordinal)
				|| string.Equals(source, destination, StringComparison.Ordinal) || HandoffToken == long.MaxValue)
				return false;
			PendingManager = destination;
			token = ++HandoffToken;
			return true;
		}

		/// <summary>Only commit after the caller atomically validates and transfers every lease.</summary>
		public bool FinishHandoff(long token, string destination, bool allClaimsTransferred, bool rollbackCompleted)
		{
			if (PendingManager == null || token != HandoffToken || !string.Equals(destination, PendingManager, StringComparison.Ordinal))
				return false;
			if (allClaimsTransferred == rollbackCompleted)
				return false;
			if (allClaimsTransferred)
				Manager = PendingManager;
			PendingManager = null;
			return allClaimsTransferred;
		}

		public bool MayIssueOrders(string manager) => Wave != null && Wave.End == StalemateWaveEndCA.None
			&& PendingManager == null && string.Equals(manager, Manager, StringComparison.Ordinal);

		public bool ReleaseEndedWave()
		{
			if (Wave == null || Wave.End == StalemateWaveEndCA.None || PendingManager != null)
				return false;
			Wave = null;
			Manager = null;
			return true;
		}
	}
}
