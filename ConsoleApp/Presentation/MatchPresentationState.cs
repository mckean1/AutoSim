using AutoSim.Domain.Management.Models;

namespace ConsoleApp.Presentation
{
    /// <summary>
    /// Tracks the active match presentation flow.
    /// </summary>
    public sealed class MatchPresentationState
    {
        /// <summary>
        /// Gets the live replay state.
        /// </summary>
        public LiveReplayState LiveReplay { get; } = new();

        /// <summary>
        /// Gets or sets the completed presented match.
        /// </summary>
        public PresentedMatch? PresentedMatch { get; set; }

        /// <summary>
        /// Gets or sets the selected round index.
        /// </summary>
        public int RoundIndex { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the first round draft is complete.
        /// </summary>
        public bool IsDraftComplete { get; set; }

        /// <summary>
        /// Gets or sets the first round draft shown before replay.
        /// </summary>
        public RoundDraft? RoundDraft { get; set; }

        /// <summary>
        /// Gets or sets the active scheduled match.
        /// </summary>
        public ScheduledMatch? ScheduledMatch { get; set; }

        /// <summary>
        /// Clears the active match flow.
        /// </summary>
        public void Clear()
        {
            LiveReplay.Reset();
            PresentedMatch = null;
            RoundIndex = 0;
            IsDraftComplete = false;
            RoundDraft = null;
            ScheduledMatch = null;
        }
    }
}
