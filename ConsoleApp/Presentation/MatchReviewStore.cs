namespace ConsoleApp.Presentation
{
    /// <summary>
    /// Stores completed match review data in memory.
    /// </summary>
    public sealed class MatchReviewStore
    {
        /// <summary>
        /// Gets or sets the most recent completed match review.
        /// </summary>
        public MatchReview? LastMatch { get; set; }
    }
}
