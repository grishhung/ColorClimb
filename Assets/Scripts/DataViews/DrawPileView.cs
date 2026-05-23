using System;
using System.Collections.Generic;
using DataClasses.CardPiles;
using UnityEngine;

namespace DataViews
{
    public class DrawPileView : MonoBehaviour
    {
        [SerializeField] private CardView cardPrefab;
        [SerializeField] private Transform spawnPoint;

        [SerializeField] private float individualSpacing = 0.01f;
        
        private readonly List<CardView> _cardViews = new();

        public event Action DrawPileClicked;

        public void Render(CardPile pile)
        {
            Clear();
            
            foreach (var card in pile.Cards)
            {
                var cardView = Instantiate(cardPrefab, spawnPoint);
                cardView.Bind(card);
                _cardViews.Add(cardView);
            }

            Layout();
        }
        
        private void Layout()
        {
            if (_cardViews.Count == 0)
            {
                return;
            }

            for (var i = 0; i < _cardViews.Count; i++)
            {
                var cardView = _cardViews[i];
                var cardViewTransform = cardView.transform;
                
                cardViewTransform.position += new Vector3(0, individualSpacing * i, 0);
                
                // Need to set this otherwise the card will vanish on mouse hover
                cardView.SetRestState(cardViewTransform.localPosition, cardViewTransform.localScale);
                cardView.SetCanHover(i == _cardViews.Count - 1);
            }

            // Wire the top card's click up to our own event
            _cardViews[^1].Clicked += _ => DrawPileClicked?.Invoke();
        }
        
        private void Clear()
        {
            foreach (var view in _cardViews)
            {
                Destroy(view.gameObject);
            }

            _cardViews.Clear();
        }
    }
}
