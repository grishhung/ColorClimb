using System.Collections.Generic;
using DataClasses.CardPiles;
using DataClasses.Enums;

namespace DataClasses.BusinessLayer
{
    public class GameState
    {
        public DrawPile DrawPile = new();
        public DiscardPile DiscardPile = new();
        public List<Player> Players = new();

        public int CurrentPlayerIndex = 0;
        public GameplayDirection Direction = GameplayDirection.CounterClockwise;
        public Suit ActiveSuit;
        public int SkipCount;

        public Card TopDiscard =>
            DiscardPile.Cards.Count > 0
                ? DiscardPile.Cards[^1]
                : null;
    }
}