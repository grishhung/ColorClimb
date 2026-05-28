using System;
using DataClasses.Enums;
using TMPro;
using UnityEngine;

namespace DataViews
{
    /// <summary>
    /// A single world-space card used by WildSuitSelectorView.
    /// Displays one of the four normal suits and fires Selected when clicked.
    ///
    /// Scene / prefab setup:
    ///   'label'        - TMP_Text child showing the suit name
    ///   'bodyRenderer' - MeshRenderer whose material.color is tinted to the suit
    ///
    /// This is intentionally simpler than CardView: no jiggle, no dimming,
    /// no tooltip, no layered transform system. It only needs hover lift and click.
    /// </summary>
    public class SuitSelectorCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Renderer bodyRenderer;

        // Hover lift
        private const float HoverLiftAmount = 0.5f;
        private const float HoverScaleMultiplier = 1.25f;

        private Vector3 _restPosition;
        private Vector3 _restScale;
        private bool _isHovering;

        public Suit Suit { get; private set; }

        /// <summary>Fired when the player clicks (mouse-up while still hovering) this card.</summary>
        public event Action<Suit> Selected;

        // Binding

        public void Bind(Suit suit)
        {
            Suit = suit;
            label.text = "WILD";
            bodyRenderer.material.color = GetSuitColor(suit);
        }

        // Rest-state registration (called by WildSuitSelectorView after placement)

        /// <summary>
        /// Records the card's intended resting position/scale so hover offsets are
        /// applied relative to it rather than clobbering the absolute transform.
        /// Call this once after positioning the card.
        /// </summary>
        public void SetRestState(Vector3 position, Vector3 scale)
        {
            _restPosition = position;
            _restScale    = scale;
            FlushTransform(false);
        }

        // Unity mouse callbacks

        private void OnMouseEnter()
        {
            _isHovering = true;
            FlushTransform(true);
            ApplyHoverColor();
        }

        private void OnMouseExit()
        {
            _isHovering = false;
            FlushTransform(false);
            ApplyBaseColor();
        }

        private void OnMouseUp()
        {
            if (_isHovering)
            {
                Selected?.Invoke(Suit);
            }
        }

        // Helpers

        private void FlushTransform(bool hovered)
        {
            transform.localPosition = _restPosition + (hovered ? Vector3.up * HoverLiftAmount : Vector3.zero);
            transform.localScale    = _restScale * (hovered ? HoverScaleMultiplier : 1f);
        }

        private void ApplyBaseColor()
        {
            bodyRenderer.material.color = GetSuitColor(Suit);
        }

        private void ApplyHoverColor()
        {
            bodyRenderer.material.color = GetSuitColor(Suit) * HoverScaleMultiplier;
        }

        private static Color GetSuitColor(Suit suit)
        {
            return suit switch
            {
                Suit.Red    => Color.crimson,
                Suit.Yellow => Color.goldenRod,
                Suit.Blue   => Color.royalBlue,
                Suit.Green  => Color.forestGreen,
                _           => Color.gray
            };
        }
    }
}