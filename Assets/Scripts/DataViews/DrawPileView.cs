using System;
using System.Collections.Generic;
using DataClasses.BusinessLayer;
using DataClasses.CardPiles;
using UnityEngine;
using UnityEngine.InputSystem;

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
        
        private const float MaxJiggleAmount = 0.1f;

        public event Action DrawPileClicked;

        public void Render(CardPile pile, int pendingDrawCount, GameState state, TooltipView tooltipView)
        {
            _pendingDrawCount = pendingDrawCount;
            Clear();

            foreach (var card in pile.Cards)
            {
                var cardView = Instantiate(cardPrefab, spawnPoint);
                cardView.Bind(card);
                _cardViews.Add(cardView);
            }

            Layout(state, tooltipView);
        }

        private void Layout(GameState state, TooltipView tooltipView)
        {
            if (_cardViews.Count == 0)
                return;

            var floatStartIndex = Mathf.Max(0, _cardViews.Count - _pendingDrawCount);

            for (var i = 0; i < _cardViews.Count; i++)
            {
                var cardView = _cardViews[i];
                var cardViewTransform = cardView.transform;

                float yOffset;
                var isFloating = i >= floatStartIndex;

                if (isFloating)
                {
                    // Make the card float
                    var floatingIndex = i - floatStartIndex;
                    yOffset = individualSpacing * floatStartIndex
                              + pendingDrawLift 
                              + floatingCardSpacing * floatingIndex;
                    
                    // Make the card jiggle proportionally to the pending draw amount
                    cardView.JiggleAmount = Mathf.Min(MaxJiggleAmount, _pendingDrawCount * (MaxJiggleAmount / 16));
                }
                else
                {
                    yOffset = individualSpacing * i;
                }

                cardViewTransform.position += new Vector3(0, yOffset, 0);

                // Flip face-down. FlushTransform() never writes rotation, so this
                // sits undisturbed for the lifetime of the card view.
                cardViewTransform.localEulerAngles = new Vector3(0f, 0f, 180f);

                cardView.SetRestState(cardViewTransform.localPosition, cardViewTransform.localScale);

                // A card is clickable when actions are allowed and it's either
                // a floating (pending-draw) card or the lone top card in normal play.
                var isClickable = state.ActionsAllowed && (isFloating || i == _cardViews.Count - 1);

                cardView.SetDimmed(!isClickable);
                cardView.SetCanHover(isClickable);
            }

            if (_pendingDrawCount > 0)
            {
                // Build the tooltip string for the burst
                var burstTooltip = _pendingDrawCount == 1
                    ? "Draw {1} card and {end your turn}."
                    : $"Draw {{{_pendingDrawCount}}} cards and {{end your turn}}.";

                var floatingCards = _cardViews.GetRange(floatStartIndex, _cardViews.Count - floatStartIndex);

                foreach (var floatingCard in floatingCards)
                {
                    var card = floatingCard; // capture for lambda

                    // All floating cards hover together
                    card.MouseEntered += _ =>
                    {
                        foreach (var c in floatingCards)
                            if (c != card) c.SetHoverState(true);
                        tooltipView.Show(burstTooltip, Mouse.current.position.ReadValue());
                    };

                    card.MouseExited += _ =>
                    {
                        foreach (var c in floatingCards)
                            if (c != card) c.SetHoverState(false);
                        tooltipView.Hide();
                    };

                    card.Selected += _ =>
                    {
                        tooltipView.Hide();
                        DrawPileClicked?.Invoke();
                    };
                }
            }
            else
            {
                // Normal draw pile; only the top card is interactive
                var topCard = _cardViews[^1];

                topCard.MouseEntered += _ => tooltipView.Show(
                    "Draw {1} card.",
                    Mouse.current.position.ReadValue());
                topCard.MouseExited += _ => tooltipView.Hide();
                topCard.Selected += _ =>
                {
                    tooltipView.Hide();
                    DrawPileClicked?.Invoke();
                };
            }
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