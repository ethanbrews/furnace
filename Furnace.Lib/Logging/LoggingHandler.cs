namespace Furnace.Lib.Logging;

public abstract class LoggingHandler
{
    protected LoggingHandler(LoggingLevel threshold)
    {
        _threshold = threshold;
    }

    private readonly LoggingLevel _threshold;

    #if DEBUG
    private static string FormatLog(LoggingLevel level, string rawMessage, string tag, string fileName, int lineNumber) =>
        $"{level.ShortLabelRepresentation()}:{Path.GetFileNameWithoutExtension(fileName)}:{lineNumber} -> {rawMessage}";
    #else
    private static string FormatLog(LoggingLevel level, string rawMessage, string tag, string fileName, int lineNumber) =>
        $"{level.ShortLabelRepresentation()}: {rawMessage}";
    #endif

    public void Log(LoggingLevel level, string tag, string rawMessage, string fileName, int lineNumber)
    {
        if (!ShouldLog(level))
            return;
        WriteLog(level, FormatLog(level, rawMessage, tag, fileName, lineNumber));
    }

    protected abstract void WriteLog(LoggingLevel level, string toWrite);

    private bool ShouldLog(LoggingLevel level) => level >= _threshold;
}