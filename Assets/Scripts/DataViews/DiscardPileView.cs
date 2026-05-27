using System;
using System.Collections.Generic;
using System.Linq;
using DataClasses.BusinessLayer;
using DataClasses.CardPiles;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = System.Random;

namespace DataViews
{
    public class DiscardPileView : MonoBehaviour
    {
        [SerializeField] private CardView cardPrefab;
        [SerializeField] private Transform spawnPoint;

        [SerializeField] private int maxCards = 16;
        [SerializeField] private float individualSpacing = 0.01f;

        [SerializeField] private float maxRotation = 45f;
        [SerializeField] private float maxDisplacement = 0.25f;

        private readonly List<CardView> _cardViews = new();

        public void Render(CardPile pile, GameState state, TooltipView tooltipView)
        {
            Clear();

            foreach (var card in pile.Cards.TakeLast(maxCards).ToList())
            {
                var cardView = Instantiate(cardPrefab, spawnPoint);
                cardView.Bind(card);
                _cardViews.Add(cardView);
            }

            Layout(state, tooltipView);
        }

        private void Layout(GameState state, TooltipView tooltipView)
        {
            if (_cardViews.Count == 0)
            {
                return;
            }

            for (var i = 0; i < _cardViews.Count; i++)
            {
                var cardView = _cardViews[i];
                var cardViewTransform = cardView.transform;

                var rand = GetSeededRand(cardView.Card.Guid);
                var rotation = GetSeededRotation(rand);

                if (!cardView.Card.IsStartingCard)
                {
                    var angle = GetSeededAngle(rand);
                    var displacementX = Mathf.Cos(angle) * maxDisplacement;
                    var displacementZ = Mathf.Sin(angle) * maxDisplacement;
                    cardViewTransform.position += new Vector3(displacementX, 0, displacementZ);
                }

                cardViewTransform.position += new Vector3(0, individualSpacing * i, 0);
                cardViewTransform.eulerAngles += Vector3.up * rotation;

                // Need to set this otherwise the card will vanish on mouse hover
                cardView.SetRestState(cardViewTransform.localPosition, cardViewTransform.localScale);
                cardView.SetDimmed(i < _cardViews.Count - 1);
                cardView.SetCanHover(i == _cardViews.Count - 1);
            }

            // Wire tooltip to the top card only
            var topCard = _cardViews[^1];
            topCard.MouseEntered += cv => tooltipView.Show(cv.Card, state, Mouse.current.position.ReadValue());
            topCard.Selected += _ => tooltipView.Hide();
            topCard.MouseExited += _ => tooltipView.Hide();
        }

        private void Clear()
        {
            foreach (var view in _cardViews)
            {
                Destroy(view.gameObject);
            }

            _cardViews.Clear();
        }

        private static Random GetSeededRand(Guid guid)
        {
            var guidBytes = guid.ToByteArray();
            var seed = BitConverter.ToInt32(guidBytes, 0);
            return new Random(seed);
        }

        private float GetSeededRotation(Random rand)
        {
            var randomFloat = (float)rand.NextDouble();
            return (randomFloat * 2f - 1f) * maxRotation;
        }

        private static float GetSeededAngle(Random rand)
        {
            var randomFloat = (float)rand.NextDouble();
            return randomFloat * 2f * Mathf.PI;
        }
    }
}
