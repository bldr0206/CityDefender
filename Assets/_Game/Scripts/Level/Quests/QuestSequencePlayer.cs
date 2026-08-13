using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Проигрыватель последовательностей: шаги катсцена/диалог/пауза по порядку, затем колбэк завершения.
/// Корутины запускаются на хост-<see cref="MonoBehaviour"/>; квестовая панель опциональна (нет у не-квестовых хостов).
/// </summary>
public sealed class QuestSequencePlayer
{
    readonly MonoBehaviour _coroutineHost;
    readonly DialogueScreen _dialogueScreen;
    readonly QuestPanel _panel;

    public QuestSequencePlayer(MonoBehaviour coroutineHost, DialogueScreen dialogueScreen, QuestPanel panel = null)
    {
        _coroutineHost = coroutineHost;
        _dialogueScreen = dialogueScreen;
        _panel = panel;
    }

    public void Play(List<QuestSequenceStep> sequence, Action onFinished, int index = 0)
    {
        int count = sequence != null ? sequence.Count : 0;
        if (index >= count)
        {
            onFinished?.Invoke();
            return;
        }

        QuestSequenceStep step = sequence[index];
        PlayStep(step, () => Play(sequence, onFinished, index + 1));
    }

    void PlayStep(QuestSequenceStep step, Action onFinished)
    {
        switch (step.type)
        {
            case QuestSequenceStepType.Cutscene:
                if (step.cutsceneShots == null || step.cutsceneShots.Count == 0)
                {
                    onFinished?.Invoke();
                    return;
                }

                ShotCutscene.Play(step.cutsceneShots, onFinished);
                break;

            case QuestSequenceStepType.Dialogue:
                if (step.dialogueData == null)
                {
                    onFinished?.Invoke();
                    return;
                }

                _dialogueScreen.Play(step.dialogueData, onFinished);
                break;

            case QuestSequenceStepType.Pause:
                _panel?.Hide();
                Actions.QuestSequencePauseStarted();
                float pauseSec = Mathf.Max(0f, step.pauseDuration);
                if (pauseSec <= 0f)
                {
                    Actions.QuestSequencePauseEnded();
                    onFinished?.Invoke();
                }
                else
                    _coroutineHost.StartCoroutine(PauseStepRoutine(pauseSec, onFinished));
                break;
        }
    }

    IEnumerator PauseStepRoutine(float seconds, Action onFinished)
    {
        yield return new WaitForSeconds(seconds);
        Actions.QuestSequencePauseEnded();
        onFinished?.Invoke();
    }
}
