using UnityEngine;

namespace Tempest.Spawning
{
    public class RiftVfx : MonoBehaviour
    {
        [SerializeField] private ParticleSystem groundCrack;
        [SerializeField] private ParticleSystem risingEnergy;
        [SerializeField] private ParticleSystem ambientEmbers;
        [SerializeField] private Light riftLight;

        [Header("Light Pulse")]
        [SerializeField] private float baseLightIntensity = 2f;
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pulseAmplitude = 0.5f;

        private float _elapsed;

        private void Update()
        {
            if (riftLight == null) return;
            _elapsed += Time.deltaTime;
            riftLight.intensity = baseLightIntensity + Mathf.Sin(_elapsed * pulseSpeed) * pulseAmplitude;
        }

        private void OnDestroy()
        {
            if (groundCrack != null) groundCrack.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (risingEnergy != null) risingEnergy.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (ambientEmbers != null) ambientEmbers.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
