using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.UI;

public class QuestPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _questText;
    [SerializeField] private GameObject _questPanel;
    [SerializeField] private Image _progressFill;

    LocalizedString _questTextString;
    string _questBaseText;
    int _progressCurrent;
    int _progressTarget;
    bool _wantsShown;

    void OnDisable()
    {
        if (_questTextString != null)
            _questTextString.StringChanged -= SetQuestText;
    }

    void ApplyVisibility()
    {
        if (_questPanel == null) return;
        _questPanel.SetActive(_wantsShown);
    }

    public void UpdateQuestText(LocalizedString questText)
    {
        if (_questTextString != null)
            _questTextString.StringChanged -= SetQuestText;

        _questTextString = questText;
        _questTextString.StringChanged += SetQuestText;
        SetQuestText(_questTextString.GetLocalizedString());
    }

    public void SetProgress(int current, int target)
    {
        _progressCurrent = Mathf.Max(0, current);
        _progressTarget = Mathf.Max(0, target);

        ApplyQuestText();
        ApplyProgressBar();
    }

    void SetQuestText(string questText)
    {
        _questBaseText = questText;
        ApplyQuestText();
    }

    void ApplyQuestText()
    {
        if (_questText == null) return;

        _questText.text = _progressTarget > 0
            ? $"{_questBaseText}\n{_progressCurrent} / {_progressTarget}"
            : _questBaseText;
    }

    void ApplyProgressBar()
    {
        if (_progressFill == null) return;

        _progressFill.fillAmount = _progressTarget > 0
            ? Mathf.Clamp01((float)_progressCurrent / _progressTarget)
            : 0f;
    }

    public void Show()
    {
        _wantsShown = true;
        ApplyVisibility();
    }

    public void Hide()
    {
        _wantsShown = false;
        SetProgress(0, 0);
        ApplyVisibility();
    }
}
