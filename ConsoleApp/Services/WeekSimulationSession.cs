namespace ConsoleApp.Services
{
    /// <summary>
    /// Represents a background week simulation session.
    /// </summary>
    public sealed class WeekSimulationSession
    {
        private readonly Task<WeekSimulationResult> _simulationTask;
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="WeekSimulationSession"/> class.
        /// </summary>
        /// <param name="simulationTask">The background simulation task.</param>
        /// <param name="cancellationTokenSource">The cancellation token source.</param>
        public WeekSimulationSession(
            Task<WeekSimulationResult> simulationTask,
            CancellationTokenSource cancellationTokenSource)
        {
            _simulationTask = simulationTask ?? throw new ArgumentNullException(nameof(simulationTask));
            _cancellationTokenSource = cancellationTokenSource ?? throw new ArgumentNullException(nameof(cancellationTokenSource));
            Status = WeekSimulationStatus.Running;
        }

        /// <summary>
        /// Gets the current simulation status.
        /// </summary>
        public WeekSimulationStatus Status { get; private set; }

        /// <summary>
        /// Gets the completed simulation result if available.
        /// </summary>
        public WeekSimulationResult? Result { get; private set; }

        /// <summary>
        /// Gets the exception if the simulation failed.
        /// </summary>
        public Exception? Exception { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether auto-play was requested during preparation.
        /// </summary>
        public bool AutoPlayRequested { get; set; }

        /// <summary>
        /// Updates the session status by checking the task state.
        /// </summary>
        public void Update()
        {
            if (Status != WeekSimulationStatus.Running)
            {
                return;
            }

            if (!_simulationTask.IsCompleted)
            {
                return;
            }

            if (_simulationTask.IsFaulted)
            {
                Status = WeekSimulationStatus.Failed;
                Exception = _simulationTask.Exception?.GetBaseException();
                return;
            }

            if (_simulationTask.IsCanceled)
            {
                Status = WeekSimulationStatus.Cancelled;
                return;
            }

            Status = WeekSimulationStatus.Completed;
            Result = _simulationTask.Result;
        }

        /// <summary>
        /// Requests cancellation of the simulation.
        /// </summary>
        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}
