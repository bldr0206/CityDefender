using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MetaSceneLogic : MonoBehaviour
{
    string metaScreenColor = "#7f5cff";

    [SerializeField] private Button playButton;
    [SerializeField] private string battleScene;


    void OnEnable()
    {
        playButton.onClick.AddListener(OnPlayButtonClicked);
    }

    void OnDisable()
    {
        playButton.onClick.RemoveListener(OnPlayButtonClicked);
    }


    #region Button Click Handlers
    public void OnPlayButtonClicked()
    {
        Debug.Log($"<color={metaScreenColor}>Play button clicked!</color>");
        SceneManager.LoadScene(battleScene);
    }
    #endregion
}
