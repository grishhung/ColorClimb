using DataClasses.CardPiles;
using DataClasses.Enums;
using System;
using TMPro;
using UnityEngine;

namespace DataViews
{
    public class CardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Renderer bodyRenderer;
        
        private Color _baseColor;
        private bool _isDimmed;
        private bool _canHover;
        
        private Vector3 _basePosition;
        private Vector3 _baseScale;

        public Card Card { get; private set; }
        public event Action<CardView> Clicked;

        public void Bind(Card card)
        {
            Card = card;
            _baseColor = GetSuitColor(Card.Suit);
            label.text = GetDisplayText(Card);
            label.fontStyle = label.text is "6" or "9" ? FontStyles.Underline : FontStyles.Normal;
            ApplyColor();
        }

        private void OnMouseEnter()
        {
            if (!_canHover)
            {
                return;
            }

            // TODO: Add edge highlighting as well and dimming for illegal cards
            // Make it so that if a card can be used to jump in, it's not dimmed
            
            bodyRenderer.material.color = _baseColor * (_isDimmed ? 0.5f : 1f) * 1.25f;
            label.color = Color.white * (_isDimmed ? 0.5f : 1f);
            
            // Make the card easier to view
            gameObject.transform.localPosition = _basePosition + Vector3.up * 0.5f;
            gameObject.transform.localScale = _baseScale * 1.25f;
            
            // TODO: Create real tooltip functionality using GetDescription()
        }

        private void OnMouseExit()
        {
            if (!_canHover)
            {
                return;
            }

            ApplyColor();
            ApplyPositionAndScale();
        }
        
        private void OnMouseDown()
        {
            if (!_canHover)
            {
                return;
            }

            Clicked?.Invoke(this);
        }
        
        private static Color GetSuitColor(Suit suit)
        {
            return suit switch
            {
                Suit.Red => Color.crimson,
                Suit.Yellow => Color.goldenRod,
                Suit.Blue => Color.royalBlue,
                Suit.Green => Color.forestGreen,
                Suit.Wild => Color.gray1,
                _ => Color.magenta
            };
        }
        
        private string GetDisplayText(Card card)
        {
            return card.Rank switch
            {
                Rank.Number0 => "0",
                Rank.Number1 => "1",
                Rank.Number2 => "2",
                Rank.Number3 => "3",
                Rank.Number4 => "4",
                Rank.Number5 => "5",
                Rank.Number6 => "6",
                Rank.Number7 => "7",
                Rank.Number8 => "8",
                Rank.Number9 => "9",

                Rank.Draw2 => "+2",
                Rank.Reverse => "REV",
                Rank.Skip => "SKIP",

                Rank.Wild => "WILD",
                Rank.WildDraw4 => "+4",

                _ => "?"
            };
        }
        
        private void ApplyColor()
        {
            var finalCardColor = _baseColor;
            var finalLabelColor = Color.white;

            if (_isDimmed)
            {
                finalCardColor *= 0.5f;
                finalLabelColor *= 0.5f;
            }

            bodyRenderer.material.color = finalCardColor;
            label.color = finalLabelColor;
        }
        
        private void ApplyPositionAndScale()
        {
            gameObject.transform.localPosition = _basePosition;
            gameObject.transform.localScale = _baseScale;
        }
        
        public void SetCanHover(bool canHover)
        {
            _canHover = canHover;
        }
        
        public void SetDimmed(bool dimmed)
        {
            _isDimmed = dimmed;
            ApplyColor();
        }
        
        public void SetRestState(Vector3 position, Vector3 scale)
        {
            _basePosition = position;
            _baseScale = scale;
        }
    }
}
