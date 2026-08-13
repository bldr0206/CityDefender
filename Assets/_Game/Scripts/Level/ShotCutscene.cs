using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

/// <summary>
/// Катсцена из списка кадров (<see cref="CutsceneShot"/>) без Timeline: временные CinemachineCamera
/// поверх камеры игрока, шаблонные ракурсы, переход плавно/мгновенно на каждый кадр.
/// Создаётся кодом через <see cref="Play"/>, по завершении отдаёт камеру игроку и уничтожается.
/// Контракт как у прежней Timeline-катсцены: Actions.CutsceneStarted/Ended, <see cref="CancelActive"/> при загрузке сейва.
/// </summary>
public class ShotCutscene : MonoBehaviour
{
    // Приоритеты выше камеры игрока (1): Active — текущий кадр, Idle — предыдущий на время бленда.
    const int IdlePriority = 10;
    const int ActivePriority = 20;
    const float SmoothBlendSeconds = 1f;

    // Шаблоны ракурсов. TopDown повторяет камеру игрока (CinemachineFollow, offset 0/20/-10, FOV 70).
    static readonly Vector3 TopDownOffset = new Vector3(0f, 20f, -10f);
    static readonly Vector3 CloseOffset = new Vector3(0f, 4f, -5f);
    static readonly Vector3 ZoomInOffset = new Vector3(0f, 10f, -5f);
    const float TopDownFov = 70f;
    const float CloseFov = 60f;
    const float ZoomInFov = 50f;

    static ShotCutscene _active;

    List<CutsceneShot> _shots;
    Action _onFinished;
    CinemachineCamera[] _cameras;
    CutsceneShotTransition _pendingTransition;

    public static void Play(List<CutsceneShot> shots, Action onFinished)
    {
        CancelActive();

        ShotCutscene cutscene = new GameObject(nameof(ShotCutscene)).AddComponent<ShotCutscene>();
        _active = cutscene;
        cutscene._shots = shots;
        cutscene._onFinished = onFinished;
        cutscene._cameras = new[] { cutscene.CreateCamera("Shot Cam A"), cutscene.CreateCamera("Shot Cam B") };
        CinemachineCore.GetBlendOverride += cutscene.OverrideBlend;
        Actions.CutsceneStarted();
        cutscene.StartCoroutine(cutscene.PlayShots());
    }

    /// <summary>Прервать активную катсцену без вызова onFinished (сброс при загрузке сейва).</summary>
    public static void CancelActive()
    {
        if (_active == null) return;

        ShotCutscene cutscene = _active;
        cutscene.Teardown();
        Destroy(cutscene.gameObject);
    }

    void OnDestroy()
    {
        CinemachineCore.GetBlendOverride -= OverrideBlend;

        // Уничтожение извне (выгрузка уровня) — освободить слушателей CutsceneStarted.
        if (_active == this)
            Teardown();
    }

    IEnumerator PlayShots()
    {
        int current = 0;
        for (int i = 0; i < _shots.Count; i++)
        {
            CutsceneShot shot = _shots[i];
            Transform target = shot.target.Resolve();
            if (target == null)
            {
                Debug.LogWarning($"ShotCutscene: у кадра {i} не найден target по id '{shot.target.id}' — кадр пропущен.", this);
                continue;
            }

            CinemachineCamera cam = _cameras[current];
            ApplyShot(cam, shot, target);
            _pendingTransition = shot.transition;
            cam.Priority = ActivePriority;
            _cameras[1 - current].Priority = IdlePriority;
            cam.gameObject.SetActive(true);

            float transitionSeconds = shot.transition == CutsceneShotTransition.Smooth ? SmoothBlendSeconds : 0f;
            yield return new WaitForSeconds(transitionSeconds + Mathf.Max(0f, shot.duration));
            current = 1 - current;
        }

        Finish();
    }

    void Finish()
    {
        Action onFinished = _onFinished;
        Teardown();
        onFinished?.Invoke();

        // Брейн доигрывает возвратный бленд к камере игрока — объект живёт чуть дольше бленда.
        Destroy(gameObject, SmoothBlendSeconds + 0.5f);
    }

    /// <summary>Общий сброс: вернуть камеру игроку, снять blend-хук, послать CutsceneEnded.</summary>
    void Teardown()
    {
        _active = null;
        _onFinished = null;
        StopAllCoroutines();
        CinemachineCore.GetBlendOverride -= OverrideBlend;

        for (int i = 0; i < _cameras.Length; i++)
        {
            if (_cameras[i] != null)
                _cameras[i].gameObject.SetActive(false);
        }

        Actions.CutsceneEnded();
    }

    CinemachineCamera CreateCamera(string cameraName)
    {
        GameObject go = new GameObject(cameraName);
        go.transform.SetParent(transform, false);
        go.SetActive(false);

        CinemachineCamera cam = go.AddComponent<CinemachineCamera>();
        cam.Priority = IdlePriority;
        // Без хинта бленд доворачивает камеру на интерполированную LookAt-точку между целями —
        // видимое «вихляние» с выравниванием в конце. С хинтом — чистый лерп позиции + слерп поворота.
        cam.BlendHint = CinemachineCore.BlendHints.IgnoreTarget;

        CinemachineFollow follow = go.AddComponent<CinemachineFollow>();
        follow.TrackerSettings.BindingMode = BindingMode.WorldSpace;
        // Без демпфирования: при переиспользовании камеры на новом кадре она встаёт на офсет сразу,
        // всё сглаживание даёт бленд между камерами.
        follow.TrackerSettings.PositionDamping = Vector3.zero;

        go.AddComponent<CinemachineHardLookAt>();
        return cam;
    }

    void ApplyShot(CinemachineCamera cam, CutsceneShot shot, Transform target)
    {
        cam.Target.TrackingTarget = target;
        cam.Lens.FieldOfView = FovFor(shot.view);
        cam.GetComponent<CinemachineFollow>().FollowOffset = OffsetFor(shot.view);
    }

    CinemachineBlendDefinition OverrideBlend(
        ICinemachineCamera fromCamera, ICinemachineCamera toCamera,
        CinemachineBlendDefinition defaultBlend, UnityEngine.Object owner)
    {
        if (!ReferenceEquals(toCamera, _cameras[0]) && !ReferenceEquals(toCamera, _cameras[1]))
            return defaultBlend;

        return _pendingTransition == CutsceneShotTransition.Instant
            ? new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f)
            : new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, SmoothBlendSeconds);
    }

    static Vector3 OffsetFor(CutsceneShotView view)
    {
        switch (view)
        {
            case CutsceneShotView.Close: return CloseOffset;
            case CutsceneShotView.ZoomIn: return ZoomInOffset;
            default: return TopDownOffset;
        }
    }

    static float FovFor(CutsceneShotView view)
    {
        switch (view)
        {
            case CutsceneShotView.Close: return CloseFov;
            case CutsceneShotView.ZoomIn: return ZoomInFov;
            default: return TopDownFov;
        }
    }
}
