using DataClasses.BusinessLayer;

namespace DataClasses.CardEffects
{
    public abstract class CardEffect
    {
        public abstract void Resolve(GameState state, Player source);
        public abstract string GetDescription(GameState state);
    }
}