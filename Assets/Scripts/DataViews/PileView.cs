using DataClasses.CardPiles;
using UnityEngine;

namespace DataViews
{
    public class PileView : MonoBehaviour
    {
        [SerializeField] private CardView cardPrefab;
        [SerializeField] private Transform spawnPoint;

        private CardView _topCard;

        public void RenderTop(CardPile pile)
        {
            if (pile.Cards.Count == 0)
            {
                return;
            }

            var top = pile.Cards[^1];

            if (_topCard == null)
            {
                _topCard = Instantiate(cardPrefab, spawnPoint);
            }

            _topCard.Bind(top);
        }
    }
}
