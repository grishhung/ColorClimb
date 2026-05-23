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
            if (card.Rank == state.TopDiscard.Rank && card.Suit == state.TopDiscard.Suit)
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
            
            // Wilds always playable
            if (card.Rank is Rank.Wild or Rank.WildDraw4)
            {
                return true;
            }

            // Match against active suit OR rank match
            // Any card can be played after a wild card
            return state.ActiveSuit == Suit.Wild || card.Suit == state.ActiveSuit || card.Rank == state.TopDiscard.Rank;
        }

        public static void PlayCard(GameState state, Player player, Card card)
        {
            // Remove from hand
            player.Hand.Remove(card);

            // Add to discard pile
            state.DiscardPile.Add(card);
            
            // ALWAYS reset suit to played card first
            state.ActiveSuit = card.Suit;

            // Apply effects (ONLY state mutations, no turn progression)
            foreach (var effect in card.Effects)
            {
                effect.Resolve(state, player);
            }
        }
    }
}