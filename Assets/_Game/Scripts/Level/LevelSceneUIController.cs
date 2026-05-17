using UnityEngine;
using UnityEngine.UI;
using Zenject;
using System.Collections;

public class LevelSceneUIController : MonoBehaviour
{
    [SerializeField] private GameObject gameHudRoot;
    [SerializeField] private GameObject winScreenRoot;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private GameObject joystickRoot;

    GameUISettings _gameSettings;
    bool _wantsGameHudShown;
    int _hudHideBlocks;

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

    void OnEnable()
    {
        Actions.OnCutsceneStarted += HideLevelHud;
        Actions.OnCutsceneEnded += ShowLevelHud;
        Actions.OnDialogueStarted += HideLevelHud;
        Actions.OnDialogueEnded += ShowLevelHud;
        Actions.OnQuestSequencePauseStarted += HideLevelHud;
        Actions.OnQuestSequencePauseEnded += ShowLevelHud;
    }

    void OnDisable()
    {
        Actions.OnCutsceneStarted -= HideLevelHud;
        Actions.OnCutsceneEnded -= ShowLevelHud;
        Actions.OnDialogueStarted -= HideLevelHud;
        Actions.OnDialogueEnded -= ShowLevelHud;
        Actions.OnQuestSequencePauseStarted -= HideLevelHud;
        Actions.OnQuestSequencePauseEnded -= ShowLevelHud;
    }

    void HideLevelHud()
    {
        _hudHideBlocks++;
        ApplyGameHudVisibility();
    }

    void ShowLevelHud()
    {
        _hudHideBlocks = Mathf.Max(0, _hudHideBlocks - 1);
        ApplyGameHudVisibility();
    }

    public void LevelStarted()
    {
        ShowGameHud();
        HideWinscreen();
    }
    public void ShowGameHud()
    {
        _wantsGameHudShown = true;
        ApplyGameHudVisibility();
    }
    public void HideGameHud()
    {
        _wantsGameHudShown = false;
        ApplyGameHudVisibility();
    }
    public void HideWinscreen()
    {
        if (winScreenRoot != null)
            winScreenRoot.SetActive(false);
    }

    public void WinLevel()
    {
        HideGameHud();
        StartCoroutine(ShowWinScreenWithDelay(_gameSettings.standardDelay));
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

    void ApplyGameHudVisibility()
    {
        bool isVisible = _wantsGameHudShown && _hudHideBlocks == 0;

        if (gameHudRoot != null)
            gameHudRoot.SetActive(isVisible);

        if (joystickRoot != null)
            joystickRoot.SetActive(isVisible);
    }
}
