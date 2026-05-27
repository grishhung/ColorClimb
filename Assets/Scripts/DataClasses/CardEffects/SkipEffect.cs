using DataClasses.BusinessLayer;

namespace DataClasses.CardEffects
{
    public class SkipEffect : CardEffect
    {
        private readonly int _amount;

        public SkipEffect(int amount = 1)
        {
            _amount = amount;
        }

        public override void Resolve(GameState state, Player source)
        {
            state.SkipCount += _amount;
        }

        public override string GetDescription(GameState state)
        {
            var prefix = _amount == 1
                ? "The next adventurer's turn is skipped entirely. "
                : $"The next {_amount} adventurers' turns are skipped entirely. ";
            return prefix + "They draw no cards and play no cards that turn.";
        }
    }
}
