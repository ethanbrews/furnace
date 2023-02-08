namespace Furnace.Log;

public partial class Logger
{
    private static List<LogHandler> _logHandlers;

    static Logger()
    {
        _logHandlers = new List<LogHandler>();
    }

    public static void AddLogHandler(LogHandler handler) => _logHandlers.Add(handler);

    public static string LabelRepresentation(LoggingLevel level) => level switch
    {
        LoggingLevel.Trace => "T",
        LoggingLevel.Debug => "D",
        LoggingLevel.Info => "I",
        LoggingLevel.Warn => "W",
        LoggingLevel.Error => "E",
        _ => throw new InvalidOperationException("Attempt to log to an unknown log level")
    };
}

public partial class Logger
{
    private readonly string _callerName;
    
    public Logger(string callerName)
    {
        _callerName = callerName;
    }
    
    public void T(string message) => Log(LoggingLevel.Trace, message);
    public void D(string message) => Log(LoggingLevel.Debug, message);
    public void I(string message) => Log(LoggingLevel.Info, message);
    public void W(string message) => Log(LoggingLevel.Warn, message);
    public void E(string message) => Log(LoggingLevel.Error, message);

    private void Log(LoggingLevel level, string message)
    {
        foreach (var handler in _logHandlers)
        {
            handler.Log(level, message, _callerName);
        }
    }
}