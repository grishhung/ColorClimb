using System.Linq;
using DataClasses.BusinessLayer;
using DataClasses.Enums;

namespace DataClasses.CardEffects
{
    public class RotateHandsEffect : CardEffect
    {
        public override void Resolve(GameState state, Player source)
        {
            var hands = state.Players.Select(p => p.Hand).ToList();

            if (state.Direction == GameplayDirection.Clockwise)
            {
                var last = hands[^1];
                for (var i = hands.Count - 1; i > 0; i--)
                    hands[i] = hands[i - 1];
                hands[0] = last;
            }
            else
            {
                var first = hands[0];
                for (var i = 0; i < hands.Count - 1; i++)
                    hands[i] = hands[i + 1];
                hands[^1] = first;
            }

            for (var i = 0; i < state.Players.Count; i++)
                state.Players[i].Hand = hands[i];
        }

        public override string GetDescription(GameState state)
        {
            return $"All players pass their entire hand {GameplayDirectionUtils.GetString(state.Direction)} in {{Turn Order}}.";
        }
    }
}
