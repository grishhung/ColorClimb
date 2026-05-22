using DataClasses.BusinessLayer;
using DataClasses.Enums;

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
            var target = GetTarget(state, source);

            for (var i = 0; i < _amount; i++)
            {
                target.Hand.Add(state.DrawPile.Draw());
            }
        }
        
        public override string GetDescription(GameState state)
        {
            return $"{{Forces the next player to draw {_amount} cards}} "
                + "unless they are able to play a card of the same type. "
                + "This effect stacks.";
        }

        private Player GetTarget(GameState state, Player source)
        {
            var index = state.Players.IndexOf(source);

            // TODO: Possibly introduce a single turn authority rather than duplicating this logic
            var nextIndex = state.Direction switch
            {
                GameplayDirection.Clockwise => (index + 1) % state.Players.Count,
                GameplayDirection.CounterClockwise => (index - 1 + state.Players.Count) % state.Players.Count,
                _ => index
            };

            return state.Players[nextIndex];
        }
    }
}