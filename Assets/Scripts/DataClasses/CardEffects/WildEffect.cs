using DataClasses.BusinessLayer;
using DataClasses.Enums;

namespace DataClasses.CardEffects
{
    public class WildEffect : CardEffect
    {
        public override void Resolve(GameState state, Player source)
        {
            // There's technically nothing special to do here
            // Any card can be played after a wild card since this is cooperative
            // However, we still need this effect so we can have a description
        }
        
        public override string GetDescription(GameState state)
        {
            return "Changes the active color to a color of the active player's choice.";
        }
    }
}