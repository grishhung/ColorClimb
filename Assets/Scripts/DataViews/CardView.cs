using DataClasses.CardPiles;
using DataClasses.Enums;
using System;
using TMPro;
using UnityEngine;

namespace DataViews
{
    /// <summary>
    /// Manages all visual state for a single card GameObject.
    ///
    /// Transform compositing; the final localPosition is the sum of three independent layers:
    ///   _basePosition    : the layout anchor assigned by the parent view; never changes at runtime
    ///   _animationOffset : written each animation tick (e.g. jiggle); always XZ-only
    ///   _hoverOffset     : applied while the cursor is over the card; always Y-only
    ///
    /// Each layer is written independently, and FlushTransform() recomposes them every frame.
    /// This ensures no layer can accidentally clobber another.
    /// </summary>
    public class CardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Renderer bodyRenderer;

        // Color / interaction state
        private Color _baseColor;
        private bool _isDimmed;
        private bool _canHover;
        private bool _isHovering;

        // Transform layer 1: layout anchor (set by parent, read-only at runtime)
        private Vector3 _basePosition;
        private Vector3 _baseScale;

        // Transform layer 2: animation offset (XZ jiggle, future animations)
        private Vector3 _animationOffset;

        // Transform layer 3: hover lift (Y only)
        private Vector3 _hoverOffset;

        // Hover scale multiplier (applied on top of _baseScale when hovering)
        private float _hoverScaleMultiplier = 1f;

        // Jiggle parameters
        public float JiggleAmount { get; set; }
        private const float JiggleUpdateRate = 1f / 30f;
        private float _jiggleTimer;

        // Hover lift constants
        private const float HoverLiftAmount = 0.5f;
        private const float HoverScaleMultiplier = 1.25f;

        // Public surface
        public Card Card { get; private set; }
        public event Action<CardView> Selected;
        public event Action<CardView> MouseEntered;
        public event Action<CardView> MouseExited;

        private void Update()
        {
            TickJiggle();
            FlushTransform();
        }

        // Binding

        public void Bind(Card card)
        {
            Card = card;
            _baseColor = GetSuitColor(Card.Suit);
            label.text = GetDisplayText(Card);
            label.fontStyle = label.text is "6" or "9" ? FontStyles.Underline : FontStyles.Normal;
            ApplyColor();
        }

        // Unity mouse callbacks

        private void OnMouseEnter()
        {
            if (!_canHover)
            {
                return;
            }

            _isHovering = true;
            SetHoverLayerActive(true);
            ApplyHoverColor();
            MouseEntered?.Invoke(this);
        }

        private void OnMouseExit()
        {
            if (!_canHover)
            {
                return;
            }

            _isHovering = false;
            SetHoverLayerActive(false);
            ApplyColor();
            MouseExited?.Invoke(this);
        }

        private void OnMouseDown()
        {
            if (!_canHover)
            {
                return;
            }

            // TODO: Enter "in the middle of clicking" animation state
        }

        private void OnMouseUp()
        {
            if (!_canHover)
            {
                return;
            }

            // TODO: Release "in the middle of clicking" animation state

            if (_isHovering)
            {
                Selected?.Invoke(this);
            }
        }

        // External hover control (for group-hover; e.g. pending draw block)

        /// <summary>
        /// Applies or removes hover visuals from outside; used to group-hover cards that
        /// aren't directly under the mouse (e.g. the pending draw block beneath the top card).
        /// Does NOT set _isHovering, so OnMouseUp still fires correctly for the top card only.
        /// </summary>
        public void SetHoverState(bool hovered)
        {
            SetHoverLayerActive(hovered);

            if (hovered)
            {
                ApplyHoverColor();
            }
            else
            {
                ApplyColor();
            }
        }

        // Public setters

        public void SetCanHover(bool canHover)
        {
            _canHover = canHover;
        }

        public void SetDimmed(bool dimmed)
        {
            _isDimmed = dimmed;
            ApplyColor();
        }

        /// <summary>
        /// Called by the parent view after it has positioned this card.
        /// Records the layout anchor so all animation and hover layers are relative to it.
        /// </summary>
        public void SetRestState(Vector3 position, Vector3 scale)
        {
            _basePosition = position;
            _baseScale = scale;

            // Reset transient layers so a re-layout starts clean
            _animationOffset = Vector3.zero;
            _hoverOffset = Vector3.zero;
            _hoverScaleMultiplier = 1f;
        }

        // Transform layer helpers

        /// <summary>
        /// Turns the hover offset layer on or off without touching _basePosition or _animationOffset.
        /// </summary>
        private void SetHoverLayerActive(bool active)
        {
            if (active)
            {
                _hoverOffset = Vector3.up * HoverLiftAmount;
                _hoverScaleMultiplier = HoverScaleMultiplier;
            }
            else
            {
                _hoverOffset = Vector3.zero;
                _hoverScaleMultiplier = 1f;
            }
        }

        /// <summary>
        /// Writes the composited transform to the GameObject.
        /// Called once per Update() after all layers have been updated for this frame.
        /// </summary>
        private void FlushTransform()
        {
            gameObject.transform.localPosition = _basePosition + _animationOffset + _hoverOffset;
            gameObject.transform.localScale = _baseScale * _hoverScaleMultiplier;
        }

        // Jiggle animation

        private void TickJiggle()
        {
            if (JiggleAmount <= 0f)
            {
                _animationOffset = Vector3.zero;
                return;
            }

            _jiggleTimer += Time.deltaTime;

            if (!(_jiggleTimer >= JiggleUpdateRate))
            {
                return;
            }

            var angle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);
            var radius = UnityEngine.Random.Range(0f, JiggleAmount);

            var xOffset = radius * Mathf.Cos(angle);
            var zOffset = radius * Mathf.Sin(angle);

            // Jiggle lives entirely in the XZ plane; _hoverOffset owns the Y axis
            _animationOffset = new Vector3(xOffset, 0f, zOffset);

            // Keep excess time to maintain tick accuracy
            _jiggleTimer -= JiggleUpdateRate;
        }

        // Color helpers

        private void ApplyHoverColor()
        {
            // TODO: Add edge highlighting; add separate visual for "illegal card" vs "not your turn"
            bodyRenderer.material.color = _baseColor * (_isDimmed ? 0.5f : 1f) * HoverScaleMultiplier;
            label.color = Color.white * (_isDimmed ? 0.5f : 1f);
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

        // Static lookup helpers

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

        private static string GetDisplayText(Card card)
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
    }
}
