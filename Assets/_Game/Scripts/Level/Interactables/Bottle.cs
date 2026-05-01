using UnityEngine;
using DG.Tweening;

public class Bottle : MonoBehaviour
{

    [SerializeField] private GameObject _uiRoot;


    void Start()
    {
        HideUI();
    }
    public void ShowUI()
    {
        _uiRoot.SetActive(true);
    }

    public void HideUI()
    {
        _uiRoot.SetActive(false);
    }
}
