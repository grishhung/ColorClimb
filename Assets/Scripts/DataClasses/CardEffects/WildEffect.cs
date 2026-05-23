using DataClasses.BusinessLayer;

namespace DataClasses.CardEffects
{
    public class WildEffect : CardEffect
    {
        public override void Resolve(GameState state, Player source)
        {
            // Nothing to do mechanically — the active suit is set by GameRules.PlayCard.
            // The effect exists so the card has a description.
        }

        public override string GetDescription(GameState state)
        {
            return "Choose any {Active Suit} for the next player to match.";
        }
    }
}
