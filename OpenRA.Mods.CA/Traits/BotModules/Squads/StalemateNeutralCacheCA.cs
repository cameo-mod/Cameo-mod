// BOTPASSIVE-FIX01 IMPL02 L2: persistence of the stalemate breaker's neutral blocker cache (actor id -> occupied cells).
// The cache is built once, lazily, at the first route query and only ever shrinks (design D8), so a straight-through game
// routes over the cells captured then. Persisting it makes a loaded game route over the same cells. No world access.
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	public static class StalemateNeutralCacheCA
	{
		/// <summary>One node per actor, in ascending id order; the value is the packed CPos bits of each occupied cell (layer included).</summary>
		public static List<MiniYamlNode> Save(IReadOnlyDictionary<uint, CPos[]> cache) =>
			cache.OrderBy(kv => kv.Key)
				.Select(kv => new MiniYamlNode(FieldSaver.FormatValue(kv.Key), FieldSaver.FormatValue(kv.Value.Select(c => c.Bits).ToArray())))
				.ToList();

		/// <summary>Restores a saved cache exactly. Malformed data throws InvalidDataException.</summary>
		public static Dictionary<uint, CPos[]> Load(MiniYaml data)
		{
			var result = new Dictionary<uint, CPos[]>();
			if (data == null)
				return result;

			foreach (var node in data.Nodes)
			{
				var bits = FieldLoader.GetValue<int[]>("StalemateNeutralCells", node.Value.Value);
				if (bits == null || !result.TryAdd(FieldLoader.GetValue<uint>("StalemateNeutralId", node.Key), bits.Select(b => new CPos(b)).ToArray()))
					throw new InvalidDataException("Malformed neutral blocker cache.");
			}

			return result;
		}
	}
}
