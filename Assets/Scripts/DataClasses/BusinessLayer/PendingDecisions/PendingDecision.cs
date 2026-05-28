namespace DataClasses.BusinessLayer.PendingDecisions
{
    /// <summary>
    /// Base class for a decision that must be resolved by the player before the game
    /// can continue. While a PendingDecision exists on GameState, all card-play and
    /// draw-pile input is blocked.
    ///
    /// Subclasses carry the typed callback that GameManager invokes once the player
    /// makes their choice in the UI. Keeping the callback here (rather than in
    /// GameManager) means each effect stays responsible for its own resolution logic.
    /// </summary>
    public abstract class PendingDecision
    {
    }
}
