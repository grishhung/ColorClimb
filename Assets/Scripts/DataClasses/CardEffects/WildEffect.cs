using System;
using DataClasses.BusinessLayer;
using DataClasses.BusinessLayer.PendingDecisions;
using DataClasses.Enums;

namespace DataClasses.CardEffects
{
    public class WildEffect : CardEffect
    {
        public override void Resolve(GameState state, Player source)
        {
            // Set a sentinel suit so that nothing is playable until the real choice
            // lands. GameManager will open the suit picker, wait for input, then
            // call OnSuitChosen to commit the chosen suit and clear the decision.
            state.ActiveSuit = Suit.Wild;

            state.PendingDecision = new PendingSuitChoice(
                chosenSuit =>
                {
                    state.ActiveSuit = chosenSuit;
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
