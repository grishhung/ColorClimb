using System;

namespace DataClasses.BusinessLayer.PendingDecisions
{
    /// <summary>
    /// Queued when a 7 is played. The game is paused until the player picks a
    /// swap target, at which point OnTargetChosen is invoked with the chosen
    /// player index and the swap can execute.
    /// </summary>
    public sealed class PendingSwapTargetChoice : PendingDecision
    {
        public readonly Action<int> OnTargetChosen;

        /// <summary>
        /// Index of the player who played the 7; this player should not appear
        /// as a chooseable target in the UI.
        /// </summary>
        public readonly int SourcePlayerIndex;

        public PendingSwapTargetChoice(int sourcePlayerIndex, Action<int> onTargetChosen)
        {
            SourcePlayerIndex = sourcePlayerIndex;
            OnTargetChosen = onTargetChosen;
        }
    }
}
