using System.Collections.Generic;
using DataClasses.BusinessLayer.PendingDecisions;
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

        // When non-null, a UI decision is waiting for player input. All card-play
        // and draw-pile actions are blocked until this is resolved and cleared.
        public PendingDecision PendingDecision;

        // When nonzero, a draw chain is in progress. The current player must either
        // counter with a matching draw card or accept the burst by clicking the draw pile.
        public int PendingDrawCount;
        // The rank that started the chain; only the same rank can continue it.
        public Rank? PendingDrawRank;

        public Card TopDiscard => DiscardPile.Cards.Count > 0 ? DiscardPile.Cards[^1] : null;

        /// <summary>
        /// Returns true when the player may interact with cards or the draw pile.
        /// False while an animation is running or a UI decision is awaiting input.
        /// </summary>
        public bool ActionsAllowed => PendingDecision == null;
    }
}
