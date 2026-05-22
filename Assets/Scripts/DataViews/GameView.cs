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
        
        // TODO: Make the draw/discard piles actually look like piles rather than only showing the top card
        // Also give the cards a random "rotation" amount when they are added to the discard pile
        [SerializeField] private PileView drawPileView;
        [SerializeField] private PileView discardPileView;

        private readonly List<PlayerView> _playerViews = new();
        public event Action<Player, Card> CardClicked;
        
        public GameState State;

        public void Bind(GameState state)
        {
            State = state;

            for (var i = 0; i < state.Players.Count; i++)
            {
                var player = state.Players[i];
                var view = Instantiate(playerViewPrefab, playerAnchors[i]);

                view.Bind(player);
                view.CardClicked += OnCardClicked;

                _playerViews.Add(view);
            }

            // Initial Pile Render
            drawPileView.RenderTop(State.DrawPile);
            discardPileView.RenderTop(State.DiscardPile);
        }

        private void OnCardClicked(Player player, Card card)
        {
            CardClicked?.Invoke(player, card);
        }

        public void Refresh()
        {
            for (var i = 0; i < _playerViews.Count; i++)
            {
                var isActive = i == State.CurrentPlayerIndex;

                _playerViews[i].SetDimmed(!isActive);
                _playerViews[i].Render();
            }

            drawPileView.RenderTop(State.DrawPile);
            discardPileView.RenderTop(State.DiscardPile);
        }
    }
}
