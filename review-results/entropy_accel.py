from pathlib import Path
r=Path(r'C:/cameo-wt/codex-context-providers')
p=r/'OpenRA.Mods.Cameo/Traits/BotModules/ContextFeatureEncoding.cs'
s=p.read_text().replace('public static BigInteger IntegerSquareRoot', '''// Binary logarithm at Q20 precision, using only exact integer intermediates.
		static BigInteger Log2Ratio(long numerator, long denominator)
		{
			var unit = BigInteger.One << 32;
			var value = (BigInteger)numerator * unit / denominator;
			BigInteger logarithm = 0;
			while (value >= 2 * unit)
			{
				value >>= 1;
				logarithm += BigInteger.One << 20;
			}

			for (var bit = 19; bit >= 0; bit--)
			{
				value = value * value / unit;
				if (value >= 2 * unit)
				{
					value >>= 1;
					logarithm += BigInteger.One << bit;
				}
			}

			return logarithm;
		}

		public static int Entropy(IReadOnlyList<int> values)
		{
			long total = 0;
			foreach (var value in values)
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(values));
				total += value;
			}

			if (total == 0 || values.Count <= 1)
				return 0;
			BigInteger weightedLog = 0;
			foreach (var value in values)
				if (value > 0)
					weightedLog += value * Log2Ratio(total, value);
			return Math.Clamp(Saturate(weightedLog * 1000 / (total * Log2Ratio(values.Count, 1))), 0, 1000);
		}

		public static BigInteger IntegerSquareRoot''')
p.write_text(s)
p=r/'OpenRA.Mods.Cameo/Traits/BotModules/OpponentFeatureWindow.cs'
s=p.read_text().replace('Set("army_value_slope", ContextFeatureEncoding.Slope(recent.Select(s => (s.Tick, s.ArmyValue)).ToArray()));', '''Set("army_value_slope", ContextFeatureEncoding.Slope(recent.Select(s => (s.Tick, s.ArmyValue)).ToArray()));
				var midpoint = ((long)recent[0].Tick + recent[^1].Tick) / 2;
				var firstHalf = recent.Where(s => s.Tick <= midpoint).ToArray();
				var secondHalf = recent.Where(s => s.Tick > midpoint).ToArray();
				if (firstHalf.Length >= 2 && secondHalf.Length >= 2 &&
					firstHalf[^1].Tick - firstHalf[0].Tick >= SampleInterval &&
					secondHalf[^1].Tick - secondHalf[0].Tick >= SampleInterval)
				{
					var firstRate = ContextFeatureEncoding.Slope(firstHalf.Select(s => (s.Tick, s.ArmyValue)).ToArray());
					var secondRate = ContextFeatureEncoding.Slope(secondHalf.Select(s => (s.Tick, s.ArmyValue)).ToArray());
					var span = ((long)secondHalf[0].Tick + secondHalf[^1].Tick - firstHalf[0].Tick - firstHalf[^1].Tick) / 2;
					Set("army_value_acceleration", ContextFeatureEncoding.Saturate(((System.Numerics.BigInteger)secondRate - firstRate) * 1000 / span));
				}''').replace('Set("military_to_econ_permille",', 'Set("role_entropy_permille", ContextFeatureEncoding.Entropy(last.RememberedRoleValues));\n\t\t\t\tSet("military_to_econ_permille",')
p.write_text(s)
p=r/'OpenRA.Mods.Cameo.Test/ContextFeaturesTest.cs'
s=p.read_text();i=s.rfind('\t}\n}')
s=s[:i]+'''		[Test]
		public void EntropyIsTrueNormalizedIntegerEntropy()
		{
			Assert.That(ContextFeatureEncoding.Entropy(new[] { 1, 1, 1, 1, 1, 1, 1 }), Is.EqualTo(1000));
			Assert.That(ContextFeatureEncoding.Entropy(new[] { 5, 0, 0, 0, 0, 0, 0 }), Is.Zero);
			Assert.That(ContextFeatureEncoding.Entropy(new[] { 1, 1, 0, 0, 0, 0, 0 }), Is.EqualTo(356));
			Assert.That(ContextFeatureEncoding.Entropy(new[] { int.MaxValue, int.MaxValue, 0, 0, 0, 0, 0 }), Is.EqualTo(356));
			Assert.Throws<ArgumentOutOfRangeException>(() => ContextFeatureEncoding.Entropy(new[] { -1, 1 }));
		}

		[Test]
		public void AccelerationRequiresTwoSeparate750TickWindows()
		{
			var window = new OpponentFeatureWindow();
			window.Add(Census(0, 100));
			window.Add(Census(750, 850));
			Assert.That(window.Features(750).Fields["army_value_acceleration"].Present, Is.False);
			window.Add(Census(1500, 1600));
			window.Add(Census(2250, 2350));
			Assert.That(window.Features(2250).Fields["army_value_acceleration"].Value, Is.Zero);
		}
''' +s[i:]
p.write_text(s)
