using System;
using System.Collections.Generic;
using System.Linq;
using DataClasses.CardPiles;
using UnityEngine;

namespace DataViews
{
    public class HandView : MonoBehaviour
    {
        [SerializeField] private Transform cardParent;
        [SerializeField] private CardView cardPrefab;
        
        [SerializeField] private float fanRadius = 20f;
        [SerializeField] private float individualSpacing = 3f;
        [SerializeField] private float maxSpacing = 24f;
        [SerializeField] private float cardTilt = -5f;
        
        public event Action<Card> CardClicked;
        
        private readonly List<CardView> _cardViews = new();
        private bool _isDimmed;
        public void Render(CardPile hand)
        {
            Clear();
            
            var cards = GetSortedCards(hand);

            foreach (var card in cards)
            {
                var view = Instantiate(cardPrefab, cardParent);
                view.Bind(card);
                view.Clicked += OnCardClicked;
                _cardViews.Add(view);
            }

            Layout();
            ApplyCurrentDimState();
        }
        
        
        private void OnCardClicked(CardView view)
        {
            CardClicked?.Invoke(view.Card);
        }

        private void Layout()
        {
            if (_cardViews.Count == 0)
            {
                return;
            }

            var cardGaps = _cardViews.Count - 1;
            var totalAngle = Mathf.Min(cardGaps * individualSpacing, maxSpacing);
            var spacingAngle = cardGaps > 0 ? totalAngle / cardGaps : 0f;
            var startAngle = -totalAngle / 2f;

            for (var i = 0; i < _cardViews.Count; i++)
            {
                var angle = startAngle + i * spacingAngle;
                var radians = Mathf.Deg2Rad * (angle + 90);

                var x = fanRadius * Mathf.Cos(radians);
                var z = fanRadius * Mathf.Sin(radians) - fanRadius;

                _cardViews[i].transform.localPosition = new Vector3(-x, 0, z);
                _cardViews[i].transform.localEulerAngles = new Vector3(0, angle, cardTilt);
                _cardViews[i].SetRestState(_cardViews[i].transform.localPosition, _cardViews[i].transform.localScale);
            }
        }

        private void Clear()
        {
            foreach (var view in _cardViews)
            {
                view.Clicked -= OnCardClicked;
                Destroy(view.gameObject);
            }

            _cardViews.Clear();
        }
        
        private IEnumerable<Card> GetSortedCards(CardPile hand)
        {
            return hand.Cards.OrderBy(c => c.Suit).ThenBy(c => c.Rank);
        }
        
        public void SetDimmed(bool dimmed)
        {
            _isDimmed = dimmed;
            ApplyCurrentDimState();
        }

        private void ApplyCurrentDimState()
        {
            foreach (var view in _cardViews)
            {
                view.SetDimmed(_isDimmed);
            }
        }
    }
}
