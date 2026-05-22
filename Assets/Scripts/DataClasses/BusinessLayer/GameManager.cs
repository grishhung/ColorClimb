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

            gameView.Refresh();

            Debug.Log("Game started");
        }

        private void TryPlayCard(Player player, Card card)
        {
            // Turn enforcement
            if (_state.Players[_state.CurrentPlayerIndex] != player)
            {
                Debug.Log("Not your turn");
                return;
            }

            // Rule check (now includes ActiveSuit)
            if (!GameRules.CanPlay(card, _state.TopDiscard, _state.ActiveSuit))
            {
                Debug.Log("Illegal move");
                return;
            }

            GameRules.PlayCard(_state, player, card);
            AdvanceTurn();
            UpdateShaderGlobals();
            gameView.Refresh();
            
            Debug.Log($"Player played {card}");
        }
        
        // TODO: Implement TryDrawCard() or something along those lines
        // Currently the game is softlocked if the player cannot draw a card

        private void AdvanceTurn()
        {
            var skips = _state.SkipCount;
            _state.SkipCount = 0;

            for (int i = 0; i < skips + 1; i++)
            {
                _state.CurrentPlayerIndex = GetNextPlayerIndex();
            }
        }

        private int GetNextPlayerIndex()
        {
            int count = _state.Players.Count;

            return _state.Direction switch
            {
                GameplayDirection.Clockwise =>
                    (_state.CurrentPlayerIndex + 1) % count,

                GameplayDirection.CounterClockwise =>
                    (_state.CurrentPlayerIndex - 1 + count) % count,

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

                    // TODO: Make Draw 2 cards skip the player's turn
                    // unless they can also play a draw 2 and stack the draw amount
                    drawPile.Add(new Card(suit, Rank.Draw2, false, new DrawEffect(2)));
                    drawPile.Add(new Card(suit, Rank.Reverse, false, new ReverseEffect()));
                    drawPile.Add(new Card(suit, Rank.Skip, false, new SkipEffect()));
                }
            }

            for (var i = 0; i < 4; i++)
            {
                // TODO: Make Draw 4 cards skip the player's turn
                // unless they can also play a draw 4 and stack the draw amount
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