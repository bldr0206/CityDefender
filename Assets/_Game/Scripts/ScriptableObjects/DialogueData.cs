using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Tables;

[CreateAssetMenu(fileName = "DialogueData", menuName = "Scriptable Objects/DialogueData")]
public class DialogueData : ScriptableObject
{
    [SerializeField] private TableReference textTable;
    [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

    public TableReference TextTable => textTable;
    public IReadOnlyList<DialogueLine> Lines => lines;
    public bool HasTextTable => textTable.ReferenceType != TableReference.Type.Empty;
}

[Serializable]
public class DialogueLine
{
    public DialogueSpeaker speaker;
    public string textKey;
    public Sprite characterImage;

    public bool HasText => !string.IsNullOrWhiteSpace(textKey);
}

public enum DialogueSpeaker
{
    FirstCharacter,
    SecondCharacter
}
