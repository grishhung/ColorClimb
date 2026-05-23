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
            ["stackable"] =
                "Multiple copies of this effect played in sequence add together. " +
                "The total is dealt all at once to the first adventurer who cannot counter.",

            ["skipped"] =
                "The affected adventurer's turn is skipped entirely. " +
                "They draw no cards and play no cards that turn.",
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
