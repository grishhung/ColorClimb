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

        /// <summary>
        /// The rank of the wild card that triggered this choice.
        /// Set by GameRules.PlayCard after effects resolve so GameManager can
        /// pass the correct display label (e.g. "WILD" vs "+4") to the selector UI.
        /// </summary>
        public Rank PlayedRank { get; set; }

        public PendingSuitChoice(Action<Suit> onSuitChosen)
        {
            OnSuitChosen = onSuitChosen;
        }
    }
}
