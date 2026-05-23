using System;
using System.Collections.Generic;
using DataClasses.CardPiles;
using UnityEngine;

namespace DataViews
{
    public class DrawPileView : MonoBehaviour
    {
        [SerializeField] private CardView cardPrefab;
        [SerializeField] private Transform spawnPoint;

        [SerializeField] private float individualSpacing = 0.01f;
        [SerializeField] private float floatingCardSpacing = 0.2f;
        [SerializeField] private float pendingDrawLift = 0.5f;

        private readonly List<CardView> _cardViews = new();
        private int _pendingDrawCount;

        // Fired when the player clicks the top card of the draw pile
        public event Action DrawPileClicked;

        public void Render(CardPile pile, int pendingDrawCount = 0)
        {
            _pendingDrawCount = pendingDrawCount;

            Clear();

            foreach (var card in pile.Cards)
            {
                var cardView = Instantiate(cardPrefab, spawnPoint);
                cardView.Bind(card);
                _cardViews.Add(cardView);
            }

            Layout();
        }

        private void Layout()
        {
            if (_cardViews.Count == 0)
            {
                return;
            }

            var floatStartIndex = Mathf.Max(0, _cardViews.Count - _pendingDrawCount);

            for (var i = 0; i < _cardViews.Count; i++)
            {
                var cardView = _cardViews[i];
                var cardViewTransform = cardView.transform;

                float yOffset;
                var isFloating = i >= floatStartIndex;

                if (isFloating)
                {
                    var floatingIndex = i - floatStartIndex;
                    yOffset = individualSpacing * floatStartIndex
                            + pendingDrawLift
                            + floatingCardSpacing * floatingIndex;
                }
                else
                {
                    yOffset = individualSpacing * i;
                }

                cardViewTransform.position += new Vector3(0, yOffset, 0);

                cardView.SetRestState(cardViewTransform.localPosition, cardViewTransform.localScale);
                cardView.SetDimmed(_pendingDrawCount > 0 && !isFloating && i == floatStartIndex - 1);
                cardView.SetCanHover(i == _cardViews.Count - 1);
            }

            // When the top card is hovered, mirror the hover state onto all other floating cards
            if (_pendingDrawCount > 1 && floatStartIndex < _cardViews.Count - 1)
            {
                var topCard = _cardViews[^1];
                var floatingCards = _cardViews.GetRange(floatStartIndex, _cardViews.Count - 1 - floatStartIndex);

                topCard.MouseEntered += _ =>
                {
                    foreach (var card in floatingCards)
                        card.SetHoverState(true);
                };

                topCard.MouseExited += _ =>
                {
                    foreach (var card in floatingCards)
                        card.SetHoverState(false);
                };
            }

            // Wire the top card's click up to our own event
            _cardViews[^1].Clicked += _ => DrawPileClicked?.Invoke();
        }

        private void Clear()
        {
            foreach (var view in _cardViews)
            {
                Destroy(view.gameObject);
            }

            _cardViews.Clear();
        }
    }
}
