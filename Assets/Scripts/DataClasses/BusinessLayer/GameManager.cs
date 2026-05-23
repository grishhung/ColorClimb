using DataClasses.CardEffects;
using DataClasses.CardPiles;
using DataClasses.Enums;
using DataViews;
using UnityEngine;

namespace DataClasses.BusinessLayer
{
    public class GameManager : MonoBehaviour
    {
        private static readonly int IsClockwise = Shader.PropertyToID("_IsClockwise");

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
            gameView.CardClicked += TryPlayCard;
            gameView.DrawPileClicked += TryDrawCard;

            gameView.Refresh();

            Debug.Log("Game started");
        }

        private void TryPlayCard(Player player, Card card)
        {
            if (!GameRules.CanPlay(player, card, _state))
            {
                Debug.Log("Illegal move");
                return;
            }

            GameRules.PlayCard(_state, player, card);

            // Realign to the jump-in player without consuming pending skips
            var pendingSkips = _state.SkipCount;
            _state.SkipCount = 0;

            while (_state.Players[_state.CurrentPlayerIndex] != player)
            {
                _state.CurrentPlayerIndex = GetNextPlayerIndex();
            }

            // Restore skips so AdvanceTurn processes them correctly
            _state.SkipCount = pendingSkips;

            AdvanceTurn();
            UpdateShaderGlobals();
            gameView.Refresh();

            Debug.Log($"Player played {card}");
        }

        private void TryDrawCard()
        {
            var currentPlayer = _state.Players[_state.CurrentPlayerIndex];

            // TODO: Uncomment the following for when we implement a "reshuffle" (lives) system
            //
            // if (_state.DrawPile.Cards.Count == 0)
            // {
            //     if (_state.DiscardPile.Cards.Count <= 1)
            //     {
            //         Debug.Log("Draw pile and discard pile are both empty — cannot draw");
            //         return;
            //     }
            //     _state.DrawPile.RefillFromDiscard(_state.DiscardPile);
            //     Debug.Log("Draw pile exhausted — reshuffled discard pile");
            // }

            if (_state.PendingDrawCount > 0)
            {
                // Player couldn't or chose not to counter the chain — deal the burst and end their turn.
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
                // Normal draw: add one card without advancing the turn.
                // Player keeps drawing until they find something playable.
                var drawnCard = _state.DrawPile.Draw();
                currentPlayer.Hand.Add(drawnCard);
                Debug.Log($"Player drew {drawnCard}");
            }

            UpdateShaderGlobals();
            gameView.Refresh();
        }

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

        // TODO: Fix softlocking if a wild card is drawn first
        // (Draw something else until we get a non-wild)
        private void InitializeDiscardPile()
        {
            var firstCard = _state.DrawPile.Draw();
            firstCard.IsStartingCard = true;
            _state.DiscardPile.Add(firstCard);
            _state.ActiveSuit = firstCard.Suit;
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
            Shader.SetGlobalFloat(IsClockwise, _state.Direction == GameplayDirection.Clockwise ? 1f : 0f);
        }
    }
}
