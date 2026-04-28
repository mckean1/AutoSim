namespace ConsoleApp.Presentation
{
    /// <summary>
    /// Defines the current live replay playback status.
    /// </summary>
    public enum ReplayPlaybackState
    {
        /// <summary>
        /// Playback is not advancing automatically.
        /// </summary>
        Paused,

        /// <summary>
        /// Playback is advancing automatically.
        /// </summary>
        Playing,

        /// <summary>
        /// Playback has reached the end of the replay.
        /// </summary>
        Complete
    }
}
