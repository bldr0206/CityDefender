using UnityEngine;
using UnityEngine.UI;
using Zenject;
using DG.Tweening;

public class BottleReturnMachine : MonoBehaviour
{
    [SerializeField] private Image _progressBar;
    [SerializeField] private float _timeToFillProgress = 1f;
    [SerializeField] private int _moneyPerBottle = 25;
    [SerializeField] private Transform _bottleReturnPoint;

    LevelValuesManager _levelValuesManager;
    GameUISettings _gameUISettings;
    PlayerCollector _playerCollector;
    Tween _currentTween;
    bool _isPlayerInside;
    bool _isReturningBottle;

    [Inject]
    public void Construct(LevelValuesManager levelValuesManager, GameUISettings gameUISettings)
    {
        _levelValuesManager = levelValuesManager;
        _gameUISettings = gameUISettings;
    }

    void Awake()
    {
        _progressBar.fillAmount = 0f;
    }

    void OnDestroy()
    {
        _currentTween?.Kill();
    }

    public void StartReturning(PlayerCollector playerCollector)
    {
        _playerCollector = playerCollector;
        _isPlayerInside = true;

        if (_isReturningBottle) return;

        FillProgress();
    }

    public void StopReturning()
    {
        _isPlayerInside = false;

        if (_isReturningBottle) return;

        RollbackProgress();
    }

    void FillProgress()
    {
        if (!_isPlayerInside || _playerCollector == null || !_playerCollector.HasItem(PickableItemType.Bottle)) return;

        _currentTween?.Kill();
        _currentTween = _progressBar
            .DOFillAmount(1f, _timeToFillProgress * (1f - _progressBar.fillAmount))
            .SetEase(Ease.Linear)
            .OnComplete(ReturnBottle);
    }

    void RollbackProgress()
    {
        _currentTween?.Kill();
        _currentTween = _progressBar
            .DOFillAmount(0f, _timeToFillProgress * _progressBar.fillAmount)
            .SetEase(Ease.Linear);
    }

    void ReturnBottle()
    {
        if (!_isPlayerInside || _playerCollector == null)
        {
            _isReturningBottle = false;
            return;
        }

        if (!_playerCollector.TryRemoveLastItem(PickableItemType.Bottle, out PickableItem bottle))
        {
            _isReturningBottle = false;
            return;
        }

        _progressBar.fillAmount = 0f;
        _isReturningBottle = true;

        bottle.transform.DOKill();
        bottle.transform.SetParent(_bottleReturnPoint);

        _currentTween = DOTween.Sequence()
            .Join(bottle.transform.DOLocalMove(Vector3.zero, _gameUISettings.shortDelay).SetEase(Ease.InOutQuad))
            .Join(bottle.transform.DOLocalRotate(Vector3.zero, _gameUISettings.shortDelay).SetEase(Ease.InOutQuad))
            .OnComplete(() =>
            {
                _levelValuesManager.AddMoney(_moneyPerBottle);
                if (bottle.IsQuestBound)
                    Actions.QuestItemTurnedIn(bottle.QuestId);

                bottle.gameObject.SetActive(false);
                ReturnBottle();
            });
    }
}
