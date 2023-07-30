using System.Runtime.CompilerServices;

namespace Furnace.Lib.Logging;

public class Logger
{
    private static readonly List<LoggingHandler> Handlers;
    private readonly string _tag;

    public static void RegisterHandler(LoggingHandler handler) => Handlers.Add(handler);

    static Logger()
    {
        Handlers = new List<LoggingHandler>();
    }
    
    private Logger(string tag)
    {
        _tag = tag;
    }

    private void Log(LoggingLevel level, string message, string path, int lineNumber)
    {
        foreach (var handler in Handlers)
        {
            handler.Log(level, _tag, message, path, lineNumber);
        }
    }

    public void T(string message, [CallerFilePath] string path = default!,
        [CallerLineNumber] int lineNumber = default!) =>
        Log(LoggingLevel.Trace, message, path, lineNumber);
    
    public void D(string message, [CallerFilePath] string path = default!,
        [CallerLineNumber] int lineNumber = default!) =>
        Log(LoggingLevel.Debug, message, path, lineNumber);
    
    public void I(string message, [CallerFilePath] string path = default!,
        [CallerLineNumber] int lineNumber = default!) =>
        Log(LoggingLevel.Info, message, path, lineNumber);
    
    public void W(string message, [CallerFilePath] string path = default!,
        [CallerLineNumber] int lineNumber = default!) =>
        Log(LoggingLevel.Warn, message, path, lineNumber);
    
    public void E(string message, [CallerFilePath] string path = default!,
        [CallerLineNumber] int lineNumber = default!) =>
        Log(LoggingLevel.Error, message, path, lineNumber);

    public static Logger GetNamedLogger(string tag) => new Logger(tag);

    public static Logger GetLogger([CallerFilePath] string path = default!) =>
        GetNamedLogger(Path.GetFileNameWithoutExtension(path));
}