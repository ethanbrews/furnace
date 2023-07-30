namespace Furnace.Lib.Logging;

public class DebugLoggingHandler : LoggingHandler
{
    public DebugLoggingHandler(LoggingLevel threshold) : base(threshold)
    {
    }

    protected override void WriteLog(LoggingLevel level, string toWrite)
    {
        System.Diagnostics.Debug.WriteLine(toWrite, level.LabelRepresentation());
    }
}