using System;
using System.Collections.Generic;
using DataClasses.BusinessLayer;
using DataClasses.BusinessLayer.PendingDecisions;
using DataClasses.CardPiles;
using DataClasses.Enums;
using UnityEngine;

namespace DataViews
{
    public class GameView : MonoBehaviour
    {
        [SerializeField] private PlayerView playerViewPrefab;
        [SerializeField] private Transform[] playerAnchors;

        [SerializeField] private DrawPileView drawPileView;
        [SerializeField] private DiscardPileView discardPileView;
        [SerializeField] private TooltipView tooltipView;

        [SerializeField] private SuitPickerView suitPickerView;

        // PlayerPickerView is kept in the scene for future use but is no longer
        // called by this view; swap picking now happens directly on the hands.
        [SerializeField] private PlayerPickerView playerPickerView;

        private readonly List<PlayerView> _playerViews = new();

        public event Action<Player, Card> CardClicked;
        public event Action DrawPileClicked;

        private GameState _state;

        // Tracks the live swap-picker listeners so they can be removed cleanly.
        private readonly List<(PlayerView view, Action<Player> handler)> _swapHandlers = new();

        public void Bind(GameState state)
        {
            _state = state;

            for (var i = 0; i < state.Players.Count; i++)
            {
                var player = state.Players[i];
                var view = Instantiate(playerViewPrefab, playerAnchors[i]);

                view.Bind(player, _state, tooltipView);
                view.CardClicked += OnCardClicked;

                _playerViews.Add(view);
            }

            drawPileView.DrawPileClicked += OnDrawPileClicked;

            drawPileView.Render(_state.DrawPile, _state.PendingDrawCount, _state, tooltipView);
            discardPileView.Render(_state.DiscardPile, _state, tooltipView);
        }

        private void OnCardClicked(Player player, Card card)
        {
            CardClicked?.Invoke(player, card);
        }

        private void OnDrawPileClicked()
        {
            DrawPileClicked?.Invoke();
        }

        public void Refresh()
        {
            foreach (var playerView in _playerViews)
            {
                playerView.ApplyCurrentDimState();
                playerView.Render();
            }

            drawPileView.Render(_state.DrawPile, _state.PendingDrawCount, _state, tooltipView);
            discardPileView.Render(_state.DiscardPile, _state, tooltipView);
        }

        // Picker panels

        /// <summary>
        /// Opens the suit picker modal. The callback fires once the player chooses;
        /// the panel closes itself before invoking it.
        /// </summary>
        public void ShowSuitPicker(Action<Suit> onChosen)
        {
            tooltipView.Hide();
            suitPickerView.Show(onChosen);
        }

        /// <summary>
        /// Activates hand-click swap-picker mode.
        ///
        /// The source player's hand dims and becomes non-interactive.
        /// All other players' hands undim, show group-hover, and fire HandChosen
        /// when clicked. The first hand click commits the choice, exits swap mode
        /// on all hands, and invokes onChosen with the chosen player index.
        /// </summary>
        public void ShowHandSwapPicker(int sourcePlayerIndex, Action<int> onChosen)
        {
            tooltipView.Hide();
            _swapHandlers.Clear();

            for (var i = 0; i < _playerViews.Count; i++)
            {
                var isSource = i == sourcePlayerIndex;
                _playerViews[i].EnterSwapPickerMode(isSource);

                if (!isSource)
                {
                    var targetIndex = i; // Capture loop variable.

                    // Named local so we can -= it later.
                    Action<Player> handler = _ => CommitHandSwap(targetIndex, onChosen);
                    _playerViews[i].HandChosen += handler;
                    _swapHandlers.Add((_playerViews[i], handler));
                }
            }
        }

        /// <summary>
        /// Unsubscribes all swap-picker listeners, exits swap mode on every hand,
        /// hides the tooltip, and fires the callback.
        /// Safe to call multiple times; subsequent calls after the first are no-ops
        /// because _swapHandlers is cleared immediately.
        /// </summary>
        private void CommitHandSwap(int targetIndex, Action<int> onChosen)
        {
            // Detach listeners first so a second click can't fire while we're committing.
            foreach (var (view, handler) in _swapHandlers)
            {
                view.HandChosen -= handler;
            }
            _swapHandlers.Clear();

            foreach (var pv in _playerViews)
            {
                pv.ExitSwapPickerMode();
            }

            tooltipView.Hide();
            onChosen?.Invoke(targetIndex);
        }
    }
}
