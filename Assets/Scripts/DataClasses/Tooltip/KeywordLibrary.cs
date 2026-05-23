using System.Collections.Generic;

namespace DataClasses.Tooltip
{
    /// <summary>
    /// Defines short gameplay definitions for every keyword term used in effect descriptions.
    /// The keys here must exactly match the text inside {curly braces} in GetDescription().
    /// </summary>
    public static class KeywordLibrary
    {
        private static readonly Dictionary<string, string> Definitions = new()
        {
            ["+2"] =
                "The next player draws 2 cards and loses their turn, " +
                "unless they play another +2 to pass the penalty along.",

            ["+4"] =
                "The next player draws 4 cards and loses their turn, " +
                "unless they play another +4 to pass the penalty along.",

            ["Stacks"] =
                "Multiple copies of this effect played in sequence add together. " +
                "The total is dealt all at once to the first player who cannot counter.",

            ["Skipped"] =
                "The affected player's turn is skipped entirely. " +
                "They draw no cards and play no card that turn.",

            ["Turn Order"] =
                "The direction in which turns pass between players — " +
                "either clockwise or counterclockwise around the table.",

            ["Swap"] =
                "Both players exchange their complete hands simultaneously. " +
                "Neither player may look at the other's hand before swapping.",

            ["Active Suit"] =
                "The current color that players must match to play a card. " +
                "Set by the last played card, or chosen freely after a Wild.",

            ["Wild"] =
                "A Wild card can be played on any card regardless of suit or rank. " +
                "The player who plays it chooses the new Active Suit.",
        };

        /// <summary>
        /// Returns the definition for the given keyword, or null if it isn't in the library.
        /// Handles dynamic multi-skip keywords like "2x Skipped", "3x Skipped", etc.
        /// </summary>
        public static string Get(string keyword)
        {
            if (Definitions.TryGetValue(keyword, out var definition))
                return definition;

            // Handle dynamic skip keywords e.g. "2x Skipped", "3x Skipped"
            if (keyword.EndsWith("x Skipped"))
            {
                var countStr = keyword[..^"x Skipped".Length].Trim();
                if (int.TryParse(countStr, out var count))
                    return $"The next {count} players each have their turn skipped entirely. " +
                           "They draw no cards and play no card that turn.";
            }

            return null;
        }
    }
}
