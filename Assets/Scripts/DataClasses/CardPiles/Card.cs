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
        
        public IReadOnlyList<CardEffect> Effects { get; }

        public Card(Suit suit, Rank rank, params CardEffect[] effects)
        {
            Suit = suit;
            Rank = rank;
            Effects = effects.ToList();
        }

        public override string ToString()
        {
            return $"{{ suit: {Suit}, rank: {Rank}, effects: {Effects.Count} }}";
        }
    }
}
