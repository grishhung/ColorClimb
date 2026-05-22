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
        public event Action<Player, Card> CardClicked;

        public void Bind(Player player)
        {
            _player = player;
            handView.CardClicked += OnCardClicked;
            Render();
        }

        private void OnCardClicked(Card card)
        {
            CardClicked?.Invoke(_player, card);
        }

        public void Render()
        {
            handView.Render(_player.Hand);
        }
        
        public void SetDimmed(bool dimmed)
        {
            handView.SetDimmed(dimmed);
        }
    }
}
