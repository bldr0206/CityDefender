using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Localization;

public class DialogueScreen : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text _dialogueText;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Image _firstCharacterImage;
    [SerializeField] private Image _secondCharacterImage;

    private DialogueData _dialogue;
    private LocalizedString _currentText;
    private Action _onFinished;
    private int _lineIndex;

    private void Awake()
    {
        _nextButton.onClick.AddListener(ShowNextLine);
        root.SetActive(false);
    }

    private void OnDestroy()
    {
        _nextButton.onClick.RemoveListener(ShowNextLine);
        UnsubscribeCurrentText();

        if (_dialogue != null)
        {
            Game.ResumeGame();
        }
    }

    public void Play(DialogueData dialogue, Action onFinished = null)
    {
        Actions.DialogueStarted();

        _dialogue = dialogue;
        _onFinished = onFinished;
        _lineIndex = 0;

        Game.PauseGame();
        root.SetActive(true);

        if (_dialogue.Lines.Count == 0)
        {
            FinishDialogue();
            return;
        }

        ShowLine();
    }

    private void ShowNextLine()
    {
        if (_dialogue == null) return;

        _lineIndex++;

        if (_lineIndex >= _dialogue.Lines.Count)
        {
            FinishDialogue();
            return;
        }

        ShowLine();
    }

    private void ShowLine()
    {
        DialogueLine line = _dialogue.Lines[_lineIndex];

        Image speakerImage = line.speaker == DialogueSpeaker.FirstCharacter
            ? _firstCharacterImage
            : _secondCharacterImage;

        _firstCharacterImage.gameObject.SetActive(line.speaker == DialogueSpeaker.FirstCharacter);
        _secondCharacterImage.gameObject.SetActive(line.speaker == DialogueSpeaker.SecondCharacter);

        speakerImage.sprite = line.characterImage;
        speakerImage.enabled = true;

        UnsubscribeCurrentText();
        if (!_dialogue.HasTextTable || !line.HasText)
        {
            _dialogueText.text = string.Empty;
            return;
        }

        _currentText = new LocalizedString(_dialogue.TextTable, line.textKey);
        _currentText.StringChanged += SetText;
        _dialogueText.text = _currentText.GetLocalizedString();
    }

    private void FinishDialogue()
    {
        Action onFinished = _onFinished;

        UnsubscribeCurrentText();
        root.SetActive(false);
        _dialogue = null;
        _onFinished = null;

        Actions.DialogueEnded();

        Game.ResumeGame();
        onFinished?.Invoke();
    }

    private void UnsubscribeCurrentText()
    {
        if (_currentText == null) return;

        _currentText.StringChanged -= SetText;
        _currentText = null;
    }

    private void SetText(string text)
    {
        _dialogueText.text = text;
    }
}
