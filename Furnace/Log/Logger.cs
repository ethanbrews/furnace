namespace Furnace.Log;

public abstract class Logger
{
    protected Logger(LoggingLevel threshold, string threadLabel)
    {
        ThreadLabel = threadLabel;
        Threshold = threshold;
    }

    public void T(string message) => LogIfPermitted(LoggingLevel.Trace, message);
    public void D(string message) => LogIfPermitted(LoggingLevel.Debug, message);
    public void I(string message) => LogIfPermitted(LoggingLevel.Info, message);
    public void W(string message) => LogIfPermitted(LoggingLevel.Warn, message);
    public void E(string message) => LogIfPermitted(LoggingLevel.Error, message);
    
    protected readonly LoggingLevel Threshold;
    protected readonly string ThreadLabel;

    private void LogIfPermitted(LoggingLevel level, string text)
    {
        if (!ShouldLog(level))
            return;
        
        Log(level, FormatLog(level, $"{LabelRepresentation(level)}:{ThreadLabel} -> {text}"));
    }

    private string FormatLog(LoggingLevel level, string rawMessage) =>
        $"{LabelRepresentation(level)}:{ThreadLabel} -> {rawMessage}";

    protected abstract void Log(LoggingLevel level, string formattedText);

    private static string LabelRepresentation(LoggingLevel level) => level switch
    {
        LoggingLevel.Trace => "T",
        LoggingLevel.Debug => "D",
        LoggingLevel.Info => "I",
        LoggingLevel.Warn => "W",
        LoggingLevel.Error => "E",
        _ => throw new InvalidOperationException("Attempt to log to an unknown log level")
    };

    private bool ShouldLog(LoggingLevel level) => level >= Threshold;
}