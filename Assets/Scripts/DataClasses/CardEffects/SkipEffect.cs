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
            return _amount == 1
                ? "The next player's turn is {Skipped}."
                : $"The next {_amount} players' turns are {{{_amount}x Skipped}}.";
        }
    }
}
