using System;
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

        private readonly List<CardView> _cardViews = new();

        // SWAP-PICKER STATE

        private enum SwapRole { None, Source, Candidate }
        private SwapRole _swapRole = SwapRole.None;

        // True while the mouse button went down inside this hand and hasn't been released.
        private bool _swapMouseDown;

        // Tooltip reference kept so the hand can show/hide it during swap mode.
        private TooltipView _tooltipView;

        private const string SwapTooltipText = "Swap to this adventurer's hand.";

        // RENDERING

        public void Render(Player player, GameState state, TooltipView tooltipView)
        {
            _tooltipView = tooltipView;
            Clear();

            var cards = GetSortedCards(player.Hand);
            foreach (var card in cards)
            {
                var view = Instantiate(cardPrefab, cardParent);
                view.Bind(card);
                view.Selected += OnCardClicked;
                view.Selected += _ => tooltipView.Hide();
                view.MouseEntered += cv => tooltipView.Show(cv.Card, player, state, Mouse.current.position.ReadValue());
                view.MouseExited += _ => tooltipView.Hide();
                _cardViews.Add(view);
            }

            Layout();
            ApplyCurrentDimState(player, state);
        }

        private void OnCardClicked(CardView view)
        {
            CardClicked?.Invoke(view.Card);
        }

        private void Layout()
        {
            if (_cardViews.Count == 0)
            {
                return;
            }

            var cardGaps = _cardViews.Count - 1;
            var totalAngle = Mathf.Min(cardGaps * individualSpacing, maxSpacing);
            var spacingAngle = cardGaps > 0 ? totalAngle / cardGaps : 0f;
            var startAngle = -totalAngle / 2f;

            for (var i = 0; i < _cardViews.Count; i++)
            {
                var angle = startAngle + i * spacingAngle;
                var radians = Mathf.Deg2Rad * (angle + 90);

                var x = fanRadius * Mathf.Cos(radians);
                var z = fanRadius * Mathf.Sin(radians) - fanRadius;

                _cardViews[i].transform.localPosition = new Vector3(-x, 0, z);
                _cardViews[i].transform.localEulerAngles = new Vector3(0, angle, cardTilt);
                _cardViews[i].SetRestState(_cardViews[i].transform.localPosition, _cardViews[i].transform.localScale);
                _cardViews[i].SetCanHover(true);
            }
        }

        private void Clear()
        {
            foreach (var view in _cardViews)
            {
                view.Selected -= OnCardClicked;
                Destroy(view.gameObject);
            }

            _cardViews.Clear();
        }

        private IEnumerable<Card> GetSortedCards(CardPile hand)
        {
            return hand.Cards.OrderBy(c => c.ActiveSuit).ThenBy(c => c.ActiveRank);
        }

        public void ApplyCurrentDimState(Player player, GameState state)
        {
            var isCurrentPlayersTurn = state.Players[state.CurrentPlayerIndex] == player;
            foreach (var cardView in _cardViews)
            {
                // TODO: Make the "unplayable" visual different between active and inactive players
                cardView.SetDimmed(!isCurrentPlayersTurn && !GameRules.CanPlay(player, cardView.Card, state));
            }
        }

        // SWAP-PICKER MODE

        /// <summary>
        /// Puts this hand into swap-picker mode.
        ///
        ///   isSource == true  → source hand: dim all cards, disable hover/click.
        ///   isSource == false → candidate hand: undim all cards, enable group-hover
        ///                       and hand-level click detection.
        /// </summary>
        public void EnterSwapPickerMode(bool isSource)
        {
            _swapRole = isSource ? SwapRole.Source : SwapRole.Candidate;
            _swapMouseDown = false;

            if (isSource)
            {
                // Dim all cards and prevent any interaction.
                foreach (var cv in _cardViews)
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
                foreach (var cv in _cardViews)
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

            foreach (var cv in _cardViews)
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
        /// When the mouse exits any card in a candidate hand, lower the entire hand —
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
            foreach (var cv in _cardViews)
            {
                cv.SetHoverState(hovered);
            }
        }
    }
}
