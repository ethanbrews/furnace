using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Reflection;
using Furnace.Cli;
using Furnace.Cli.Command;
using Furnace.Lib.Logging;
using Spectre.Console;




namespace Furnace.Cli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand(description: "Install and launch modrinth packs with fabric support.");
        GlobalOptions.RegisterGlobalOptions(rootCommand);

        var commands = new ICommand[]
        {
            new InstallCommand(),
            new UserCommand(),
            new LaunchCommand(),
            new OpenCommand(),
            new SearchCommand()
        };

        foreach (var command in commands)
        {
            command.Register(rootCommand);
        }

#if DEBUG
        Logger.RegisterHandler(new DebugLoggingHandler(LoggingLevel.Trace));
#else
Logger.RegisterHandler(new DebugLoggingHandler(LoggingLevel.Debug));
#endif

        var parser = new CommandLineBuilder(rootCommand)
            .UseVersionOption()
            .UseHelp()
            .UseEnvironmentVariableDirective()
            .UseParseDirective()
            .UseSuggestDirective()
            .RegisterWithDotnetSuggest()
            .UseTypoCorrections()
            .UseParseErrorReporting()
            .CancelOnProcessTermination()
            .Build();

        try
        {
            Furnace.Cli.Program.Cfg = await AppConfig.ReadConfig();
        }
        catch (Newtonsoft.Json.JsonSerializationException exception)
        {
            AnsiConsole.Foreground = Color.Red;
            AnsiConsole.WriteLine($"Exception loading config from {AppConfig.ConfigFileName}");
            AnsiConsole.WriteLine(exception.Message);
            AnsiConsole.ResetColors();
            return -1;
        }


        var code = await parser.InvokeAsync(args);

        await Furnace.Cli.Program.Cfg.WriteConfig();

        return code;
    }
    
    
    internal static AppConfig Cfg { get; set; }

    public static DirectoryInfo TestsRootDirectory => RootDirectory;

    internal static readonly DirectoryInfo RootDirectory = new DirectoryInfo(
        AssemblyDirectory ?? throw new NullReferenceException("AssemblyDirectory is null. Where is home directory?")
    ).CreateSubdirectory("data");
        
    private static string? AssemblyDirectory
    {
        get
        {
            var codeBase = Assembly.GetExecutingAssembly().Location;
            var uri = new UriBuilder(codeBase);
            var path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
    }
}