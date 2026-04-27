using TMPro;
using UnityEngine;
using Zenject;

public class PlayerMoneyCounter : MonoBehaviour
{
    [SerializeField] TMP_Text _currentMoneyText;
    LevelValuesManager _levelValuesManager;
    [Inject]
    public void Construct(LevelValuesManager levelValuesManager)
    {
        _levelValuesManager = levelValuesManager;
    }

    private void OnEnable()
    {
        Actions.OnPlayerMoneyChanged += HandlePlayerMoneyChanged;
        UpdateMoneyCounter();
    }

    private void OnDisable()
    {
        Actions.OnPlayerMoneyChanged -= HandlePlayerMoneyChanged;
    }

    private void HandlePlayerMoneyChanged(int _)
    {
        UpdateMoneyCounter();
    }

    public void UpdateMoneyCounter()
    {
        if (_currentMoneyText == null)
            return;

        int money = _levelValuesManager?.GetMoney() ?? 0;
        _currentMoneyText.text = $"${money:N0}";
    }
}
