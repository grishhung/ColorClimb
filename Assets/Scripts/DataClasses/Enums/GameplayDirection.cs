using System.Collections.Generic;

namespace DataClasses.Enums
{
    public enum GameplayDirection 
    {
        Clockwise,
        CounterClockwise,
    }
    
    public static class GameplayDirectionUtils
    {
        private static readonly Dictionary<GameplayDirection, string> ReadableDirections = new()
        {
            { GameplayDirection.CounterClockwise, "counterclockwise" },
            { GameplayDirection.Clockwise, "clockwise" },
        };

        public static string GetString(GameplayDirection gameplayDirection)
        {
            return ReadableDirections[gameplayDirection];
        }
        
        public static GameplayDirection GetOppositeDirection(GameplayDirection gameplayDirection)
        {
            return GameplayDirection.CounterClockwise == gameplayDirection
                ? GameplayDirection.Clockwise
                : GameplayDirection.CounterClockwise;
        }
    }
}
