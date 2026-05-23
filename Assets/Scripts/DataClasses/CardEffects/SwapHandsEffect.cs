using DataClasses.BusinessLayer;

namespace DataClasses.CardEffects
{
    public class SwapHandsEffect : CardEffect
    {
        public override void Resolve(GameState state, Player source)
        {
            var target = state.Players[ChooseTarget(state, source)];
            (source.Hand, target.Hand) = (target.Hand, source.Hand);
        }

        public override string GetDescription(GameState state)
        {
            return "Choose another player and {Swap} your hand with theirs.";
        }

        private static int ChooseTarget(GameState state, Player source)
        {
            // TODO: Replace this placeholder with proper UI selection
            return (state.CurrentPlayerIndex + 1) % state.Players.Count;
        }
    }
}
