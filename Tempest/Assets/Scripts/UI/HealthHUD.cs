using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HealthHUD : MonoBehaviour
{
    private PlayerHealth _playerHealth;
    private Label _healthLabel;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _healthLabel = root.Q<Label>("health-label");

        _playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (_playerHealth == null) return;

        GameEventBus.OnPlayerDamaged += HandleHealthChanged;
        GameEventBus.OnPlayerRevived += HandleRevived;

        UpdateDisplay();
    }

    private void OnDisable()
    {
        GameEventBus.OnPlayerDamaged -= HandleHealthChanged;
        GameEventBus.OnPlayerRevived -= HandleRevived;
    }

    private void HandleHealthChanged(float damage)
    {
        UpdateDisplay();
    }

    private void HandleRevived()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_playerHealth == null) return;

        int current = Mathf.CeilToInt(_playerHealth.CurrentHealth);
        int max = Mathf.CeilToInt(_playerHealth.MaxHealth);
        _healthLabel.text = $"{current} / {max}";

        bool isLow = max > 0 && _playerHealth.CurrentHealth / _playerHealth.MaxHealth <= 0.25f;
        _healthLabel.EnableInClassList("health-label--low", isLow);
    }
}
