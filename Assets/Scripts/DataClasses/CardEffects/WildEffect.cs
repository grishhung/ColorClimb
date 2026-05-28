using DataClasses.BusinessLayer;
using DataClasses.BusinessLayer.PendingDecisions;

namespace DataClasses.CardEffects
{
    public class WildEffect : CardEffect
    {
        public override void Resolve(GameState state, Player source)
        {
            // The wild card is already in state.StagedCard (placed there by
            // GameRules.PlayCard). When the player picks a suit, we lock that
            // colour into the card's ActiveSuit and commit it to the discard pile.
            // No sentinel is needed on GameState; ActionsAllowed == false while
            // PendingDecision exists, so nothing else can interact with the board.
            state.PendingDecision = new PendingSuitChoice(
                chosenSuit =>
                {
                    var staged = state.StagedCard;
                    staged.ActiveSuit = chosenSuit;
                    state.DiscardPile.Add(staged);
                    state.StagedCard = null;
                    state.PendingDecision = null;
                }
            );
        }

        public override string GetDescription(GameState state)
        {
            return "Can be played regardless of the active color or symbol. Allows the active color to be changed.";
        }
    }
}
