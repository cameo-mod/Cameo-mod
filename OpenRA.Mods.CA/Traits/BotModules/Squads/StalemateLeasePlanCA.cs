// BOTPASSIVE-FIX01 draft: atomic lease admission; no live manager caller is wired yet.
using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>
	/// Claim only unowned roster members, preserve existing same-owner claims, and
	/// roll back exactly newly acquired claims on rejection. No orders are issued.
	/// </summary>
	public static class StalemateLeasePlanCA
	{
		public static bool TryClaimAll(IBotUnitLeases leases, IEnumerable<Actor> roster,
			string owner, int duration, Func<Actor, bool> isValid, out List<Actor> acquired)
		{
			acquired = new List<Actor>();
			if (leases == null || roster == null || string.IsNullOrEmpty(owner) || duration <= 0 || isValid == null)
				return false;

			try
			{
				var actors = roster.ToList();
				if (actors.Count == 0 || actors.Any(a => a == null || a.ActorID == 0 || !isValid(a))
					|| actors.Select(a => a.ActorID).Distinct().Count() != actors.Count)
					return false;

				actors.Sort((a, b) => a.ActorID.CompareTo(b.ActorID));
				foreach (var actor in actors)
				{
					var existing = leases.LeaseOf(actor);
					if (existing != null)
					{
						if (existing.Value.Owner != owner || existing.Value.Purpose != BotLeasePurpose.Squad)
						{
							Rollback(leases, owner, acquired);
							return false;
						}

						continue;
					}

					// Track before calling: a provider can throw after mutating its table.
					acquired.Add(actor);
					if (!leases.TryClaim(actor, owner, BotLeasePurpose.Squad, duration))
					{
						Rollback(leases, owner, acquired);
						return false;
					}
				}

				// A claim may have been preempted or an actor invalidated during admission.
				foreach (var actor in actors)
				{
					var lease = leases.LeaseOf(actor);
					if (!isValid(actor) || lease == null || lease.Value.Owner != owner || lease.Value.Purpose != BotLeasePurpose.Squad)
					{
						Rollback(leases, owner, acquired);
						return false;
					}
				}

				return true;
			}
			catch (Exception)
			{
				Rollback(leases, owner, acquired);
				return false;
			}
		}

		/// <summary>Release only claims this plan acquired, never a pre-existing lease.</summary>
		public static bool Rollback(IBotUnitLeases leases, string owner, List<Actor> acquired)
		{
			for (var i = acquired.Count - 1; i >= 0; i--)
			{
				try
				{
					leases.Release(acquired[i], owner);
					var remaining = leases.LeaseOf(acquired[i]);
					if (remaining == null || remaining.Value.Owner != owner)
						acquired.RemoveAt(i);
				}
				catch (Exception)
				{
					// Retain unresolved identities so the caller can HOLD safely.
				}
			}
			return acquired.Count == 0;
		}
	}
}
