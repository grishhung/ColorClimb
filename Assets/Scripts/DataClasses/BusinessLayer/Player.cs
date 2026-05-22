using DataClasses.CardPiles;

namespace DataClasses.BusinessLayer
{
    public class Player
    {
        public Hand Hand { get; set; }

        public Player(Hand hand)
        {
            Hand = hand;
        }
    }
}