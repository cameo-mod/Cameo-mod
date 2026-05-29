using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using OpenRA;

namespace OpenRA.Mods.Common.Traits
{
	sealed class PhysicalStateShieldManager
	{
		readonly World world;
		readonly Dictionary<string, HashSet<ProvidesShieldFromPhysicalState>> providersByState = new(StringComparer.OrdinalIgnoreCase);

		static readonly ConditionalWeakTable<World, PhysicalStateShieldManager> Instances = new();

		PhysicalStateShieldManager(World world)
		{
			this.world = world;
		}

		public static PhysicalStateShieldManager ForWorld(World world)
		{
			if (world == null)
				throw new ArgumentNullException(nameof(world));

			return Instances.GetValue(world, w => new PhysicalStateShieldManager(w));
		}

		public void RegisterProvider(ProvidesShieldFromPhysicalState provider)
		{
			if (provider == null)
				return;

			var stateName = provider.Info.PhysicalStateName;
			if (string.IsNullOrEmpty(stateName))
				return;

			if (!providersByState.TryGetValue(stateName, out var set))
			{
				set = new HashSet<ProvidesShieldFromPhysicalState>();
				providersByState.Add(stateName, set);
			}

			set.Add(provider);
		}

		public void UnregisterProvider(ProvidesShieldFromPhysicalState provider)
		{
			if (provider == null)
				return;

			var stateName = provider.Info.PhysicalStateName;
			if (string.IsNullOrEmpty(stateName))
				return;

			if (!providersByState.TryGetValue(stateName, out var set))
				return;

			set.Remove(provider);
			if (set.Count == 0)
				providersByState.Remove(stateName);
		}

		public int AbsorbDamage(Actor victim, Actor attacker, int damageAmount, string projectileType, string stateName, ProvidesShieldFromPhysicalState preferredProvider)
		{
			if (victim == null || damageAmount <= 0 || string.IsNullOrEmpty(stateName))
				return damageAmount;

			if (!providersByState.TryGetValue(stateName, out var set) || set.Count == 0)
				return damageAmount;

			var networks = CollectNetworks(victim, stateName, preferredProvider);
			if (networks.Count == 0)
				return damageAmount;

			var remaining = damageAmount;
			foreach (var network in networks)
			{
				remaining = DrainNetwork(network, victim, attacker, projectileType, remaining);
				if (remaining <= 0)
					break;
			}

			if (remaining == damageAmount)
				return damageAmount;

			return remaining;
		}

		public ShieldStatus GetShieldStatus(Actor victim, string stateName)
		{
			if (victim == null || string.IsNullOrEmpty(stateName))
				return default;

			if (!providersByState.TryGetValue(stateName, out var set) || set.Count == 0)
				return default;

			var networks = CollectNetworks(victim, stateName, null);
			if (networks.Count == 0)
				return default;

			var current = 0;
			var maximum = 0;

			foreach (var network in networks)
			{
				foreach (var provider in network)
				{
					if (!provider.CanShareWith(victim.Owner))
						continue;

					current += provider.GetAvailableShield();
					maximum += provider.GetMaximumShield();
				}
			}

			return new ShieldStatus(current, maximum);
		}

		List<List<ProvidesShieldFromPhysicalState>> CollectNetworks(Actor victim, string stateName, ProvidesShieldFromPhysicalState preferred)
		{
			var result = new List<List<ProvidesShieldFromPhysicalState>>();

			if (!providersByState.TryGetValue(stateName, out var providerSet) || providerSet.Count == 0)
				return result;

			var owner = victim.Owner;
			var candidates = providerSet.Where(p => p != null && p.CanShareWith(owner)).ToList();
			if (candidates.Count == 0)
				return result;
			var zeroRangeProviders = candidates.Where(p => p.Info.ShareRange <= WDist.Zero).ToList();
			if (zeroRangeProviders.Count > 0)
			{
				foreach (var provider in zeroRangeProviders)
				{
					if (!provider.CanProtectActor(victim))
						continue;
					result.Add(new List<ProvidesShieldFromPhysicalState> { provider });
				}
			}
			var shareCandidates = candidates.Where(p => p.Info.ShareRange > WDist.Zero).ToList();
			if (shareCandidates.Count == 0)
				return result;
			var visited = new HashSet<ProvidesShieldFromPhysicalState>();
			if (preferred != null && preferred.Info.ShareRange > WDist.Zero && shareCandidates.Contains(preferred) && preferred.CanShareWith(owner))
			{
				var network = BuildNetworkComponent(preferred, owner, shareCandidates, visited);
				if (NetworkCoversActor(network, victim))
					result.Add(network);
			}
			foreach (var provider in shareCandidates)
			{
				if (visited.Contains(provider))
					continue;
				if (!provider.CanProtectActor(victim))
					continue;
				var network = BuildNetworkComponent(provider, owner, shareCandidates, visited);
				if (NetworkCoversActor(network, victim))
					result.Add(network);
			}
			if (result.Count <= 1)
				return result;

			result = result
				.OrderBy(n => preferred != null && n.Contains(preferred) ? 0 : 1)
				.ThenBy(n => n.Min(p => p.Actor.ActorID))
				.ToList();

			return result;
		}

		static List<ProvidesShieldFromPhysicalState> BuildNetworkComponent(ProvidesShieldFromPhysicalState start, Player owner, List<ProvidesShieldFromPhysicalState> candidates, HashSet<ProvidesShieldFromPhysicalState> visited)
		{
			var component = new List<ProvidesShieldFromPhysicalState>();
			if (start == null)
				return component;

			var queue = new Queue<ProvidesShieldFromPhysicalState>();
			queue.Enqueue(start);
			visited.Add(start);

			while (queue.Count > 0)
			{
				var current = queue.Dequeue();
				component.Add(current);

				foreach (var candidate in candidates)
				{
					if (visited.Contains(candidate))
						continue;

					if (!current.CanLinkWith(candidate, owner))
						continue;

					visited.Add(candidate);
					queue.Enqueue(candidate);
				}
			}

			return component;
		}

		static bool NetworkCoversActor(List<ProvidesShieldFromPhysicalState> network, Actor victim)
		{
			return network.Any(p => p.CanProtectActor(victim));
		}

		static int DrainNetwork(List<ProvidesShieldFromPhysicalState> network, Actor victim, Actor attacker, string projectileType, int incomingDamage)
		{
			
			var providers = network
				.Where(p => p.AllowsProjectile(projectileType) && p.GetAvailableShield() > 0 && p.CanShareWith(victim.Owner))
				.Select(p => new ProviderRuntime(p))
				.ToList();

			if (providers.Count == 0)
				return incomingDamage;

			var remaining = incomingDamage;

			while (remaining > 0)
			{
				var active = providers.Where(p => p.Available > 0).ToList();
				if (active.Count == 0)
					break;

				var share = Math.Max(1, remaining / active.Count);
				var distributed = 0;

				foreach (var info in active)
				{
					if (remaining <= 0)
						break;

					var take = Math.Min(share, info.Available);
					if (take <= 0)
						continue;

					info.Available -= take;
					info.Consumed += take;
					remaining -= take;
					distributed += take;
				}

				if (distributed == 0)
				{
					foreach (var info in active)
					{
						if (remaining <= 0)
							break;

						if (info.Available <= 0)
							continue;

						info.Available--;
						info.Consumed++;
						remaining--;
					}
				}
			}

			foreach (var info in providers)
			{
				if (info.Consumed <= 0)
					continue;

				info.Provider.DrainShield(info.Consumed, attacker);
			}

			return remaining;
		}

		sealed class ProviderRuntime
		{
			public ProviderRuntime(ProvidesShieldFromPhysicalState provider)
			{
				Provider = provider;
				Available = provider.GetAvailableShield();
				Consumed = 0;
			}

			public ProvidesShieldFromPhysicalState Provider { get; }
			public int Available { get; set; }
			public int Consumed { get; set; }
		}

		public readonly struct ShieldStatus
		{
			public ShieldStatus(int current, int maximum)
			{
				Current = current;
				Maximum = maximum;
			}

			public int Current { get; }
			public int Maximum { get; }
			public bool HasShield => Current > 0;
		}
	}
}
