using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BreakableTrigger : MonoBehaviour
{
    [SerializeField] private GameObject _breakableAimObject;
    [SerializeField] private Transform _breakableAimObjectRoot;
    [SerializeField] private float _aimRootScaleInDuration = 0.25f;

    [SerializeField] private GameObject _handPointerObject;
    [SerializeField] private float _handScaleDuration = 0.2f;
    [SerializeField] private float _handPulseDuration = 0.45f;
    [SerializeField] private float _handPulseZScale = 1.12f;

    private readonly List<Transform> _breakables = new();
    private Transform _target;
    private Vector3 _handStartScale;
    private Tween _handScaleTween;
    private Tween _handPulseTween;
    private Tween _aimRootScaleTween;
    private Transform _lastAimTarget;
    private bool _isHandVisible;

    public Transform Target => _target;

    private void Awake()
    {
        _handStartScale = _handPointerObject.transform.localScale;
        _handPointerObject.transform.localScale = Vector3.zero;
        _handPointerObject.SetActive(false);
    }

    private void Update()
    {
        UpdateTarget();
        UpdateAimObject();
    }

    private void OnDestroy()
    {
        _handScaleTween?.Kill();
        _handPulseTween?.Kill();
        _aimRootScaleTween?.Kill();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(GameTags.Breakable) && !_breakables.Contains(other.transform))
        {
            _breakables.Add(other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(GameTags.Breakable))
        {
            _breakables.Remove(other.transform);
        }
    }

    private void UpdateTarget()
    {
        _target = null;
        if (!Game.HasHiredAgents)
        {
            return;
        }

        float closestSqrDistance = float.MaxValue;

        for (int i = _breakables.Count - 1; i >= 0; i--)
        {
            Transform breakable = _breakables[i];
            if (breakable == null ||
                !breakable.gameObject.activeInHierarchy ||
                !breakable.TryGetComponent(out Breakable breakableComponent) ||
                breakableComponent.IsBroken ||
                !breakableComponent.AllowsAgentDamage)
            {
                _breakables.RemoveAt(i);
                continue;
            }

            float sqrDistance = (breakable.position - transform.position).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                _target = breakable;
            }
        }
    }

    private void UpdateAimObject()
    {
        bool hasTarget = _target != null;
        _breakableAimObject.SetActive(hasTarget);
        UpdateHandPointer(hasTarget);

        if (hasTarget)
        {
            if (_target != _lastAimTarget)
            {
                _lastAimTarget = _target;
                PlayAimRootScaleIn();
            }

            _breakableAimObject.transform.position = _target.position;
        }
        else
        {
            _lastAimTarget = null;
            _aimRootScaleTween?.Kill();
            AimRootTransform.localScale = Vector3.one;
        }
    }

    private Transform AimRootTransform =>
        _breakableAimObjectRoot != null ? _breakableAimObjectRoot : _breakableAimObject.transform;

    private void PlayAimRootScaleIn()
    {
        Transform root = AimRootTransform;
        _aimRootScaleTween?.Kill();
        root.localScale = Vector3.one * 3f;
        _aimRootScaleTween = root
            .DOScale(Vector3.one, _aimRootScaleInDuration)
            .SetEase(Ease.OutCubic);
    }

    private void UpdateHandPointer(bool hasTarget)
    {
        if (hasTarget)
        {
            ShowHandPointer();
        }
        else
        {
            HideHandPointer();
        }
    }

    private void ShowHandPointer()
    {
        if (_isHandVisible)
        {
            return;
        }

        _isHandVisible = true;
        _handScaleTween?.Kill();
        _handPulseTween?.Kill();
        _handPointerObject.SetActive(true);
        _handPointerObject.transform.localScale = Vector3.zero;
        _handScaleTween = _handPointerObject.transform
            .DOScale(_handStartScale, _handScaleDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(StartHandPulse);
    }

    private void HideHandPointer()
    {
        if (!_isHandVisible)
        {
            return;
        }

        _isHandVisible = false;
        _handScaleTween?.Kill();
        _handPulseTween?.Kill();
        _handScaleTween = _handPointerObject.transform
            .DOScale(Vector3.zero, _handScaleDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => _handPointerObject.SetActive(false));
    }

    private void StartHandPulse()
    {
        _handPulseTween = _handPointerObject.transform
            .DOScaleZ(_handStartScale.z * _handPulseZScale, _handPulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }
}
