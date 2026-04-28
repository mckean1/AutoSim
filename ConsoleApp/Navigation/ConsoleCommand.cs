namespace ConsoleApp.Navigation
{
    internal sealed record ConsoleCommand(ConsoleCommandKind Kind, string Text)
    {
        public static ConsoleCommand Unknown(string text) => new(ConsoleCommandKind.Unknown, text);
    }
}
