using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class LevelSceneLogic : MonoBehaviour
{
    // SERIALIZED FIELDS
    [SerializeField] private List<GameObject> levels;

    // VARIABLES
    private const string DebugColor = "#ff3aa6";
    private DiContainer _container;

    [Inject]
    public void Construct(DiContainer container)
    {
        _container = container;
    }

    public void LoadNextLevel()
    {
        if (levels.Count > 0)
        {
            // ДОПИСАТЬ НОРМАЛЬНУЮ СИСТЕМУ ЗАГРУЗКИ УРОВНЕЙ
            // СЕЙЧАС ПРОСТО ВЗЯТЬ ПЕРВЫЙ ИЗ СПИСКА
            GameObject nextLevel = levels[0];
            _container.InstantiatePrefab(nextLevel, Vector3.zero, Quaternion.identity, null);
            Debug.Log($"<color={DebugColor}>Loaded level: {nextLevel.name}</color>");
        }
        else
        {
            Debug.Log($"<color={DebugColor}>No more levels to load!</color>");
        }
    }
    private void LevelFinished()
    {
        // НАДО ИНЖЕКТИТЬ ЭТОТ СКРИПТ В ПЛЕЕРА
        // И ВЫЗЫВАТЬ КОНЕЦ УРОВНЯ
        // КОГДА ВЫПОЛНИЛИ КВЕСТ
        Debug.Log($"<color={DebugColor}>Level finished!</color>");
        // show win screen, give rewards, etc.

    }

    // LIFE CYCLE
    private void Start()
    {
        LoadNextLevel();
    }
    private void Awake()
    {
        Actions.OnNextLevelButtonPressed += LoadNextLevel;
        Actions.OnPlayerReachedFinish += LevelFinished;
    }
    private void OnDisable()
    {
        Actions.OnNextLevelButtonPressed -= LoadNextLevel;
        Actions.OnPlayerReachedFinish -= LevelFinished;
    }
}