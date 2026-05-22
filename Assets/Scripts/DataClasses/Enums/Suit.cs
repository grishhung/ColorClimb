using System.Collections.Generic;
using NUnit.Framework;

namespace DataClasses.Enums
{
    public enum Suit
    {
        Red,
        Yellow,
        Blue,
        Green,
        
        Wild,
    }

    public static class SuitUtils
    {
        public static List<Suit> GetNormalSuits()
        {
            return new()
            {
                Suit.Red,
                Suit.Yellow,
                Suit.Blue,
                Suit.Green
            };
        }
    }
}