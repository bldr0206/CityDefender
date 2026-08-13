using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Экран «Боты»: сверху характеристики выбранного бота, ниже карусель
/// текущих ботов, внизу кнопка закрытия. Открывается кнопкой в HUD — кнопка
/// скрывается вместе с HUD на катсценах, диалогах и пауз-секвенциях.
/// На время показа игра на паузе (как DialogueScreen).
/// </summary>
public class BotsMenuScreen : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private Button _openButton;
    [SerializeField] private TMP_Text _openButtonText;
    [SerializeField] private Button _closeButton;
    [SerializeField] private TMP_Text _closeButtonText;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private GameObject _emptyPanel;
    [SerializeField] private TMP_Text _emptyText;
    [SerializeField] private BotStatsPanel _statsPanel;
    [SerializeField] private BotsCarousel _carousel;

    private readonly LocalizedTextSlot _openLabelSlot = new LocalizedTextSlot();
    private readonly LocalizedTextSlot _titleSlot = new LocalizedTextSlot();
    private readonly LocalizedTextSlot _closeLabelSlot = new LocalizedTextSlot();
    private readonly LocalizedTextSlot _emptySlot = new LocalizedTextSlot();
    private readonly List<Bot> _bots = new List<Bot>();
    private bool _isOpen;

    private void Awake()
    {
        _openButton.onClick.AddListener(Open);
        _closeButton.onClick.AddListener(Close);
        _carousel.SelectionChanged += HandleBotSelected;

        _openLabelSlot.Bind(_openButtonText, BotsMenuLoc.Make(BotsMenuLoc.Title));
        _titleSlot.Bind(_titleText, BotsMenuLoc.Make(BotsMenuLoc.Title));
        _closeLabelSlot.Bind(_closeButtonText, BotsMenuLoc.Make(BotsMenuLoc.Close));
        _emptySlot.Bind(_emptyText, BotsMenuLoc.Make(BotsMenuLoc.Empty));

        _root.SetActive(false);
    }

    private void OnDestroy()
    {
        _openButton.onClick.RemoveListener(Open);
        _closeButton.onClick.RemoveListener(Close);
        _carousel.SelectionChanged -= HandleBotSelected;

        _openLabelSlot.Unbind();
        _titleSlot.Unbind();
        _closeLabelSlot.Unbind();
        _emptySlot.Unbind();

        // Сцена может перезагрузиться с открытым меню (загрузка сейва) — не оставляем игру на паузе.
        if (_isOpen)
            Game.ResumeGame();
    }

    public void Open()
    {
        if (_isOpen) return;

        _isOpen = true;
        CollectBots();
        Game.PauseGame();
        _root.SetActive(true);

        bool hasBots = _bots.Count > 0;
        _emptyPanel.SetActive(!hasBots);
        _statsPanel.gameObject.SetActive(hasBots);
        _carousel.gameObject.SetActive(hasBots);

        if (hasBots)
            _carousel.Show(_bots);
    }

    public void Close()
    {
        if (!_isOpen) return;

        _isOpen = false;
        _root.SetActive(false);
        Game.ResumeGame();
    }

    private void CollectBots()
    {
        _bots.Clear();
        List<Bot> allBots = SaveableRegistry.GetAll<Bot>();
        for (int i = 0; i < allBots.Count; i++)
        {
            if (!allBots[i].IsForSale)
                _bots.Add(allBots[i]);
        }
    }

    private void HandleBotSelected(Bot bot, int index)
    {
        _statsPanel.Show(bot, index + 1);
    }
}
