using CityDef.Gameplay.Logic;
using TMPro;
using UnityEngine;

/// <summary> Строка характеристики в меню ботов: название, уровень, значение. </summary>
public class BotStatRowView : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _valueText;

    private readonly LocalizedTextSlot _nameSlot = new LocalizedTextSlot();
    private readonly LocalizedTextSlot _levelSlot = new LocalizedTextSlot();
    private string _levelWord = string.Empty;
    private int _level = 1;

    private void OnDestroy()
    {
        _nameSlot.Unbind();
        _levelSlot.Unbind();
    }

    public void Setup(BotStatType type)
    {
        _nameSlot.Bind(_nameText, BotsMenuLoc.MakeStatName(type));
        _levelSlot.Bind(word => { _levelWord = word; ApplyLevelText(); }, BotsMenuLoc.Make(BotsMenuLoc.LevelShort));
    }

    public void ShowValues(int level, float value)
    {
        _level = level;
        ApplyLevelText();
        _valueText.text = value.ToString("0.##");
    }

    private void ApplyLevelText()
    {
        _levelText.text = $"{_levelWord} {_level}";
    }
}
