using DataClasses.BusinessLayer.PendingDecisions;
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
            if (state.PendingDrawCount > 0)
            {
                return card.ActiveRank == state.PendingDrawRank;
            }

            // Wilds are always playable from hand (their ActiveSuit is still Wild at
            // this point; it only changes when they land in the discard pile).
            if (card.ActiveRank is Rank.Wild or Rank.WildDraw4)
            {
                return true;
            }

            // Safety valve: if the top discard never had a suit locked in (e.g. the
            // starting card was a wild — see TODO in GameManager), treat it as open.
            if (state.TopDiscard.ActiveSuit == Suit.Wild)
            {
                return true;
            }

            // Normal match: suit or rank.
            return card.ActiveSuit == state.TopDiscard.ActiveSuit
                || card.ActiveRank == state.TopDiscard.ActiveRank;
        }

        /// <summary>
        /// Returns true if the card is an exact ActiveRank + ActiveSuit match for the
        /// top discard, qualifying it as a jump-in regardless of whose turn it is.
        ///
        /// Wild cards in hand always have ActiveSuit == Suit.Wild, which cannot match
        /// a played wild whose ActiveSuit was locked to a colour — so previously-played
        /// wilds are correctly immune to jump-ins.
        /// </summary>
        public static bool IsJumpIn(Player player, Card card, GameState state)
        {
            if (card == null || state.TopDiscard == null)
            {
                return false;
            }

            return state.Players[state.CurrentPlayerIndex] != player
                   && card.ActiveRank == state.TopDiscard.ActiveRank
                   && card.ActiveSuit == state.TopDiscard.ActiveSuit;
        }

        public static void PlayCard(GameState state, Player player, Card card)
        {
            // Remove from hand
            player.Hand.Remove(card);

            // Wild cards are held in StagedCard rather than sent straight to the discard
            // pile.  WildEffect's OnSuitChosen callback will set ActiveSuit on the staged
            // card and move it to the discard once the player has chosen a colour.
            // All other cards go to the discard immediately.
            if (card.ActiveRank is Rank.Wild or Rank.WildDraw4)
            {
                state.StagedCard = card;
            }
            else
            {
                state.DiscardPile.Add(card);
            }

            // Track which rank started or continues the draw chain.
            if (card.ActiveRank is Rank.Draw2 or Rank.WildDraw4)
            {
                state.PendingDrawRank = card.ActiveRank;
            }

            // Apply effects (state mutations only — no turn progression)
            foreach (var effect in card.Effects)
            {
                effect.Resolve(state, player);
            }

            // Tag any PendingSuitChoice queued by WildEffect with the played card's rank
            // so GameManager can source the correct label for the suit-selector UI.
            if (state.PendingDecision is PendingSuitChoice suitChoice)
            {
                suitChoice.PlayedRank = card.ActiveRank;
            }
        }
    }
}
