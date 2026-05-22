using System;
using System.Collections.Generic;
using System.Linq;
using DataClasses.CardEffects;
using DataClasses.Enums;

namespace DataClasses.CardPiles
{
    public class Card
    {
        public Suit Suit { get; }
        public Rank Rank { get; }

        public bool IsStartingCard { get; set; }
        
        public IReadOnlyList<CardEffect> Effects { get; }
        
        // Used for things like keeping track of random rotation, modifier mapping, etc.
        public Guid Guid;

        public Card(Suit suit, Rank rank, bool isStartingCard, params CardEffect[] effects)
        {
            Suit = suit;
            Rank = rank;
            IsStartingCard = isStartingCard;
            Effects = effects.ToList();
            
            Guid = Guid.NewGuid();
        }

        public override string ToString()
        {
            return $"{{ suit: {Suit}, rank: {Rank}, isStartingCard: {IsStartingCard}. effects: {Effects.Count} }}";
        }
    }
}
