using DataClasses.BusinessLayer;

namespace DataClasses.CardEffects
{
    public class WildEffect : CardEffect
    {
        public override void Resolve(GameState state, Player source)
        {
            // Nothing to do mechanically; the active suit is set by GameRules.PlayCard.
            // The effect exists so the card has a description.
        }

        public override string GetDescription(GameState state)
        {
            return "Can be played regardless of the active color or rank. Allows the active color to be changed.";
        }
    }
}
