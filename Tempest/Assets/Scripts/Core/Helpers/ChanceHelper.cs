using System;
using System.Collections.Generic;
using System.Linq;

namespace Tempest.Core.Helpers
{
    public static class ChanceHelper
    {
        private static readonly Random random = new();

        public static bool FlipCoin()
        {
            return random.NextDouble() < 0.5;
        }

        public static int Roll(int sides = 6)
        {
            if (sides <= 0)
                throw new ArgumentOutOfRangeException(nameof(sides), "Must be greater than 0");
            return random.Next(1, sides + 1);
        }

        public static bool Chance(float percent)
        {
            return random.NextDouble() < (percent / 100f);
        }

        public static T WeightedChoice<T>(List<WeightedOption<T>> options)
        {
            float totalWeight = options.Sum(opt => opt.Weight);
            float roll = (float)(random.NextDouble() * totalWeight);

            float cumulative = 0f;
            foreach (var option in options)
            {
                cumulative += option.Weight;
                if (roll < cumulative)
                    return option.Value;
            }

            return options.Last().Value;
        }
    }
}
