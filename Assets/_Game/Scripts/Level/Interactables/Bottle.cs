using System;
using UnityEngine;

public class Bottle : MonoBehaviour
{
    [SerializeField] private GameObject _uiRoot;

    bool _isCollected;

    public event Action<Bottle> TakeClicked;

    void Start()
    {
        HideUI();
    }

    public void ShowUI()
    {
        if (_isCollected) return;

        _uiRoot.SetActive(true);
    }

    public void HideUI()
    {
        _uiRoot.SetActive(false);
    }

    public void TakeButtonClicked()
    {
        if (_isCollected) return;

        TakeClicked?.Invoke(this);
    }

    public void Collect()
    {
        _isCollected = true;
        TakeClicked = null;
        HideUI();
    }
}
