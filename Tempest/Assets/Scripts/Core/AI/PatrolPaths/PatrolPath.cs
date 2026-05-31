using System.Collections.Generic;
using UnityEngine;

namespace Tempest.AI
{
    public class PatrolPath : MonoBehaviour
    {
        public List<Transform> Waypoints = new();
        public PatrolMode Mode = PatrolMode.Loop;

        private int _currentIndex;
        private int _direction = 1;

        public Transform CurrentWaypoint =>
            Waypoints.Count > 0 ? Waypoints[_currentIndex] : null;

        public void ResetPath()
        {
            _currentIndex = 0;
            _direction = 1;
        }

        public Transform AdvanceWaypoint()
        {
            if (Waypoints.Count == 0) return null;

            switch (Mode)
            {
                case PatrolMode.Loop:
                    _currentIndex = (_currentIndex + 1) % Waypoints.Count;
                    break;

                case PatrolMode.PingPong:
                    _currentIndex += _direction;
                    if (_currentIndex >= Waypoints.Count - 1 || _currentIndex <= 0)
                        _direction *= -1;
                    _currentIndex = Mathf.Clamp(_currentIndex, 0, Waypoints.Count - 1);
                    break;

                case PatrolMode.Random:
                    int prev = _currentIndex;
                    if (Waypoints.Count > 1)
                    {
                        do { _currentIndex = Random.Range(0, Waypoints.Count); }
                        while (_currentIndex == prev);
                    }
                    break;
            }

            return Waypoints[_currentIndex];
        }

        private void Reset()
        {
            Waypoints.Clear();
            foreach (Transform child in transform)
                Waypoints.Add(child);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Waypoints == null || Waypoints.Count == 0) return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < Waypoints.Count; i++)
            {
                Transform wp = Waypoints[i];
                if (wp == null) continue;

                Gizmos.DrawSphere(wp.position, 0.25f);

                if (i < Waypoints.Count - 1)
                {
                    Transform next = Waypoints[i + 1];
                    if (next != null)
                        Gizmos.DrawLine(wp.position, next.position);
                }
                else if (Mode == PatrolMode.Loop)
                {
                    Transform first = Waypoints[0];
                    if (first != null)
                        Gizmos.DrawLine(wp.position, first.position);
                }
            }
        }
#endif
    }
}
