using UnityEngine;
using DG.Tweening;
using Zenject;

public class Door : MonoBehaviour
{
    public int requiredValue;
    public GameObject dynamicPart;
    public float offsetY = -2f;

    GameUISettings _gameUISettings;
    [Inject]
    public void Construct(GameUISettings gameUISettings)
    {
        _gameUISettings = gameUISettings;
    }

    public void OpenDoor()
    {
        if (dynamicPart == null) return;
        Vector3 targetPosition = dynamicPart.transform.localPosition + new Vector3(0, offsetY, 0);
        dynamicPart.transform.DOLocalMove(targetPosition, _gameUISettings.shortDelay)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                Debug.Log("Door opened!");
            });
    }
}
