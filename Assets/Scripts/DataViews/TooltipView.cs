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
    /// Left column  - status line (jump-in / cannot play) + one label per card effect.
    /// Right column - one label per unique keyword with a library definition.
    ///
    /// Scene setup:
    ///   'panel'               - root GameObject toggled on/off (Screen Space Overlay Canvas)
    ///   'effectLabelPrefab'   - TMP_Text prefab for one effect row (left column)
    ///   'effectContainer'     - child Transform with VerticalLayoutGroup
    ///   'keywordLabelPrefab'  - TMP_Text prefab for one keyword definition (right column)
    ///   'keywordContainer'    - child Transform with VerticalLayoutGroup
    ///   'keywordColumn'       - parent GameObject for the right column (toggled when empty)
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
        [SerializeField] private string statusOpenTag   = "<i><color=#AAAAAA>";
        [SerializeField] private string statusCloseTag  = "</color></i>";

        [SerializeField] private Vector2 cursorOffset = new(16f, -16f);

        private readonly List<TMP_Text> _effectLabels  = new();
        private readonly List<TMP_Text> _keywordLabels = new();

        private void Awake()
        {
            Hide();
        }

        /// <summary>
        /// Shows a tooltip for a card in a player's hand.
        /// Handles jump-in callout, "cannot be played" status, effect descriptions,
        /// and keyword definitions.
        /// </summary>
        public void Show(Card card, Player player, GameState state, Vector2 screenPosition)
        {
            ClearLabels();

            var seenKeywords = new List<string>();

            // Status line; shown above effects, mutually exclusive
            if (player != null && GameRules.IsJumpIn(player, card, state))
            {
                AddRawLabel($"{statusOpenTag}Jump-in available! Play out of turn.{statusCloseTag}");
            }
            else if (player != null && !GameRules.CanPlay(player, card, state))
            {
                AddRawLabel($"{statusOpenTag}This card cannot be played right now.{statusCloseTag}");
            }

            // Effect descriptions
            if (card.Effects.Count == 0)
            {
                AddRawLabel($"{statusOpenTag}No special effect.{statusCloseTag}");
            }
            else
            {
                foreach (var effect in card.Effects)
                {
                    AddEffectLabel(effect.GetDescription(state), seenKeywords);
                }
            }

            PopulateKeywordColumn(seenKeywords);

            panel.SetActive(true);
            PositionAt(screenPosition);
        }

        /// <summary>
        /// Shows a tooltip for a card with no player context (e.g. discard pile top card).
        /// No status line is shown since playability cannot be determined.
        /// </summary>
        public void Show(Card card, GameState state, Vector2 screenPosition)
        {
            Show(card, null, state, screenPosition);
        }

        /// <summary>
        /// Shows a tooltip from a plain description string (e.g. draw pile).
        /// Supports the same {keyword} syntax as card effects.
        /// </summary>
        public void Show(string description, Vector2 screenPosition)
        {
            ClearLabels();

            var seenKeywords = new List<string>();
            AddEffectLabel(description, seenKeywords);
            PopulateKeywordColumn(seenKeywords);

            panel.SetActive(true);
            PositionAt(screenPosition);
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

        private void AddRawLabel(string text)
        {
            var label = Instantiate(effectLabelPrefab, effectContainer);
            label.text = text;
            _effectLabels.Add(label);
        }

        private void AddEffectLabel(string description, List<string> seenKeywords)
        {
            var parsed = DescriptionParser.Parse(description);

            var sb = new StringBuilder();
            foreach (var segment in parsed.Segments)
            {
                sb.Append(segment.IsKeyword
                    ? $"{keywordOpenTag}{segment.Text}{keywordCloseTag}"
                    : segment.Text);
            }

            AddRawLabel(sb.ToString());

            foreach (var keyword in parsed.Keywords)
            {
                if (!seenKeywords.Contains(keyword))
                {
                    seenKeywords.Add(keyword);
                }
            }
        }

        private void PopulateKeywordColumn(List<string> seenKeywords)
        {
            var anyDefinitions = false;

            foreach (var keyword in seenKeywords)
            {
                var definition = KeywordLibrary.Get(keyword);
                if (definition == null)
                {
                    continue;
                }

                var keywordLabel = Instantiate(keywordLabelPrefab, keywordContainer);
                keywordLabel.text = $"{keywordOpenTag}{ToTitleCase(keyword)}{keywordCloseTag}\n{definition}";
                _keywordLabels.Add(keywordLabel);
                anyDefinitions = true;
            }

            keywordColumn.SetActive(anyDefinitions);
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
        
        private static string ToTitleCase(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);
        }
    }
}
