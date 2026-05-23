using System;
using System.Collections.Generic;
using System.Linq;
using DataClasses.BusinessLayer;
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

        public void Render(Player player, GameState state)
        {
            Clear();
            
            var cards = GetSortedCards(player.Hand);

            foreach (var card in cards)
            {
                var view = Instantiate(cardPrefab, cardParent);
                view.Bind(card);
                view.Clicked += OnCardClicked;
                _cardViews.Add(view);
            }

            Layout();
            ApplyCurrentDimState(player, state);
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
                _cardViews[i].SetCanHover(true);
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
        
        public void ApplyCurrentDimState(Player player, GameState state)
        {
            foreach (var cardView in _cardViews)
            {
                cardView.SetDimmed(!GameRules.CanPlay(player, cardView.Card, state));
            }
        }
    }
}
