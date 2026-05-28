using System;
using System.Collections.Generic;
using DataClasses.Enums;
using UnityEngine;

namespace DataViews
{
    /// <summary>
    /// World-space suit picker for wild cards.
    ///
    /// When Show() is called, four <see cref="SuitSelectorCardView"/> prefabs are
    /// spawned above the draw pile in a row. Clicking any one of them commits the
    /// suit choice, despawns all four, and invokes the callback.
    ///
    /// Scene setup
    ///   • Attach this MonoBehaviour to any persistent GameObject (e.g. GameView).
    ///   • selectorCardPrefab  – prefab with a SuitSelectorCardView component.
    ///   • spawnAnchor         – Transform positioned above the draw pile.
    ///                           The four cards are laid out along its local X axis,
    ///                           centred on this point.
    ///   • cardSpacing         – world-space gap between card centres (default 1.5).
    ///
    /// The spawn anchor should be placed at the world position you want the centre
    /// of the four-card row to appear. A good default is slightly above and in
    /// front of the draw pile GameObject.
    /// </summary>
    public class WildSuitSelectorView : MonoBehaviour
    {
        [SerializeField] private SuitSelectorCardView selectorCardPrefab;

        /// <summary>
        /// Centre point of the four-card row. Position this above the draw pile
        /// in the scene.
        /// </summary>
        [SerializeField] private Transform spawnAnchor;

        /// <summary>World-space distance between the centres of adjacent selector cards.</summary>
        [SerializeField] private float cardSpacing = 1.5f;

        private readonly List<SuitSelectorCardView> _selectorCards = new();
        private Action<Suit> _onChosen;

        // PUBLIC API

        /// <summary>
        /// Spawns the four suit-selector cards and wires up the callback.
        /// Safe to call while already shown; it will clear previous cards first.
        /// </summary>
        public void Show(Action<Suit> onChosen, string selectorLabel)
        {
            Hide(); // defensive clear in case Show() is called twice

            _onChosen = onChosen;

            var suits = new[] { Suit.Red, Suit.Yellow, Suit.Blue, Suit.Green };

            var totalWidth = cardSpacing * (suits.Length - 1);
            var startX     = -totalWidth / 2f;

            for (var i = 0; i < suits.Length; i++)
            {
                var card = Instantiate(selectorCardPrefab, spawnAnchor);

                var localPos = new Vector3(startX + i * cardSpacing, 0f, 0f);
                card.transform.localPosition = localPos;
                card.Bind(suits[i], selectorLabel);
                card.SetRestState(localPos, card.transform.localScale);

                var chosenSuit = suits[i];
                card.Selected += _ => Commit(chosenSuit);

                _selectorCards.Add(card);
            }
        }

        /// <summary>
        /// Despawns all selector cards and clears the callback.
        /// Called automatically when a suit is chosen; also safe to call manually
        /// to cancel the picker (e.g. if the wild card play is rolled back).
        /// </summary>
        public void Hide()
        {
            foreach (var card in _selectorCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            _selectorCards.Clear();
            _onChosen = null;
        }

        // INTERNAL

        /// <summary>
        /// Despawns all selector cards, then fires the callback.
        /// Capturing and nulling _onChosen before invoking prevents re-entrancy
        /// if the callback somehow triggers another Show().
        /// </summary>
        private void Commit(Suit suit)
        {
            var callback = _onChosen;
            Hide(); // cards gone before the callback fires
            callback?.Invoke(suit);
        }
    }
}