namespace ConsoleApp.Screens
{
    /// <summary>
    /// Represents a console screen that can produce a render model.
    /// </summary>
    internal interface IScreen
    {
        /// <summary>
        /// Builds the render model for this screen.
        /// </summary>
        /// <returns>The screen render model.</returns>
        ScreenRenderModel Build();
    }
}
