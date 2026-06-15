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
    ///   _animationOffset : written each animation tick (e.g. jiggle, deal); XZ for jiggle, Y for deal
    ///   _hoverOffset     : applied while the cursor is over the card; always Y-only
    ///
    /// Each layer is written independently, and FlushTransform() recomposes them every frame.
    /// This ensures no layer can accidentally clobber another.
    /// </summary>
    public class CardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Renderer bodyRenderer;

        [SerializeField] private TMP_Text backLabel;
        [SerializeField] private Renderer backRenderer;

        private readonly Color _backColor = Color.rebeccaPurple * 1.25f;
        private const float DimAmount = 0.5f;
        private const float DimFadeDuration = 0.25f;

        // Color / interaction state
        private Color _baseColor;
        private bool _canHover;
        private bool _isHovering;

        // Dim fade state; _dimFadeProgress tracks 0-1 progress along the current fade.
        // _currentDimAmount is the eased value actually applied to the material each frame.
        // _dimFadeStartAmount is the dim amount at the moment the most recent fade began,
        // so reversals mid-fade start from wherever the card currently is visually.
        // Starts at 1 (fully undimmed) so newly spawned cards don't flash before Bind().
        private float _currentDimAmount = 1f;
        private float _targetDimAmount = 1f;
        private float _dimFadeStartAmount = 1f;
        private float _dimFadeProgress = 1f;

        // Transform layer 1: layout anchor (set by parent, read-only at runtime)
        private Vector3 _basePosition;
        private Vector3 _baseScale;

        // Transform layer 2: animation offset (XZ for jiggle; Y for deal lift/land)
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
            TickDimFade();
            FlushTransform();
        }

        // BINDING

        public void Bind(Card card)
        {
            Card = card;
            _baseColor = GetSuitColor(card.ActiveSuit);
            label.text = GetDisplayText(card.ActiveRank);
            label.fontStyle = label.text is "6" or "9" ? FontStyles.Underline : FontStyles.Normal;
            ApplyColor();
        }

        // UNITY MOUSE CALLBACKS

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

        // EXTERNAL HOVER CONTROL (for group-hover; e.g. pending draw block)

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

        // PUBLIC SETTERS

        public void SetCanHover(bool canHover)
        {
            _canHover = canHover;
        }

        public void SetDimmed(bool dimmed)
        {
            var newTarget = dimmed ? DimAmount : 1f;

            if (Mathf.Approximately(newTarget, _targetDimAmount))
            {
                return;
            }

            // Start a new fade from wherever the card currently sits visually so
            // reversals mid-fade don't jump.
            _targetDimAmount   = newTarget;
            _dimFadeStartAmount = _currentDimAmount;
            _dimFadeProgress   = 0f;
        }

        /// <summary>
        /// Snaps the dim state immediately without fading; use during initial layout
        /// so cards don't fade in from the wrong dim state on first appearance.
        /// </summary>
        public void SnapDimmed(bool dimmed)
        {
            _targetDimAmount    = dimmed ? DimAmount : 1f;
            _dimFadeStartAmount = _targetDimAmount;
            _currentDimAmount   = _targetDimAmount;
            _dimFadeProgress    = 1f;
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

        /// <summary>
        /// Writes the Y component of the animation offset layer directly.
        /// Used by the deal animation to lift and land cards without touching the
        /// XZ jiggle component. The jiggle path only ever writes X and Z, so there
        /// is no collision between the two.
        /// </summary>
        public void SetAnimationYOffset(float y)
        {
            _animationOffset = new Vector3(_animationOffset.x, y, _animationOffset.z);
        }

        // TRANSFORM LAYER HELPERS

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

        // DIM FADE ANIMATION

        private void TickDimFade()
        {
            if (_dimFadeProgress >= 1f)
            {
                return;
            }

            _dimFadeProgress = Mathf.Clamp01(_dimFadeProgress + Time.deltaTime / DimFadeDuration);

            // Smoothstep (ease in-out): 3t^2 - 2t^3
            var eased = _dimFadeProgress * _dimFadeProgress * (3f - 2f * _dimFadeProgress);

            _currentDimAmount = Mathf.Lerp(_dimFadeStartAmount, _targetDimAmount, eased);

            ApplyColor();
        }

        // JIGGLE ANIMATION

        private void TickJiggle()
        {
            if (JiggleAmount <= 0f)
            {
                // Clear only the XZ components; Y belongs to the deal animation layer
                _animationOffset = new Vector3(0f, _animationOffset.y, 0f);
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

            // Jiggle lives entirely in the XZ plane; Y is owned by the deal animation layer
            _animationOffset = new Vector3(xOffset, _animationOffset.y, zOffset);

            // Keep excess time to maintain tick accuracy
            _jiggleTimer -= JiggleUpdateRate;
        }

        // COLOR HELPERS

        private void ApplyHoverColor()
        {
            // TODO: Add edge highlighting; add separate visual for "illegal card" vs "not your turn"
            bodyRenderer.material.color = _baseColor * _currentDimAmount * HoverScaleMultiplier;
            label.color = Color.white * _currentDimAmount;

            backRenderer.material.color = _backColor * _currentDimAmount * HoverScaleMultiplier;
            backLabel.color = Color.white * _currentDimAmount;
        }

        private void ApplyColor()
        {
            bodyRenderer.material.color = _baseColor * _currentDimAmount;
            label.color = Color.white * _currentDimAmount;

            backRenderer.material.color = _backColor * _currentDimAmount;
            backLabel.color = Color.white * _currentDimAmount;
        }

        // STATIC LOOKUP HELPERS

        public static Color GetSuitColor(Suit suit)
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

        public static string GetDisplayText(Card card)
        {
            return GetDisplayText(card.ActiveRank);
        }

        public static string GetDisplayText(Rank rank)
        {
            return rank switch
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
