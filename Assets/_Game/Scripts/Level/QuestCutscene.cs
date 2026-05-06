using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(PlayableDirector))]
public class QuestCutscene : MonoBehaviour
{
    static readonly List<QuestCutscene> ActiveCutscenes = new List<QuestCutscene>();

    [SerializeField] private PlayableDirector _director;

    private Action _onFinished;
    private bool _isCancelled;

    void Awake()
    {
        if (_director == null)
            _director = GetComponent<PlayableDirector>();
    }

    void OnDestroy()
    {
        _isCancelled = true;
        _onFinished = null;
        ActiveCutscenes.Remove(this);

        if (_director != null)
            _director.stopped -= HandleStopped;
    }

    public void Play(Action onFinished)
    {
        _isCancelled = false;
        _onFinished = onFinished;
        ActiveCutscenes.Add(this);
        Actions.CutsceneStarted();
        _director.stopped += HandleStopped;
        _director.Play();
    }

    public void Cancel()
    {
        if (!ActiveCutscenes.Remove(this)) return;

        _isCancelled = true;
        _director.stopped -= HandleStopped;
        _onFinished = null;
        _director.Stop();
        Actions.CutsceneEnded();
        Destroy(gameObject);
    }

    public static void CancelAllActive()
    {
        for (int i = ActiveCutscenes.Count - 1; i >= 0; i--)
        {
            QuestCutscene cutscene = ActiveCutscenes[i];
            if (cutscene != null)
                cutscene.Cancel();
        }

        ActiveCutscenes.Clear();
    }

    void HandleStopped(PlayableDirector director)
    {
        if (director != _director) return;

        _director.stopped -= HandleStopped;
        ActiveCutscenes.Remove(this);

        Actions.CutsceneEnded();
        if (_isCancelled)
        {
            Destroy(gameObject);
            return;
        }

        Action onFinished = _onFinished;
        _onFinished = null;
        onFinished?.Invoke();

        Destroy(gameObject);
    }
}
