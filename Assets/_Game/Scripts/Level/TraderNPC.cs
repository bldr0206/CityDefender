using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

public class TraderNPC : MonoBehaviour
{
    [SerializeField] private GameObject _uiRoot;
    [SerializeField] private Button _buyButton;
    [SerializeField] private TMP_Text _buyButtonText;
    [SerializeField] private GameObject _botPrefab;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private int _price = 10;
    [Tooltip("Сколько ботов торговец продаёт за уровень. На уровне 1 — один.")]
    [SerializeField] private int _maxBotsForSale = 1;

    private LevelValuesManager _levelValuesManager;
    private DiContainer _container;
    private bool _isPlayerInRange;
    private Bot _saleBot;

    private bool IsSoldOut => Game.HiredBotsCount >= _maxBotsForSale;

    [Inject]
    public void Construct(LevelValuesManager levelValuesManager, DiContainer container)
    {
        _levelValuesManager = levelValuesManager;
        _container = container;
    }

    private void OnEnable()
    {
        Actions.OnPlayerMoneyChanged += HandlePlayerMoneyChanged;
        Actions.OnLevelStarted += RefreshSaleBot;
        Actions.OnLoadCompleted += HandleLoadCompleted;
    }

    private void Start()
    {
        HideUI();
    }

    private void OnDisable()
    {
        Actions.OnPlayerMoneyChanged -= HandlePlayerMoneyChanged;
        Actions.OnLevelStarted -= RefreshSaleBot;
        Actions.OnLoadCompleted -= HandleLoadCompleted;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        _isPlayerInRange = true;
        ShowUI();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;

        _isPlayerInRange = false;
        HideUI();
    }

    public void BuyBot()
    {
        if (!_isPlayerInRange || IsSoldOut || _saleBot == null) return;
        if (!_levelValuesManager.TrySpendMoney(_price)) return;

        Bot bought = _saleBot;
        _saleBot = null;
        Game.RegisterHiredBot();
        Actions.BotHired();
        bought.SellToPlayer();

        RestockSaleBot();
        RefreshUI();
    }

    public Bot SpawnBot(Vector3 position, Quaternion rotation)
    {
        if (_botPrefab == null) return null;

        return _container.InstantiatePrefabForComponent<Bot>(_botPrefab, position, rotation, null);
    }

    private void HandleLoadCompleted(string _) => RefreshSaleBot();

    /// <summary> Приводит витрину в соответствие с текущим стоком: показать бота, пока есть что продавать. </summary>
    private void RefreshSaleBot()
    {
        if (IsSoldOut)
            DespawnSaleBot();
        else
            RestockSaleBot();
    }

    /// <summary> Ставит на витрину следующего бота, если торговцу ещё есть что продавать. </summary>
    private void RestockSaleBot()
    {
        if (IsSoldOut || _saleBot != null || _botPrefab == null || _spawnPoint == null)
            return;

        _saleBot = SpawnBot(_spawnPoint.position, _spawnPoint.rotation);
        if (_saleBot != null)
            _saleBot.PutUpForSale();
    }

    private void DespawnSaleBot()
    {
        if (_saleBot == null) return;

        Destroy(_saleBot.gameObject);
        _saleBot = null;
    }

    private void RefreshUI()
    {
        if (!_isPlayerInRange)
            return;

        if (IsSoldOut)
            HideUI();
        else
            ShowUI();
    }

    private void ShowUI()
    {
        if (_uiRoot == null || IsSoldOut) return;

        _uiRoot.SetActive(true);
        UpdateBuyButton();
    }

    private void HideUI()
    {
        if (_uiRoot == null) return;

        _uiRoot.SetActive(false);
    }

    private void HandlePlayerMoneyChanged(int _)
    {
        if (_isPlayerInRange)
            UpdateBuyButton();
    }

    private void UpdateBuyButton()
    {
        if (_buyButton == null) return;

        bool hasEnoughMoney = _levelValuesManager.GetMoney() >= _price;
        _buyButton.interactable = hasEnoughMoney;

        if (_buyButtonText != null)
            _buyButtonText.color = hasEnoughMoney ? Color.white : Color.red;
    }

    private bool IsPlayer(Collider other)
    {
        return other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(GameTags.Player);
    }
}
