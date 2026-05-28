using System;

namespace Tempest.Core.Helpers
{
    [Serializable]
    public struct WeightedOption<T>
    {
        public T Value;
        public float Weight;

        public WeightedOption(T value, float weight)
        {
            Value = value;
            Weight = weight;
        }
    }
}
