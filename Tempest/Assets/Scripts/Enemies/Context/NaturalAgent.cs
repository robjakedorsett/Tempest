using UnityEngine;
using UnityEngine.AI;

namespace Tempest.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NaturalAgent : MonoBehaviour
    {
        [SerializeField] private float minSpeed = 0.6f;
        [SerializeField] private float slowDownDistance = 2.5f;
        [SerializeField] private float turnSpeed = 540f;
        [SerializeField] private float arriveVelThreshold = 0.15f;

        private NavMeshAgent _agent;
        private float _speed;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.updateRotation = false;
            _agent.autoBraking = true;
            _agent.autoRepath = true;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            _agent.avoidancePriority = 50;
        }

        public void Initialize(float moveSpeed)
        {
            _speed = moveSpeed;
            _agent.speed = _speed;
        }

        private void Update()
        {
            Vector3 desired = _agent.desiredVelocity;
            desired.y = 0f;
            if (desired.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(desired);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }

            float remaining = _agent.remainingDistance;
            float t = Mathf.InverseLerp(0f, slowDownDistance, remaining);
            _agent.speed = Mathf.Lerp(minSpeed, _speed, t);

            if (!_agent.pathPending && remaining <= _agent.stoppingDistance)
            {
                if (_agent.velocity.magnitude <= arriveVelThreshold)
                    _agent.isStopped = true;
            }
            else if (_agent.isStopped)
            {
                _agent.isStopped = false;
            }
        }

        public void SetDestination(Vector3 worldPos)
        {
            _agent.isStopped = false;
            _agent.SetDestination(worldPos);
        }

        public bool IsAtDestination()
        {
            return !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance;
        }

        public void Stop()
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        public float GetSpeed()
        {
            return _agent.velocity.magnitude;
        }
    }
}
