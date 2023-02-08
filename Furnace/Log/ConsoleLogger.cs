namespace Furnace.Log;

public class ConsoleLogger : LogHandler
{
    public ConsoleLogger(LoggingLevel threshold, string callerName) : base(threshold, callerName)
    {
    }

    public override void Log(LoggingLevel level, string rawString, string caller) =>
        Console.WriteLine(FormatString(level, rawString, caller));
}