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
    private LevelSaveController _saveController;
    [Inject]
    public void Construct(DiContainer container, LevelSaveController saveController)
    {
        _container = container;
        _saveController = saveController;
    }
    private GameObject _currentLevel;
    private int _currentLevelIndex = -1;
    private bool _isLevelFinished;
    private LevelSceneUIController _uiController;
    [Inject]
    public void Init(LevelSceneUIController uiController)
    {
        _uiController = uiController;
    }



    public void LoadNextLevel()
    {
        LoadLevel(_currentLevelIndex + 1);
    }

    public void LoadLevel(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < levels.Count)
        {
            DeleteCurrentLevel();
            GameObject nextLevel = levels[levelIndex];
            _currentLevel = _container.InstantiatePrefab(nextLevel, Vector3.zero, Quaternion.identity, null);
            _saveController.SetLevelContext(_currentLevel);
            _currentLevelIndex = levelIndex;
            Game.SetCurrentLevelIndex(_currentLevelIndex);
            Debug.Log($"<color={DebugColor}>Loaded level: {nextLevel.name}</color>");
            StartLevel();
            return;
        }

        Debug.Log($"<color={DebugColor}>No more levels to load!</color>");
    }
    void DeleteCurrentLevel()
    {
        if (_currentLevel != null)
        {
            if (_currentLevel.scene.IsValid())
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
        _isLevelFinished = true;
        Game.SetLevelFinished(true);
        _uiController.WinLevel();
    }

    // LIFE CYCLE
    private void Start()
    {
        SaveData pendingSave = _saveController.LoadPendingSaveData();
        if (pendingSave != null)
        {
            if (testLevelPrefab == null)
                LoadLevel(pendingSave.levelIndex);
            else
                UseTestLevelPrefab();

            _saveController.ApplyLoadedData(pendingSave, resumeFromAutoCheckpoint: false);
            RestoreLevelFinished(pendingSave.isLevelFinished);
            return;
        }

        SaveData autoSave = _saveController.PeekAutoSaveData();
        if (autoSave != null)
        {
            if (testLevelPrefab == null)
                LoadLevel(autoSave.levelIndex);
            else
                UseTestLevelPrefab();

            _saveController.ApplyLoadedData(autoSave, resumeFromAutoCheckpoint: true);
            RestoreLevelFinished(autoSave.isLevelFinished);
            return;
        }

        if (testLevelPrefab == null)
        {
            Debug.Log($"<color={DebugColor}>Test level prefab is not assigned! Loading next level from the list.</color>");
            LoadNextLevel();
        }
        else
        {
            UseTestLevelPrefab();
        }

    }

    private void UseTestLevelPrefab()
    {
        Debug.Log($"<color={DebugColor}>Loading test level prefab: {testLevelPrefab.name}</color>");
        _currentLevel = testLevelPrefab;
        _saveController.SetLevelContext(_currentLevel);
        _currentLevelIndex = 0;
        Game.SetCurrentLevelIndex(_currentLevelIndex);
        StartLevel();
    }

    private void StartLevel()
    {
        _isLevelFinished = false;
        Game.SetLevelFinished(false);
        Game.ResetHiredBots();
        _uiController.LevelStarted();
        Actions.LevelStarted();
    }

    public bool IsLevelFinished()
    {
        return _isLevelFinished;
    }

    public void RestoreLevelFinished(bool isLevelFinished)
    {
        _isLevelFinished = isLevelFinished;
        Game.SetLevelFinished(isLevelFinished);
        if (isLevelFinished)
            _uiController.WinLevel();
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