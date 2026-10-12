from pathlib import Path
r=Path(r'C:/cameo-wt/codex-context-providers')
p=r/'OpenRA.Mods.Cameo/Traits/BotModules/ContextFeatureEncoding.cs'
s=p.read_text().replace('using System.IO;', 'using System.IO;\nusing System.Linq;')
s=s.replace('public static int Slope(IReadOnlyList<(int Tick, int Value)> samples)\n\t\t{', '''public static int Slope(IReadOnlyList<(int Tick, int Value)> samples) =>
			Slope(samples.Select(s => (s.Tick, (long)s.Value)).ToArray());

		public static int Slope(IReadOnlyList<(int Tick, long Value)> samples)
		{
			var rate = SlopeRatio(samples);
			return Saturate(rate.Numerator / rate.Denominator);
		}

		public static (BigInteger Numerator, BigInteger Denominator) SlopeRatio(IReadOnlyList<(int Tick, long Value)> samples)
		{''')
s=s.replace('return denominator == 0 ? 0 : Saturate((samples.Count * xy - x * y) * 1000 / denominator);', 'return denominator == 0 ? (0, 1) : ((samples.Count * xy - x * y) * 1000, denominator);')
p.write_text(s)
p=r/'OpenRA.Mods.Cameo/Traits/BotModules/OpponentFeatureWindow.cs'
s=p.read_text().replace('public int ArmyValue { get; }','public long ArmyValue { get; }').replace('ArmyValue = ContextFeatureEncoding.Saturate(RoleValueTotal);', 'ArmyValue = RoleValueTotal;').replace('var firstRate = ContextFeatureEncoding.Slope(', 'var firstRate = ContextFeatureEncoding.SlopeRatio(').replace('var secondRate = ContextFeatureEncoding.Slope(', 'var secondRate = ContextFeatureEncoding.SlopeRatio(')
s=s.replace('ContextFeatureEncoding.Saturate(((System.Numerics.BigInteger)secondRate - firstRate) * 1000 / span)', 'ContextFeatureEncoding.Saturate((secondRate.Numerator * firstRate.Denominator - firstRate.Numerator * secondRate.Denominator) * 1000 /\n\t\t\t\t\t\t(firstRate.Denominator * secondRate.Denominator * span))')
p.write_text(s)
