using System;
using System.Collections;
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

        // The Y height (in local space, relative to the draw pile's base position)
        // that cards tween up to during the deal lift phase before flying to hand
        public const float CeilingHeight = 15f;

        // Duration for a single card's lift tween
        private const float LiftTweenDuration = 0.3f;

        // Stagger between the start of each card's lift tween
        private const float LiftStaggerInterval = 0.05f;

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

        // DEAL LIFT ANIMATION

        /// <summary>
        /// Lifts the top <paramref name="cardCount"/> cards off the draw pile one at a time,
        /// staggered by LiftStaggerInterval seconds each, easing in upward to CeilingHeight.
        ///
        /// The cards are left in place (still parented to the draw pile's spawnPoint) with their
        /// animation Y offset at CeilingHeight when the coroutine finishes. HandView is
        /// responsible for de-parenting and landing them in the correct hands.
        ///
        /// Yields until every tween has completed.
        /// </summary>
        public IEnumerator PlayLiftAnimation(int cardCount)
        {
            // The cards to lift are the topmost ones in _cardViews (the last cardCount entries)
            var liftStartIndex = Mathf.Max(0, _cardViews.Count - cardCount);
            var tweensRunning = 0;
            var tweensFinished = 0;

            for (var i = liftStartIndex; i < _cardViews.Count; i++)
            {
                var cardView = _cardViews[i];
                tweensRunning++;

                // The topmost card (highest index) gets localIndex 0 and lifts first;
                // each card below it waits one additional stagger interval, so the
                // animation reads top-to-bottom as expected.
                var localIndex = (_cardViews.Count - 1) - i;

                StartCoroutine(LiftCardTween(cardView, localIndex * LiftStaggerInterval, () =>
                {
                    tweensFinished++;
                }));
            }

            // Wait until every tween has finished
            yield return new WaitUntil(() => tweensFinished >= tweensRunning);
        }

        /// <summary>
        /// Animates a single card rising from Y = 0 to Y = CeilingHeight, easing in
        /// (starts slow, accelerates). Waits <paramref name="delay"/> seconds before starting.
        /// Invokes <paramref name="onComplete"/> when done.
        /// </summary>
        private IEnumerator LiftCardTween(CardView cardView, float delay, Action onComplete)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            var elapsed = 0f;

            while (elapsed < LiftTweenDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / LiftTweenDuration);

                // Ease in: t^2 (starts slow, finishes fast).
                var eased = t * t;

                cardView.SetAnimationYOffset(Mathf.Lerp(0f, CeilingHeight, eased));
                yield return null;
            }

            cardView.SetAnimationYOffset(CeilingHeight);
            onComplete?.Invoke();
        }

        // NORMAL RENDERING

        private void Layout(GameState state, TooltipView tooltipView)
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

                // Views are always freshly created when Layout() runs; snap so
                // there is no single-frame flash at full brightness on a dimmed card.
                cardView.SnapDimmed(!isClickable);
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
                        {
                            if (c != card)
                            {
                                c.SetHoverState(true);
                            }
                        }
                        tooltipView.Show(burstTooltip, Mouse.current.position.ReadValue());
                    };

                    card.MouseExited += _ =>
                    {
                        foreach (var c in floatingCards)
                        {
                            if (c != card)
                            {
                                c.SetHoverState(false);
                            }
                        }
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
