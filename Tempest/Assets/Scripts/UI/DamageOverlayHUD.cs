using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class DamageOverlayHUD : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float minOpacity = 0.3f;
    [SerializeField] private float maxOpacity = 1.0f;
    [SerializeField] private int textureSize = 256;

    private VisualElement _vignette;
    private float _currentOpacity;
    private float _targetOpacity;
    private PlayerHealth _playerHealth;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _vignette = root.Q<VisualElement>("damage-vignette");

        _vignette.style.backgroundImage = new StyleBackground(GenerateVignetteTexture());

        _playerHealth = FindFirstObjectByType<PlayerHealth>();

        GameEventBus.OnPlayerDamaged += HandleDamage;
    }

    private void OnDisable()
    {
        GameEventBus.OnPlayerDamaged -= HandleDamage;
    }

    private void Update()
    {
        if (_currentOpacity <= 0f) return;

        _currentOpacity = Mathf.MoveTowards(_currentOpacity, 0f, Time.deltaTime / fadeDuration);
        _vignette.style.opacity = _currentOpacity;
    }

    private void HandleDamage(float damage)
    {
        float maxHealth = _playerHealth != null ? _playerHealth.MaxHealth : 100f;
        float intensity = Mathf.Clamp(damage / maxHealth, 0f, 1f);
        float targetOpacity = Mathf.Lerp(minOpacity, maxOpacity, intensity);

        if (targetOpacity > _currentOpacity)
            _currentOpacity = targetOpacity;

        _vignette.style.opacity = _currentOpacity;
    }

    private Texture2D GenerateVignetteTexture()
    {
        var tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        float center = textureSize / 2f;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha = Mathf.Clamp01((dist - 0.4f) / 0.6f);
                alpha = alpha * alpha;

                tex.SetPixel(x, y, new Color(0.7f, 0f, 0f, alpha));
            }
        }

        tex.Apply();
        return tex;
    }
}
