using DataClasses.BusinessLayer;

namespace DataClasses.CardEffects
{
    public class DrawEffect : CardEffect
    {
        private readonly int _amount;

        public DrawEffect(int amount)
        {
            _amount = amount;
        }

        public override void Resolve(GameState state, Player source)
        {
            // Don't deal cards immediately — accumulate into the pending draw chain.
            // The burst is dealt in GameManager.TryDrawCard when the chain is accepted.
            state.PendingDrawCount += _amount;
        }

        public override string GetDescription(GameState state)
        {
            return $"{{Forces the next player to draw {_amount} cards}} "
                + "unless they are able to play a card of the same type. "
                + "This effect stacks.";
        }
    }
}
