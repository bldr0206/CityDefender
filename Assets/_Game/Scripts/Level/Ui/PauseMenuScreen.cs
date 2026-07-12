using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

/// <summary>
/// Экран паузы: открывается кнопкой-паузой в углу HUD, на время показа игра на
/// паузе (Game.PauseGame — как DialogueScreen/BotsMenuScreen). Содержит дебажный
/// слайдер скорости перемещения персонажа (дискретно 1–10) и кнопку продолжения.
/// </summary>
public class PauseMenuScreen : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private Button _openButton;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Slider _speedSlider;
    [SerializeField] private TMP_Text _speedValueText;

    private PlayerController _player;
    private bool _isOpen;

    [Inject]
    public void Construct(PlayerController player)
    {
        _player = player;
    }

    private void Awake()
    {
        _openButton.onClick.AddListener(Open);
        _resumeButton.onClick.AddListener(Close);
        _speedSlider.onValueChanged.AddListener(HandleSpeedChanged);
        _root.SetActive(false);
    }

    private void OnDestroy()
    {
        _openButton.onClick.RemoveListener(Open);
        _resumeButton.onClick.RemoveListener(Close);
        _speedSlider.onValueChanged.RemoveListener(HandleSpeedChanged);

        // Сцена может перезагрузиться с открытым меню (загрузка сейва) — не оставляем игру на паузе.
        if (_isOpen)
            Game.ResumeGame();
    }

    public void Open()
    {
        if (_isOpen) return;

        _isOpen = true;
        Game.PauseGame();
        _speedSlider.SetValueWithoutNotify(_player.MoveSpeed);
        UpdateSpeedValueText(_player.MoveSpeed);
        _root.SetActive(true);
    }

    public void Close()
    {
        if (!_isOpen) return;

        _isOpen = false;
        _root.SetActive(false);
        Game.ResumeGame();
    }

    private void HandleSpeedChanged(float value)
    {
        _player.MoveSpeed = value;
        UpdateSpeedValueText(value);
    }

    private void UpdateSpeedValueText(float value)
    {
        if (_speedValueText != null)
            _speedValueText.text = Mathf.RoundToInt(value).ToString();
    }
}
