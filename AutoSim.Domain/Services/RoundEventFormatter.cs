using AutoSim.Domain.Objects;

namespace AutoSim.Domain.Services
{
    /// <summary>
    /// Formats round event messages and times.
    /// </summary>
    public static class RoundEventFormatter
    {
        /// <summary>
        /// Formats round seconds as MM:SS.s.
        /// </summary>
        /// <param name="timeSeconds">The round time in seconds.</param>
        /// <returns>The formatted time.</returns>
        public static string FormatTime(double timeSeconds)
        {
            int totalSeconds = (int)Math.Floor(timeSeconds);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            int tenths = (int)Math.Round((timeSeconds - totalSeconds) * 10, MidpointRounding.AwayFromZero);

            if (tenths == 10)
            {
                seconds++;
                tenths = 0;
            }

            if (seconds == 60)
            {
                minutes++;
                seconds = 0;
            }

            return $"{minutes:00}:{seconds:00}.{tenths}";
        }

        /// <summary>
        /// Gets a side-prefixed champion display name.
        /// </summary>
        /// <param name="champion">The champion.</param>
        /// <returns>The formatted champion display name.</returns>
        public static string ChampionName(ChampionInstance champion)
        {
            ArgumentNullException.ThrowIfNull(champion);

            return $"{champion.TeamSide} {champion.Definition.Name}";
        }

        /// <summary>
        /// Gets the champion source fields for an event.
        /// </summary>
        /// <param name="champion">The source champion.</param>
        /// <returns>The source field values.</returns>
        public static (
            string TeamSide,
            string ChampionId,
            string ChampionName,
            string PlayerId) SourceFields(ChampionInstance champion)
        {
            ArgumentNullException.ThrowIfNull(champion);

            return (
                champion.TeamSide.ToString(),
                champion.Definition.Id,
                champion.Definition.Name,
                champion.PlayerId);
        }
    }
}
