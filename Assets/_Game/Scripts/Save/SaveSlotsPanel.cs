using UnityEngine;

public class SaveSlotsPanel : MonoBehaviour
{
    [SerializeField] SaveSlotButton[] _slotButtons;

    void OnEnable()
    {
        Actions.OnSaveCompleted += HandleSlotsChanged;
        Actions.OnLoadCompleted += HandleSlotsChanged;
        Refresh();
    }

    void OnDisable()
    {
        Actions.OnSaveCompleted -= HandleSlotsChanged;
        Actions.OnLoadCompleted -= HandleSlotsChanged;
    }

    public void Refresh()
    {
        for (int i = 0; i < _slotButtons.Length; i++)
        {
            if (_slotButtons[i] != null)
                _slotButtons[i].Refresh();
        }
    }

    void HandleSlotsChanged(string _)
    {
        Refresh();
    }
}
