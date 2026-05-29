using Tempest.Weapons;
using UnityEngine;

public class WeaponFeedbackController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private WeaponVisualController weaponVisualController;
    [SerializeField] private PlayerCameraController cameraController;
    [SerializeField] private AudioSource weaponAudioSource;
    [SerializeField] private AudioSource impactAudioSource;

    [Header("Screen Shake — Fire")]
    [SerializeField] private float fireShakeIntensity = 0.015f;
    [SerializeField] private float fireShakeDuration = 0.06f;

    [Header("Screen Shake — Hit")]
    [SerializeField] private float hitShakeIntensity = 0.04f;
    [SerializeField] private float hitShakeDuration = 0.1f;

    [Header("Screen Shake — Kill")]
    [SerializeField] private float killShakeIntensity = 0.08f;
    [SerializeField] private float killShakeDuration = 0.2f;

    [Header("Kill Camera Punch")]
    [SerializeField] private float killPunchAngle = 3f;
    [SerializeField] private float killPunchDuration = 0.15f;

    [Header("Muzzle Flash Light")]
    [SerializeField] private float muzzleLightIntensity = 8f;
    [SerializeField] private float muzzleLightDuration = 0.08f;
    [SerializeField] private Color muzzleLightColor = new(1f, 0.8f, 0.3f);

    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip killSound;
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    [Header("Kill VFX")]
    [SerializeField] private GameObject killEffectPrefab;

    private float _shakeIntensity;
    private float _shakeDuration;
    private float _shakeTimer;
    private float _punchTimer;
    private Vector3 _shakeOffset;

    private void OnEnable()
    {
        if (weaponController == null) return;
        weaponController.OnWeaponFired += HandleWeaponFired;
        weaponController.OnHitConfirmed += HandleHitConfirmed;
        weaponController.OnKillConfirmed += HandleKillConfirmed;
    }

    private void OnDisable()
    {
        if (weaponController == null) return;
        weaponController.OnWeaponFired -= HandleWeaponFired;
        weaponController.OnHitConfirmed -= HandleHitConfirmed;
        weaponController.OnKillConfirmed -= HandleKillConfirmed;
    }

    private void Update()
    {
        UpdateShake();
        UpdatePunch();
    }

    private void UpdateShake()
    {
        cameraHolder.localPosition -= _shakeOffset;

        if (_shakeTimer > 0f)
        {
            _shakeTimer -= Time.deltaTime;
            float t = _shakeDuration > 0f ? Mathf.Clamp01(_shakeTimer / _shakeDuration) : 0f;
            Vector2 random = Random.insideUnitCircle * (_shakeIntensity * t);
            _shakeOffset = new Vector3(random.x, random.y, 0f);
        }
        else
        {
            _shakeOffset = Vector3.zero;
        }

        cameraHolder.localPosition += _shakeOffset;
    }

    private void UpdatePunch()
    {
        if (_punchTimer > 0f)
        {
            _punchTimer -= Time.deltaTime;
            float t = killPunchDuration > 0f ? Mathf.Clamp01(_punchTimer / killPunchDuration) : 0f;
            cameraController.PitchOffset = -killPunchAngle * t;
        }
        else
        {
            cameraController.PitchOffset = 0f;
        }
    }

    private void Shake(float intensity, float duration)
    {
        _shakeIntensity = intensity;
        _shakeDuration = duration;
        _shakeTimer = duration;
    }

    private void HandleWeaponFired()
    {
        Shake(fireShakeIntensity, fireShakeDuration);
        SpawnMuzzleFlash();
        PlayFireSound();
    }

    private void HandleHitConfirmed(Vector3 hitPoint, Vector3 hitNormal)
    {
        Shake(hitShakeIntensity, hitShakeDuration);
        SpawnHitEffect(hitPoint, hitNormal);
        PlaySound(impactAudioSource, hitSound);
    }

    private void HandleKillConfirmed(Vector3 hitPoint, Vector3 hitNormal)
    {
        Shake(killShakeIntensity, killShakeDuration);
        _punchTimer = killPunchDuration;
        SpawnHitEffect(hitPoint, hitNormal);
        SpawnKillEffect(hitPoint, hitNormal);
        PlaySound(impactAudioSource, killSound);
    }

    private void SpawnMuzzleFlash()
    {
        var model = weaponVisualController != null ? weaponVisualController.ActiveModel : null;
        if (model == null || model.MuzzlePoint == null) return;

        var weapon = weaponController.CurrentWeapon;
        if (weapon != null && weapon.muzzleFlashPrefab != null)
            Instantiate(weapon.muzzleFlashPrefab, model.MuzzlePoint.position, model.MuzzlePoint.rotation, model.MuzzlePoint);

        SpawnMuzzleLight(model.MuzzlePoint);
    }

    private void SpawnMuzzleLight(Transform muzzlePoint)
    {
        var lightObj = new GameObject("MuzzleLight");
        lightObj.transform.position = muzzlePoint.position;
        var pointLight = lightObj.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.intensity = muzzleLightIntensity;
        pointLight.color = muzzleLightColor;
        pointLight.range = 5f;
        Destroy(lightObj, muzzleLightDuration);
    }

    private void SpawnHitEffect(Vector3 hitPoint, Vector3 hitNormal)
    {
        var weapon = weaponController.CurrentWeapon;
        if (weapon == null || weapon.hitEffectPrefab == null) return;
        var hit = Instantiate(weapon.hitEffectPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
        Destroy(hit, 1f);
    }

    private void SpawnKillEffect(Vector3 hitPoint, Vector3 hitNormal)
    {
        var weapon = weaponController.CurrentWeapon;
        var prefab = weapon != null && weapon.killEffectPrefab != null
            ? weapon.killEffectPrefab
            : killEffectPrefab;
        if (prefab == null) return;
        var kill = Instantiate(prefab, hitPoint, Quaternion.LookRotation(hitNormal));
        Destroy(kill, 1.5f);
    }

    private void PlayFireSound()
    {
        var weapon = weaponController.CurrentWeapon;
        if (weapon == null || weapon.fireSound == null || weaponAudioSource == null) return;
        weaponAudioSource.pitch = Random.Range(minPitch, maxPitch);
        weaponAudioSource.PlayOneShot(weapon.fireSound);
    }

    private void PlaySound(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null) return;
        source.pitch = Random.Range(minPitch, maxPitch);
        source.PlayOneShot(clip);
    }
}
