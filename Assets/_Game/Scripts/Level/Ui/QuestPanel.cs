using UnityEngine;
using TMPro;

public class QuestPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _questText;
    [SerializeField] private GameObject _questPanel;

    public void UpdateQuestText(string questText)
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
