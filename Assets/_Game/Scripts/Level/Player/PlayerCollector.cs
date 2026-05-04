using UnityEngine;
using DG.Tweening;
using Zenject;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class PlayerCollector : MonoBehaviour
{
    // STACK OF PICKABLE ITEMS
    List<PickableItem> _items = new List<PickableItem>();
    [SerializeField, FormerlySerializedAs("bottleYOffset")] float _itemYOffset = 0.5f;
    [SerializeField, FormerlySerializedAs("maxBottles")] int _maxItems = 5;

    // CURRENT ITEM
    Collectable _currentItem;

    // BACKPACK POINT
    public Transform backpackPoint;
    GameUISettings _gameUISettings;
    LevelValuesManager _levelValuesManager;

    public bool HasItem(PickableItemType type) => _items.Count > 0 && _items[0].Type == type;
    bool IsInventoryFull => _items.Count >= _maxItems;

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
            PickableItem item = other.GetComponent<PickableItem>();
            if (item != null)
            {
                item.TakeClicked -= CollectItem;
                item.TakeClicked += CollectItem;
                item.ShowUI();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Interactable"))
        {
            PickableItem item = other.GetComponent<PickableItem>();
            if (item != null)
            {
                item.TakeClicked -= CollectItem;
                item.HideUI();
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
        if (IsInventoryFull)
        {
            Debug.Log("You can't carry a key while your inventory is full!");
            return;
        }

        if (_currentItem != null) return;

        PullToBackpack(collectable, () =>
        {
            _currentItem = collectable;
        });
    }

    public void CollectItem(PickableItem item)
    {
        if (IsInventoryFull)
        {
            Debug.Log("You can't carry more items!");
            return;
        }

        if (_items.Count > 0 && _items[0].Type != item.Type)
        {
            Debug.Log("You can't carry different item types!");
            return;
        }

        item.Collect();

        int stackIndex = _items.Count;
        _items.Add(item);
        item.transform.SetParent(backpackPoint);

        Vector3 targetWorld = backpackPoint.position + Vector3.up * (stackIndex * _itemYOffset);
        Vector3 targetLocal = backpackPoint.InverseTransformPoint(targetWorld);
        PullItemToBackpack(item, targetLocal);
    }

    public bool TryRemoveLastItem(PickableItemType type, out PickableItem item)
    {
        item = null;

        if (!HasItem(type)) return false;

        item = _items[_items.Count - 1];
        _items.RemoveAt(_items.Count - 1);
        return true;
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

    private void PullItemToBackpack(PickableItem item, Vector3 localPosition)
    {
        DOTween.Sequence()
            .Join(item.transform.DOLocalMove(localPosition, _gameUISettings.shortDelay).SetEase(Ease.InOutQuad))
            .Join(item.transform.DOLocalRotate(new Vector3(180, 0, 90), _gameUISettings.shortDelay).SetEase(Ease.InOutQuad));
    }


}
