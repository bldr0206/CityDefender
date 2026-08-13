using System;
using TMPro;
using UnityEngine.Localization;

/// <summary>
/// Слот локализованной строки: подписка на StringChanged с корректной
/// переподпиской и отпиской (паттерн QuestPanel/DialogueScreen без ручного бойлерплейта).
/// </summary>
public class LocalizedTextSlot
{
    private LocalizedString _current;
    private LocalizedString.ChangeHandler _handler;

    /// <summary> Привязать строку к TMP-тексту. </summary>
    public void Bind(TMP_Text target, LocalizedString text)
    {
        Bind(value => target.text = value, text);
    }

    /// <summary> Привязать строку к произвольному применению (композиция с числами и т.п.). </summary>
    public void Bind(Action<string> apply, LocalizedString text)
    {
        Unbind();

        if (text == null || text.IsEmpty)
        {
            apply(string.Empty);
            return;
        }

        _current = text;
        _handler = value => apply(value);
        _current.StringChanged += _handler;
        apply(_current.GetLocalizedString());
    }

    public void Unbind()
    {
        if (_current == null) return;

        _current.StringChanged -= _handler;
        _current = null;
        _handler = null;
    }
}
