using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class CrosshairHUD : MonoBehaviour
{
    [SerializeField] private float spreadPixelsPerDegree = 8f;
    [SerializeField] private float baseGap = 4f;

    private PlayerWeaponController _weaponController;
    private VisualElement _hitmarker;
    private IVisualElementScheduledItem _hideTask;

    private VisualElement _top;
    private VisualElement _bottom;
    private VisualElement _left;
    private VisualElement _right;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _hitmarker = root.Q<VisualElement>("hitmarker");
        _top = root.Q<VisualElement>("crosshair-top");
        _bottom = root.Q<VisualElement>("crosshair-bottom");
        _left = root.Q<VisualElement>("crosshair-left");
        _right = root.Q<VisualElement>("crosshair-right");

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

    private void Update()
    {
        if (_weaponController == null || !_weaponController.HasWeapon) return;

        float spread = _weaponController.CurrentSpread;
        float gap = baseGap + spread * spreadPixelsPerDegree;

        _top.style.translate = new Translate(-1f, -(gap + 16f));
        _bottom.style.translate = new Translate(-1f, gap);
        _left.style.translate = new Translate(-(gap + 16f), -1f);
        _right.style.translate = new Translate(gap, -1f);
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
