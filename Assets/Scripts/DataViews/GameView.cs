using System;
using System.Collections;
using System.Collections.Generic;
using DataClasses.BusinessLayer;
using DataClasses.CardPiles;
using DataClasses.Enums;
using UnityEngine;

namespace DataViews
{
    public class GameView : MonoBehaviour
    {
        [SerializeField] private PlayerView playerViewPrefab;
        [SerializeField] private Transform[] playerAnchors;

        [SerializeField] private DrawPileView drawPileView;
        [SerializeField] private DiscardPileView discardPileView;
        [SerializeField] private TooltipView tooltipView;

        [SerializeField] private WildSuitSelectorView wildSuitSelectorView;

        private readonly List<PlayerView> _playerViews = new();

        public event Action<Player, Card> CardClicked;
        public event Action DrawPileClicked;

        private GameState _state;

        // Tracks the live swap-picker listeners so they can be removed cleanly.
        private readonly List<(PlayerView view, Action<Player> handler)> _swapHandlers = new();

        public void Bind(GameState state)
        {
            _state = state;

            for (var i = 0; i < state.Players.Count; i++)
            {
                var player = state.Players[i];
                var view = Instantiate(playerViewPrefab, playerAnchors[i]);

                view.Bind(player, _state, tooltipView);
                view.CardClicked += OnCardClicked;

                _playerViews.Add(view);
            }

            drawPileView.DrawPileClicked += OnDrawPileClicked;

            drawPileView.Render(_state.DrawPile, _state.PendingDrawCount, _state, tooltipView);
            discardPileView.Render(_state.DiscardPile, _state, tooltipView);
        }

        private void OnCardClicked(Player player, Card card)
        {
            CardClicked?.Invoke(player, card);
        }

        private void OnDrawPileClicked()
        {
            DrawPileClicked?.Invoke();
        }

        public void Refresh()
        {
            foreach (var playerView in _playerViews)
            {
                playerView.ApplyCurrentDimState();
                playerView.Render();
            }

            drawPileView.Render(_state.DrawPile, _state.PendingDrawCount, _state, tooltipView);
            discardPileView.Render(_state.DiscardPile, _state, tooltipView);
        }

        // DEAL ANIMATION PASS-THROUGHS

        /// <summary>
        /// Animates the starting card falling from CeilingHeight onto the discard pile.
        /// See DiscardPileView.PlayDealLandAnimation for full details.
        /// </summary>
        public IEnumerator PlayDiscardDealAnimation(GameState state)
        {
            yield return discardPileView.PlayDealLandAnimation(state, tooltipView);
        }

        /// <summary>
        /// Delegates to DrawPileView to lift <paramref name="totalCards"/> cards up to
        /// CeilingHeight. See DrawPileView.PlayLiftAnimation for full details.
        /// </summary>
        public IEnumerator PlayDealLiftAnimation(int totalCards)
        {
            yield return drawPileView.PlayLiftAnimation(totalCards);
        }

        /// <summary>
        /// Deals cards from the pre-populated game state hands into their respective
        /// PlayerViews using the land animation. Cards appear at CeilingHeight and tween
        /// down to their rest positions. All four players animate simultaneously; cards
        /// within each hand still stagger individually via HandView.LandStaggerInterval.
        ///
        /// Each player's hand already contains its cards (GameManager dealt them before
        /// calling this) but no card views exist yet; each PlayerView creates its views
        /// here as part of the animation.
        /// </summary>
        public IEnumerator PlayDealLandAnimation(GameState state)
        {
            var coroutinesFinished = 0;

            foreach (var playerView in _playerViews)
            {
                // All players' land sequences start at the same time; cards within
                // each hand still stagger individually via LandStaggerInterval
                var playerDelay = 0f;

                StartCoroutine(DelayedLandRoutine(playerView, state, playerDelay, () =>
                {
                    coroutinesFinished++;
                }));
            }

            yield return new WaitUntil(() => coroutinesFinished >= _playerViews.Count);
        }

        /// <summary>
        /// Waits <paramref name="delay"/> seconds, then runs the land animation for one
        /// PlayerView and invokes <paramref name="onComplete"/> when it finishes.
        /// </summary>
        private IEnumerator DelayedLandRoutine(PlayerView playerView, GameState state, float delay, Action onComplete)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            yield return playerView.PlayDealLandAnimation(state, tooltipView);

            onComplete?.Invoke();
        }

        // PLAY-CARD FLY ANIMATION

        /// <summary>
        /// Returns the world position and rotation of <paramref name="card"/> in
        /// <paramref name="player"/>'s hand view before the hand is re-rendered, or the
        /// discard pile's transform as a fallback. Call this before the state mutation.
        /// </summary>
        public (Vector3 position, Quaternion rotation) GetCardWorldTransform(Player player, Card card)
        {
            foreach (var pv in _playerViews)
            {
                if (pv.Player != player)
                {
                    continue;
                }

                var t = pv.GetCardWorldTransform(card);
                if (t.HasValue)
                {
                    return t.Value;
                }

                break;
            }

            return (discardPileView.transform.position, discardPileView.transform.rotation);
        }

        /// <summary>
        /// Launches the fly animation that moves <paramref name="card"/> from
        /// <paramref name="startTransform"/> to its rest position in the discard pile.
        /// The card must already be committed to the discard pile in the data model.
        ///
        /// The coroutine is fire-and-forget and does not block gameplay.
        /// </summary>
        public void StartPlayCardFlyAnimation(
            Card card,
            (Vector3 position, Quaternion rotation) startTransform,
            GameState state)
        {
            StartCoroutine(discardPileView.PlayCardFlyAnimation(
                card,
                startTransform.position,
                startTransform.rotation,
                state,
                tooltipView));
        }

        // PICKER PANELS

        /// <summary>
        /// Opens the suit picker modal. The callback fires once the player chooses;
        /// the panel closes itself before invoking it.
        /// </summary>
        public void ShowSuitPicker(Action<Suit> onChosen, string selectorLabel)
        {
            tooltipView.Hide();
            wildSuitSelectorView.Show(onChosen, selectorLabel);
        }

        /// <summary>
        /// Activates hand-click swap-picker mode.
        ///
        /// The source player's hand dims and becomes non-interactive.
        /// All other players' hands undim, show group-hover, and fire HandChosen
        /// when clicked. The first hand click commits the choice, exits swap mode
        /// on all hands, and invokes onChosen with the chosen player index.
        /// </summary>
        public void ShowHandSwapPicker(int sourcePlayerIndex, Action<int> onChosen)
        {
            tooltipView.Hide();
            _swapHandlers.Clear();

            for (var i = 0; i < _playerViews.Count; i++)
            {
                var isSource = i == sourcePlayerIndex;
                _playerViews[i].EnterSwapPickerMode(isSource);

                if (!isSource)
                {
                    var targetIndex = i; // Capture loop variable.

                    // Named local so we can -= it later.
                    Action<Player> handler = _ => CommitHandSwap(targetIndex, onChosen);
                    _playerViews[i].HandChosen += handler;
                    _swapHandlers.Add((_playerViews[i], handler));
                }
            }
        }

        /// <summary>
        /// Unsubscribes all swap-picker listeners, exits swap mode on every hand,
        /// hides the tooltip, and fires the callback.
        /// Safe to call multiple times; subsequent calls after the first are no-ops
        /// because _swapHandlers is cleared immediately.
        /// </summary>
        private void CommitHandSwap(int targetIndex, Action<int> onChosen)
        {
            // Detach listeners first so a second click can't fire while we're committing.
            foreach (var (view, handler) in _swapHandlers)
            {
                view.HandChosen -= handler;
            }
            _swapHandlers.Clear();

            foreach (var pv in _playerViews)
            {
                pv.ExitSwapPickerMode();
            }

            tooltipView.Hide();
            onChosen?.Invoke(targetIndex);
        }
    }
}
