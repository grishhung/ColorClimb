using DataClasses.CardPiles;
using DataClasses.Enums;

namespace DataClasses.BusinessLayer
{
    public static class GameRules
    {
        public static bool CanPlay(Card card, Card topCard, Suit activeSuit)
        {
            if (card == null || topCard == null)
            {
                return false;
            }

            // Wilds always playable
            if (card.Rank is Rank.Wild or Rank.WildDraw4)
                return true;

            // Match against active suit OR rank match
            // Any card can be played after a wild card
            return activeSuit == Suit.Wild || card.Suit == activeSuit || card.Rank == topCard.Rank;
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