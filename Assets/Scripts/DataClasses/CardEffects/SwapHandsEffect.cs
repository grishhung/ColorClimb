using DataClasses.BusinessLayer;
using DataClasses.BusinessLayer.PendingDecisions;

namespace DataClasses.CardEffects
{
    public class SwapHandsEffect : CardEffect
    {
        public override void Resolve(GameState state, Player source)
        {
            var sourceIndex = state.Players.IndexOf(source);

            state.PendingDecision = new PendingSwapTargetChoice(
                sourceIndex,
                targetIndex =>
                {
                    var target = state.Players[targetIndex];
                    (source.Hand, target.Hand) = (target.Hand, source.Hand);
                    state.PendingDecision = null;
                }
            );
        }

        public override string GetDescription(GameState state)
        {
            return "Choose another adventurer and swap this hand with theirs.";
        }
    }
}
