using DataClasses.BusinessLayer;
using DataClasses.BusinessLayer.PendingDecisions;
using DataClasses.Enums;

namespace DataClasses.CardEffects
{
    public class DrawEffect : CardEffect
    {
        private readonly int _amount;

        public DrawEffect(int amount)
        {
            _amount = amount;
        }

        public override void Resolve(GameState state, Player source)
        {
            // If a suit choice is still pending (i.e. this DrawEffect is running as part
            // of a WildDraw4 and WildEffect queued PendingSuitChoice earlier in the same
            // Effects list), defer draw accumulation until after the suit is chosen. This
            // ensures PendingDrawCount is only incremented once the new suit is locked in;
            // the next player sees the correct active suit before deciding whether to counter.
            if (state.PendingDecision is PendingSuitChoice suitChoice)
            {
                var originalCallback = suitChoice.OnSuitChosen;
                var deferredRank = suitChoice.PlayedRank;

                state.PendingDecision = new PendingSuitChoice(
                    chosenSuit =>
                    {
                        originalCallback(chosenSuit);
                        state.PendingDrawCount += _amount;
                    })
                {
                    PlayedRank = deferredRank
                };
            }
            else
            {
                // Normal draw card (e.g. +2); accumulate immediately.
                state.PendingDrawCount += _amount;
            }
        }

        public override string GetDescription(GameState state)
        {
            return $"The next adventurer must draw {{{_amount}}} cards unless they can counter with another {{+{_amount}}} card. This effect is {{stackable}}.";
        }
    }
}
