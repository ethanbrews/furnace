namespace Furnace.Log;

public class ConsoleLogger : Logger
{
    public ConsoleLogger(LoggingLevel threshold, string threadLabel) : base(threshold, threadLabel)
    {
    }

    protected override void Log(LoggingLevel _, string formattedText)
    {
        Console.WriteLine(formattedText);
    }
}