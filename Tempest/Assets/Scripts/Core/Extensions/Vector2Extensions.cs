using UnityEngine;

namespace Tempest.Core.Extensions
{
    public static class Vector2Extensions
    {
        public static bool IsBetween(this Vector2 a, float compare)
        {
            return a.x < compare && a.y > compare;
        }
    }
}
