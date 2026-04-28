namespace ConsoleApp.Presentation
{
    /// <summary>
    /// Tracks replay review pagination state.
    /// </summary>
    public sealed class ReplayReviewState
    {
        /// <summary>
        /// Gets or sets the current page index.
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// Gets or sets the page size.
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Gets or sets the selected round number, or null for match-wide replay.
        /// </summary>
        public int? RoundNumber { get; set; }

        /// <summary>
        /// Resets pagination.
        /// </summary>
        /// <param name="roundNumber">The selected round number, or null for match-wide replay.</param>
        public void Reset(int? roundNumber = null)
        {
            PageIndex = 0;
            RoundNumber = roundNumber;
        }
    }
}
