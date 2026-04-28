using ConsoleApp.Presentation;

namespace ConsoleApp.Services
{
    /// <summary>
    /// Tracks live replay wall-clock timing and scheduled auto-advance cadence.
    /// </summary>
    public sealed class LiveReplayPlaybackClock
    {
        private DateTime _playbackStartedAtUtc;
        private TimeSpan _playbackStartedFrom;

        /// <summary>
        /// Initializes a new instance of the <see cref="LiveReplayPlaybackClock"/> class.
        /// </summary>
        public LiveReplayPlaybackClock()
        {
            _playbackStartedAtUtc = DateTime.MinValue;
            _playbackStartedFrom = TimeSpan.Zero;
            NextAdvanceAtUtc = DateTime.MinValue;
        }

        /// <summary>
        /// Gets the next wall-clock time when replay playback should advance.
        /// </summary>
        public DateTime NextAdvanceAtUtc { get; private set; }

        /// <summary>
        /// Gets a value indicating whether playback should wait before advancing.
        /// </summary>
        /// <returns>True when playback should wait; otherwise false.</returns>
        public bool ShouldWaitToAdvance() => DateTime.UtcNow < NextAdvanceAtUtc;

        /// <summary>
        /// Updates live replay playback time from the elapsed wall-clock time.
        /// </summary>
        /// <param name="replayState">The live replay state to update.</param>
        public void UpdatePlaybackTime(LiveReplayState replayState)
        {
            ArgumentNullException.ThrowIfNull(replayState);

            if (replayState.PlaybackState != ReplayPlaybackState.Playing)
            {
                return;
            }

            replayState.CurrentPlaybackTime = _playbackStartedAtUtc == DateTime.MinValue
                ? TimeSpan.Zero
                : _playbackStartedFrom
                    + TimeSpan.FromTicks((long)((DateTime.UtcNow - _playbackStartedAtUtc).Ticks
                        * GetReplaySpeedMultiplier(replayState.ReplaySpeed)));
        }

        /// <summary>
        /// Resets the wall-clock anchor for currently playing replay state.
        /// </summary>
        /// <param name="replayState">The live replay state.</param>
        public void ResetPlaybackAnchor(LiveReplayState replayState)
        {
            ArgumentNullException.ThrowIfNull(replayState);

            _playbackStartedFrom = replayState.CurrentPlaybackTime;
            _playbackStartedAtUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// Schedules the next automatic replay advance.
        /// </summary>
        /// <param name="replaySpeed">The replay speed.</param>
        /// <param name="immediate">A value indicating whether to advance immediately.</param>
        public void ScheduleNextAdvance(ReplaySpeed replaySpeed, bool immediate)
        {
            NextAdvanceAtUtc = immediate
                ? DateTime.UtcNow
                : DateTime.UtcNow.Add(GetReplayDelay(replaySpeed));
        }

        /// <summary>
        /// Stops playback timing.
        /// </summary>
        public void Stop()
        {
            _playbackStartedAtUtc = DateTime.MinValue;
            _playbackStartedFrom = TimeSpan.Zero;
            NextAdvanceAtUtc = DateTime.MinValue;
        }

        private static TimeSpan GetReplayDelay(ReplaySpeed replaySpeed) =>
            replaySpeed switch
            {
                ReplaySpeed.Slow => TimeSpan.FromMilliseconds(1500),
                ReplaySpeed.Fast => TimeSpan.FromMilliseconds(400),
                ReplaySpeed.VeryFast => TimeSpan.FromMilliseconds(150),
                _ => TimeSpan.FromMilliseconds(900)
            };

        private static double GetReplaySpeedMultiplier(ReplaySpeed replaySpeed) =>
            replaySpeed switch
            {
                ReplaySpeed.Slow => 0.5,
                ReplaySpeed.Fast => 2.0,
                ReplaySpeed.VeryFast => 4.0,
                _ => 1.0
            };
    }
}
