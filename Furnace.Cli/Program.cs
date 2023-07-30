using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Reflection;
using Furnace.Cli;
using Furnace.Cli.Command;
using Furnace.Lib.Logging;
using Spectre.Console;

var forceOption = new Option<bool>(new[]{ "--force", "-f" }, () => false, "Perform destructive operation without confirmation.");
var selectNewUserOption = new Option<bool>("--select", () => true, "Select the newly added user account as the default.");
var userUuidArgument = new Argument<string?>("uuid", () => null, "The user to target the operation towards.");
var packIdArgument = new Argument<string?>("packId", () => null, "The modrinth pack id.");
var queryArgument = new Argument<string?>("query", () => null, "The query string.");
var minecraftVersionOption = new Option<string?>("--minecraft-version", () => null, "The target minecraft version.");
var modrinthVersionOption = new Option<string?>("--pack-version", () => null, "The target pack version id.");
var createScriptOnlyOption = new Option<bool>("--create-script", () => false, "Create a launch script without launching the game.");
var serverSideOption = new Option<bool>(new[] { "--server", "-s" }, () => false, "Target the minecraft server.");
var showPromptOption = new Option<bool>(new[] { "--prompt", "-q" }, () => false,
    "Show an interactive prompt when a value would be inferred.");

var verboseOption = new Option<bool>(new[]{ "--verbose", "-v" }, () => false, "Show verbose output.");
var throwErrorsOption = new Option<bool>("--debug-throw-errors", () => false, "Throw errors, used to debug the furnace application.")
{
    IsHidden = true
};

var rootCommand = new RootCommand(description: "Install and launch modrinth packs with fabric support.");
rootCommand.AddGlobalOption(verboseOption);
rootCommand.AddGlobalOption(throwErrorsOption);

var usersCommand = new Command("users", "add, delete and select user accounts.");
var usersAddCommand = new Command("add", "Add a new user.");
usersAddCommand.AddOption(selectNewUserOption);
usersAddCommand.SetHandler(UserCommand.AddUserAsync, selectNewUserOption);
var usersDeleteCommand = new Command("delete", "Delete the specified user.");
usersDeleteCommand.AddOption(forceOption);
usersDeleteCommand.AddArgument(userUuidArgument);
usersDeleteCommand.SetHandler(UserCommand.DeleteUserAsync, userUuidArgument, forceOption);
var userSelectCommand = new Command("select", "Select a new user as default.");
userSelectCommand.AddArgument(userUuidArgument);
userSelectCommand.SetHandler(UserCommand.SetUserSelectedAsync, userUuidArgument);
var userListCommand = new Command("list", "List all user accounts that are logged in.");
userListCommand.SetHandler(UserCommand.ListUsersAsync, verboseOption);

var installCommand = new Command("install", "Install a modrinth pack.");
installCommand.AddOption(modrinthVersionOption);
installCommand.AddOption(minecraftVersionOption);
installCommand.AddArgument(packIdArgument);
installCommand.SetHandler(InstallCommand.InstallPack, packIdArgument, modrinthVersionOption, minecraftVersionOption, verboseOption);

var launchCommand = new Command("launch", "Launch a modrinth pack.");
launchCommand.AddArgument(packIdArgument);
launchCommand.AddOption(createScriptOnlyOption);
launchCommand.AddOption(serverSideOption);
launchCommand.SetHandler(LaunchPack.LaunchAsync, packIdArgument, verboseOption, createScriptOnlyOption, serverSideOption);

var listCommand = new Command("list", "List installed modrinth packs");
listCommand.SetHandler(LaunchPack.ListPacks, verboseOption);

var deleteCommand = new Command("delete", "Delete an installed modrinth pack.");
deleteCommand.AddArgument(packIdArgument);
deleteCommand.AddOption(forceOption);
deleteCommand.SetHandler(InstallCommand.DeletePackAsync, packIdArgument, forceOption);

var openFolderCommand = new Command("open", "Open the folder containing the given pack files.");
openFolderCommand.AddArgument(packIdArgument);
openFolderCommand.AddOption(showPromptOption);
openFolderCommand.SetHandler(PackManagementCommand.OpenFolder, packIdArgument, showPromptOption);

var searchModrinthCommand = new Command("search", "Search modrinth for a modpack.");
searchModrinthCommand.AddArgument(queryArgument);
searchModrinthCommand.AddOption(showPromptOption);
searchModrinthCommand.AddGlobalOption(verboseOption);
searchModrinthCommand.SetHandler(MiscModrinthCommand.SearchPacksAsync, queryArgument, showPromptOption, verboseOption);

usersCommand.AddCommand(usersAddCommand);
usersCommand.AddCommand(userSelectCommand);
usersCommand.AddCommand(usersDeleteCommand);
usersCommand.AddCommand(userListCommand);
rootCommand.AddCommand(usersCommand);
rootCommand.AddCommand(installCommand);
rootCommand.AddCommand(launchCommand);
rootCommand.AddCommand(listCommand);
rootCommand.AddCommand(deleteCommand);
rootCommand.AddCommand(openFolderCommand);
rootCommand.AddCommand(searchModrinthCommand);

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


namespace Furnace.Cli
{
    public partial class Program
    {
        //internal static readonly DirectoryInfo RootDirectory = new("data");

        internal static AppConfig Cfg { get; set; }

        public static DirectoryInfo TestsRootDirectory => RootDirectory;

        internal static readonly DirectoryInfo RootDirectory = new(AssemblyDirectory ?? throw new NullReferenceException("AssemblyDirectory is null. Where is home directory?"));
        
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
}