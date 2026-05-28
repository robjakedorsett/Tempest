using UnityEngine;

namespace Tempest.Core.Extensions
{
    public static class Vector3Extensions
    {
        public static bool IsMoving(this Vector3 vector)
        {
            return vector != Vector3.zero;
        }

        public static bool IsMovingHorizontal(this Vector3 vector)
        {
            return vector.x != 0;
        }

        public static bool IsMovingVertical(this Vector3 vector)
        {
            return vector.z != 0;
        }
    }
}
