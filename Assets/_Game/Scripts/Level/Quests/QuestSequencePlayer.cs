using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Проигрыватель квестовых последовательностей: шаги катсцена/диалог/пауза по порядку,
/// затем колбэк завершения. Корутины и Instantiate идут через хост-<see cref="QuestManager"/>.
/// </summary>
public sealed class QuestSequencePlayer
{
    readonly QuestManager _ctx;

    public QuestSequencePlayer(QuestManager ctx)
    {
        _ctx = ctx;
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
                if (step.cutscenePrefab == null)
                {
                    onFinished?.Invoke();
                    return;
                }

                UnityEngine.Object.Instantiate(step.cutscenePrefab).Play(onFinished);
                break;

            case QuestSequenceStepType.Dialogue:
                if (step.dialogueData == null)
                {
                    onFinished?.Invoke();
                    return;
                }

                _ctx.DialogueScreen.Play(step.dialogueData, onFinished);
                break;

            case QuestSequenceStepType.Pause:
                _ctx.Panel.Hide();
                Actions.QuestSequencePauseStarted();
                float pauseSec = Mathf.Max(0f, step.pauseDuration);
                if (pauseSec <= 0f)
                {
                    Actions.QuestSequencePauseEnded();
                    onFinished?.Invoke();
                }
                else
                    _ctx.StartCoroutine(PauseStepRoutine(pauseSec, onFinished));
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
