using System.Runtime.CompilerServices;
using Furnace.Minecraft.Data.GameManifest;

namespace Furnace.Log;

public static class LogManager
{
    private static Type _loggerType = typeof(ConsoleLogger);
    public static LoggingLevel Level { get; set; } = LoggingLevel.Info;
    public static void SetLogHandler<T>() where T : Logger => _loggerType = typeof(T);
    public static Logger GetLogger([CallerFilePath] string callerName = "") => (Logger)Activator.CreateInstance(_loggerType, Level, callerName)!;
}