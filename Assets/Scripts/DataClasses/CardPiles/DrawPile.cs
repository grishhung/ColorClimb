namespace DataClasses.CardPiles
{
    public class DrawPile : CardPile
    {
        public void RefillFromDiscard(DiscardPile discardPile)
        {
            for (var i = 0; i < discardPile.Cards.Count - 1; i++)
            {
                Add(discardPile.Cards[i]);
            }

            discardPile.ClearExceptTop();
            Shuffle();

            // The cards we just shuffled in should not carry IsStartingCard forward
            // (Shuffle already resets that flag, so no extra work needed here)
        }
    }
}
