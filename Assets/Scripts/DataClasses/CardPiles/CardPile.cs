using System.Collections.Generic;
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
            if (_cards.Count == 0)
            {
                return null;
            }

            var topCard = _cards[^1];
            _cards.RemoveAt(_cards.Count - 1);

            return topCard;
        }

        public void Shuffle()
        {
            for (var i = 0; i < _cards.Count; i++)
            {
                // Reset the starting card flag since shuffling screws with the order
                _cards[i].IsStartingCard = false;

                // Restore wild cards (and any future rank/suit-mutating effects) to
                // their original state so they don't carry play-state into the next round.
                // Uncomment when the reshuffle / lives system is implemented:
                // _cards[i].ResetActiveState();

                var randomIndex = Random.Range(i, _cards.Count);
                (_cards[i], _cards[randomIndex]) = (_cards[randomIndex], _cards[i]);
            }
        }

        protected void RemoveAllExceptLast()
        {
            if (_cards.Count <= 1)
            {
                return;
            }

            _cards.RemoveRange(0, _cards.Count - 1);
        }
    }
}
