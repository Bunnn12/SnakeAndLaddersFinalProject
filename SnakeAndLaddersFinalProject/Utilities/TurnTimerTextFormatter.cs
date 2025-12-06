using System;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public static class TurnTimerTextFormatter
    {
        private const string ZERO_TIME_TEXT = "00:00";

        public static string Format(int seconds)
        {
            if (seconds <= 0)
            {
                return ZERO_TIME_TEXT;
            }

            int minutes = seconds / 60;
            int remainingSeconds = seconds % 60;

            return string.Format("{0:00}:{1:00}", minutes, remainingSeconds);
        }
    }
}
