using UnityEngine;

namespace Tempest.Player.Helpers
{
    public static class MovementHelpers
    {
        public static Vector3 GetMovementDirectionForTransform(Transform transform, Vector3 input)
        {
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();
            return forward * input.z + right * input.x;
        }

        public static void MoveRigidbody(Rigidbody rb, Vector3 direction, float speed, float acceleration)
        {
            Vector3 desiredVelocity = direction.normalized * speed;
            desiredVelocity.y = rb.linearVelocity.y;

            rb.linearVelocity = Vector3.Lerp(
                rb.linearVelocity,
                desiredVelocity,
                acceleration * Time.fixedDeltaTime
            );
        }
    }
}
