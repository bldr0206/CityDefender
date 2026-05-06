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

    private LevelValuesManager _levelValuesManager;
    private DiContainer _container;
    private bool _isPlayerInRange;

    [Inject]
    public void Construct(LevelValuesManager levelValuesManager, DiContainer container)
    {
        _levelValuesManager = levelValuesManager;
        _container = container;
    }

    private void OnEnable()
    {
        Actions.OnPlayerMoneyChanged += HandlePlayerMoneyChanged;
    }

    private void Start()
    {
        HideUI();
    }

    private void OnDisable()
    {
        Actions.OnPlayerMoneyChanged -= HandlePlayerMoneyChanged;
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
        if (_agentPrefab == null || _spawnPoint == null) return;
        if (!_isPlayerInRange || !_levelValuesManager.TrySpendMoney(_price)) return;

        Agent agent = SpawnAgent(_spawnPoint.position, _spawnPoint.rotation);
        Game.RegisterHiredAgent();
        agent.StartFollowingPlayer();
        UpdateBuyButton();
    }

    public Agent SpawnAgent(Vector3 position, Quaternion rotation)
    {
        if (_agentPrefab == null) return null;

        return _container.InstantiatePrefabForComponent<Agent>(_agentPrefab, position, rotation, null);
    }

    private void ShowUI()
    {
        if (_uiRoot == null) return;

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
        return other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player");
    }
}
