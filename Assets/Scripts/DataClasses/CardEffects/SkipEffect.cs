using DataClasses.BusinessLayer;

namespace DataClasses.CardEffects
{
    public class SkipEffect : CardEffect
    {
        public override void Resolve(GameState state, Player source)
        {
            state.SkipCount += 1;
        }
        
        public override string GetDescription(GameState state)
        {
            var pluralSuffix = state.SkipCount != 1 ? "'s" : "s'";
            return $"{{Skips the next {state.SkipCount} player{pluralSuffix} turns.}}";
        }
    }
}