using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataClasses.BusinessLayer;
using DataClasses.CardPiles;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = System.Random;

namespace DataViews
{
    public class DiscardPileView : MonoBehaviour
    {
        [SerializeField] private CardView cardPrefab;
        [SerializeField] private Transform spawnPoint;

        [SerializeField] private int maxCards = 16;
        [SerializeField] private float individualSpacing = 0.01f;

        [SerializeField] private float maxRotation = 45f;
        [SerializeField] private float maxDisplacement = 0.25f;

        private readonly List<CardView> _cardViews = new();

        // Duration for the starting card's land tween; matches the hand land duration
        private const float LandTweenDuration = 0.3f;

        public void Render(CardPile pile, GameState state, TooltipView tooltipView)
        {
            Clear();

            foreach (var card in pile.Cards.TakeLast(maxCards).ToList())
            {
                var cardView = Instantiate(cardPrefab, spawnPoint);
                cardView.Bind(card);
                _cardViews.Add(cardView);
            }

            Layout(state, tooltipView);
        }

        // DEAL LAND ANIMATION

        /// <summary>
        /// Animates the starting card landing on the discard pile. The card view is
        /// created at its final rest position with a Y animation offset of CeilingHeight,
        /// then tweens downward to Y = 0 easing out, matching the feel of the hand deal.
        ///
        /// Layout() is called internally after the tween so rest-state and tooltip wiring
        /// are applied correctly; the caller does not need to call Render() afterward.
        ///
        /// Yields until the card has fully landed.
        /// </summary>
        public IEnumerator PlayDealLandAnimation(GameState state, TooltipView tooltipView)
        {
            Clear();

            var startingCard = state.DiscardPile.Cards.LastOrDefault();
            if (startingCard == null)
            {
                yield break;
            }

            var cardView = Instantiate(cardPrefab, spawnPoint);
            cardView.Bind(startingCard);
            cardView.SetCanHover(false);
            cardView.SetDimmed(true);
            _cardViews.Add(cardView);

            // Bake the final rest transform (displacement, rotation, spacing) into the
            // card view. Layout() calls SetRestState(), which records _basePosition and
            // resets _animationOffset to zero.
            Layout(state, tooltipView);

            // After Layout(), cardView.transform.localPosition holds the baked rest
            // position (the result of the world-space += mutations). Capture it so we
            // can write transform.localPosition directly.
            var restLocalPos = cardView.transform.localPosition;

            // SetRestState() (called inside Layout()) sets _basePosition = restLocalPos
            // and zeroes _animationOffset. FlushTransform() will therefore output
            // restLocalPos on the next Update() -- which would show the card at the table
            // surface for one frame before the tween begins.
            //
            // To prevent that flash we set both the logical layer AND the physical
            // transform to the ceiling position right now, before yielding. This matches
            // HandView's approach of writing transform.localPosition directly so the card
            // is never visible at Y = 0.
            cardView.SetAnimationYOffset(DrawPileView.CeilingHeight);
            cardView.transform.localPosition = restLocalPos + Vector3.up * DrawPileView.CeilingHeight;

            // Layout() also overrides the dim/hover flags we set above, because it
            // treats this as the sole top card. Re-apply the animation-phase values.
            cardView.SetCanHover(false);
            cardView.SetDimmed(true);

            var elapsed = 0f;

            while (elapsed < LandTweenDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / LandTweenDuration);

                // Ease out: 1 - (1-t)^2 (starts fast, decelerates into the landing position)
                var eased = 1f - (1f - t) * (1f - t);

                cardView.SetAnimationYOffset(Mathf.Lerp(DrawPileView.CeilingHeight, 0f, eased));
                yield return null;
            }

            cardView.SetAnimationYOffset(0f);

            // Restore normal interactivity now that the card has landed
            cardView.SetDimmed(false);
            cardView.SetCanHover(true);
        }

        // NORMAL RENDERING

        private void Layout(GameState state, TooltipView tooltipView)
        {
            if (_cardViews.Count == 0)
            {
                return;
            }

            for (var i = 0; i < _cardViews.Count; i++)
            {
                var cardView = _cardViews[i];
                var cardViewTransform = cardView.transform;

                // TODO: Tie the offset to the card so we don't need to keep recalculating this
                // When we update the discard pile, the positions must remain the same
                var rand = GetSeededRand(cardView.Card.Guid);
                var rotation = GetSeededRotation(rand);

                if (!cardView.Card.IsStartingCard)
                {
                    var angle = GetSeededAngle(rand);
                    var displacementX = Mathf.Cos(angle) * maxDisplacement;
                    var displacementZ = Mathf.Sin(angle) * maxDisplacement;
                    cardViewTransform.position += new Vector3(displacementX, 0, displacementZ);
                }

                cardViewTransform.position += new Vector3(0, individualSpacing * i, 0);
                cardViewTransform.eulerAngles += Vector3.up * rotation;

                // Need to set this otherwise the card will vanish on mouse hover
                cardView.SetRestState(cardViewTransform.localPosition, cardViewTransform.localScale);
                cardView.SetDimmed(i < _cardViews.Count - 1);
                cardView.SetCanHover(i == _cardViews.Count - 1);
            }

            // Wire tooltip to the top card only
            var topCard = _cardViews[^1];
            topCard.MouseEntered += cv => tooltipView.Show(cv.Card, state, Mouse.current.position.ReadValue());
            topCard.Selected += _ => tooltipView.Hide();
            topCard.MouseExited += _ => tooltipView.Hide();
        }

        private void Clear()
        {
            foreach (var view in _cardViews)
            {
                Destroy(view.gameObject);
            }

            _cardViews.Clear();
        }

        private static Random GetSeededRand(Guid guid)
        {
            var guidBytes = guid.ToByteArray();
            var seed = BitConverter.ToInt32(guidBytes, 0);
            return new Random(seed);
        }

        private float GetSeededRotation(Random rand)
        {
            var randomFloat = (float)rand.NextDouble();
            return (randomFloat * 2f - 1f) * maxRotation;
        }

        private static float GetSeededAngle(Random rand)
        {
            var randomFloat = (float)rand.NextDouble();
            return randomFloat * 2f * Mathf.PI;
        }
    }
}
