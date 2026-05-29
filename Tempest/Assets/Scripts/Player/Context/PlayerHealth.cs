using Tempest.Weapons;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDown { get; private set; }

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public bool TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (IsDown) return false;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        GameEventBus.RaisePlayerDamaged(damage);

        if (CurrentHealth <= 0f)
        {
            GoDown();
            return true;
        }

        return false;
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
