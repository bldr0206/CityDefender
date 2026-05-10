using System.Collections.Generic;
using UnityEngine;
using Zenject;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.AI;

[RequireComponent(typeof(SaveId))]
public class Breakable : MonoBehaviour
{
    [SerializeField] Transform _modelRoot;
    [SerializeField] GameObject _uiRoot;
    [SerializeField] Image _healthBarImage;
    [SerializeField] int _health = 100;
    [SerializeField] int _maxHealth = 100;
    [SerializeField] float _hitPunchScale = 0.12f;
    [SerializeField] float _hitPunchDuration = 0.2f;
    [SerializeField] ParticleSystem _breakParticles;

    [SerializeField] List<BreakableDropEntry> _dropEntries = new List<BreakableDropEntry>();
    [SerializeField] float _maxScatterDistance = 1.5f;
    [SerializeField] float _navMeshSampleRadius = 0.4f;
    [SerializeField] float _lootSeparation = 0.35f;
    [SerializeField] float _flightDurationSeconds = 0.45f;
    [SerializeField] float _arcHeight = 0.65f;

    [SerializeField] Transform _lootRoot;
    [SerializeField] NavMeshObstacle _navMeshObstacle;
    [SerializeField] string _questId;

    GameUISettings _gameUISettings;
    SaveId _saveId;
    Collider[] _colliders;
    Vector3 _modelStartScale;
    Tween _healthTween;
    Tween _hitTween;
    bool _isBroken;
    int _lootSpawnCounter;
    bool _questDamageEnabled;
    bool _questBreakProgressReported;

    public bool IsBroken => _isBroken;
    public string SaveId => GetSaveId().Id;
    public string QuestId => _questId;
    public bool IsQuestBound => !string.IsNullOrEmpty(_questId);
    public bool AllowsAgentDamage => !IsQuestBound || _questDamageEnabled;

    public void SetQuestDamageEnabled(bool enabled)
    {
        _questDamageEnabled = enabled;
    }

    [Inject]
    public void Construct(GameUISettings gameUISettings)
    {
        _gameUISettings = gameUISettings;
    }

    void Awake()
    {
        EnsureRuntimeCaches();
        _health = Mathf.Clamp(_health, 0, _maxHealth);
        _isBroken = _health <= 0;
        ApplyState(false);
    }

    void EnsureRuntimeCaches()
    {
        GetSaveId();
        if (_navMeshObstacle == null)
            TryGetComponent(out _navMeshObstacle);

        if (_colliders == null || _colliders.Length == 0)
            _colliders = GetComponentsInChildren<Collider>();

        if (_modelRoot != null)
            _modelStartScale = _modelRoot.localScale;

        EnsureLootRoot();
    }

    void OnEnable()
    {
        if (IsQuestBound && !_isBroken)
            RegisterQuestBreakable();
    }

    void OnDisable()
    {
        if (IsQuestBound)
            Actions.QuestBreakableUnregistered(this);
    }

    void Start()
    {
        RegisterQuestBreakable();
    }

    void RegisterQuestBreakable()
    {
        if (IsQuestBound && !_isBroken)
            Actions.QuestBreakableRegistered(this);
    }

    void OnDestroy()
    {
        _healthTween?.Kill();
        _hitTween?.Kill();
    }

    public void TakeDamage(int damage)
    {
        if (_isBroken || damage <= 0)
            return;

        if (IsQuestBound && !_questDamageEnabled)
            return;

        _health = Mathf.Max(0, _health - damage);
        UpdateHealthBar(true);
        PlayHitReaction();

        if (_health > 0)
            return;

        Break();
    }

    public BreakableSaveData CaptureSaveData()
    {
        return new BreakableSaveData
        {
            id = SaveId,
            health = _health,
            isBroken = _isBroken,
            lootIdCounter = _lootSpawnCounter,
        };
    }

    public void RestoreSaveData(BreakableSaveData data)
    {
        EnsureRuntimeCaches();
        _health = Mathf.Clamp(data.health, 0, _maxHealth);
        _lootSpawnCounter = data.lootIdCounter;
        _isBroken = data.isBroken || _health <= 0;
        _questBreakProgressReported = false;
        ApplyState(false);
    }

    /// <summary>Воссоздание Collectable-слота после загрузки, если объект не в сцене.</summary>
    public bool TryRestoreDroppedCollectable(CollectableSaveData data)
    {
        EnsureRuntimeCaches();
        if (!_isBroken || data == null || !data.spawnedLootFromBreak ||
            !string.Equals(data.spawnedByBreakableId, SaveId))
            return false;
        if (data.lootEntryIndex < 0 || data.lootEntryIndex >= _dropEntries.Count)
            return false;
        if (_dropEntries[data.lootEntryIndex].prefab == null)
            return false;
        if (SaveableRegistry.TryGet(data.id, out Collectable existing))
        {
            existing.RestoreSaveData(data);
            if (data.activeSelf && existing.CanCollect)
                NotifyLootPickupReady(existing.gameObject);
            return true;
        }

        return InstantiateDroppedCollectable(data.lootEntryIndex, data.id, data);
    }

    /// <summary>Воссоздание Pickable-слота после загрузки, если объект не в сцене.</summary>
    public bool TryRestoreDroppedPickable(PickableItemSaveData data)
    {
        EnsureRuntimeCaches();
        if (!_isBroken || data == null || !data.spawnedLootFromBreak ||
            data.isCollected || string.IsNullOrEmpty(data.spawnedByBreakableId))
            return false;
        if (!string.Equals(data.spawnedByBreakableId, SaveId))
            return false;
        if (data.lootEntryIndex < 0 || data.lootEntryIndex >= _dropEntries.Count)
            return false;
        if (_dropEntries[data.lootEntryIndex].prefab == null)
            return false;
        if (SaveableRegistry.TryGet(data.id, out PickableItem existing))
        {
            existing.RestoreSaveData(data);
            if (!data.isInInventory && data.activeSelf && !data.isCollected)
                NotifyLootPickupReady(existing.gameObject);
            return true;
        }

        GameObject prefab = _dropEntries[data.lootEntryIndex].prefab;
        EnsureLootRoot();
        GameObject go = Instantiate(prefab, _lootRoot);
        go.SetActive(false);
        go.GetComponent<SaveId>().SetRuntimeId(data.id);

        PickableItem item = go.GetComponent<PickableItem>();
        if (item == null)
        {
            Destroy(go);
            return false;
        }

        item.SetSpawnedFromBreakable(SaveId, data.lootEntryIndex);
        go.SetActive(true);
        item.RestoreSaveData(data);
        if (!data.isInInventory && !data.isCollected)
            item.FinishLootReveal();

        NotifyLootPickupReady(go);
        return true;
    }

    void Break()
    {
        _isBroken = true;
        _hitTween?.Kill();
        _modelRoot.localScale = _modelStartScale;
        PlayBreakParticles();
        ApplyState(false);

        if (IsQuestBound && _questDamageEnabled && !_questBreakProgressReported)
        {
            _questBreakProgressReported = true;
            Actions.QuestBreakableBroken(_questId);
        }

        SpawnLootWave();
    }

    void SpawnLootWave()
    {
        if (_dropEntries == null || _dropEntries.Count == 0)
            return;

        EnsureLootRoot();
        var waveAngles = new LootScatterAngleWave();
        var placed = new List<Vector3>();
        Vector3 originXZ = ResolveScatterOrigin(transform.position);

        for (int e = 0; e < _dropEntries.Count; e++)
        {
            BreakableDropEntry entry = _dropEntries[e];
            int spawnCount = entry.GetSpawnCount();
            GameObject prefab = entry.prefab;
            if (prefab == null || spawnCount == 0)
                continue;

            for (int k = 0; k < spawnCount; k++)
            {
                string runtimeId = $"{SaveId}_loot_{_lootSpawnCounter++}";
                Vector3 landed = BreakableLootPlacement.FindLandingPosition(
                    originXZ,
                    _maxScatterDistance,
                    _navMeshSampleRadius,
                    _lootSeparation,
                    waveAngles,
                    placed);
                placed.Add(landed);
                SpawnFlyingLoot(prefab, e, runtimeId, originXZ + Vector3.up * 0.15f, landed);
            }
        }
    }

    void SpawnFlyingLoot(GameObject prefab, int lootEntryIndex, string runtimeSaveId, Vector3 startWorld,
        Vector3 endWorldWorld)
    {
        GameObject go = Instantiate(prefab, _lootRoot);
        go.SetActive(false);
        go.GetComponent<SaveId>().SetRuntimeId(runtimeSaveId);

        var collectable = go.GetComponent<Collectable>();
        var pickable = go.GetComponent<PickableItem>();
        bool hasPickup = collectable != null || pickable != null;
        if (!hasPickup)
        {
            Destroy(go);
            Debug.LogWarning($"Breakable loot prefab has no Collectable or PickableItem: {prefab.name}");
            return;
        }

        if (collectable != null)
        {
            collectable.SetSpawnedFromBreakable(SaveId, lootEntryIndex);
            collectable.SetCanCollect(false);
        }

        if (pickable != null)
        {
            pickable.SetSpawnedFromBreakable(SaveId, lootEntryIndex);
            pickable.SetInteractionEnabled(false);
        }

        go.SetActive(true);

        LootScatterMotion motion = go.GetComponent<LootScatterMotion>();
        if (motion == null)
            motion = go.AddComponent<LootScatterMotion>();

        motion.FlyToWorldPosition(startWorld, endWorldWorld,
            Mathf.Max(0.05f, _flightDurationSeconds),
            _arcHeight,
            () =>
            {
                if (collectable != null && collectable.gameObject.activeSelf)
                    collectable.SetCanCollect(true);

                pickable?.FinishLootReveal();
                NotifyLootPickupReady(go);
            });
    }

    bool InstantiateDroppedCollectable(int entryIndex, string runtimeId, CollectableSaveData data)
    {
        GameObject prefab = _dropEntries[entryIndex].prefab;
        EnsureLootRoot();
        GameObject go = Instantiate(prefab, _lootRoot);
        go.SetActive(false);
        go.GetComponent<SaveId>().SetRuntimeId(runtimeId);

        var col = go.GetComponent<Collectable>();
        if (col == null)
        {
            Destroy(go);
            return false;
        }

        col.SetSpawnedFromBreakable(SaveId, entryIndex);
        col.SetCanCollect(true);
        go.SetActive(true);
        col.RestoreSaveData(data);
        if (data.activeSelf && col.CanCollect)
            NotifyLootPickupReady(go);
        return true;
    }

    void EnsureLootRoot()
    {
        if (_lootRoot != null)
            return;

        Transform child = transform.Find("LootRoot");
        if (child != null)
        {
            _lootRoot = child;
            return;
        }

        GameObject root = new GameObject("LootRoot");
        root.transform.SetParent(transform, false);
        _lootRoot = root.transform;
    }

    Vector3 ResolveScatterOrigin(Vector3 worldGuess)
    {
        float sample = Mathf.Max(_navMeshSampleRadius, _maxScatterDistance + _navMeshSampleRadius);
        if (NavMesh.SamplePosition(worldGuess, out NavMeshHit hit, sample, NavMesh.AllAreas))
            return hit.position;

        return worldGuess;
    }

    void ApplyState(bool animateHealthBar)
    {
        _modelRoot.gameObject.SetActive(!_isBroken);
        SetCollidersEnabled(!_isBroken);
        SetNavMeshObstacleEnabled(!_isBroken);
        UpdateHealthBar(animateHealthBar);
    }

    void UpdateHealthBar(bool animate)
    {
        _uiRoot.SetActive(!_isBroken && _health < _maxHealth);
        float fillAmount = (float)_health / _maxHealth;

        _healthTween?.Kill();
        if (animate)
        {
            _healthTween = _healthBarImage.DOFillAmount(fillAmount, _gameUISettings.shortDelay);
            return;
        }

        _healthBarImage.fillAmount = fillAmount;
    }

    void PlayHitReaction()
    {
        _hitTween?.Kill();
        _modelRoot.localScale = _modelStartScale;
        _hitTween = _modelRoot
            .DOPunchScale(Vector3.one * _hitPunchScale, _hitPunchDuration, 8)
            .OnComplete(() => _modelRoot.localScale = _modelStartScale);
    }

    void SetCollidersEnabled(bool isEnabled)
    {
        for (int i = 0; i < _colliders.Length; i++)
            _colliders[i].enabled = isEnabled;
    }

    void PlayBreakParticles()
    {
        _breakParticles?.Play();
    }

    void SetNavMeshObstacleEnabled(bool isEnabled)
    {
        if (_navMeshObstacle != null)
            _navMeshObstacle.enabled = isEnabled;
    }

    SaveId GetSaveId()
    {
        if (_saveId == null && !TryGetComponent(out _saveId))
            _saveId = gameObject.AddComponent<SaveId>();

        return _saveId;
    }

    static void NotifyLootPickupReady(GameObject lootRoot)
    {
        if (lootRoot != null && lootRoot.activeInHierarchy)
            Actions.WorldLootPickupReady(lootRoot);
    }
}
