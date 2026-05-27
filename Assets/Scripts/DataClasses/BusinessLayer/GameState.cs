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

        // Don't let the players perform actions while animations are occurring and the like
        public bool ActionsAllowed = true;

        // When nonzero, a draw chain is in progress. The current player must either
        // counter with a matching draw card or accept the burst by clicking the draw pile.
        public int PendingDrawCount;
        // The rank that started the chain; only the same rank can continue it.
        public Rank? PendingDrawRank;

        public Card TopDiscard => DiscardPile.Cards.Count > 0 ? DiscardPile.Cards[^1] : null;
    }
}
