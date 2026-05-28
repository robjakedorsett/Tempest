using System;

public static class GameEventBus
{
    public static event Action<float> OnPlayerDamaged;
    public static event Action OnPlayerDowned;
    public static event Action OnPlayerRevived;
    public static event Action<int> OnEnemyKilled;
    public static event Action OnExtractionStarted;
    public static event Action OnExtractionCompleted;
    public static event Action OnRunFailed;

    public static void RaisePlayerDamaged(float damage) => OnPlayerDamaged?.Invoke(damage);
    public static void RaisePlayerDowned() => OnPlayerDowned?.Invoke();
    public static void RaisePlayerRevived() => OnPlayerRevived?.Invoke();
    public static void RaiseEnemyKilled(int xpValue) => OnEnemyKilled?.Invoke(xpValue);
    public static void RaiseExtractionStarted() => OnExtractionStarted?.Invoke();
    public static void RaiseExtractionCompleted() => OnExtractionCompleted?.Invoke();
    public static void RaiseRunFailed() => OnRunFailed?.Invoke();
}
