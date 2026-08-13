using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Карусель ботов: горизонтальный ScrollRect со снапом к ближайшей карточке
/// после свайпа. Выбранная карточка держится по центру вьюпорта; тап по карточке
/// выбирает её. Твины на unscaled-времени — меню открыто на паузе.
/// </summary>
public class BotsCarousel : MonoBehaviour, IEndDragHandler
{
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private BotCardView _cardTemplate;
    [SerializeField] private float _snapDuration = 0.2f;

    private readonly List<BotCardView> _cards = new List<BotCardView>();
    private int _selectedIndex = -1;
    private Tween _snapTween;

    public event Action<Bot, int> SelectionChanged;

    private void OnDestroy()
    {
        _snapTween?.Kill();
    }

    public void Show(IReadOnlyList<Bot> bots)
    {
        Clear();

        for (int i = 0; i < bots.Count; i++)
        {
            BotCardView card = Instantiate(_cardTemplate, _scrollRect.content);
            card.gameObject.SetActive(true);
            card.Setup(bots[i], i + 1);
            card.Clicked += HandleCardClicked;
            _cards.Add(card);
        }

        RebuildLayout();
        _selectedIndex = -1;
        Select(0, instant: true);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Select(FindNearestCardIndex(), instant: false);
    }

    private void HandleCardClicked(BotCardView card)
    {
        Select(_cards.IndexOf(card), instant: false);
    }

    private void Select(int index, bool instant)
    {
        if (_cards.Count == 0) return;

        index = Mathf.Clamp(index, 0, _cards.Count - 1);
        if (index != _selectedIndex)
        {
            if (_selectedIndex >= 0)
                _cards[_selectedIndex].SetSelected(false);

            _selectedIndex = index;
            _cards[index].SetSelected(true);
            SelectionChanged?.Invoke(_cards[index].Bot, index);
        }

        SnapTo(index, instant);
    }

    /// <summary> Паддинги по краям, чтобы крайние карточки могли встать по центру вьюпорта. </summary>
    private void RebuildLayout()
    {
        HorizontalLayoutGroup layout = _scrollRect.content.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            float viewportWidth = _scrollRect.viewport.rect.width;
            float cardWidth = ((RectTransform)_cardTemplate.transform).rect.width;
            int padding = Mathf.Max(0, Mathf.RoundToInt((viewportWidth - cardWidth) * 0.5f));
            layout.padding.left = padding;
            layout.padding.right = padding;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.content);
    }

    private void SnapTo(int index, bool instant)
    {
        _snapTween?.Kill();
        _scrollRect.velocity = Vector2.zero;

        RectTransform content = _scrollRect.content;
        float targetX = _scrollRect.viewport.rect.width * 0.5f - _cards[index].Rect.anchoredPosition.x;

        if (instant)
        {
            content.anchoredPosition = new Vector2(targetX, content.anchoredPosition.y);
            return;
        }

        _snapTween = content
            .DOAnchorPosX(targetX, _snapDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    private int FindNearestCardIndex()
    {
        float viewportCenter = _scrollRect.viewport.rect.width * 0.5f;
        float contentX = _scrollRect.content.anchoredPosition.x;

        int nearest = 0;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < _cards.Count; i++)
        {
            float distance = Mathf.Abs(_cards[i].Rect.anchoredPosition.x + contentX - viewportCenter);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = i;
            }
        }

        return nearest;
    }

    private void Clear()
    {
        _snapTween?.Kill();

        for (int i = 0; i < _cards.Count; i++)
        {
            _cards[i].Clicked -= HandleCardClicked;
            Destroy(_cards[i].gameObject);
        }

        _cards.Clear();
        _selectedIndex = -1;
    }
}
