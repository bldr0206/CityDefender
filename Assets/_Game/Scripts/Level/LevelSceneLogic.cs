using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class LevelSceneLogic : MonoBehaviour
{
    // SERIALIZED FIELDS
    [SerializeField] private List<GameObject> levels;
    [SerializeField] private GameObject testLevelPrefab;

    // VARIABLES
    private const string DebugColor = "#ff3aa6";
    private DiContainer _container;
    [Inject]
    public void Construct(DiContainer container)
    {
        _container = container;
    }
    private GameObject _currentLevel;
    private LevelSceneUIController _uiController;
    [Inject]
    public void Init(LevelSceneUIController uiController)
    {
        _uiController = uiController;
    }



    public void LoadNextLevel()
    {
        DeleteCurrentLevel();

        if (levels.Count > 0)
        {
            // ДОПИСАТЬ НОРМАЛЬНУЮ СИСТЕМУ ЗАГРУЗКИ УРОВНЕЙ
            // СЕЙЧАС ПРОСТО ВЗЯТЬ ПЕРВЫЙ ИЗ СПИСКА
            GameObject nextLevel = levels[0];
            _container.InstantiatePrefab(nextLevel, Vector3.zero, Quaternion.identity, null);
            _currentLevel = nextLevel;
            Debug.Log($"<color={DebugColor}>Loaded level: {nextLevel.name}</color>");
        }
        else
        {
            Debug.Log($"<color={DebugColor}>No more levels to load!</color>");
        }
        _uiController.LevelStarted();
        Actions.LevelStarted();
    }
    void DeleteCurrentLevel()
    {
        if (_currentLevel != null)
        {
            Destroy(_currentLevel);
            _currentLevel = null;
        }
    }
    private void LevelFinished()
    {
        // НАДО ИНЖЕКТИТЬ ЭТОТ СКРИПТ В ПЛЕЕРА
        // И ВЫЗЫВАТЬ КОНЕЦ УРОВНЯ
        // КОГДА ВЫПОЛНИЛИ КВЕСТ
        Debug.Log($"<color={DebugColor}>Level finished!</color>");
        // show win screen, give rewards, etc.
        _uiController.WinLevel();
    }

    // LIFE CYCLE
    private void Start()
    {
        if (testLevelPrefab == null)
        {
            Debug.Log($"<color={DebugColor}>Test level prefab is not assigned! Loading next level from the list.</color>");
            LoadNextLevel();
        }
        else
        {
            Debug.Log($"<color={DebugColor}>Loading test level prefab: {testLevelPrefab.name}</color>");
            _currentLevel = testLevelPrefab;
            _uiController.LevelStarted();
            Actions.LevelStarted();
        }

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