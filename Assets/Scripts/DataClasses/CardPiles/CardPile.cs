using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DataClasses.CardPiles
{
    public class CardPile
    {
        private List<Card> _cards = new();

        public IReadOnlyList<Card> Cards => _cards;

        public void Add(Card card)
        {
            _cards.Add(card);
        }
        
        public bool Remove(Card card)
        {
            return _cards.Remove(card);
        }

        public Card Draw()
        {
            if (Cards.Count == 0)
            {
                return null;
            }

            var topCard = Cards[0];
            _cards.RemoveAt(0);

            return topCard;
        }

        public void Shuffle()
        {
            for (var i = 0; i < _cards.Count; i++)
            {
                var randomIndex = Random.Range(i, _cards.Count);

                (_cards[i], _cards[randomIndex]) = (_cards[randomIndex], _cards[i]);
            }
        }
    }
}