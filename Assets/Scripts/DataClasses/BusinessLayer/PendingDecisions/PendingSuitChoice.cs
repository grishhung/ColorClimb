using System;
using DataClasses.Enums;

namespace DataClasses.BusinessLayer.PendingDecisions
{
    /// <summary>
    /// Queued when a wild card is played. The game is paused until the player
    /// picks a suit, at which point OnSuitChosen is invoked with their choice
    /// and the turn can proceed.
    /// </summary>
    public sealed class PendingSuitChoice : PendingDecision
    {
        public readonly Action<Suit> OnSuitChosen;

        public PendingSuitChoice(Action<Suit> onSuitChosen)
        {
            OnSuitChosen = onSuitChosen;
        }
    }
}
