namespace ConsoleApp.Services
{
    /// <summary>
    /// Represents a user-facing round log read failure.
    /// </summary>
    public sealed class RoundLogReadException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RoundLogReadException"/> class.
        /// </summary>
        /// <param name="message">The failure message.</param>
        public RoundLogReadException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoundLogReadException"/> class.
        /// </summary>
        /// <param name="message">The failure message.</param>
        /// <param name="innerException">The inner exception.</param>
        public RoundLogReadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
