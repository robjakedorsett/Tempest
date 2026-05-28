using UnityEngine;

public class FollowGameObject : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private bool smoothFollow = false;

    private void Update()
    {
        if (target == null) return;

        if (smoothFollow)
        {
            transform.position = Vector3.Lerp(transform.position, target.position, Time.deltaTime * 5f);
            transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, Time.deltaTime * 5f);
        }
        else
        {
            transform.position = target.position;
            transform.rotation = target.rotation;
        }
    }
}
