namespace DataClasses.CardPiles
{
    public class DiscardPile : CardPile
    {
        public void ClearExceptTop()
        {
            RemoveAllExceptLast();
        }
    }
}
