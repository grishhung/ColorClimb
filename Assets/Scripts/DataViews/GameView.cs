using System;
using System.Collections.Generic;
using DataClasses.BusinessLayer;
using DataClasses.CardPiles;
using UnityEngine;

namespace DataViews
{
    public class GameView : MonoBehaviour
    {
        [SerializeField] private PlayerView playerViewPrefab;
        [SerializeField] private Transform[] playerAnchors;

        [SerializeField] private DrawPileView drawPileView;
        [SerializeField] private DiscardPileView discardPileView;

        private readonly List<PlayerView> _playerViews = new();

        public event Action<Player, Card> CardClicked;
        public event Action DrawPileClicked;

        private GameState _state;

        public void Bind(GameState state)
        {
            _state = state;

            for (var i = 0; i < state.Players.Count; i++)
            {
                var player = state.Players[i];
                var view = Instantiate(playerViewPrefab, playerAnchors[i]);

                view.Bind(player, _state);
                view.CardClicked += OnCardClicked;

                _playerViews.Add(view);
            }

            drawPileView.DrawPileClicked += OnDrawPileClicked;

            // Initial pile render
            drawPileView.Render(_state.DrawPile);
            discardPileView.Render(_state.DiscardPile);
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

            drawPileView.Render(_state.DrawPile);
            discardPileView.Render(_state.DiscardPile);
        }
    }
}
