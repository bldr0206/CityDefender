using UnityEngine;
using DG.Tweening;
using Zenject;

[RequireComponent(typeof(SaveId))]
public class Door : MonoBehaviour
{
    public int requiredValue;
    public GameObject dynamicPart;
    public float offsetY = -2f;

    GameUISettings _gameUISettings;
    SaveId _saveId;
    Vector3 _closedLocalPosition;
    bool _isOpen;

    public string SaveId => GetSaveId().Id;

    [Inject]
    public void Construct(GameUISettings gameUISettings)
    {
        _gameUISettings = gameUISettings;
    }

    void Awake()
    {
        GetSaveId();
        if (dynamicPart != null)
            _closedLocalPosition = dynamicPart.transform.localPosition;
    }

    public void OpenDoor()
    {
        if (dynamicPart == null || _isOpen) return;

        _isOpen = true;
        Vector3 targetPosition = GetOpenPosition();
        dynamicPart.transform.DOLocalMove(targetPosition, _gameUISettings.shortDelay)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                Debug.Log("Door opened!");
            });
    }

    public DoorSaveData CaptureSaveData()
    {
        return new DoorSaveData
        {
            id = SaveId,
            isOpen = _isOpen,
        };
    }

    public void RestoreSaveData(DoorSaveData data)
    {
        _isOpen = data.isOpen;
        if (dynamicPart != null)
            dynamicPart.transform.localPosition = _isOpen ? GetOpenPosition() : _closedLocalPosition;
    }

    Vector3 GetOpenPosition()
    {
        return _closedLocalPosition + new Vector3(0f, offsetY, 0f);
    }

    SaveId GetSaveId()
    {
        if (_saveId == null && !TryGetComponent(out _saveId))
            _saveId = gameObject.AddComponent<SaveId>();

        return _saveId;
    }
}
