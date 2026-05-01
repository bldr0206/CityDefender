using UnityEngine;
using DG.Tweening;
using Zenject;

public class PlayerCollectorTriggerChecker : MonoBehaviour
{

    Collectable _currentItem;
    public Transform backpackPoint;

    GameUISettings _gameUISettings;
    LevelValuesManager _levelValuesManager;
    [Inject]
    public void Construct(GameUISettings gameUISettings, LevelValuesManager levelValuesManager)
    {
        _gameUISettings = gameUISettings;
        _levelValuesManager = levelValuesManager;
    }


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Collectable"))
        {
            TryCollect(other);
        }

        if (other.CompareTag("Door"))
        {
            Door door = other.GetComponent<Door>();

            if (
                door != null
                && _currentItem != null
                && _currentItem.type == CollectableType.Key
                && _currentItem.value == door.requiredValue
                )
            {
                door.OpenDoor();
                Destroy(_currentItem.gameObject);
                _currentItem = null;
            }

            else
                Debug.Log("You need the correct key to open this door!");
        }

        if (other.CompareTag("Interactable"))
        {
            Bottle bottle = other.GetComponent<Bottle>();
            if (bottle != null)
            {
                bottle.ShowUI();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Interactable"))
        {
            Bottle bottle = other.GetComponent<Bottle>();
            if (bottle != null)
            {
                bottle.HideUI();
            }
        }
    }

    private void TryCollect(Collider other)
    {
        Collectable collectable = other.GetComponent<Collectable>();
        if (collectable == null) return;

        if (collectable.type == CollectableType.Money)
        {
            CollectMoney(collectable);
            return;
        }

        if (collectable.type == CollectableType.Key)
        {
            CollectKey(collectable);
        }
    }

    private void CollectMoney(Collectable collectable)
    {
        PullToBackpack(collectable, () =>
        {
            _levelValuesManager.AddMoney(collectable.value);
            Destroy(collectable.gameObject);
        });
    }

    private void CollectKey(Collectable collectable)
    {
        if (_currentItem != null) return;

        PullToBackpack(collectable, () =>
        {
            _currentItem = collectable;
        });
    }

    private void PullToBackpack(Collectable collectable, TweenCallback onComplete)
    {
        Debug.Log($"Player collected a {collectable.type} worth {collectable.value}!");

        collectable.transform.SetParent(backpackPoint);
        DOTween.Sequence()
            .Join(collectable.transform.DOLocalMove(Vector3.zero, _gameUISettings.shortDelay).SetEase(Ease.InOutQuad))
            .Join(collectable.transform.DOLocalRotate(Vector3.zero, _gameUISettings.shortDelay).SetEase(Ease.InOutQuad))
            .OnComplete(onComplete);
    }
}
