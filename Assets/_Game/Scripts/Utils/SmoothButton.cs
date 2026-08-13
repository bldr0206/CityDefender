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
        // Анимация нажатия - уменьшение; SetUpdate(true) — кнопки работают и на паузе (timeScale = 0)
        transform.DOScale(_originalScale * pressScale, animationDuration).SetUpdate(true);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        // Восстановление размера
        transform.DOScale(_originalScale, animationDuration).SetUpdate(true);
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
        transform.DOScale(_originalScale, animationDuration).SetUpdate(true);
    }
}
