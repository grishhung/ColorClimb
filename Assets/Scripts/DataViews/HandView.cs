using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataClasses.BusinessLayer;
using DataClasses.CardPiles;
using UnityEngine.InputSystem;
using UnityEngine;

namespace DataViews
{
    /// <summary>
    /// Renders and manages a player's hand of cards.
    ///
    /// Swap-picker mode (entered via EnterSwapPickerMode / ExitSwapPickerMode):
    ///
    ///   Source hand  - all cards dimmed, no hover, no click, tooltip silent.
    ///   Candidate hand - all cards undimmed; hovering ANY card in the hand
    ///     raises ALL cards together (group hover). Clicking (mouse-down then
    ///     mouse-up while still inside the hand) fires HandClicked.
    ///     Tooltip shows "Swap to this adventurer's hand."
    ///
    /// The mode is fully reversible: ExitSwapPickerMode restores the dim state
    /// and hover flags that were active before the mode was entered.
    /// </summary>
    public class HandView : MonoBehaviour
    {
        [SerializeField] private Transform cardParent;
        [SerializeField] private CardView cardPrefab;

        [SerializeField] private float fanRadius = 20f;
        [SerializeField] private float individualSpacing = 3f;
        [SerializeField] private float maxSpacing = 22.5f;
        [SerializeField] private float cardTilt = -5f;

        // Fired when any card in the hand is clicked (normal play mode).
        public event Action<Card> CardClicked;

        // Fired when this hand is chosen as a swap target.
        public event Action HandClicked;

        // Persistent card view pool; keyed by Card so views survive Render() calls
        // and can slide to their new positions rather than snapping.
        private readonly Dictionary<Card, CardView> _viewByCard = new();

        // Card order as of the most recent Render(); drives Layout() slot assignment.
        private readonly List<Card> _orderedCards = new();

        // SWAP-PICKER STATE

        private enum SwapRole { None, Source, Candidate }
        private SwapRole _swapRole = SwapRole.None;

        // True while the mouse button went down inside this hand and hasn't been released.
        private bool _swapMouseDown;

        // Tooltip reference kept so the hand can show/hide it during swap mode.
        private TooltipView _tooltipView;

        private const string SwapTooltipText = "Swap to this adventurer's hand.";

        // Duration for a single card's land tween.
        private const float LandTweenDuration = 0.3f;

        // Stagger between the start of each card's land tween.
        // Public so GameView can compute per-player delays from the same value.
        public const float LandStaggerInterval = 0.05f;

        // RENDERING

        public void Render(Player player, GameState state, TooltipView tooltipView)
        {
            _tooltipView = tooltipView;

            var isCurrentPlayersTurn = state.Players[state.CurrentPlayerIndex] == player;
            var newCards = GetSortedCards(player.Hand).ToList();
            var newCardSet = new HashSet<Card>(newCards);

            // Destroy views for cards that left the hand
            var toRemove = _viewByCard.Keys.Where(c => !newCardSet.Contains(c)).ToList();
            foreach (var card in toRemove)
            {
                var view = _viewByCard[card];
                view.Selected -= OnCardClicked;
                Destroy(view.gameObject);
                _viewByCard.Remove(card);
            }

            // Create views for cards that are new to the hand
            foreach (var card in newCards)
            {
                if (_viewByCard.ContainsKey(card))
                {
                    continue;
                }

                var view = Instantiate(cardPrefab, cardParent);
                view.Bind(card);

                // Snap dim before the view is ever rendered; no meaningful previous
                // state exists for a brand-new card view.
                var shouldDim = !isCurrentPlayersTurn && !GameRules.CanPlay(player, card, state);
                view.SnapDimmed(shouldDim);

                view.Selected += OnCardClicked;
                view.Selected += _ => tooltipView.Hide();
                view.MouseEntered += cv => tooltipView.Show(cv.Card, player, state, Mouse.current.position.ReadValue());
                view.MouseExited += _ => tooltipView.Hide();

                _viewByCard[card] = view;
            }

            _orderedCards.Clear();
            _orderedCards.AddRange(newCards);

            // Slide surviving cards to their new slots; snap brand-new ones
            Layout(snapAll: false);
            // ApplyCurrentDimState still runs so subsequent SetDimmed calls are correct.
            ApplyCurrentDimState(player, state);
        }

        // WORLD TRANSFORM QUERY

        /// <summary>
        /// Returns the world position and rotation of the CardView currently bound to
        /// <paramref name="card"/>, or null if no such view exists. Used by the play-card
        /// animation to determine where and at what angle the card was sitting in the hand
        /// before the hand is re-rendered.
        /// </summary>
        public (Vector3 position, Quaternion rotation)? GetCardWorldTransform(Card card)
        {
            if (!_viewByCard.TryGetValue(card, out var view))
            {
                return null;
            }

            return (view.transform.position, view.transform.rotation);
        }

        // DEAL LAND ANIMATION

        /// <summary>
        /// Spawns card views for <paramref name="player.Hand"/> at their final layout
        /// positions plus CeilingHeight on the Y axis, then tweens each one downward to
        /// its rest position, staggered by LandStaggerInterval seconds, easing out
        /// (starts fast, decelerates into the landing position).
        ///
        /// Called by PlayerView during the deal animation phase. At the point this runs,
        /// the hand data model is already populated (GameManager dealt into it before
        /// triggering the animation) but no card views exist yet, so this method
        /// creates them.
        ///
        /// Yields until all cards have landed.
        /// </summary>
        public IEnumerator PlayDealLandAnimation(Player player, GameState state, TooltipView tooltipView)
        {
            _tooltipView = tooltipView;
            ClearAll();

            var cards = GetSortedCards(player.Hand).ToList();

            // Pre-compute layout positions so we can apply the ceiling offset before
            // handing off to the tween
            var layoutPositions = ComputeLayoutPositions(cards.Count);

            for (var i = 0; i < cards.Count; i++)
            {
                var view = Instantiate(cardPrefab, cardParent);
                view.Bind(cards[i]);

                // Place the card at its final rest position + ceiling offset so it starts
                // at CeilingHeight and needs to animate down to Y = 0 in animation space
                var restPos = layoutPositions[i].position;
                view.transform.localPosition = restPos;
                view.transform.localEulerAngles = layoutPositions[i].rotation;
                view.SnapRestState(restPos, view.transform.localScale);
                view.SetAnimationYOffset(DrawPileView.CeilingHeight);
                view.SetCanHover(false);
                view.SnapDimmed(true);

                // Wire events; the card is not interactive yet but we wire now so Render()
                // is not required again after the animation finishes
                view.Selected += OnCardClicked;
                view.Selected += _ => tooltipView.Hide();
                view.MouseEntered += cv => tooltipView.Show(cv.Card, player, state, Mouse.current.position.ReadValue());
                view.MouseExited += _ => tooltipView.Hide();

                _viewByCard[cards[i]] = view;
                _orderedCards.Add(cards[i]);
            }

            var tweensFinished = 0;
            var totalCards = _orderedCards.Count;

            for (var i = 0; i < _orderedCards.Count; i++)
            {
                var cardView = _viewByCard[_orderedCards[i]];
                var delay = i * LandStaggerInterval;

                StartCoroutine(LandCardTween(cardView, delay, () =>
                {
                    tweensFinished++;
                }));
            }

            yield return new WaitUntil(() => tweensFinished >= totalCards);
        }

        /// <summary>
        /// Animates a single card falling from Y = CeilingHeight to Y = 0 in animation
        /// space, easing out (1 - (1-t)^2; starts fast, decelerates). Waits
        /// <paramref name="delay"/> seconds before starting, then invokes
        /// <paramref name="onComplete"/> when done.
        /// </summary>
        private IEnumerator LandCardTween(CardView cardView, float delay, Action onComplete)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            var elapsed = 0f;

            while (elapsed < LandTweenDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / LandTweenDuration);

                // Ease out: 1 - (1-t)^2 (starts fast, slows to a stop)
                var eased = 1f - (1f - t) * (1f - t);

                cardView.SetAnimationYOffset(Mathf.Lerp(DrawPileView.CeilingHeight, 0f, eased));
                yield return null;
            }

            cardView.SetAnimationYOffset(0f);
            onComplete?.Invoke();
        }

        // LAYOUT HELPERS

        /// <param name="snapAll">
        /// When true, all cards snap to their new positions immediately (used during
        /// the deal animation and other cases where views are brand new or invisible).
        /// When false, surviving cards slide and only newly created cards snap.
        /// </param>
        private void Layout(bool snapAll)
        {
            if (_orderedCards.Count == 0)
            {
                return;
            }

            var positions = ComputeLayoutPositions(_orderedCards.Count);

            for (var i = 0; i < _orderedCards.Count; i++)
            {
                if (!_viewByCard.TryGetValue(_orderedCards[i], out var cardView))
                {
                    continue;
                }

                var targetLocalPos = positions[i].position;
                var targetLocalRot = positions[i].rotation;

                cardView.transform.localEulerAngles = targetLocalRot;

                if (snapAll)
                {
                    cardView.transform.localPosition = targetLocalPos;
                    cardView.SnapRestState(targetLocalPos, cardView.transform.localScale);
                }
                else
                {
                    // SlideToRestState reads the card's current visual position to
                    // compute the slide offset, so we must NOT move the transform
                    // before calling it; the card slides from where it currently appears.
                    cardView.SlideToRestState(targetLocalPos, cardView.transform.localScale);
                }

                cardView.SetCanHover(true);
            }
        }

        /// <summary>
        /// Returns the final local position and euler rotation for each card slot
        /// based on the fan layout, without moving any actual GameObjects.
        /// Both Layout() and PlayDealLandAnimation() call this so their positioning
        /// logic stays in sync.
        /// </summary>
        private List<(Vector3 position, Vector3 rotation)> ComputeLayoutPositions(int count)
        {
            var result = new List<(Vector3, Vector3)>(count);

            if (count == 0)
            {
                return result;
            }

            var cardGaps = count - 1;
            var totalAngle = Mathf.Min(cardGaps * individualSpacing, maxSpacing);
            var spacingAngle = cardGaps > 0 ? totalAngle / cardGaps : 0f;
            var startAngle = -totalAngle / 2f;

            for (var i = 0; i < count; i++)
            {
                var angle = startAngle + i * spacingAngle;
                var radians = Mathf.Deg2Rad * (angle + 90);

                var x = fanRadius * Mathf.Cos(radians);
                var z = fanRadius * Mathf.Sin(radians) - fanRadius;

                var position = new Vector3(-x, 0, z);
                var rotation = new Vector3(0, angle, cardTilt);

                result.Add((position, rotation));
            }

            return result;
        }

        private void OnCardClicked(CardView view)
        {
            CardClicked?.Invoke(view.Card);
        }

        private void ClearAll()
        {
            foreach (var view in _viewByCard.Values)
            {
                view.Selected -= OnCardClicked;
                Destroy(view.gameObject);
            }

            _viewByCard.Clear();
            _orderedCards.Clear();
        }

        private IEnumerable<Card> GetSortedCards(CardPile hand)
        {
            return hand.Cards.OrderBy(c => c.ActiveSuit).ThenBy(c => c.ActiveRank);
        }

        public void ApplyCurrentDimState(Player player, GameState state)
        {
            var isCurrentPlayersTurn = state.Players[state.CurrentPlayerIndex] == player;
            foreach (var (card, cardView) in _viewByCard)
            {
                // TODO: Make the "unplayable" visual different between active and inactive players
                cardView.SetDimmed(!isCurrentPlayersTurn && !GameRules.CanPlay(player, card, state));
            }
        }

        // SWAP-PICKER MODE

        /// <summary>
        /// Puts this hand into swap-picker mode.
        ///
        ///   isSource == true  -> source hand: dim all cards, disable hover/click.
        ///   isSource == false -> candidate hand: undim all cards, enable group-hover
        ///                        and hand-level click detection.
        /// </summary>
        public void EnterSwapPickerMode(bool isSource)
        {
            _swapRole = isSource ? SwapRole.Source : SwapRole.Candidate;
            _swapMouseDown = false;

            if (isSource)
            {
                // Dim all cards and prevent any interaction.
                foreach (var cv in _viewByCard.Values)
                {
                    cv.SetDimmed(true);
                    cv.SetCanHover(false);
                    cv.MouseEntered -= OnSwapCandidateCardEntered;
                    cv.MouseExited  -= OnSwapCandidateCardExited;
                    cv.Selected     -= OnSwapCandidateCardSelected;
                }
            }
            else
            {
                // Undim all cards and wire group-hover + hand-click callbacks.
                foreach (var cv in _viewByCard.Values)
                {
                    cv.SetDimmed(false);
                    cv.SetHoverState(false);
                    cv.SetCanHover(true);

                    // Remove normal play callback; we don't want individual-card clicks.
                    cv.Selected -= OnCardClicked;

                    // Wire swap-specific callbacks (guard against double-subscription).
                    cv.MouseEntered -= OnSwapCandidateCardEntered;
                    cv.MouseExited  -= OnSwapCandidateCardExited;
                    cv.Selected     -= OnSwapCandidateCardSelected;

                    cv.MouseEntered += OnSwapCandidateCardEntered;
                    cv.MouseExited  += OnSwapCandidateCardExited;
                    cv.Selected     += OnSwapCandidateCardSelected;
                }
            }
        }

        /// <summary>
        /// Restores normal hover/dim state and removes all swap-picker callbacks.
        /// Call this after the swap decision has been resolved (whether committed or cancelled).
        /// </summary>
        public void ExitSwapPickerMode()
        {
            if (_swapRole == SwapRole.None)
            {
                return;
            }

            _swapRole = SwapRole.None;
            _swapMouseDown = false;

            foreach (var cv in _viewByCard.Values)
            {
                // Remove swap callbacks.
                cv.MouseEntered -= OnSwapCandidateCardEntered;
                cv.MouseExited  -= OnSwapCandidateCardExited;
                cv.Selected     -= OnSwapCandidateCardSelected;

                // Restore the normal play callback on candidate cards
                // (source cards never lost it, so guard against double-add).
                cv.Selected -= OnCardClicked;
                cv.Selected += OnCardClicked;

                // Re-enable hover and clear any group-hover visual.
                cv.SetCanHover(true);
                cv.SetHoverState(false);
                cv.SetDimmed(false);
            }
        }

        // SWAP-PICKER INTERNAL CALLBACKS

        /// <summary>
        /// When the mouse enters any card in a candidate hand, raise the entire hand.
        /// </summary>
        private void OnSwapCandidateCardEntered(CardView _)
        {
            SetAllCardsGroupHovered(true);
            _tooltipView?.Show(SwapTooltipText, Mouse.current.position.ReadValue());
        }

        /// <summary>
        /// When the mouse exits any card in a candidate hand, lower the entire hand;
        /// but only if the cursor is no longer over any card in the hand.
        /// </summary>
        private void OnSwapCandidateCardExited(CardView _)
        {
            // Unity fires OnMouseExit before the next OnMouseEnter, so we defer
            // by one frame to see whether another card in this hand picks it up.
            // We use a simple flag approach: lower the hand now; EnterSwapCandidateCardEntered
            // will raise it again immediately if the cursor moved to a sibling card.
            SetAllCardsGroupHovered(false);
            _tooltipView?.Hide();
        }

        /// <summary>
        /// CardView.Selected fires on mouse-up while still hovering the card.
        /// We treat any card in the candidate hand as confirming a hand-level click.
        /// </summary>
        private void OnSwapCandidateCardSelected(CardView _)
        {
            if (_swapRole != SwapRole.Candidate)
            {
                return;
            }

            HandClicked?.Invoke();
        }

        private void SetAllCardsGroupHovered(bool hovered)
        {
            foreach (var cv in _viewByCard.Values)
            {
                cv.SetHoverState(hovered);
            }
        }
    }
}
