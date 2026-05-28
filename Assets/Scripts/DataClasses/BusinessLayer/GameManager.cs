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

            DealStartingHands();
            InitializeDiscardPile();

            UpdateShaderGlobals();

            gameView.Bind(_state);
            gameView.CardClicked += OnCardClicked;
            gameView.DrawPileClicked += OnDrawPileClicked;

            gameView.Refresh();

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

        private void DealStartingHands()
        {
            for (var i = 0; i < PlayerCount; i++)
            {
                Hand hand = new();

                for (var j = 0; j < StartingHandSize; j++)
                {
                    hand.Add(_state.DrawPile.Draw());
                }

                _state.Players.Add(new Player(hand));
            }
        }

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
