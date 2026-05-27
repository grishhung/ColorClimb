using DataClasses.BusinessLayer;
using DataClasses.Enums;

namespace DataClasses.CardEffects
{
    public class ReverseEffect : CardEffect
    {
        public override void Resolve(GameState state, Player source)
        {
            state.Direction = state.Direction == GameplayDirection.Clockwise
                ? GameplayDirection.CounterClockwise
                : GameplayDirection.Clockwise;
        }

        public override string GetDescription(GameState state)
        {
            var opposite = GameplayDirectionUtils.GetOppositeDirection(state.Direction);
            return $"Changes the turn order to {{{GameplayDirectionUtils.GetString(opposite)}}}.";
        }
    }
}
