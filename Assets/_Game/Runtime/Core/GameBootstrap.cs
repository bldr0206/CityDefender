using System;
using System.Collections;
using ColorChargeTD.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColorChargeTD.Core
{
    public static class GameSceneIds
    {
        public const string Boot = "Boot";
        public const string MainMenu = "MainMenu";
        public const string Meta = "Meta";
        public const string Battle = "Battle";
    }

    public interface ISceneLoader
    {
        Coroutine LoadScene(MonoBehaviour runner, string sceneName, LoadSceneMode loadSceneMode = LoadSceneMode.Single, Action onLoaded = null);
    }

    public interface IGameStateMachine
    {
        GameFlowState CurrentState { get; }
        event Action<GameFlowState> StateChanged;
        void Enter(GameFlowState newState);
    }

    public interface ILevelSelectionService
    {
        string SelectedLevelId { get; }
        void SelectLevel(string levelId);
    }

    public sealed class UnitySceneLoader : ISceneLoader
    {
        public Coroutine LoadScene(MonoBehaviour runner, string sceneName, LoadSceneMode loadSceneMode = LoadSceneMode.Single, Action onLoaded = null)
        {
            if (runner == null)
            {
                throw new ArgumentNullException(nameof(runner));
            }

            return runner.StartCoroutine(LoadSceneRoutine(sceneName, loadSceneMode, onLoaded));
        }

        private static IEnumerator LoadSceneRoutine(string sceneName, LoadSceneMode loadSceneMode, Action onLoaded)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
            if (operation == null)
            {
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }

            onLoaded?.Invoke();
        }
    }

    public sealed class GameStateMachine : IGameStateMachine
    {
        public GameFlowState CurrentState { get; private set; } = GameFlowState.Boot;

        public event Action<GameFlowState> StateChanged;

        public void Enter(GameFlowState newState)
        {
            if (CurrentState == newState)
            {
                return;
            }

            CurrentState = newState;
            StateChanged?.Invoke(CurrentState);
        }
    }

    public sealed class LevelSelectionService : ILevelSelectionService
    {
        public string SelectedLevelId { get; private set; } = string.Empty;

        public void SelectLevel(string levelId)
        {
            SelectedLevelId = levelId ?? string.Empty;
        }
    }
}
