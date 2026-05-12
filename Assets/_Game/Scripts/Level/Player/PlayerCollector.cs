using UnityEngine;
using DG.Tweening;
using Zenject;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class PlayerCollector : MonoBehaviour
{
    List<PickableItem> _items = new List<PickableItem>();
    [SerializeField, FormerlySerializedAs("bottleYOffset")] float _itemYOffset = 0.5f;
    [SerializeField, FormerlySerializedAs("maxBottles")] int _maxItems = 5;

    PickableItem _heldDoorKey;

    public Transform backpackPoint;
    GameUISettings _gameUISettings;
    LevelValuesManager _levelValuesManager;

    public bool HasItem(PickableItemType type) => _items.Count > 0 && _items[0].Type == type;
    bool IsInventoryFull => _items.Count >= _maxItems;

    public bool HoldsDoorKey => _heldDoorKey != null && _heldDoorKey.Type == PickableItemType.Key;

    [Inject]
    public void Construct(GameUISettings gameUISettings, LevelValuesManager levelValuesManager)
    {
        _gameUISettings = gameUISettings;
        _levelValuesManager = levelValuesManager;
    }

    void OnEnable()
    {
        Actions.OnWorldLootPickupReady += HandleWorldLootPickupReady;
    }

    void OnDisable()
    {
        Actions.OnWorldLootPickupReady -= HandleWorldLootPickupReady;
    }

    void HandleWorldLootPickupReady(GameObject lootRoot)
    {
        if (!isActiveAndEnabled || lootRoot == null || !lootRoot.activeInHierarchy)
            return;

        Collider playerReach = GetComponent<Collider>();
        if (playerReach == null || !playerReach.enabled)
            return;

        Physics.SyncTransforms();
        Bounds pb = playerReach.bounds;

        Collider[] lootColliders = lootRoot.GetComponentsInChildren<Collider>(false);
        for (int i = 0; i < lootColliders.Length; i++)
        {
            Collider lc = lootColliders[i];
            if (lc == null || !lc.enabled || !lc.gameObject.activeInHierarchy)
                continue;
            if (!pb.Intersects(lc.bounds))
                continue;

            if (lc.CompareTag("Collectable"))
                TryCollect(lc);
            if (lc.CompareTag("Interactable"))
                SubscribePickableFromCollider(lc);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Collectable"))
            TryCollect(other);

        if (other.CompareTag("Door"))
        {
            Door door = other.GetComponent<Door>();
            if (
                door != null
                && HoldsDoorKey
                && _heldDoorKey.DoorKeyValue == door.requiredValue
                )
            {
                door.OpenDoor();
                if (_heldDoorKey.IsQuestBound)
                    Actions.QuestItemTurnedIn(_heldDoorKey.QuestId);

                _heldDoorKey.Collect();
                _heldDoorKey.gameObject.SetActive(false);
                _heldDoorKey = null;
            }
            else if (door != null)
                Debug.Log("You need the correct key to open this door!");
        }

        if (other.CompareTag("Interactable"))
            SubscribePickableFromCollider(other);
    }

    void SubscribePickableFromCollider(Collider other)
    {
        PickableItem item = other.GetComponent<PickableItem>() ?? other.GetComponentInParent<PickableItem>();
        if (item == null) return;

        item.TakeClicked -= CollectItem;
        item.TakeClicked += CollectItem;
        item.ShowUI();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Interactable"))
        {
            PickableItem item = other.GetComponent<PickableItem>() ?? other.GetComponentInParent<PickableItem>();
            if (item != null)
            {
                item.TakeClicked -= CollectItem;
                item.HideUI();
            }
        }
    }

    void TryCollect(Collider other)
    {
        Collectable collectable = other.GetComponent<Collectable>() ?? other.GetComponentInParent<Collectable>();
        if (collectable == null || !collectable.CanCollect) return;

        if (collectable.type == CollectableType.Money)
            CollectMoney(collectable);
    }

    void CollectMoney(Collectable collectable)
    {
        PullToBackpackMoney(collectable, () =>
        {
            _levelValuesManager.AddMoney(collectable.value);
            collectable.SetCollected();
        });
    }

    public void CollectItem(PickableItem item)
    {
        if (item.Type == PickableItemType.Key)
        {
            CollectDoorKeyPickable(item);
            return;
        }

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
        item.transform.DOKill();
        item.transform.SetParent(backpackPoint, true);
        item.transform.localScale = Vector3.one;

        Vector3 targetWorld = backpackPoint.position + Vector3.up * (stackIndex * _itemYOffset);
        Vector3 targetLocal = backpackPoint.InverseTransformPoint(targetWorld);
        PullPickableIntoStack(item, targetLocal);

        if (item.IsQuestBound)
            Actions.QuestCarryingPickablesChanged();
    }

    void CollectDoorKeyPickable(PickableItem key)
    {
        if (key.Type != PickableItemType.Key) return;

        if (IsInventoryFull)
        {
            Debug.Log("You can't carry a key while your inventory is full!");
            return;
        }

        if (_heldDoorKey != null) return;

        key.Collect();
        _heldDoorKey = key;
        key.transform.DOKill();
        key.transform.SetParent(backpackPoint, true);
        key.transform.localScale = Vector3.one;
        DOTween.Sequence()
            .Join(key.transform.DOLocalMove(Vector3.zero, _gameUISettings.shortDelay).SetEase(Ease.InOutQuad))
            .Join(key.transform.DOLocalRotate(Vector3.zero, _gameUISettings.shortDelay).SetEase(Ease.InOutQuad));

        if (key.IsQuestBound)
            Actions.QuestCarryingPickablesChanged();
    }

    public bool TryRemoveLastItem(PickableItemType type, out PickableItem item)
    {
        item = null;

        if (!HasItem(type)) return false;

        item = _items[_items.Count - 1];
        _items.RemoveAt(_items.Count - 1);
        return true;
    }

    /// <summary>Стак бутылок / предметов в рюкзаке с привязкой к квесту.</summary>
    public bool HasQuestItemInInventory(string questId)
    {
        if (string.IsNullOrEmpty(questId))
            return false;

        for (int i = 0; i < _items.Count; i++)
        {
            PickableItem pickable = _items[i];
            if (pickable != null && pickable.IsQuestBound && pickable.QuestId == questId)
                return true;
        }

        return false;
    }

    /// <summary>Любой квестовый pickable в руках: стак или ключ.</summary>
    public bool IsCarryingQuestPickable(string questId)
    {
        if (string.IsNullOrEmpty(questId))
            return false;

        if (HasQuestItemInInventory(questId))
            return true;

        return _heldDoorKey != null
            && _heldDoorKey.IsQuestBound
            && _heldDoorKey.QuestId == questId;
    }

    public List<string> CaptureInventoryItemIds()
    {
        List<string> itemIds = new List<string>();
        for (int i = 0; i < _items.Count; i++)
            itemIds.Add(_items[i].SaveId);

        return itemIds;
    }

    public int GetInventoryIndex(PickableItem item) => _items.IndexOf(item);

    public string CaptureHeldKeyPickableSaveId() => _heldDoorKey != null ? _heldDoorKey.SaveId : null;

    public void ClearHeldDoorKeySlotOnly() => _heldDoorKey = null;

    public void RestoreHeldKeyPickable(PickableItem pickable)
    {
        _heldDoorKey = null;
        if (pickable == null) return;

        pickable.transform.DOKill();
        pickable.gameObject.SetActive(true);
        pickable.Collect();
        pickable.transform.SetParent(backpackPoint, false);
        pickable.transform.localScale = Vector3.one;
        pickable.transform.localPosition = Vector3.zero;
        pickable.transform.localRotation = Quaternion.identity;
        _heldDoorKey = pickable;
    }

    public void RestoreInventory(List<string> itemIds, Dictionary<string, Queue<PickableItem>> poolsById)
    {
        _items.Clear();
        if (itemIds == null) return;

        for (int i = 0; i < itemIds.Count; i++)
        {
            string id = itemIds[i];
            if (string.IsNullOrEmpty(id)) continue;
            if (!poolsById.TryGetValue(id, out Queue<PickableItem> q) || q.Count == 0) continue;

            PickableItem item = q.Dequeue();
            _items.Add(item);
            item.RestoreAsInventoryItem(backpackPoint, i, _itemYOffset);
        }
    }

    void PullToBackpackMoney(Collectable collectable, TweenCallback onComplete)
    {
        Debug.Log($"Player collected a {collectable.type} worth {collectable.value}!");

        collectable.transform.DOKill();
        collectable.transform.SetParent(transform, true);
        collectable.transform.localScale = Vector3.one;
        DOTween.Sequence()
            .Join(collectable.transform.DOLocalMove(Vector3.zero, _gameUISettings.shortDelay).SetEase(Ease.InOutQuad))
            .Join(collectable.transform.DOLocalRotate(Vector3.zero, _gameUISettings.shortDelay).SetEase(Ease.InOutQuad))
            .OnComplete(onComplete);
    }

    void PullPickableIntoStack(PickableItem item, Vector3 localPosition)
    {
        DOTween.Sequence()
            .Join(item.transform.DOLocalMove(localPosition, _gameUISettings.shortDelay).SetEase(Ease.InOutQuad))
            .Join(item.transform.DOLocalRotate(new Vector3(180, 0, 90), _gameUISettings.shortDelay).SetEase(Ease.InOutQuad));
    }
}
