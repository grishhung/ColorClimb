using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataClasses.BusinessLayer;
using DataClasses.BusinessLayer.PendingDecisions;
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

        // Duration for the starting card's deal-land tween
        private const float LandTweenDuration = 0.3f;

        // Duration for a played card's fly-in tween (position + rotation)
        private const float FlyTweenDuration = 0.4f;

        // Persistent card view pool; keyed by Card so views survive across Render() calls
        // and in-flight animations are never destroyed mid-tween.
        private readonly Dictionary<Card, CardView> _viewByCard = new();

        // Ordered list mirroring the discard pile's card order (oldest first)
        private readonly List<Card> _orderedCards = new();

        // TOOLTIP TRACKING
        // We wire tooltip callbacks only on the top card. To cleanly remove them when
        // the top card changes, we store the exact delegate instances used at wiring
        // time so we can -= them precisely. Storing them on the field avoids closure
        // allocation on every Layout call since we only rewire when the top card changes.

        private CardView _tooltipTarget;
        private Action<CardView> _onTopMouseEntered;
        private Action<CardView> _onTopSelected;
        private Action<CardView> _onTopMouseExited;

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
            ClearAll();

            var startingCard = state.DiscardPile.Cards.LastOrDefault();
            if (startingCard == null)
            {
                yield break;
            }

            var cardView = Instantiate(cardPrefab, spawnPoint);
            cardView.Bind(startingCard);
            cardView.SetCanHover(false);
            cardView.SetDimmed(true);
            _viewByCard[startingCard] = cardView;
            _orderedCards.Add(startingCard);

            // Bake the final rest transform into the card view so SetRestState records
            // _basePosition. BakeLayout does this without touching dim/hover.
            BakeLayout(state);

            var restLocalPos = cardView.transform.localPosition;

            // Prevent a one-frame flash at the rest position before the tween begins
            // by immediately writing both the logical layer and the raw transform.
            cardView.SetAnimationYOffset(DrawPileView.CeilingHeight);
            cardView.transform.localPosition = restLocalPos + Vector3.up * DrawPileView.CeilingHeight;

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

            // Apply dim/hover/tooltip now that the card has landed
            ApplyDimState(state);
            RewireTooltip(state, tooltipView);
        }

        // PLAY-CARD FLY ANIMATION

        /// <summary>
        /// Spawns (or reuses) a CardView for the newly-played card and animates it flying
        /// from <paramref name="startWorldPosition"/> / <paramref name="startWorldRotation"/>
        /// (the card's world transform in the hand) to its rest position in the discard pile.
        /// Position and rotation both ease out over FlyTweenDuration seconds.
        ///
        /// Rotation uses the shorter arc between the hand angle and the rest angle:
        /// the signed euler delta on each axis is clamped to [-180, 180] and then driven
        /// directly so the card never spins more than 180 degrees on any axis.
        ///
        /// Because the card is already committed to the discard pile in the data model
        /// before this is called, the next player can act immediately; this coroutine runs
        /// independently and does not block gameplay.
        ///
        /// The card is treated as the new top card from the moment this coroutine begins,
        /// so it is the only non-dimmed card while in flight and after landing.
        /// </summary>
        public IEnumerator PlayCardFlyAnimation(
            Card card,
            Vector3 startWorldPosition,
            Quaternion startWorldRotation,
            GameState state,
            TooltipView tooltipView)
        {
            // Add the card view to the persistent pool immediately so subsequent
            // Render() calls (from other players' actions during the flight) see it.
            if (!_viewByCard.TryGetValue(card, out var cardView))
            {
                cardView = Instantiate(cardPrefab, spawnPoint);
                cardView.Bind(card);
                _viewByCard[card] = cardView;
            }

            // Sync the ordered list to the current discard pile state.
            // (The card was already added to the data pile before this was called.)
            SyncOrderedCards(state.DiscardPile);

            // Compute rest transforms for all cards without touching dim/hover.
            BakeLayout(state);

            // The rest world position and rotation are whatever BakeLayout wrote.
            var restWorldPos = cardView.transform.position;
            var restWorldRot = cardView.transform.rotation;

            // Decompose both rotations to euler angles, then compute the signed
            // per-axis delta clamped to [-180, 180] so we always take the shorter
            // arc on every axis independently (CW vs CCW is determined per-axis).
            var startEuler = startWorldRotation.eulerAngles;
            var restEuler  = restWorldRot.eulerAngles;

            var deltaEuler = new Vector3(
                ShortestAngleDelta(startEuler.x, restEuler.x),
                ShortestAngleDelta(startEuler.y, restEuler.y),
                ShortestAngleDelta(startEuler.z, restEuler.z));

            // Teleport the view to the hand world transform to begin the tween from there.
            cardView.transform.position = startWorldPosition;
            cardView.transform.rotation = startWorldRotation;

            // Make this the only non-dimmed card and wire its tooltip immediately.
            ApplyDimState(state);
            RewireTooltip(state, tooltipView);

            var elapsed = 0f;

            while (elapsed < FlyTweenDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / FlyTweenDuration);

                // Ease out: 1 - (1-t)^2 (fast start, decelerates to rest)
                var eased = 1f - (1f - t) * (1f - t);

                cardView.transform.position    = Vector3.Lerp(startWorldPosition, restWorldPos, eased);
                cardView.transform.eulerAngles = startEuler + deltaEuler * eased;

                yield return null;
            }

            // Snap to exact rest transform and sync SetRestState so hover/jiggle layers
            // are relative to the correct anchor.
            cardView.transform.position = restWorldPos;
            cardView.transform.rotation = restWorldRot;
            cardView.SetRestState(cardView.transform.localPosition, cardView.transform.localScale);
        }

        /// <summary>
        /// Returns the shortest signed delta from <paramref name="from"/> to
        /// <paramref name="to"/> in degrees, in the range [-180, 180]. Positive values
        /// rotate counter-clockwise; negative values rotate clockwise.
        /// </summary>
        private static float ShortestAngleDelta(float from, float to)
        {
            var delta = Mathf.Repeat(to - from + 180f, 360f) - 180f;
            return delta;
        }

        // NORMAL RENDERING

        /// <summary>
        /// Synchronises card views with the current discard pile state. Views for cards
        /// that are still in the pile are reused; views for cards no longer in the pile
        /// are destroyed. New cards (e.g. added during a previous animation) get views
        /// created here if they were not already in the pool.
        ///
        /// This never destroys a view that is currently mid-animation; the animation
        /// coroutine holds a local reference to the CardView and continues tweening
        /// regardless of what Render() does to the pool.
        /// </summary>
        public void Render(CardPile pile, GameState state, TooltipView tooltipView)
        {
            SyncOrderedCards(pile);
            ReconcileViewPool(pile);
            Layout(state, tooltipView);
        }

        // INTERNAL LAYOUT

        /// <summary>
        /// Updates the visual transform and dim/hover/tooltip state of every card in
        /// the current ordered list without creating or destroying any views.
        /// </summary>
        private void Layout(GameState state, TooltipView tooltipView)
        {
            if (_orderedCards.Count == 0)
            {
                return;
            }

            BakeLayout(state);
            ApplyDimState(state);
            RewireTooltip(state, tooltipView);
        }

        /// <summary>
        /// Writes the baked position and rotation for every card in the visible window
        /// to its CardView transform, without touching dim/hover/tooltip. Both Layout()
        /// and PlayCardFlyAnimation() call this to compute rest transforms before the
        /// tween starts so those two code paths stay in sync.
        /// </summary>
        private void BakeLayout(GameState state)
        {
            var visibleCards = _orderedCards.TakeLast(maxCards).ToList();

            for (var i = 0; i < visibleCards.Count; i++)
            {
                var card = visibleCards[i];

                if (!_viewByCard.TryGetValue(card, out var cardView))
                {
                    continue;
                }

                // Reset to spawnPoint so the += displacement operations are relative
                // to the pile's origin on every Layout pass.
                cardView.transform.SetPositionAndRotation(
                    spawnPoint.position,
                    spawnPoint.rotation);

                EnsureDiscardLayoutBaked(card);

                if (!card.IsStartingCard)
                {
                    cardView.transform.position += new Vector3(
                        card.CachedDiscardDisplacement.Value.x,
                        0f,
                        card.CachedDiscardDisplacement.Value.y);
                }

                cardView.transform.position    += new Vector3(0f, individualSpacing * i, 0f);
                cardView.transform.eulerAngles += Vector3.up * card.CachedDiscardRotation.Value;

                cardView.SetRestState(cardView.transform.localPosition, cardView.transform.localScale);
            }
        }

        /// <summary>
        /// Computes and caches the rotation and displacement for a card if they have
        /// not already been baked. Safe to call many times; the PRNG only runs once
        /// per card per lifetime.
        /// </summary>
        private void EnsureDiscardLayoutBaked(Card card)
        {
            if (card.CachedDiscardRotation.HasValue)
            {
                return;
            }

            var rand = GetSeededRand(card.Guid);
            card.CachedDiscardRotation    = GetSeededRotation(rand);

            var angle = GetSeededAngle(rand);
            card.CachedDiscardDisplacement = new Vector2(
                Mathf.Cos(angle) * maxDisplacement,
                Mathf.Sin(angle) * maxDisplacement);
        }

        // DIM / HOVER HELPERS

        private void ApplyDimState(GameState state)
        {
            var visibleCards = _orderedCards.TakeLast(maxCards).ToList();
            var suitSelectorOpen = state.PendingDecision is PendingSuitChoice;

            for (var i = 0; i < visibleCards.Count; i++)
            {
                var card = visibleCards[i];

                if (!_viewByCard.TryGetValue(card, out var cardView))
                {
                    continue;
                }

                var isTop = i == visibleCards.Count - 1;

                // Dim all non-top cards unconditionally; dim the top card too while the
                // suit selector is open (a wild was just played).
                cardView.SetDimmed(!isTop || suitSelectorOpen);
                cardView.SetCanHover(isTop && !suitSelectorOpen);
            }
        }

        // TOOLTIP HELPERS

        /// <summary>
        /// Removes tooltip event subscriptions from the previous top card (if any) and
        /// wires them to the new top card. Storing the exact delegate instances avoids
        /// double-subscription if RewireTooltip is called multiple times for the same
        /// top card.
        /// </summary>
        private void RewireTooltip(GameState state, TooltipView tooltipView)
        {
            var visibleCards = _orderedCards.TakeLast(maxCards).ToList();
            if (visibleCards.Count == 0)
            {
                return;
            }

            var topCard = visibleCards[^1];
            if (!_viewByCard.TryGetValue(topCard, out var topView))
            {
                return;
            }

            // Unsubscribe from the previous top card if it changed.
            if (_tooltipTarget != null && _tooltipTarget != topView)
            {
                if (_onTopMouseEntered != null) _tooltipTarget.MouseEntered -= _onTopMouseEntered;
                if (_onTopSelected     != null) _tooltipTarget.Selected     -= _onTopSelected;
                if (_onTopMouseExited  != null) _tooltipTarget.MouseExited  -= _onTopMouseExited;
            }

            // Only rewire if the top card has actually changed; this prevents duplicate
            // subscriptions when Render() is called multiple times for the same top card.
            if (_tooltipTarget == topView)
            {
                return;
            }

            _tooltipTarget = topView;

            _onTopMouseEntered = cv => tooltipView.Show(cv.Card, state, Mouse.current.position.ReadValue());
            _onTopSelected     = _ => tooltipView.Hide();
            _onTopMouseExited  = _ => tooltipView.Hide();

            topView.MouseEntered += _onTopMouseEntered;
            topView.Selected     += _onTopSelected;
            topView.MouseExited  += _onTopMouseExited;
        }

        // POOL MANAGEMENT

        private void SyncOrderedCards(CardPile pile)
        {
            _orderedCards.Clear();
            _orderedCards.AddRange(pile.Cards.TakeLast(maxCards));
        }

        /// <summary>
        /// Destroys views for cards that are no longer in the pile and creates views for
        /// any cards in the pile that do not yet have one.
        /// </summary>
        private void ReconcileViewPool(CardPile pile)
        {
            var pileSet = new HashSet<Card>(pile.Cards);

            // Destroy views for cards that left the pile; clear their tooltip wiring first.
            var toRemove = _viewByCard.Keys.Where(c => !pileSet.Contains(c)).ToList();
            foreach (var card in toRemove)
            {
                var view = _viewByCard[card];
                if (_tooltipTarget == view)
                {
                    if (_onTopMouseEntered != null) view.MouseEntered -= _onTopMouseEntered;
                    if (_onTopSelected     != null) view.Selected     -= _onTopSelected;
                    if (_onTopMouseExited  != null) view.MouseExited  -= _onTopMouseExited;
                    _tooltipTarget = null;
                }
                Destroy(view.gameObject);
                _viewByCard.Remove(card);
            }

            // Create views for cards that entered the pile without going through the fly animation.
            foreach (var card in pile.Cards)
            {
                if (!_viewByCard.ContainsKey(card))
                {
                    var cardView = Instantiate(cardPrefab, spawnPoint);
                    cardView.Bind(card);
                    _viewByCard[card] = cardView;
                }
            }
        }

        private void ClearAll()
        {
            _tooltipTarget = null;
            _onTopMouseEntered = null;
            _onTopSelected = null;
            _onTopMouseExited = null;

            foreach (var view in _viewByCard.Values)
            {
                Destroy(view.gameObject);
            }

            _viewByCard.Clear();
            _orderedCards.Clear();
        }

        // SEEDED RANDOM HELPERS

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
