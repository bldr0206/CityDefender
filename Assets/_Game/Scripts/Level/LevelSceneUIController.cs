using UnityEngine;
using UnityEngine.UI;
using Zenject;
using System.Collections;
using System.Runtime.InteropServices;

public class LevelSceneUIController : MonoBehaviour
{
    [SerializeField] private GameObject gameHudRoot;
    [SerializeField] private GameObject winScreenRoot;
    [SerializeField] private Button nextLevelButton;

    GameUISettings _gameSettings;

    [Inject]
    public void Construct(GameUISettings gameSettings)
    {
        _gameSettings = gameSettings;
    }

    // LIFE CYCLE
    void Awake()
    {
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevelButtonClicked);
    }



    public void LevelStarted()
    {
        ShowGameHud();
        HideWinscreen();
    }
    public void ShowGameHud()
    {
        if (gameHudRoot != null)
            gameHudRoot.SetActive(true);
    }
    public void HideGameHud()
    {
        if (gameHudRoot != null)
            gameHudRoot.SetActive(false);
    }
    public void HideWinscreen()
    {
        if (winScreenRoot != null)
            winScreenRoot.SetActive(false);
    }

    Coroutine _winScreenCoroutine;
    public void WinLevel()
    {
        HideGameHud();
        _winScreenCoroutine = StartCoroutine(ShowWinScreenWithDelay(_gameSettings.standardDelay));
    }
    IEnumerator ShowWinScreenWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (winScreenRoot != null)
            winScreenRoot.SetActive(true);
    }

    private void OnNextLevelButtonClicked()
    {
        Actions.NextLevelButtonPressed();
    }


}
