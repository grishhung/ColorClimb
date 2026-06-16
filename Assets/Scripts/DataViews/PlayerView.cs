using System;
using System.Collections;
using DataClasses.BusinessLayer;
using DataClasses.CardPiles;
using UnityEngine;

namespace DataViews
{
    public class PlayerView : MonoBehaviour
    {
        [SerializeField] private HandView handView;

        private Player _player;
        private GameState _state;
        private TooltipView _tooltipView;

        public event Action<Player, Card> CardClicked;

        // Fired when this player's hand is chosen as a swap target.
        public event Action<Player> HandChosen;

        public Player Player => _player;

        public void Bind(Player player, GameState state, TooltipView tooltipView)
        {
            _player = player;
            _state = state;
            _tooltipView = tooltipView;
            handView.CardClicked += OnCardClicked;
            handView.HandClicked += OnHandClicked;
            Render();
        }

        private void OnCardClicked(Card card)
        {
            CardClicked?.Invoke(_player, card);
        }

        private void OnHandClicked()
        {
            HandChosen?.Invoke(_player);
        }

        public void Render()
        {
            handView.Render(_player, _state, _tooltipView);
        }

        public void ApplyCurrentDimState()
        {
            handView.ApplyCurrentDimState(_player, _state);
        }

        // WORLD TRANSFORM QUERY PASS-THROUGH

        /// <summary>
        /// Returns the world position and rotation of the view currently bound to
        /// <paramref name="card"/> in this player's hand, or null if no view exists.
        /// </summary>
        public (Vector3 position, Quaternion rotation)? GetCardWorldTransform(Card card)
        {
            return handView.GetCardWorldTransform(card);
        }

        // TRANSFER ANIMATION DIM PASS-THROUGH

        /// <summary>
        /// Dims or undims every card in this hand unconditionally. Used during hand
        /// transfer animations to signal that input is paused.
        /// </summary>
        public void SetAllDimmed(bool dimmed)
        {
            handView.SetAllDimmed(dimmed);
        }

        // HAND TRANSFER ANIMATION PASS-THROUGH

        /// <summary>
        /// Tells the CardView bound to <paramref name="card"/> to slide in from
        /// <paramref name="worldPosition"/>. Call after Refresh() has created the new
        /// view at its rest position so the slide goes from old world pos to new rest.
        /// </summary>
        public void SlideCardFromWorldPosition(Card card, Vector3 worldPosition)
        {
            handView.SlideCardFromWorldPosition(card, worldPosition);
        }

        // DEAL ANIMATION PASS-THROUGH

        /// <summary>
        /// Delegates to HandView to run the deal land animation for this player.
        /// See HandView.PlayDealLandAnimation for full details.
        /// </summary>
        public IEnumerator PlayDealLandAnimation(GameState state, TooltipView tooltipView)
        {
            yield return handView.PlayDealLandAnimation(_player, state, tooltipView);
        }

        // SWAP-PICKER PASS-THROUGHS

        /// <summary>
        /// Puts this player's hand into swap-picker mode.
        /// isSource == true  -> the player who played the 7 (hand dims, no interaction).
        /// isSource == false -> a chooseable swap target (undimmed, group-hover, clickable).
        /// </summary>
        public void EnterSwapPickerMode(bool isSource)
        {
            handView.EnterSwapPickerMode(isSource);
        }

        /// <summary>
        /// Restores normal hand state. Call after the swap decision is resolved.
        /// </summary>
        public void ExitSwapPickerMode()
        {
            handView.ExitSwapPickerMode();
        }
    }
}
