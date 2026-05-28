using System.Collections;
using DataClasses.BusinessLayer.PendingDecisions;
using DataClasses.CardEffects;
using DataClasses.CardPiles;
using DataClasses.Enums;
using DataViews;
using UnityEngine;

namespace DataClasses.BusinessLayer
{
    public class GameManager : MonoBehaviour
    {
        private static readonly int IsClockwiseId = Shader.PropertyToID("_IsClockwise");

        [SerializeField] private GameView gameView;

        private const int PlayerCount = 4;
        private const int StartingHandSize = 7;

        private GameState _state;

        private void Start()
        {
            _state = new GameState();

            CreateStartingDeck(_state.DrawPile);
            _state.DrawPile.Shuffle();

            // Hands are populated by the deal animation coroutine rather than
            // immediately, so we only add empty Player entries here
            for (var i = 0; i < PlayerCount; i++)
            {
                _state.Players.Add(new Player(new Hand()));
            }

            // InitializeDiscardPile() is called inside DealAnimationRoutine just
            // before phase 3 so the starting card lands as part of the animation
            UpdateShaderGlobals();

            gameView.Bind(_state);
            gameView.CardClicked += OnCardClicked;
            gameView.DrawPileClicked += OnDrawPileClicked;

            // Set the flag before Refresh so the draw pile renders with its top card
            // already dimmed and non-interactive. StartCoroutine defers to the next
            // frame, so without this the flag would still be false when Refresh runs
            _state.IsDealAnimating = true;

            gameView.Refresh();

            StartCoroutine(DealAnimationRoutine());

            Debug.Log("Game started");
        }

        // INPUT HANDLERS (synchronous entry points from the view layer)

        private void OnCardClicked(Player player, Card card)
        {
            // Reject input while a decision panel is open or an animation is running.
            if (!_state.ActionsAllowed)
            {
                return;
            }

            if (!GameRules.CanPlay(player, card, _state))
            {
                Debug.Log("Illegal move");
                return;
            }

            StartCoroutine(PlayCardRoutine(player, card));
        }

        private void OnDrawPileClicked()
        {
            if (!_state.ActionsAllowed)
            {
                return;
            }

            StartCoroutine(DrawCardRoutine());
        }

        // DEAL ANIMATION COROUTINE

        /// <summary>
        /// Runs the three-phase opening deal animation, then starts gameplay.
        ///
        /// Phase 1 (lift): StartingHandSize * PlayerCount cards rise off the draw pile
        ///   one at a time, easing in upward to CeilingHeight.
        ///
        /// Phase 2 (land hands): all four players' cards land simultaneously in their
        ///   hands, staggered per card, easing out.
        ///
        /// Phase 3 (land discard): the starting discard card falls from CeilingHeight
        ///   onto the discard pile, easing out.
        ///
        /// IsDealAnimating is held true for the full duration so no gameplay input can
        /// slip through. It is cleared only after all three phases complete.
        /// </summary>
        private IEnumerator DealAnimationRoutine()
        {
            // IsDealAnimating is already set in Start() before Refresh(), so the
            // draw pile renders dimmed from the very first frame.

            var totalCards = StartingHandSize * PlayerCount;

            // Phase 1: lift all cards off the draw pile up to CeilingHeight.
            yield return gameView.PlayDealLiftAnimation(totalCards);

            // Populate the hand data model now that the lift is done. The land
            // animation reads from each player's Hand, so this must happen before
            // phase 2 starts.
            for (var i = 0; i < PlayerCount; i++)
            {
                for (var j = 0; j < StartingHandSize; j++)
                {
                    _state.Players[i].Hand.Add(_state.DrawPile.Draw());
                }
            }

            // Phase 2: land each card into its player's hand view.
            yield return gameView.PlayDealLandAnimation(_state);

            // Phase 3: flip the starting discard card down from the ceiling.
            InitializeDiscardPile();
            yield return gameView.PlayDiscardDealAnimation(_state);

            _state.IsDealAnimating = false;

            UpdateShaderGlobals();
            gameView.Refresh();

            Debug.Log("Deal animation complete; gameplay started");
        }

        // PLAY-CARD COROUTINE

        /// <summary>
        /// Handles the full lifecycle of a card play:
        ///   1. Realign turn to the jump-in player (if applicable).
        ///   2. Mutate game state via GameRules.PlayCard (effects fire here).
        ///   3. If an effect queued a PendingDecision, open the appropriate UI panel
        ///      and yield until the player resolves it.
        ///   4. Advance the turn and refresh the view.
        /// </summary>
        private IEnumerator PlayCardRoutine(Player player, Card card)
        {
            // Step 1: Realign to the jump-in player
            var pendingSkips = _state.SkipCount;
            _state.SkipCount = 0;

            while (_state.Players[_state.CurrentPlayerIndex] != player)
            {
                _state.CurrentPlayerIndex = GetNextPlayerIndex();
            }

            _state.SkipCount = pendingSkips;

            // Step 2: Apply the card to game state
            GameRules.PlayCard(_state, player, card);

            Debug.Log($"Player played {card}");

            // Step 3: Refresh the view immediately after state is mutated.
            // This ensures the card is removed from the hand (and, for non-wilds,
            // visible in the discard pile) before any decision panel opens, and
            // that all interactive elements are correctly dimmed while the decision
            // is pending (ActionsAllowed == false at this point if a decision was queued).
            UpdateShaderGlobals();
            gameView.Refresh();

            // Step 4: Resolve any pending decision
            if (_state.PendingDecision != null)
            {
                yield return ResolvePendingDecisionRoutine(player);
            }

            // Step 5: Advance turn and refresh
            AdvanceTurn();
            UpdateShaderGlobals();
            gameView.Refresh();
        }

        // DRAW-CARD COROUTINE

        private IEnumerator DrawCardRoutine()
        {
            var currentPlayer = _state.Players[_state.CurrentPlayerIndex];

            // TODO: Uncomment when the reshuffle / lives system is implemented.
            //
            // if (_state.DrawPile.Cards.Count == 0)
            // {
            //     if (_state.DiscardPile.Cards.Count <= 1)
            //     {
            //         Debug.Log("Draw pile and discard pile are both empty; cannot draw");
            //         yield break;
            //     }
            //     _state.DrawPile.RefillFromDiscard(_state.DiscardPile);
            //     Debug.Log("Draw pile exhausted; reshuffled discard pile");
            // }

            if (_state.PendingDrawCount > 0)
            {
                // Player couldn't or chose not to counter the chain; deal the burst.
                var burst = _state.PendingDrawCount;
                _state.PendingDrawCount = 0;
                _state.PendingDrawRank = null;

                for (var i = 0; i < burst; i++)
                {
                    currentPlayer.Hand.Add(_state.DrawPile.Draw());
                }

                Debug.Log($"Player accepted draw burst of {burst} cards");

                AdvanceTurn();
            }
            else
            {
                // Normal draw: add one card, keep the turn.
                var drawnCard = _state.DrawPile.Draw();
                currentPlayer.Hand.Add(drawnCard);
                Debug.Log($"Player drew {drawnCard}");
            }

            // Draw never triggers a decision panel, so no yield needed here.
            UpdateShaderGlobals();
            gameView.Refresh();

            yield break;
        }

        // DECISION RESOLVER

        /// <summary>
        /// Inspects the current PendingDecision, opens the matching UI panel, and
        /// yields until the player makes their choice (i.e. until PendingDecision
        /// is cleared back to null by the effect's own callback).
        ///
        /// Adding a new decision type only requires:
        ///   1. A new PendingDecision subclass.
        ///   2. A new UI view.
        ///   3. A new 'else if' branch here.
        /// </summary>
        private IEnumerator ResolvePendingDecisionRoutine(Player sourcePlayer)
        {
            if (_state.PendingDecision is PendingSuitChoice suitChoice)
            {
                var label = CardView.GetDisplayText(suitChoice.PlayedRank);
                gameView.ShowSuitPicker(suit =>
                {
                    // The callback commits the choice and clears PendingDecision.
                    suitChoice.OnSuitChosen(suit);
                    Debug.Log($"Suit chosen: {suit}");
                }, label);
            }
            else if (_state.PendingDecision is PendingSwapTargetChoice swapChoice)
            {
                gameView.ShowHandSwapPicker(swapChoice.SourcePlayerIndex, targetIndex =>
                {
                    // The callback executes the swap and clears PendingDecision.
                    swapChoice.OnTargetChosen(targetIndex);
                    Debug.Log($"Swap target chosen: Player {targetIndex + 1}");
                });
            }

            // Yield until the effect callback nulls out PendingDecision.
            while (_state.PendingDecision != null)
            {
                yield return null;
            }
        }

        // TURN MANAGEMENT

        private void AdvanceTurn()
        {
            var skips = _state.SkipCount;
            _state.SkipCount = 0;

            for (var i = 0; i < skips + 1; i++)
            {
                _state.CurrentPlayerIndex = GetNextPlayerIndex();
            }
        }

        private int GetNextPlayerIndex()
        {
            var count = _state.Players.Count;

            return _state.Direction switch
            {
                GameplayDirection.Clockwise => (_state.CurrentPlayerIndex + 1) % count,
                GameplayDirection.CounterClockwise => (_state.CurrentPlayerIndex - 1 + count) % count,
                _ => _state.CurrentPlayerIndex
            };
        }

        // SETUP

        private void InitializeDiscardPile()
        {
            var firstCard = _state.DrawPile.Draw();
            firstCard.IsStartingCard = true;
            _state.DiscardPile.Add(firstCard);
            // No ActiveSuit assignment needed; firstCard.ActiveSuit is already correct.
            // TODO: Fix softlocking if a wild card is drawn first
            // (Draw something else until we get a non-wild)
        }

        private static void CreateStartingDeck(CardPile drawPile)
        {
            foreach (var suit in SuitUtils.GetNormalSuits())
            {
                drawPile.Add(new Card(suit, Rank.Number7, false, new SwapHandsEffect()));

                for (var i = 0; i < 2; i++)
                {
                    drawPile.Add(new Card(suit, Rank.Number1, false));
                    drawPile.Add(new Card(suit, Rank.Number2, false));
                    drawPile.Add(new Card(suit, Rank.Number3, false));
                    drawPile.Add(new Card(suit, Rank.Number4, false));
                    drawPile.Add(new Card(suit, Rank.Number5, false));
                    drawPile.Add(new Card(suit, Rank.Number6, false));
                    drawPile.Add(new Card(suit, Rank.Number8, false));
                    drawPile.Add(new Card(suit, Rank.Number9, false));

                    drawPile.Add(new Card(suit, Rank.Number0, false, new RotateHandsEffect()));
                    drawPile.Add(new Card(suit, Rank.Draw2, false, new DrawEffect(2)));
                    drawPile.Add(new Card(suit, Rank.Reverse, false, new ReverseEffect()));
                    drawPile.Add(new Card(suit, Rank.Skip, false, new SkipEffect()));
                }
            }

            for (var i = 0; i < 4; i++)
            {
                drawPile.Add(new Card(Suit.Wild, Rank.Wild, false, new WildEffect()));
                drawPile.Add(new Card(Suit.Wild, Rank.WildDraw4, false, new WildEffect(), new DrawEffect(4)));
            }
        }

        private void UpdateShaderGlobals()
        {
            Shader.SetGlobalFloat(IsClockwiseId, _state.Direction == GameplayDirection.Clockwise ? 1f : 0f);
        }
    }
}