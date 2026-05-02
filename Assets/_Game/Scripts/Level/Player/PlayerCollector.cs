using UnityEngine;
using DG.Tweening;
using Zenject;
using System.Collections.Generic;

public class PlayerCollector : MonoBehaviour
{
    // STACK OF BOTTLES
    List<Bottle> _bottles = new List<Bottle>();
    [SerializeField] float bottleYOffset = 0.5f;
    [SerializeField] int maxBottles = 5;

    // CURRENT ITEM
    Collectable _currentItem;

    // BACKPACK POINT
    public Transform backpackPoint;
    GameUISettings _gameUISettings;
    LevelValuesManager _levelValuesManager;

    public bool HasBottles => _bottles.Count > 0;

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
                bottle.TakeClicked -= CollectBottle;
                bottle.TakeClicked += CollectBottle;
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
                bottle.TakeClicked -= CollectBottle;
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

    public void CollectBottle(Bottle bottle)
    {
        if (_bottles.Count >= maxBottles)
        {
            Debug.Log("You can't carry more bottles!");
            return;
        }

        bottle.Collect();

        int stackIndex = _bottles.Count;
        _bottles.Add(bottle);
        bottle.transform.SetParent(backpackPoint);

        Vector3 targetWorld = backpackPoint.position + Vector3.up * (stackIndex * bottleYOffset);
        Vector3 targetLocal = backpackPoint.InverseTransformPoint(targetWorld);
        PullBottleToBackpack(bottle, targetLocal);
    }

    public Bottle RemoveLastBottle()
    {
        if (_bottles.Count == 0) return null;
        Bottle bottle = _bottles[_bottles.Count - 1];
        _bottles.RemoveAt(_bottles.Count - 1);
        return bottle;
    }

    private void PullToBackpack(Collectable collectable, TweenCallback onComplete)
    {
        Debug.Log($"Player collected a {collectable.type} worth {collectable.value}!");

        collectable.transform.SetParent(backpackPoint);
        DOTween.Sequence()
            .Join(collectable.transform.DOLocalMove(Vector3.zero, _gameUISettings.shortDelay).SetEase(Ease.InOutQuad))
            .Join(collectable.transform.DOLocalRotate(new Vector3(0, 0, 0), _gameUISettings.shortDelay).SetEase(Ease.InOutQuad))
            .OnComplete(onComplete);
    }

    private void PullBottleToBackpack(Bottle bottle, Vector3 localPosition)
    {
        DOTween.Sequence()
            .Join(bottle.transform.DOLocalMove(localPosition, _gameUISettings.shortDelay).SetEase(Ease.InOutQuad))
            .Join(bottle.transform.DOLocalRotate(new Vector3(180, 0, 90), _gameUISettings.shortDelay).SetEase(Ease.InOutQuad));
    }


}
