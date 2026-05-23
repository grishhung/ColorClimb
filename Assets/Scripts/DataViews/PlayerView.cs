using System;
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

        public void Bind(Player player, GameState state, TooltipView tooltipView)
        {
            _player = player;
            _state = state;
            _tooltipView = tooltipView;
            handView.CardClicked += OnCardClicked;
            Render();
        }

        private void OnCardClicked(Card card)
        {
            CardClicked?.Invoke(_player, card);
        }

        public void Render()
        {
            handView.Render(_player, _state, _tooltipView);
        }

        public void ApplyCurrentDimState()
        {
            handView.ApplyCurrentDimState(_player, _state);
        }
    }
}
