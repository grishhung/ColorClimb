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
        public int SkipCount;

        // When non-null, a UI decision is waiting for player input. All card-play
        // and draw-pile actions are blocked until this is resolved and cleared.
        public PendingDecision PendingDecision;

        // When nonzero, a draw chain is in progress. The current player must either
        // counter with a matching draw card or accept the burst by clicking the draw pile.
        public int PendingDrawCount;
        // The rank that started the chain; only the same rank can continue it.
        public Rank? PendingDrawRank;

        /// <summary>
        /// Holds a wild card that has been played (and removed from the player's hand)
        /// but not yet committed to the discard pile. The card waits here while the
        /// suit-selector is open; WildEffect's OnSuitChosen callback sets ActiveSuit
        /// on it, moves it to the discard pile, and clears this field.
        /// </summary>
        public Card StagedCard;

        // ANIMATION LOCKS
        // Each flag corresponds to a category of blocking animation. GameManager sets
        // them before starting a sequence and clears them when the coroutine finishes.
        // Add a new flag here whenever a new animation type needs to block gameplay.

        /// <summary>
        /// True while the opening deal animation is running (cards lifting off the draw
        /// pile and landing in players' hands). Gameplay is locked for the full duration.
        /// </summary>
        public bool IsDealAnimating;

        public Card TopDiscard => DiscardPile.Cards.Count > 0 ? DiscardPile.Cards[^1] : null;

        /// <summary>
        /// Returns true when the player may interact with cards or the draw pile.
        /// False while a UI decision is awaiting input or a blocking animation is playing.
        /// </summary>
        public bool ActionsAllowed => PendingDecision == null && !IsDealAnimating;
    }
}
