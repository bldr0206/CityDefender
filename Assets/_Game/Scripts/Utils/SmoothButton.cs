using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class SmoothButton : Button
{
    [Header("Animation Settings")]
    [Range(0.5f, 1f)]
    public float pressScale = 0.95f;

    [Range(0.05f, 0.5f)]
    public float animationDuration = 0.1f;

    private Vector3 _originalScale;

    protected override void Start()
    {
        base.Start();
        _originalScale = transform.localScale;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        // Анимация нажатия - уменьшение
        transform.DOScale(_originalScale * pressScale, animationDuration);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        // Восстановление размера
        transform.DOScale(_originalScale, animationDuration);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        // Можно добавить анимацию наведения
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        // Сброс к оригинальному размеру если выходим во время анимации
        transform.DOScale(_originalScale, animationDuration);
    }
}
