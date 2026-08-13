using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Карточка бота в карусели: номерное имя и рамка выбора. </summary>
public class BotCardView : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private GameObject _selectedOutline;

    private readonly LocalizedTextSlot _nameSlot = new LocalizedTextSlot();
    private int _number;

    public event Action<BotCardView> Clicked;

    public Bot Bot { get; private set; }
    public RectTransform Rect => (RectTransform)transform;

    private void Awake()
    {
        _button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        _button.onClick.RemoveListener(HandleClick);
        _nameSlot.Unbind();
    }

    public void Setup(Bot bot, int number)
    {
        Bot = bot;
        _number = number;
        _nameSlot.Bind(word => _nameText.text = $"{word} {_number}", BotsMenuLoc.Make(BotsMenuLoc.BotName));
        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        _selectedOutline.SetActive(isSelected);
    }

    private void HandleClick()
    {
        Clicked?.Invoke(this);
    }
}
