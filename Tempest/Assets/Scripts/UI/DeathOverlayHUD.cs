using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class DeathOverlayHUD : MonoBehaviour
{
    private VisualElement _overlay;
    private Label _title;
    private Label _subtitle;
    private Button _restartButton;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _overlay = root.Q<VisualElement>("death-overlay");
        _title = root.Q<Label>("death-title");
        _subtitle = root.Q<Label>("death-subtitle");
        _restartButton = root.Q<Button>("restart-button");

        _restartButton.clicked += HandleRestart;

        GameEventBus.OnPlayerDowned += HandlePlayerDowned;
        GameEventBus.OnPlayerRevived += HandlePlayerRevived;
        GameEventBus.OnRunFailed += HandleRunFailed;
    }

    private void OnDisable()
    {
        _restartButton.clicked -= HandleRestart;

        GameEventBus.OnPlayerDowned -= HandlePlayerDowned;
        GameEventBus.OnPlayerRevived -= HandlePlayerRevived;
        GameEventBus.OnRunFailed -= HandleRunFailed;
    }

    private void HandlePlayerDowned()
    {
        _title.text = "DOWNED";
        _overlay.AddToClassList("death-overlay--visible");

        bool isSolo = GetRegisteredPlayerCount() <= 1;

        if (isSolo)
        {
            _subtitle.RemoveFromClassList("death-subtitle--visible");
            _restartButton.AddToClassList("restart-button--visible");
        }
        else
        {
            _subtitle.text = "Waiting for revive...";
            _subtitle.AddToClassList("death-subtitle--visible");
            _restartButton.RemoveFromClassList("restart-button--visible");
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void HandleRunFailed()
    {
        _title.text = "DOWNED";
        _subtitle.text = "Squad eliminated";
        _subtitle.AddToClassList("death-subtitle--visible");
        _restartButton.AddToClassList("restart-button--visible");
        _overlay.AddToClassList("death-overlay--visible");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void HandlePlayerRevived()
    {
        _overlay.RemoveFromClassList("death-overlay--visible");
        _subtitle.RemoveFromClassList("death-subtitle--visible");
        _restartButton.RemoveFromClassList("restart-button--visible");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void HandleRestart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private int GetRegisteredPlayerCount()
    {
        return PlayerRegistry.PlayerCount;
    }
}
