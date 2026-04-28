namespace ConsoleApp.Presentation
{
    /// <summary>
    /// Tracks live replay playback progress and display settings.
    /// </summary>
    public sealed class LiveReplayState
    {
        /// <summary>
        /// Gets or sets the current playback time.
        /// </summary>
        public TimeSpan CurrentPlaybackTime { get; set; }

        /// <summary>
        /// Gets or sets the current revealed event count.
        /// </summary>
        public int CurrentEventIndex { get; set; }

        /// <summary>
        /// Gets or sets the playback state.
        /// </summary>
        public ReplayPlaybackState PlaybackState { get; set; } = ReplayPlaybackState.Paused;

        /// <summary>
        /// Gets or sets the playback speed.
        /// </summary>
        public ReplaySpeed ReplaySpeed { get; set; } = ReplaySpeed.Normal;

        /// <summary>
        /// Gets or sets the maximum visible replay message count.
        /// </summary>
        public int VisibleMessageCount { get; set; } = 10;

        /// <summary>
        /// Resets replay playback state.
        /// </summary>
        public void Reset()
        {
            CurrentPlaybackTime = TimeSpan.Zero;
            CurrentEventIndex = 0;
            PlaybackState = ReplayPlaybackState.Paused;
            ReplaySpeed = ReplaySpeed.Normal;
            VisibleMessageCount = 10;
        }
    }
}
