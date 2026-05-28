using UnityEngine;

namespace Tempest.Core.Sensors
{
    public class RaycastSensor
    {
        private RaycastHit _hit;
        private bool _isDetected;
        private readonly LayerMask _layers;
        private readonly float _distance;
        private readonly string _tagCompare;

        public RaycastSensor(float distance, LayerMask? layers = null, string tagCompare = null)
        {
            _layers = layers ?? Physics.DefaultRaycastLayers;
            _distance = distance;
            _tagCompare = tagCompare;
        }

        public bool IsDetected => _isDetected;
        public RaycastHit Hit => _hit;

        public void Monitor(Transform tr, Vector3 dir, bool debug = false)
        {
            if (debug)
                DebugRay(tr, dir);

            if (Physics.Raycast(tr.position, dir, out _hit, _distance, _layers))
            {
                _isDetected = _tagCompare != null
                    ? _hit.collider.CompareTag(_tagCompare)
                    : true;
            }
            else
            {
                _isDetected = false;
            }
        }

        public bool CheckRay(Transform tr, Vector3 dir, bool debug = false)
        {
            if (debug)
                DebugRay(tr, dir);

            return Physics.Raycast(tr.position, dir, out _hit, _distance, _layers);
        }

        public void DebugRay(Transform tr, Vector3 dir)
        {
            Debug.DrawRay(tr.position, dir * _distance, _isDetected ? Color.green : Color.red);
        }
    }
}
