using UnityEngine;
using TMPro;
using UnityEngine.Localization;

public class QuestPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _questText;
    [SerializeField] private GameObject _questPanel;

    LocalizedString _questTextString;

    public void UpdateQuestText(LocalizedString questText)
    {
        if (_questTextString != null)
            _questTextString.StringChanged -= SetQuestText;

        _questTextString = questText;
        _questTextString.StringChanged += SetQuestText;
        _questText.text = _questTextString.GetLocalizedString();
    }

    void OnDisable()
    {
        if (_questTextString != null)
            _questTextString.StringChanged -= SetQuestText;
    }

    void SetQuestText(string questText)
    {
        _questText.text = questText;
    }

    public void Show()
    {
        _questPanel.SetActive(true);
    }

    public void Hide()
    {
        _questPanel.SetActive(false);
    }
}
