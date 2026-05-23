using System.Collections.Generic;

namespace DataClasses.Tooltip
{
    /// <summary>
    /// Parses an effect description string into a list of segments.
    /// Text wrapped in {curly braces} is treated as a keyword token;
    /// everything else is plain text.
    ///
    /// Example:
    ///   "The next player must draw 2 cards unless they counter with another {+2}."
    ///   => [ Plain("The next player must draw 2 cards unless they counter with another "),
    ///        Keyword("+2"),
    ///        Plain(".") ]
    /// </summary>
    public static class DescriptionParser
    {
        public static ParsedDescription Parse(string raw)
        {
            var segments = new List<DescriptionSegment>();
            var keywords = new List<string>();

            var i = 0;
            while (i < raw.Length)
            {
                var open = raw.IndexOf('{', i);

                if (open == -1)
                {
                    // No more keywords — remainder is plain text
                    if (i < raw.Length)
                        segments.Add(new DescriptionSegment(raw[i..], isKeyword: false));
                    break;
                }

                // Plain text before the opening brace
                if (open > i)
                    segments.Add(new DescriptionSegment(raw[i..open], isKeyword: false));

                var close = raw.IndexOf('}', open + 1);
                if (close == -1)
                {
                    // Malformed — no closing brace, treat the rest as plain text
                    segments.Add(new DescriptionSegment(raw[open..], isKeyword: false));
                    break;
                }

                var keyword = raw[(open + 1)..close];
                segments.Add(new DescriptionSegment(keyword, isKeyword: true));

                if (!keywords.Contains(keyword))
                    keywords.Add(keyword);

                i = close + 1;
            }

            return new ParsedDescription(segments, keywords);
        }
    }

    public class ParsedDescription
    {
        public IReadOnlyList<DescriptionSegment> Segments { get; }

        /// <summary>Unique keywords found in this description, in order of appearance.</summary>
        public IReadOnlyList<string> Keywords { get; }

        public ParsedDescription(List<DescriptionSegment> segments, List<string> keywords)
        {
            Segments = segments;
            Keywords = keywords;
        }
    }

    public class DescriptionSegment
    {
        public string Text { get; }
        public bool IsKeyword { get; }

        public DescriptionSegment(string text, bool isKeyword)
        {
            Text = text;
            IsKeyword = isKeyword;
        }
    }
}
