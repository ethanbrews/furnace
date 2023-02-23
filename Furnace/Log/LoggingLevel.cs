using System.Diagnostics;

namespace Furnace.Log;

public enum LoggingLevel
{
    Trace = 1,
    Debug = 2,
    Info = 3,
    Warn = 4,
    Error = 5,
    NeverLog = 99
}

public static class LoggingLevelExtensions
{
    public static string ShortLabelRepresentation(this LoggingLevel level) => level switch
    {
        LoggingLevel.Trace => "T",
        LoggingLevel.Debug => "D",
        LoggingLevel.Info => "I",
        LoggingLevel.Warn => "W",
        LoggingLevel.Error => "E",
        LoggingLevel.NeverLog => "N",
        _ => throw new UnreachableException("Attempt to convert an unknown log level")
    };
    
    public static string LabelRepresentation(this LoggingLevel level) => level switch
    {
        LoggingLevel.Trace => "Trace",
        LoggingLevel.Debug => "Debug",
        LoggingLevel.Info => "Info",
        LoggingLevel.Warn => "Warn",
        LoggingLevel.Error => "Error",
        LoggingLevel.NeverLog => "Never",
        _ => throw new UnreachableException("Attempt to convert an unknown log level")
    };

    public static LoggingLevel FromLabel(string label)
    {
        switch(label)
        {
            case "t":
            case "trace":
                return LoggingLevel.Trace;
            case "d":
            case "debug":
                return LoggingLevel.Debug;
            case "i":
            case "info":
                return LoggingLevel.Info;
            case "w":
            case "warn":
                return LoggingLevel.Warn;
            case "e":
            case "error":
                return LoggingLevel.Error;
            case "n":
            case "never":
                return LoggingLevel.NeverLog;
            default:
                throw new UnreachableException("Attempt to convert an unknown tag");
        }
    } 
}