using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

public class TraderNPC : MonoBehaviour
{
    [SerializeField] private GameObject _uiRoot;
    [SerializeField] private Button _buyButton;
    [SerializeField] private TMP_Text _buyButtonText;
    [SerializeField] private GameObject _agentPrefab;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private int _price = 10;
    [Tooltip("Сколько агентов торговец продаёт за уровень. На уровне 1 — один.")]
    [SerializeField] private int _maxAgentsForSale = 1;

    private LevelValuesManager _levelValuesManager;
    private DiContainer _container;
    private bool _isPlayerInRange;
    private Agent _saleAgent;

    private bool IsSoldOut => Game.HiredAgentsCount >= _maxAgentsForSale;

    [Inject]
    public void Construct(LevelValuesManager levelValuesManager, DiContainer container)
    {
        _levelValuesManager = levelValuesManager;
        _container = container;
    }

    private void OnEnable()
    {
        Actions.OnPlayerMoneyChanged += HandlePlayerMoneyChanged;
        Actions.OnLevelStarted += RefreshSaleAgent;
        Actions.OnLoadCompleted += HandleLoadCompleted;
    }

    private void Start()
    {
        HideUI();
    }

    private void OnDisable()
    {
        Actions.OnPlayerMoneyChanged -= HandlePlayerMoneyChanged;
        Actions.OnLevelStarted -= RefreshSaleAgent;
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

    public void BuyAgent()
    {
        if (!_isPlayerInRange || IsSoldOut || _saleAgent == null) return;
        if (!_levelValuesManager.TrySpendMoney(_price)) return;

        Agent bought = _saleAgent;
        _saleAgent = null;
        Game.RegisterHiredAgent();
        Actions.AgentHired();
        bought.SellToPlayer();

        RestockSaleAgent();
        RefreshUI();
    }

    public Agent SpawnAgent(Vector3 position, Quaternion rotation)
    {
        if (_agentPrefab == null) return null;

        return _container.InstantiatePrefabForComponent<Agent>(_agentPrefab, position, rotation, null);
    }

    private void HandleLoadCompleted(string _) => RefreshSaleAgent();

    /// <summary> Приводит витрину в соответствие с текущим стоком: показать агента, пока есть что продавать. </summary>
    private void RefreshSaleAgent()
    {
        if (IsSoldOut)
            DespawnSaleAgent();
        else
            RestockSaleAgent();
    }

    /// <summary> Ставит на витрину следующего агента, если торговцу ещё есть что продавать. </summary>
    private void RestockSaleAgent()
    {
        if (IsSoldOut || _saleAgent != null || _agentPrefab == null || _spawnPoint == null)
            return;

        _saleAgent = SpawnAgent(_spawnPoint.position, _spawnPoint.rotation);
        if (_saleAgent != null)
            _saleAgent.PutUpForSale();
    }

    private void DespawnSaleAgent()
    {
        if (_saleAgent == null) return;

        Destroy(_saleAgent.gameObject);
        _saleAgent = null;
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
