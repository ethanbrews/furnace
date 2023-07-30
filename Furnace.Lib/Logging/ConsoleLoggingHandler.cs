namespace Furnace.Lib.Logging;

public class ConsoleLoggingHandler : LoggingHandler
{
    public ConsoleLoggingHandler(LoggingLevel threshold) : base(threshold)
    {
    }

    protected override void WriteLog(LoggingLevel level, string toWrite)
    {
        if (level > LoggingLevel.Info)
            Console.Error.WriteLine(toWrite);
        else
            Console.Out.WriteLine(toWrite); 
    }
}