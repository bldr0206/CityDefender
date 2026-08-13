using CityDef.Gameplay.Logic;
using TMPro;
using UnityEngine;

/// <summary>
/// Блок характеристик выбранного бота: заголовок (имя + специализация)
/// и строка на каждую характеристику из <see cref="BotStatType"/>.
/// </summary>
public class BotStatsPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _botNameText;
    [SerializeField] private TMP_Text _specializationText;
    [SerializeField] private RectTransform _rowsRoot;
    [SerializeField] private BotStatRowView _rowTemplate;

    private readonly LocalizedTextSlot _botNameSlot = new LocalizedTextSlot();
    private readonly LocalizedTextSlot _specializationSlot = new LocalizedTextSlot();
    private BotStatRowView[] _rows;
    private string _botWord = string.Empty;
    private int _botNumber;

    private void OnDestroy()
    {
        _botNameSlot.Unbind();
        _specializationSlot.Unbind();
    }

    public void Show(Bot bot, int botNumber)
    {
        EnsureRows();

        _botNumber = botNumber;
        _botNameSlot.Bind(word => { _botWord = word; ApplyBotName(); }, BotsMenuLoc.Make(BotsMenuLoc.BotName));

        BotSpecializationConfig specialization = bot.Specialization;
        _specializationSlot.Bind(_specializationText, specialization != null ? specialization.DisplayName : null);

        for (int i = 0; i < _rows.Length; i++)
        {
            BotStatType type = BotStats.AllTypes[i];
            _rows[i].ShowValues(bot.Stats.GetLevel(type), bot.Stats.GetValue(type));
        }
    }

    private void EnsureRows()
    {
        if (_rows != null) return;

        _rows = new BotStatRowView[BotStats.AllTypes.Length];
        for (int i = 0; i < _rows.Length; i++)
        {
            BotStatRowView row = Instantiate(_rowTemplate, _rowsRoot);
            row.gameObject.SetActive(true);
            row.Setup(BotStats.AllTypes[i]);
            _rows[i] = row;
        }
    }

    private void ApplyBotName()
    {
        _botNameText.text = $"{_botWord} {_botNumber}";
    }
}
