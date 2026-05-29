using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class CrosshairHUD : MonoBehaviour
{
    private PlayerWeaponController _weaponController;
    private VisualElement _hitmarker;
    private IVisualElementScheduledItem _hideTask;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _hitmarker = root.Q<VisualElement>("hitmarker");

        _weaponController = FindFirstObjectByType<PlayerWeaponController>();
        if (_weaponController == null) return;

        _weaponController.OnHitConfirmed += HandleHit;
        _weaponController.OnKillConfirmed += HandleKill;
    }

    private void OnDisable()
    {
        if (_weaponController == null) return;
        _weaponController.OnHitConfirmed -= HandleHit;
        _weaponController.OnKillConfirmed -= HandleKill;
    }

    private void HandleHit(Vector3 hitPoint, Vector3 hitNormal)
    {
        ShowHitmarker(false);
    }

    private void HandleKill(Vector3 hitPoint, Vector3 hitNormal)
    {
        ShowHitmarker(true);
    }

    private void ShowHitmarker(bool isKill)
    {
        _hideTask?.Pause();

        _hitmarker.RemoveFromClassList("hitmarker--hit");
        _hitmarker.RemoveFromClassList("hitmarker--kill");

        _hitmarker.AddToClassList(isKill ? "hitmarker--kill" : "hitmarker--hit");

        long durationMs = isKill ? 150 : 100;
        _hideTask = _hitmarker.schedule.Execute(() =>
        {
            _hitmarker.RemoveFromClassList("hitmarker--hit");
            _hitmarker.RemoveFromClassList("hitmarker--kill");
        }).StartingIn(durationMs);
    }
}
