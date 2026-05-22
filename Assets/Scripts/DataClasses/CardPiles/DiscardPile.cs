namespace DataClasses.CardPiles
{
    public class DiscardPile : CardPile
    {
        public Card Top => Cards.Count > 0 ? Cards[^1] : null;
    }
}