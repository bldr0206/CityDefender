using UnityEngine;

public class LevelValuesManager : MonoBehaviour
{
    private int _playersMoney;

    private void Awake()
    {
        InitializeLevelValues();
    }

    private void InitializeLevelValues()
    {

        _playersMoney = 0;
        Actions.PlayerMoneyChanged(_playersMoney);
    }

    public void AddMoney(int amount)
    {
        _playersMoney += amount;
        Actions.PlayerMoneyChanged(_playersMoney);

    }
    public bool CheckMoneyAmount(int amount)
    {
        if (amount > _playersMoney)
        {
            Debug.LogWarning($"Not enough money! Current: {_playersMoney}, Required: {amount}");
        }
        return _playersMoney >= amount;
    }
    public bool TrySpendMoney(int amount)
    {
        if (CheckMoneyAmount(amount))
        {
            _playersMoney -= amount;
            Actions.PlayerMoneyChanged(_playersMoney);
            return true;
        }
        return false;
    }
    public int GetMoney()
    {
        return _playersMoney;
    }
}
