using System;
using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(PlayableDirector))]
public class QuestCutscene : MonoBehaviour
{
    [SerializeField] private PlayableDirector _director;

    private Action _onFinished;

    void Awake()
    {
        if (_director == null)
            _director = GetComponent<PlayableDirector>();
    }

    void OnDestroy()
    {
        _director.stopped -= HandleStopped;
    }

    public void Play(Action onFinished)
    {
        _onFinished = onFinished;
        Actions.CutsceneStarted();
        _director.stopped += HandleStopped;
        _director.Play();
    }

    void HandleStopped(PlayableDirector director)
    {
        if (director != _director) return;

        _director.stopped -= HandleStopped;

        Actions.CutsceneEnded();

        Action onFinished = _onFinished;
        _onFinished = null;
        onFinished?.Invoke();

        Destroy(gameObject);
    }
}
