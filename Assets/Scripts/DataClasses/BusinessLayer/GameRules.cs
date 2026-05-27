using DataClasses.CardPiles;
using DataClasses.Enums;
using UnityEngine;

namespace DataClasses.BusinessLayer
{
    public static class GameRules
    {
        public static bool CanPlay(Player player, Card card, GameState state)
        {
            if (card == null || state.TopDiscard == null)
            {
                return false;
            }

            // Identical cards can always be played regardless of turn order ("jump-ins")
            if (IsJumpIn(player, card, state))
            {
                Debug.Log("Jump-in allowed");
                return true;
            }

            // Turn enforcement
            if (state.Players[state.CurrentPlayerIndex] != player)
            {
                Debug.Log("Not your turn");
                return false;
            }

            // While a draw chain is active, only the matching draw rank can be played.
            // Everything else is locked out until the chain is accepted or countered.
            if (state.PendingDrawCount > 0)
            {
                return card.Rank == state.PendingDrawRank;
            }

            // Wilds always playable
            if (card.Rank is Rank.Wild or Rank.WildDraw4)
            {
                return true;
            }

            // Match against active suit OR rank match.
            // Any card can be played after a wild card.
            return state.ActiveSuit == Suit.Wild || card.Suit == state.ActiveSuit || card.Rank == state.TopDiscard.Rank;
        }

        /// <summary>
        /// Returns true if the card is an exact rank+suit match for the top discard,
        /// qualifying it as a jump-in regardless of whose turn it is.
        /// </summary>
        public static bool IsJumpIn(Player player, Card card, GameState state)
        {
            if (card == null || state.TopDiscard == null)
            {
                return false;
            }

            return state.Players[state.CurrentPlayerIndex] != player 
                   && card.Rank == state.TopDiscard.Rank 
                   && card.Suit == state.TopDiscard.Suit;
        }

        public static void PlayCard(GameState state, Player player, Card card)
        {
            // Remove from hand
            player.Hand.Remove(card);

            // Add to discard pile
            state.DiscardPile.Add(card);

            // ALWAYS reset suit to played card first
            state.ActiveSuit = card.Suit;

            // Track which rank started or continues the draw chain.
            // This must be set before effects resolve so PendingDrawCount
            // is attributed to the correct rank.
            if (card.Rank is Rank.Draw2 or Rank.WildDraw4)
            {
                state.PendingDrawRank = card.Rank;
            }

            // Apply effects (ONLY state mutations, no turn progression)
            foreach (var effect in card.Effects)
            {
                effect.Resolve(state, player);
            }
        }
    }
}
