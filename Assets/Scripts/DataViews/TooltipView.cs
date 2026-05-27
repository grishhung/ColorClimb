using System.Collections.Generic;
using System.Text;
using DataClasses.BusinessLayer;
using DataClasses.CardPiles;
using DataClasses.Tooltip;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DataViews
{
    /// <summary>
    /// Two-column tooltip panel inspired by Slay the Spire.
    ///
    /// Left column  — one TMP label per card effect. Keywords in the description
    ///                are highlighted inline using TMP rich text tags.
    /// Right column — one TMP label per unique keyword that appears across all
    ///                effects on the hovered card, showing its definition from
    ///                KeywordLibrary.
    ///
    /// Scene setup:
    ///   'panel'               — root GameObject toggled on/off (Screen Space Overlay Canvas)
    ///   'effectLabelPrefab'   — TMP_Text prefab for one effect row (left column)
    ///   'effectContainer'     — Transform with VerticalLayoutGroup + ContentSizeFitter
    ///   'keywordLabelPrefab'  — TMP_Text prefab for one keyword definition (right column)
    ///   'keywordContainer'    — Transform with VerticalLayoutGroup + ContentSizeFitter
    ///   'keywordColumn'       — parent GameObject for the right column (toggled when empty)
    /// </summary>
    public class TooltipView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;

        [SerializeField] private TMP_Text effectLabelPrefab;
        [SerializeField] private Transform effectContainer;

        [SerializeField] private TMP_Text keywordLabelPrefab;
        [SerializeField] private Transform keywordContainer;
        [SerializeField] private GameObject keywordColumn;

        [SerializeField] private string keywordOpenTag  = "<color=#FFD700>";
        [SerializeField] private string keywordCloseTag = "</color>";

        [SerializeField] private Vector2 cursorOffset = new(16f, -16f);

        private readonly List<TMP_Text> _effectLabels  = new();
        private readonly List<TMP_Text> _keywordLabels = new();

        private void Awake()
        {
            Hide();
        }

        public void Show(Card card, GameState state, Vector2 screenPosition)
        {
            if (card.Effects.Count == 0)
            {
                return;
            }

            ClearLabels();

            // Collect all unique keywords across every effect on this card
            var seenKeywords = new List<string>();

            foreach (var effect in card.Effects)
            {
                var parsed = DescriptionParser.Parse(effect.GetDescription(state));

                // Build the left-column label with inline keyword highlighting
                var sb = new StringBuilder();
                foreach (var segment in parsed.Segments)
                {
                    if (segment.IsKeyword)
                    {
                        sb.Append($"{keywordOpenTag}{segment.Text}{keywordCloseTag}");
                    }
                    else
                    {
                        sb.Append(segment.Text);
                    }
                }

                var effectLabel = Instantiate(effectLabelPrefab, effectContainer);
                effectLabel.text = sb.ToString();
                _effectLabels.Add(effectLabel);

                // Accumulate unique keywords for the right column
                foreach (var keyword in parsed.Keywords)
                {
                    if (!seenKeywords.Contains(keyword))
                    {
                        seenKeywords.Add(keyword);
                    }
                }
            }

            // Populate the right column with keyword definitions
            var anyDefinitions = false;
            foreach (var keyword in seenKeywords)
            {
                var definition = KeywordLibrary.Get(keyword);
                if (definition == null)
                {
                    continue;
                }

                var keywordLabel = Instantiate(keywordLabelPrefab, keywordContainer);
                keywordLabel.text = $"{keywordOpenTag}{keyword}{keywordCloseTag}\n{definition}";
                _keywordLabels.Add(keywordLabel);
                anyDefinitions = true;
            }

            // Only show the right column if there's something to show
            keywordColumn.SetActive(anyDefinitions);

            panel.SetActive(true);
            PositionAt(Mouse.current.position.ReadValue());
        }

        public void Hide()
        {
            panel.SetActive(false);
            ClearLabels();
        }

        private void Update()
        {
            if (panel.activeSelf)
            {
                PositionAt(Mouse.current.position.ReadValue());
            }
        }

        private void PositionAt(Vector2 screenPosition)
        {
            var rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.position = new Vector3(
                screenPosition.x + cursorOffset.x,
                screenPosition.y + cursorOffset.y,
                0f
            );
        }

        private void ClearLabels()
        {
            foreach (var label in _effectLabels)
            {
                Destroy(label.gameObject);
            }
            _effectLabels.Clear();

            foreach (var label in _keywordLabels)
            {
                Destroy(label.gameObject);
            }
            _keywordLabels.Clear();
        }
    }
}
