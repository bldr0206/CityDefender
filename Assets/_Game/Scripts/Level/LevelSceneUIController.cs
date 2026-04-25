using UnityEngine;
using UnityEngine.UI;

public class LevelSceneUIController : MonoBehaviour
{
    [SerializeField] private GameObject gameHudRoot;
    [SerializeField] private Button nextLevelButton;

    // LIFE CYCLE
    void Awake()
    {
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevelButtonClicked);
    }



    public void InitUI()
    {
        ShowGameHud();
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
    private void OnNextLevelButtonClicked()
    {
        Actions.NextLevelButtonPressed();
    }

}
