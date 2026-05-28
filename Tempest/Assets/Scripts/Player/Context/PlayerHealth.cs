using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDown { get; private set; }

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDown) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        GameEventBus.RaisePlayerDamaged(amount);

        if (CurrentHealth <= 0f)
            GoDown();
    }

    public void Heal(float amount)
    {
        if (IsDown) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
    }

    private void GoDown()
    {
        IsDown = true;
        GameEventBus.RaisePlayerDowned();
    }

    public void Revive(float healthPercent = 0.5f)
    {
        IsDown = false;
        CurrentHealth = maxHealth * healthPercent;
        GameEventBus.RaisePlayerRevived();
    }

    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
        IsDown = false;
    }
}
