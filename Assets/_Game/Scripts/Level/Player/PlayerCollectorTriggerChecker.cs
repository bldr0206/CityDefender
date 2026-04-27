using System;
using UnityEngine;
using DG.Tweening;
using Zenject;

public class PlayerCollectorTriggerChecker : MonoBehaviour
{

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
            Collectable collectable = other.GetComponent<Collectable>();
            if (collectable != null)
            {
                Collect(collectable);
            }
        }

    }

    private void Collect(Collectable collectable)
    {
        Debug.Log($"Player collected a {collectable.type} worth {collectable.value}!");
        collectable.transform.SetParent(transform);
        collectable.transform.DOLocalMove(Vector3.zero,
        _gameUISettings.shortDelay).SetEase(Ease.InOutQuad).OnComplete(() =>
        {
            if (collectable.type == CollectableType.Money)
            {
                _levelValuesManager.AddMoney(collectable.value);
            }

            Destroy(collectable.gameObject);
        });
    }
}
