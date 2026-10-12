import re
from pathlib import Path
fleet=Path('C:/Users/AedisToru/Documents/GitHub/Cameo-mod-fleet')
def names(file):
 out=[]
 for line in (fleet/file).read_text(encoding='utf-8-sig').splitlines():
  cols=line.split('|')
  if len(cols)<5 or not cols[2].strip().startswith('`'):continue
  raw=cols[2].replace('`','').strip()
  for n in raw.split(','):
   n=n.strip()
   if n=='role_share_*':out += ['role_share_'+r for r in ('infantry','vehicle','air','naval','defence','artillery','other')]
   elif n=='air/naval/stealth_share_permille':out += [r+'_share_permille' for r in ('air','naval','stealth')]
   elif re.fullmatch('[a-z][a-z0-9_]*',n):out.append(n)
 return sorted(set(out))
mapfields=names('MAP_FEATURES_2026-10-06.md');oppfields=names('OPPONENT_FEATURES_2026-10-06.md')
print('Map fields',len(mapfields),'opponent fields',len(oppfields))
text='''using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenRA.Mods.CA.Traits.BotModules
{
	/// <summary>Read-only public map attributes. This interface does not select actions.</summary>
	public interface IBotMapFeatures { ContextFeatureVector MapFeatures { get; } }

	/// <summary>Transient opposing-seat observations within this world only, never a cross-match key.</summary>
	public interface IBotOpponentFeatures
	{
		bool TryObservedFeatures(int currentEnemySeat, out ContextFeatureVector features);
	}

	public enum ContextFeatureKind { Map, Opponent }

	/// <summary>Zero is measured zero; absent values have an explicit missing bit and no confidence.</summary>
	public readonly record struct ContextFeatureMeasurement(int Value, bool Present, int ConfidencePermille)
	{
		public static ContextFeatureMeasurement Missing => new(-1, false, 0);
		public static ContextFeatureMeasurement Observed(int value, int confidence = 1000) =>
			new(value, true, Math.Clamp(confidence, 0, 1000));
	}

	/// <summary>Immutable closed schema. No actor/player/map identifiers, names or coordinates can become fields.</summary>
	public sealed class ContextFeatureVector
	{
		public const int Version = 1;
		public const int MaximumSerializedBytes = 16 * 1024;
		public const int MaximumLearnedBytes = 12 * 1024 * 1024;
		static readonly ReadOnlyCollection<string> mapFields = Array.AsReadOnly(new[]
		{
MAP_FIELDS
		});
		static readonly ReadOnlyCollection<string> opponentFields = Array.AsReadOnly(new[]
		{
OPPONENT_FIELDS
		});

		public ContextFeatureKind Kind { get; }
		public IReadOnlyDictionary<string, ContextFeatureMeasurement> Fields { get; }
		public static IReadOnlyList<string> SchemaFields(ContextFeatureKind kind) => kind switch
		{
			ContextFeatureKind.Map => mapFields,
			ContextFeatureKind.Opponent => opponentFields,
			_ => throw new ArgumentOutOfRangeException(nameof(kind))
		};

		public ContextFeatureVector(ContextFeatureKind kind,
			IEnumerable<KeyValuePair<string, ContextFeatureMeasurement>> observations = null)
		{
			Kind = kind;
			var fields = new SortedDictionary<string, ContextFeatureMeasurement>(StringComparer.Ordinal);
			foreach (var name in SchemaFields(kind))
				fields.Add(name, ContextFeatureMeasurement.Missing);
			var assigned = new HashSet<string>(StringComparer.Ordinal);
			foreach (var pair in observations ?? [])
			{
				if (!fields.ContainsKey(pair.Key) || !assigned.Add(pair.Key))
					throw new ArgumentException("Unknown or duplicate context feature.", nameof(observations));
				if (pair.Value.ConfidencePermille < 0 || pair.Value.ConfidencePermille > 1000)
					throw new ArgumentException("Invalid feature confidence.", nameof(observations));
				fields[pair.Key] = pair.Value.Present ? pair.Value : ContextFeatureMeasurement.Missing;
			}

			Fields = new ReadOnlyDictionary<string, ContextFeatureMeasurement>(fields);
		}
	}
}
'''
text=text.replace('MAP_FIELDS','\n'.join('\t\t\t"'+n+'",' for n in mapfields)).replace('OPPONENT_FIELDS','\n'.join('\t\t\t"'+n+'",' for n in oppfields))
Path('OpenRA.Mods.CA/Traits/BotModules/IBotMapFeatures.cs').write_text(text,encoding='utf-8')
